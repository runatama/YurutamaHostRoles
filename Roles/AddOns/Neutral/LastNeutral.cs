using TownOfHost.Roles.Core;
using static TownOfHost.Options;
namespace TownOfHost.Roles.AddOns.Neutral
{
    public static class LastNeutral
    {
        private static readonly int Id = 79300;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
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
            KillCooldown = FloatOptionItem.Create(Id + 8, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral])
                .SetValueFormat(OptionFormat.Seconds);
            ChKilldis = BooleanOptionItem.Create(Id + 7, "ChKilldis", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            OverrideKilldistance.Create(Id + 5, TabGroup.Addons, CustomRoles.LastNeutral);
            GiveOpportunist = BooleanOptionItem.Create(Id + 10, "GiveOpportunist", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            CanNotCrewWin = BooleanOptionItem.Create(Id + 30, "LastNeutralCanNotCrewWin", false, TabGroup.Addons, false).SetParent(GiveOpportunist);
            CanNotTaskWin = BooleanOptionItem.Create(Id + 31, "LastNeutralCanNottaskwWin", false, TabGroup.Addons, false).SetParent(GiveOpportunist);
            GiveGuesser = BooleanOptionItem.Create(Id + 11, "GiveGuesser", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            CanGuessTime = FloatOptionItem.Create(Id + 12, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(GiveGuesser)
                .SetValueFormat(OptionFormat.Players);
            AddTama = BooleanOptionItem.Create(Id + 13, "Addtama", false, TabGroup.Addons, false).SetParent(GiveGuesser);
            OwnCanGuessTime = FloatOptionItem.Create(Id + 14, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(GiveGuesser)
                    .SetValueFormat(OptionFormat.Players);
            ICanGuessVanilla = BooleanOptionItem.Create(Id + 16, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(GiveGuesser);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 17, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(GiveGuesser);
            ICanWhiteCrew = BooleanOptionItem.Create(Id + 18, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(GiveGuesser);
            GiveManagement = BooleanOptionItem.Create(Id + 19, "GiveManagement", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            PercentGage = BooleanOptionItem.Create(Id + 20, "PercentGage", false, TabGroup.Addons, false).SetParent(GiveManagement);
            PonkotuPercernt = BooleanOptionItem.Create(Id + 21, "PonkotuPercernt", true, TabGroup.Addons, false).SetParent(PercentGage);
            comms = BooleanOptionItem.Create(Id + 22, "CanseeComms", false, TabGroup.Addons, false).SetParent(GiveManagement);
            Meeting = BooleanOptionItem.Create(Id + 23, "CanseeMeeting", false, TabGroup.Addons, false).SetParent(GiveManagement);
            GiveWatching = BooleanOptionItem.Create(Id + 24, "GiveWatching", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            Giveseeing = BooleanOptionItem.Create(Id + 25, "Giveseeing", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            SCanSeeComms = BooleanOptionItem.Create(Id + 26, "CanseeComms", true, TabGroup.Addons, false).SetParent(Giveseeing);
            GiveAutopsy = BooleanOptionItem.Create(Id + 27, "GiveAutopsy", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            ACanSeeComms = BooleanOptionItem.Create(Id + 28, "CanseeComms", true, TabGroup.Addons, false).SetParent(GiveAutopsy);
            GiveTiebreaker = BooleanOptionItem.Create(Id + 29, "GiveTiebreaker", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown()
        {
            if (currentId == byte.MaxValue) return;
            Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
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
            || !CustomRoles.LastNeutral.IsPresent() || Main.AliveNeutalCount != 1)
                return;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (CanBeLastNeutral(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastNeutral);
                    Add(pc.PlayerId);
                    SetKillCooldown();
                    pc.SyncSettings();
                    UtilsNotifyRoles.NotifyRoles();
                    Main.LastLogRole[pc.PlayerId] = "<b>" + Utils.ColorString(UtilsRoleText.GetRoleColor(pc.GetCustomRole()), Translator.GetString("Last-")) + Main.LastLogRole[pc.PlayerId] + "</b>";
                    break;
                }
            }
        }
    }
}