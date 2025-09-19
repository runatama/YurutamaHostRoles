using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    // 共存勝利にも対応するため IAdditionalWinner を実装
    public sealed class Tuna : RoleBase, IAdditionalWinner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tuna),
                player => new Tuna(player),
                CustomRoles.Tuna,
                () => RoleTypes.Crewmate, // 常にベント不可
                CustomRoleTypes.Neutral,
                23400,
                SetupOptionItem,
                "tuna",
                "#00bfff",
                from:From.YurutamaHostRoles
            );

        public Tuna(PlayerControl player)
        : base(RoleInfo, player)
        {
            waitTime = OptWaitTime.GetFloat();
            allowAddWin = OptAllowAdditionalWin.GetBool();

            afterFirstMeeting = false;
            stillCounter = 0f;
            lastPos = Vector3.zero;
            tick = 0f;
        }

        // ====== Options ======
        private static OptionItem OptWaitTime;
        private static OptionItem OptAllowAdditionalWin;

        private float waitTime;
        private bool allowAddWin;

        // ランタイム
        private float tick;
        private float stillCounter;
        private Vector3 lastPos;
        private bool afterFirstMeeting;

        private static void SetupOptionItem()
        {
            // 立ち止まり秒（0～30, 1秒刻み, 既定=3）
            OptWaitTime = FloatOptionItem.Create(RoleInfo, 10, "TunaWaitTime", new(0f, 30f, 1f), 3f, false)
                .SetValueFormat(OptionFormat.Seconds);

            // 追加勝利ON/OFF
            OptAllowAdditionalWin = BooleanOptionItem.Create(RoleInfo, 11, "TunaAllowAdditionalWin", false, false);

            // 勝利優先度 (1～50) デフォルトは10
            SoloWinOption.Create(RoleInfo, 12, defo: 10);
        }

        // ====== 共存勝利 (IAdditionalWinner) ======
        public bool CheckWin(ref CustomRoles winnerRole)
        {
            if (!Player.IsAlive()) return false;

            if (allowAddWin)
            {
                // 共存勝利モード → 他陣営勝利に便乗
                winnerRole = CustomRoles.Tuna;
                return true;
            }
            return false;
        }

        // ====== 単独勝利 ======
        public override void CheckWinner()
        {
            if (!Player.IsAlive()) return;

            if (!allowAddWin)
            {
                // 単独勝利モード → Neutral 勝利を確定
                if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Tuna, Player.PlayerId))
                {
                    CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                }
            }
        }

        public override void OnStartMeeting()
        {
            afterFirstMeeting = true;
        }

        // ====== 自爆処理 ======
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ExileController.Instance) return;

            if (!GameStates.IsInTask) return;
            if (!afterFirstMeeting) return;
            if (!Player.IsAlive()) return;
            tick -= Time.fixedDeltaTime;
            UtilsNotifyRoles.NotifyRoles();
            if (tick <= 0f)
            {
                tick = 1f;
                if (player.CanMove) CheckStillAndMaybeSuicide();
            }
        }

        private void CheckStillAndMaybeSuicide()
        {
            if (lastPos == Player.transform.position)
            {
                stillCounter += 1f;
                if (stillCounter >= waitTime && waitTime > 0f && Player.IsAlive())
                {
                    var state = PlayerState.GetByPlayerId(Player.PlayerId);
                    if (state != null)
                        state.DeathReason = CustomDeathReason.Suicide;

                    // ホストで強制死亡処理
                    Player.SetRealKiller(Player);          // 自分をキラーに設定
                    Player.RpcMurderPlayer(Player);        // 見た目の殺害
                    state?.SetDead();                      // 状態を死亡に更新
                }
            }
            else
            {
                lastPos = Player.transform.position;
                stillCounter = 0f;
            }
        }

        // ====== 名前横に残り秒数表示（自分だけ） ======
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            // 自分だけ見える
            if (seer.PlayerId != Player.PlayerId) return "";

            if (isForMeeting || !Player.IsAlive()) return "";
            if (!afterFirstMeeting) return "";

            float remaining = Mathf.Max(0f, waitTime - stillCounter);
            if (remaining > 0f)
            {
                return Utils.ColorString(RoleInfo.RoleColor, $" ({remaining:F0}s)");
            }

            return "";
        }
    }
}
