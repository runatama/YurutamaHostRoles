using AmongUs.GameOptions;

using TownOfHost;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class Dictator : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Dictator),
                player => new Dictator(player),
                CustomRoles.Dictator,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                9800,
                SetupOptionItem,
                "dic",
                "#df9b00",
                (3, 6),
                from: From.TownOfHost
                
            );

        public Dictator(PlayerControl player)
            : base(RoleInfo, player)
        { }

        // ====== Options ======
        private enum OptionName
        {
            DictatorSelfVote, // 自投票UIを使うか
            DictatorSurvive,  // 発動しても自殺しない（生存）
            DictatorOneShot   // ★追加：一度使ったら以後は能力を無効化する
        }

        private static OptionItem OptionSelfVote;
        private static OptionItem OptionSurvive;
        private static OptionItem OptionOneShot;

        private static void SetupOptionItem()
        {
            // 既存
            OptionSelfVote = BooleanOptionItem.Create(RoleInfo, 10, OptionName.DictatorSelfVote, false, false);
            OptionSurvive = BooleanOptionItem.Create(RoleInfo, 11, OptionName.DictatorSurvive, false, false);

            // ★追加：一回限りモード（既定OFFで互換維持）
            OptionOneShot = BooleanOptionItem.Create(RoleInfo, 12, OptionName.DictatorOneShot, false, false);
        }

        public override void Add()
        {
            // 自投票UIを使うときのセットアップ
            AddSelfVotes(Player);
            _used = false; // 役職付与時に初期化
        }

        // ====== State ======
        // 一回限りモードの消費フラグ（ホスト判定で十分。必要なら同期ロジックに載せても可）
        private bool _used;

        // 能力が利用可能か（OneShotがONかつ既に使っていたら不可）
        private bool AbilityAvailable => !(OptionOneShot.GetBool() && _used);

        // ====== Voting Hooks ======
        public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
        {
            // 能力が使えない状況 or 一回限りを消費済みなら通常処理
            if (!Canuseability() || !AbilityAvailable) return true;

            // 自分（独裁者）の投票のみ扱う
            if (Is(voter))
            {
                // 自投票UIモードを使わない場合、このフックでは何もしない
                if (!OptionSelfVote.GetBool()) return true;

                // 自投票UIの状態（Self/Skip/Vote）を確認
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                    {
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Dictator"), GetString("Vote.Dictator")) + GetString("VoteSkillMode"), Player.PlayerId);
                    }
                    else if (status is VoteStatus.Skip)
                    {
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    }
                    else if (status is VoteStatus.Vote)
                    {
                        // ★発動：相手を即追放
                        PlayerCatch.GetPlayerById(votedForId).SetRealKiller(Player);
                        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, votedForId);
                        UtilsGameLog.AddGameLog("Dictator", string.Format(GetString("Dictator.log"), UtilsName.GetPlayerColor(Player)));

                        // ★自分の自殺はオプションで制御
                        if (!OptionSurvive.GetBool())
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
                        }

                        // ★OneShotがONなら消費
                        if (OptionOneShot.GetBool())
                        {
                            _used = true;
                            // （任意）通知したい場合はメッセージ出す
                            // Utils.SendMessage("Dictator ability has been consumed.", Player.PlayerId);
                        }
                    }

                    // Self でモード維持、それ以外で解除（従来仕様）
                    SetMode(Player, status is VoteStatus.Self);

                    // Self/Skip は通常の票処理を止める（UIボタン化）
                    // Vote のときだけ通常進行（ただし ModifyVote 側で doVote を制御）
                    return status is VoteStatus.Vote;
                }
            }

            // ここまで来たら通常の票処理
            return true;
        }

        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;

            if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability())
            {
                // ベース説明＋Survive/OneShot/Usedの簡易状態表示（翻訳キー不要）
                var baseMsg = OptionSelfVote.GetBool() ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg");
                var surviveNote = OptionSurvive.GetBool() ? " [Survive: ON]" : " [Survive: OFF]";
                var oneShotNote = OptionOneShot.GetBool() ? " [OneShot: ON]" : " [OneShot: OFF]";
                var usedNote = (OptionOneShot.GetBool() && _used) ? " [USED]" : "";
                var mes = $"<color={RoleInfo.RoleColorCode}>{baseMsg}{surviveNote}{oneShotNote}{usedNote}</color>";
                return isForHud ? mes : $"<size=40%>{mes}</size>";
            }
            return "";
        }

        public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
        {
            var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
            var baseVote = (votedForId, numVotes, doVote);

            // 条件外は何もしない（従来通り）
            if (!isIntentional
                || !Canuseability()
                || !AbilityAvailable                     // ★一回限り消費済みなら発動しない
                || OptionSelfVote.GetBool()              // 自投票UIモード時の発動は CheckVoteAsVoter 側で処理
                || voterId != Player.PlayerId
                || sourceVotedForId == Player.PlayerId   // 自分投票では発動しない
                || sourceVotedForId >= 253               // Skip/NoVote 等の予約IDを弾く
                || !Player.IsAlive())
            {
                return baseVote;
            }

            // ★自投票UIモード OFF 時：意図的に誰かへ投票した瞬間に独裁発動
            PlayerCatch.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
            UtilsGameLog.AddGameLog("Dictator", string.Format(GetString("Dictator.log"), UtilsName.GetPlayerColor(Player)));

            // ★自分を退場させない設定なら自殺登録をスキップ
            if (!OptionSurvive.GetBool())
            {
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            }

            // ★OneShotがONなら消費
            if (OptionOneShot.GetBool())
            {
                _used = true;
                // （任意）通知したい場合はメッセージ出す
                // Utils.SendMessage("Dictator ability has been consumed.", Player.PlayerId);
            }

            // 通常の投票集計には流さない（多重処理防止）
            return (votedForId, numVotes, false);
        }
    }
}
