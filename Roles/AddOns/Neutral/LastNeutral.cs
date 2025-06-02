using Rewired;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;
namespace TownOfHost.Roles.AddOns.Neutral
{
    public static class LastNeutral
    {
        private static readonly int Id = 79300;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        public static OptionItem GiveKillCooldown;
        public static readonly string[] Givekillcooldownmode =
        {
            "ColoredOff","GiveKillcoolShort","AllGiveKillCoolShort","ColoredOn"
        };
        public static OptionItem ChKilldis;
        //追加勝利
        public static OptionItem GiveOpportunist;
        public static OptionItem CanNotCrewWin;
        public static OptionItem CanNotTaskWin;
        //ゲッサー
        public static OptionItem GiveGuesser;
        public static OptionItem CanGuessTime; public static OptionItem OwnCanGuessTime;
        public static OptionItem ICanGuessVanilla; public static OptionItem ICanGuessTaskDoneSnitch; public static OptionItem ICanWhiteCrew;
        public static OptionItem AddTama;
        //マネジメント
        public static OptionItem GiveManagement;
        public static OptionItem comms; public static OptionItem PercentGage; public static OptionItem Meeting;
        public static OptionItem PonkotuPercernt;
        //ウォッチング
        public static OptionItem GiveWatching;
        //シーイング
        public static OptionItem Giveseeing;
        public static OptionItem SCanSeeComms;
        //オートプシー
        public static OptionItem GiveAutopsy;
        public static OptionItem ACanSeeComms;
        //タイブレーカー
        public static OptionItem GiveTiebreaker;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.LastNeutral, new(1, 1, 1));
            GiveKillCooldown = StringOptionItem.Create(Id + 6, "Givekillcoondown", Givekillcooldownmode, 3, TabGroup.Addons, false).SetParentRole(CustomRoles.LastNeutral).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            KillCooldown = FloatOptionItem.Create(Id + 8, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParentRole(CustomRoles.LastNeutral).SetParent(GiveKillCooldown)
                .SetValueFormat(OptionFormat.Seconds);
            ChKilldis = BooleanOptionItem.Create(Id + 7, "ChKilldis", false, TabGroup.Addons, false).SetParentRole(CustomRoles.LastNeutral).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            OverrideKilldistance.Create(Id + 5, TabGroup.Addons, CustomRoles.LastNeutral);
            GiveOpportunist = BooleanOptionItem.Create(Id + 10, "GiveOpportunist", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            CanNotCrewWin = BooleanOptionItem.Create(Id + 30, "LastNeutralCanNotCrewWin", false, TabGroup.Addons, false).SetParent(GiveOpportunist).SetParentRole(CustomRoles.LastNeutral);
            CanNotTaskWin = BooleanOptionItem.Create(Id + 31, "LastNeutralCanNottaskwWin", false, TabGroup.Addons, false).SetParent(GiveOpportunist).SetParentRole(CustomRoles.LastNeutral);
            GiveGuesser = BooleanOptionItem.Create(Id + 11, "GiveGuesser", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            CanGuessTime = FloatOptionItem.Create(Id + 12, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral)
                .SetValueFormat(OptionFormat.Players);
            AddTama = BooleanOptionItem.Create(Id + 13, "Addtama", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            OwnCanGuessTime = FloatOptionItem.Create(Id + 14, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral)
                    .SetValueFormat(OptionFormat.Players);
            ICanGuessVanilla = BooleanOptionItem.Create(Id + 16, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 17, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            ICanWhiteCrew = BooleanOptionItem.Create(Id + 18, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            GiveManagement = BooleanOptionItem.Create(Id + 19, "GiveManagement", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            PercentGage = BooleanOptionItem.Create(Id + 20, "PercentGage", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastNeutral);
            PonkotuPercernt = BooleanOptionItem.Create(Id + 21, "PonkotuPercernt", false, TabGroup.Addons, false).SetParent(PercentGage).SetParentRole(CustomRoles.LastNeutral);
            comms = BooleanOptionItem.Create(Id + 22, "CanseeComms", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastNeutral);
            Meeting = BooleanOptionItem.Create(Id + 23, "CanseeMeeting", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastNeutral);
            GiveWatching = BooleanOptionItem.Create(Id + 24, "GiveWatching", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            Giveseeing = BooleanOptionItem.Create(Id + 25, "Giveseeing", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            SCanSeeComms = BooleanOptionItem.Create(Id + 26, "CanseeComms", true, TabGroup.Addons, false).SetParent(Giveseeing).SetParentRole(CustomRoles.LastNeutral);
            GiveAutopsy = BooleanOptionItem.Create(Id + 27, "GiveAutopsy", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            ACanSeeComms = BooleanOptionItem.Create(Id + 28, "CanseeComms", true, TabGroup.Addons, false).SetParent(GiveAutopsy).SetParentRole(CustomRoles.LastNeutral);
            GiveTiebreaker = BooleanOptionItem.Create(Id + 29, "GiveTiebreaker", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown(PlayerControl player)
        {
            if (currentId == byte.MaxValue) return;
            var roleclass = player.GetRoleClass();
            switch (Givekillcooldownmode[GiveKillCooldown.GetValue()])
            {
                case "GiveKillcoolShort"://短くなる場合のみ
                    if (KillCooldown.GetFloat() < Main.AllPlayerKillCooldown[currentId] &&
                        (roleclass is ILNKiller))//かつラスポスキルク受け取る
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
                case "AllGiveKillCoolShort"://ラスポルでキルク恩恵受け取るかに関わらず短くなるなら貰う
                    if (KillCooldown.GetFloat() < Main.AllPlayerKillCooldown[currentId])
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
                case "ColoredOn":
                    if (roleclass is ILNKiller)
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
            }
        }
        public static bool CanBeLastNeutral(PlayerControl pc)
        {
            if (!pc.IsAlive() || pc.Is(CustomRoles.LastNeutral) || !pc.Is(CustomRoleTypes.Neutral))
            {
                return false;
            }
            return true;
        }
        public static void SetSubRole()
        {
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastNeutral.IsPresent() || PlayerCatch.AliveNeutalCount != 1)
                return;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (CanBeLastNeutral(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastNeutral);
                    Add(pc.PlayerId);
                    SetKillCooldown(pc);
                    pc.SyncSettings();
                    UtilsNotifyRoles.NotifyRoles();
                    UtilsGameLog.LastLogRole[pc.PlayerId] = "<b>" + Utils.ColorString(UtilsRoleText.GetRoleColor(pc.GetCustomRole()), Translator.GetString("Last-")) + UtilsGameLog.LastLogRole[pc.PlayerId] + "</b>";
                    break;
                }
            }
        }
        public static bool CheckAddWin(PlayerControl pc, GameOverReason reason)
        {
            if (!pc.Is(CustomRoles.LastNeutral) || GiveOpportunist.GetBool()) return false;
            if (reason.Equals(GameOverReason.CrewmatesByTask) && !CanNotTaskWin.GetBool()) return false;
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate
            && reason.Equals(GameOverReason.CrewmatesByVote) && !reason.Equals(GameOverReason.CrewmatesByTask)
            && !CanNotCrewWin.GetBool()) return false;

            if (pc.GetCustomRole() is CustomRoles.Terrorist or CustomRoles.Madonna) return false;

            if (pc.IsAlive() && !pc.IsRiaju())
            {
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.LastNeutral);
                return true;
            }
            return false;
        }
    }
}