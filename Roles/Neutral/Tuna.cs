using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    /// <summary>
    /// マグロ(Tuna)
    /// ・一定秒数動かないと自滅（放置死）
    /// ・ただし「初手会議が終わるまで」は自滅だけ無効（＝キルはされる）
    /// ・会議中は自滅チェックを停止（キルは通常通り通る）
    /// ・共存勝利(追加勝利) / 単独勝利UI（Santa式）
    /// ・スポーン/会議明けの保護で自己キルが弾かれても遅延リトライ
    /// </summary>
    public sealed class Tuna : RoleBase, IAdditionalWinner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tuna),
                player => new Tuna(player),
                CustomRoles.Tuna,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Neutral,
                7300,
                SetupOptionItem,
                "tuna",
                "#5bc0de",
                (8, 2)
            );

        public Tuna(PlayerControl player)
            : base(RoleInfo, player)
        {
            _lastPos = (Vector2)player.transform.position;

            // Idle起点は「タスクフェーズ入り」でアームする（スポーン演出や割当待ち時間で誤爆しない）
            _armedIdleTimerAfterIntro = false;
            _lastMovedTime = 0f;

            IdleDeathSeconds = OptIdleDeathSeconds.GetFloat();
            AddWin = OptAddWin.GetBool();

            _sawAnyMeeting = false;
            _firstMeetingEnded = false;
            _wasMeeting = GameStates.IsMeeting;

            _iAdditionalWinFlag = false;

            _suicideRetryToken = 0;
        }

        // ====== Options ======
        private enum OptionName
        {
            TunaIdleDeathSeconds,
            CountKillerAddWin
        }

        private static OptionItem OptIdleDeathSeconds;
        private static OptionItem OptAddWin;

        private static void SetupOptionItem()
        {
            OptIdleDeathSeconds = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TunaIdleDeathSeconds, new(3, 60, 1), 10, false)
                .SetValueFormat(OptionFormat.Seconds);

            // 共存(追加勝利)ON/OFF + 単独勝利UI（Santa方式）
            OptAddWin = BooleanOptionItem.Create(RoleInfo, 15, OptionName.CountKillerAddWin, false, false);
            SoloWinOption.Create(RoleInfo, 16, show: () => !OptAddWin.GetBool(), defo: 1);
        }

        // ====== Cached Options ======
        private float IdleDeathSeconds;
        private bool AddWin;

        // ====== State ======
        private Vector2 _lastPos;
        private float _lastMovedTime;
        private bool _armedIdleTimerAfterIntro;

        // 初手会議の状態管理
        private bool _sawAnyMeeting;        // 会議が一度でも始まったか
        private bool _firstMeetingEnded;    // 初手会議が終了したか（これがtrueになるまで自滅は起きない）
        private bool _wasMeeting;

        // 追加勝利表示用
        private bool _iAdditionalWinFlag;

        // 自己キルのリトライ制御
        private int _suicideRetryToken; // 同時多発を防ぐための簡易トークン

        // ====== Hooks ======
        public override void OnStartMeeting()
        {
            _sawAnyMeeting = true; // 「会議が始まった」ことを記録
        }

        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!Player || !Player.IsAlive()) return;

            // タスクフェーズに入った瞬間にIdle起点をセット（これで開始直後の誤爆を防ぐ）
            if (!_armedIdleTimerAfterIntro && GameStates.IsInTask)
            {
                _lastMovedTime = Time.time;
                _armedIdleTimerAfterIntro = true;
            }

            bool isMeeting = GameStates.IsMeeting;

            // 会議開始→終了の遷移（＝初手会議終了）を検知
            if (_sawAnyMeeting && _wasMeeting && !isMeeting && !_firstMeetingEnded)
            {
                _firstMeetingEnded = true; // ここから自滅が有効化
            }
            _wasMeeting = isMeeting;

            // 位置監視（微小ぶれは無視）
            var pos = (Vector2)Player.transform.position;
            if (Vector2.SqrMagnitude(pos - _lastPos) > 0.0004f)
            {
                _lastMovedTime = Time.time;
                _lastPos = pos;
            }

            // 会議中は自滅チェックを停止（キルは通常通り通る）
            if (isMeeting) return;

            // 自滅条件
            // ・Idleカウントが有効化済み（Intro後）
            // ・初手会議が終わっている
            // ・放置秒数を超過
            if (_armedIdleTimerAfterIntro && _firstMeetingEnded && (Time.time - _lastMovedTime >= IdleDeathSeconds))
            {
                TrySuicideStayedTooLong(); // バリアに弾かれても内部でリトライ
            }

            // 共存勝利のフラグ（追加勝利は「生存で勝ち」）
            _iAdditionalWinFlag = AddWin && Player.IsAlive();
        }

        public override string GetProgressText(bool comms = false, bool gamelog = false)
        {
            // 簡易表記：現在の放置閾値だけ出す
            return Utils.ColorString(Color.cyan, $"({(int)IdleDeathSeconds}s)");
        }

        // ====== 勝利処理（共存：追加勝利に載せる） ======
        public bool CheckWin(ref CustomRoles winnerRole)
        {
            if (AddWin && Player && Player.IsAlive())
            {
                winnerRole = CustomRoles.Tuna;
                return true;
            }
            return false;
        }

        public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (AddWin && _iAdditionalWinFlag && seen == seer) return Utils.AdditionalWinnerMark;
            return "";
        }

        // ====== 自滅（放置死） ======
        private void TrySuicideStayedTooLong()
        {
            if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost) return;

            // 多重呼び出しを控える（保護解除待ちの間に連打しない）
            int token = ++_suicideRetryToken;

            // 即試行
            if (AttemptSelfKill()) return;

            // 失敗＝保護に弾かれた可能性 → 少し待って再試行（LateTask）
            // ※ K環境では LateTask.New(Action, delay, name) が使える（ログにも出力される）のでそれに合わせる
            RetrySelfDestruct(token, tries: 4, delay: 0.6f); // 合計 ~2.4s ほど粘る
        }

        private bool AttemptSelfKill()
        {
            if (!Player || !Player.IsAlive()) return true;       // 既に死んでいれば成功扱い
            if (GameStates.IsMeeting) return false;              // 会議中は実行しない（方針）

            try
            {
                // 共通ヘルパがあれば推奨: Utils.RpcMurderPlayer(Player, Player);
                Player.RpcMurderPlayer(Player);
                return true;
            }
            catch
            {
                // RPCが弾かれた/失敗。フォールバックでローカル死亡は行わない（保護解除まで待つ）
                return false;
            }
        }

        private void RetrySelfDestruct(int token, int tries, float delay)
        {
            if (tries <= 0) return;
            LateTask.New(() =>
            {
                // 他のリトライが走っていたら中止
                if (token != _suicideRetryToken) return;

                if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost) return;
                if (!Player || !Player.IsAlive()) return;
                if (GameStates.IsMeeting) return;

                if (!AttemptSelfKill())
                {
                    RetrySelfDestruct(token, tries - 1, delay);
                }
            }, delay, "TunaSelfDestructRetry");
        }
    }
}
