using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using HarmonyLib;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class Angel : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Angel),
                player => new Angel(player),
                CustomRoles.Angel,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                94000,
                SetupOptionItem,
                "ang",
                "#88CCFF",
                from: From.TownOfHost_K
            );

        public Angel(PlayerControl player) : base(RoleInfo, player) { }

        // ====== オプション ======
        private static OptionItem OptionProtectUses;
        private static OptionItem OptionRevealPopup;

        private static OptionItem OptSuicideIfImpostor;
        private static OptionItem OptSuicideIfImpostorRate;
        private static OptionItem OptSuicideIfCrewmate;
        private static OptionItem OptSuicideIfCrewmateRate;
        private static OptionItem OptSuicideIfMadmate;
        private static OptionItem OptSuicideIfMadmateRate;
        private static OptionItem OptSuicideIfNeutral;
        private static OptionItem OptSuicideIfNeutralRate;

        private enum OptionName
        {
            AngelProtectUses,
            AngelRevealPopup,
            AngelSuicideIfImpostor,
            AngelSuicideIfImpostorRate,
            AngelSuicideIfCrewmate,
            AngelSuicideIfCrewmateRate,
            AngelSuicideIfMadmate,
            AngelSuicideIfMadmateRate,
            AngelSuicideIfNeutral,
            AngelSuicideIfNeutralRate
        }

        private static void SetupOptionItem()
        {
            OptionProtectUses = IntegerOptionItem.Create(
                RoleInfo,
                10,
                OptionName.AngelProtectUses,
                new IntegerValueRule(1, 5, 1),
                1,
                false
            ).SetValueFormat(OptionFormat.Times);

            OptionRevealPopup = BooleanOptionItem.Create(RoleInfo, 11, OptionName.AngelRevealPopup, true, false);

            OptSuicideIfImpostor = BooleanOptionItem.Create(RoleInfo, 20, OptionName.AngelSuicideIfImpostor, true, false);
            OptSuicideIfImpostorRate = IntegerOptionItem.Create(RoleInfo, 21, OptionName.AngelSuicideIfImpostorRate, new IntegerValueRule(0, 100, 5), 100, false)
                .SetValueFormat(OptionFormat.Percent);

            OptSuicideIfCrewmate = BooleanOptionItem.Create(RoleInfo, 22, OptionName.AngelSuicideIfCrewmate, false, false);
            OptSuicideIfCrewmateRate = IntegerOptionItem.Create(RoleInfo, 23, OptionName.AngelSuicideIfCrewmateRate, new IntegerValueRule(0, 100, 5), 100, false)
                .SetValueFormat(OptionFormat.Percent);

            OptSuicideIfMadmate = BooleanOptionItem.Create(RoleInfo, 24, OptionName.AngelSuicideIfMadmate, false, false);
            OptSuicideIfMadmateRate = IntegerOptionItem.Create(RoleInfo, 25, OptionName.AngelSuicideIfMadmateRate, new IntegerValueRule(0, 100, 5), 100, false)
                .SetValueFormat(OptionFormat.Percent);

            OptSuicideIfNeutral = BooleanOptionItem.Create(RoleInfo, 26, OptionName.AngelSuicideIfNeutral, false, false);
            OptSuicideIfNeutralRate = IntegerOptionItem.Create(RoleInfo, 27, OptionName.AngelSuicideIfNeutralRate, new IntegerValueRule(0, 100, 5), 100, false)
                .SetValueFormat(OptionFormat.Percent);
        }

        // ====== 状態管理 ======
        private int usesLeft;
        private byte protectedTarget = byte.MaxValue;
        private bool pendingProtect;
        private static bool suicideNextRound;

        public override void Add()
        {
            usesLeft = OptionProtectUses.GetInt();
            protectedTarget = byte.MaxValue;
            pendingProtect = false;
            suicideNextRound = false;
        }

        public override void OnStartMeeting()
        {
            pendingProtect = false;
            protectedTarget = byte.MaxValue;
        }

        // ====== 投票フック ======
        public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
        {
            if (voter != Player) return true;
            if (usesLeft <= 0) return true;

            // 自投票で守護モードへ移行
            if (votedForId == Player.PlayerId)
            {
                pendingProtect = true;
                Utils.SendMessage("エンジェルの力が目覚めた… 次の投票で守護対象を選んでください！", Player.PlayerId);
                return false; // 自投票はキャンセル
            }
            return true;
        }

        public override (byte? votedForId, int? numVotes, bool doVote)
    ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
        {
            var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);

            // Angel本人が守護対象を決める処理
            if (voterId == Player.PlayerId && pendingProtect && sourceVotedForId != Player.PlayerId)
            {
                protectedTarget = sourceVotedForId;
                usesLeft--;
                Utils.SendMessage($"あなたは {PlayerCatch.GetPlayerById(protectedTarget)?.name} を守護対象に選びました！", Player.PlayerId);
                pendingProtect = false;

                // 投票をキャンセル（自分の票は無効）
                return (votedForId, numVotes, false);
            }

            // ★ 守護発動：守護対象に入った票をスキップに上書え
            if (protectedTarget != byte.MaxValue && sourceVotedForId == protectedTarget)
            {
                if (OptionRevealPopup.GetBool())
                    Utils.SendMessage($"{PlayerCatch.GetPlayerById(protectedTarget)?.name} はエンジェルに守られた！", 255);
                else
                    Utils.SendMessage("あなたの守護が発動しました！", Player.PlayerId);

                CheckSuicide(protectedTarget);

                // 引数を直接上書き
                votedForId = 255;   // スキップ票
                numVotes = 1;       // 1票分に固定
                doVote = true;      // 投票完了フラグを必ず立てる

                return (votedForId, numVotes, doVote);
            }

            return (votedForId, numVotes, doVote);
        }

        // ====== 自滅チェック ======
        private void CheckSuicide(byte targetId)
        {
            var target = PlayerCatch.GetPlayerById(targetId);
            if (target == null) return;
            var roleType = target.GetCustomRole();

            if (OptSuicideIfImpostor.GetBool() && roleType.IsImpostor())
                if (IRandom.Instance.Next(0, 100) < OptSuicideIfImpostorRate.GetInt()) suicideNextRound = true;
            if (OptSuicideIfCrewmate.GetBool() && roleType.IsCrewmate())
                if (IRandom.Instance.Next(0, 100) < OptSuicideIfCrewmateRate.GetInt()) suicideNextRound = true;
            if (OptSuicideIfMadmate.GetBool() && roleType.IsMadmate())
                if (IRandom.Instance.Next(0, 100) < OptSuicideIfMadmateRate.GetInt()) suicideNextRound = true;
            if (OptSuicideIfNeutral.GetBool() && roleType.IsNeutral())
                if (IRandom.Instance.Next(0, 100) < OptSuicideIfNeutralRate.GetInt()) suicideNextRound = true;
        }

        // ====== 会議後処理 ======
        public override void AfterMeetingTasks()
        {
            if (suicideNextRound)
            {
                suicideNextRound = false;

                // 全体に死亡を通知
                Player.RpcMurderPlayer(Player);
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;

                Utils.SendMessage("あなたは守護の代償で命を落としました…", Player.PlayerId);
            }
        }
    }
}
