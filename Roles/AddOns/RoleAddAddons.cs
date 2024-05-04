using System.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class RoleAddAddons
    {
        public static Dictionary<CustomRoles, RoleAddAddons> AllData = new();
        public static Dictionary<CustomRoles, CustomRoles> chRoles = new(); //1人までしか対応していない、
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem GiveAddons;
        //ゲッサー
        public OptionItem GiveGuesser;
        public OptionItem CanGuessTime; public OptionItem OwnCanGuessTime; public OptionItem TryHideMsg;
        public OptionItem ICanGuessVanilla; public OptionItem ICanGuessNakama; public OptionItem ICanGuessTaskDoneSnitch;
        public OptionItem ICanWhiteCrew; public OptionItem AddTama;
        //マネジメント
        public OptionItem GiveManagement;
        public OptionItem comms; public OptionItem PercentGage; public OptionItem Meeting;
        public OptionItem PonkotuPercernt;
        //ウォッチング
        public OptionItem GiveWatching;
        //シーイング
        public OptionItem Giveseeing;
        public OptionItem SCanSeeComms;
        //オートプシー
        public OptionItem GiveAutopsy;
        public OptionItem ACanSeeComms;
        //タイブレーカー
        public OptionItem GiveTiebreaker;
        //プラスポート
        public OptionItem GivePlusVote;
        public OptionItem AdditionalVote;
        //リベンジャー
        public OptionItem GiveRevenger;
        public OptionItem Imp; public OptionItem Crew; public OptionItem Mad; public OptionItem Neu;
        //オープナー
        public OptionItem GiveOpener;
        //スピーディング
        public OptionItem GiveSpeedingding;
        public OptionItem Speed;
        //イレクター
        public OptionItem GiveElector;
        //ノンレポート
        public OptionItem GiveNonReport;
        public static OptionItem OptionConvener; public static Convener Mode;
        public enum Convener { NotButton, NotReport, ConvenerAll }
        //トランスパレント
        public OptionItem GiveTransparent;
        //ノットヴォウター
        public OptionItem GiveNotvoter;
        //ウォーター
        public OptionItem GiveWater;
        //クラムシー
        public OptionItem GiveClumsy;
        //スラッカー
        public OptionItem GiveSlacker;
        //ムーン
        public OptionItem GiveMoon;
        //ライティング
        public OptionItem GiveLighting;
        public RoleAddAddons(int idStart, TabGroup tab, CustomRoles role, CustomRoles chrole = CustomRoles.NotAssigned)
        {
            this.IdStart = idStart;
            this.Role = role;
            GiveAddons = BooleanOptionItem.Create(idStart++, "addaddons", false, tab, false).SetParent(Options.CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.None);
            GiveGuesser = BooleanOptionItem.Create(idStart++, "GiveGuesser", false, tab, false).SetParent(GiveAddons);
            CanGuessTime = FloatOptionItem.Create(idStart++, "CanGuessTime", new(1, 15, 1), 3, tab, false).SetParent(GiveGuesser)
                .SetValueFormat(OptionFormat.Players);
            AddTama = BooleanOptionItem.Create(idStart++, "Addtama", false, tab, false).SetParent(GiveGuesser);
            OwnCanGuessTime = FloatOptionItem.Create(idStart++, "OwnCanGuessTime", new(1, 15, 1), 1, tab, false).SetParent(GiveGuesser)
                    .SetValueFormat(OptionFormat.Players);
            TryHideMsg = BooleanOptionItem.Create(idStart++, "TryHideMsg", true, tab, false).SetParent(GiveGuesser);
            ICanGuessVanilla = BooleanOptionItem.Create(idStart++, "CanGuessVanilla", true, tab, false).SetParent(GiveGuesser);
            ICanGuessNakama = BooleanOptionItem.Create(idStart++, "CanGuessNakama", true, tab, false).SetParent(GiveGuesser);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(idStart++, "CanGuessTaskDoneSnitch", false, tab, false).SetParent(GiveGuesser);
            ICanWhiteCrew = BooleanOptionItem.Create(idStart++, "CanWhiteCrew", false, tab, false).SetParent(GiveGuesser);
            GiveWatching = BooleanOptionItem.Create(idStart++, "GiveWatching", false, tab, false).SetParent(GiveAddons);
            GivePlusVote = BooleanOptionItem.Create(idStart++, "GivePlusVote", false, tab, false).SetParent(GiveAddons);
            AdditionalVote = IntegerOptionItem.Create(idStart++, "MayorAdditionalVote", new(1, 99, 1), 1, tab, false).SetValueFormat(OptionFormat.Votes).SetParent(GivePlusVote);
            GiveTiebreaker = BooleanOptionItem.Create(idStart++, "GiveTiebreaker", false, tab, false).SetParent(GiveAddons);
            GiveAutopsy = BooleanOptionItem.Create(idStart++, "GiveAutopsy", false, tab, false).SetParent(GiveAddons);
            ACanSeeComms = BooleanOptionItem.Create(idStart++, "CanseeComms", true, tab, false).SetParent(GiveAutopsy);
            GiveRevenger = BooleanOptionItem.Create(idStart++, "GiveRevenger", false, tab, false).SetParent(GiveAddons);
            Imp = BooleanOptionItem.Create(idStart++, "NekoKabochaImpostorsGetRevenged", true, tab, false).SetParent(GiveRevenger);
            Crew = BooleanOptionItem.Create(idStart++, "NekomataCanCrew", true, tab, false).SetParent(GiveRevenger);
            Mad = BooleanOptionItem.Create(idStart++, "NekoKabochaMadmatesGetRevenged", true, tab, false).SetParent(GiveRevenger);
            Neu = BooleanOptionItem.Create(idStart++, "NekomataCanNeu", true, tab, false).SetParent(GiveRevenger);
            GiveSpeedingding = BooleanOptionItem.Create(idStart++, "GiveSpeeding", false, tab, false).SetParent(GiveAddons);
            Speed = FloatOptionItem.Create(idStart++, "Speed", new(0.5f, 10f, 0.25f), 2f, tab, false).SetParent(GiveSpeedingding);
            GiveManagement = BooleanOptionItem.Create(idStart++, "GiveManagement", false, tab, false).SetParent(GiveAddons);
            PercentGage = BooleanOptionItem.Create(idStart++, "PercentGage", false, tab, false).SetParent(GiveManagement);
            PonkotuPercernt = BooleanOptionItem.Create(idStart++, "PonkotuPercernt", true, tab, false).SetParent(PercentGage);
            comms = BooleanOptionItem.Create(idStart++, "CanseeComms", false, tab, false).SetParent(GiveManagement);
            Meeting = BooleanOptionItem.Create(idStart++, "CanseeMeeting", false, tab, false).SetParent(GiveManagement);
            Giveseeing = BooleanOptionItem.Create(idStart++, "Giveseeing", false, tab, false).SetParent(GiveAddons);
            SCanSeeComms = BooleanOptionItem.Create(idStart++, "CanseeComms", true, tab, false).SetParent(Giveseeing);
            GiveOpener = BooleanOptionItem.Create(idStart++, "GiveOpener", false, tab, false).SetParent(GiveAddons);
            if (!role.IsImpostor())
            {
                GiveLighting = BooleanOptionItem.Create(idStart++, "GiveLighting", false, tab, false).SetParent(GiveAddons);
                GiveMoon = BooleanOptionItem.Create(idStart++, "GiveMoon", false, tab, false).SetParent(GiveAddons);
            }
            //デバフ
            GiveNotvoter = BooleanOptionItem.Create(idStart++, "GiveNotvoter", false, tab, false).SetParent(GiveAddons);
            GiveElector = BooleanOptionItem.Create(idStart++, "GiveElector", false, tab, false).SetParent(GiveAddons);
            GiveNonReport = BooleanOptionItem.Create(idStart++, "GiveNonReport", false, tab, false).SetParent(GiveAddons);
            OptionConvener = StringOptionItem.Create(idStart++, "ConverMode", EnumHelper.GetAllNames<Convener>(), 0, tab, false).SetParent(GiveNonReport);

            GiveTransparent = BooleanOptionItem.Create(idStart++, "GiveTransparent", false, tab, false).SetParent(GiveAddons);
            GiveWater = BooleanOptionItem.Create(idStart++, "GiveWater", false, tab, false).SetParent(GiveAddons);
            GiveClumsy = BooleanOptionItem.Create(idStart++, "GiveClumsy", false, tab, false).SetParent(GiveAddons);
            GiveSlacker = BooleanOptionItem.Create(idStart++, "GiveSlacker", false, tab, false).SetParent(GiveAddons);

            role = chrole == CustomRoles.NotAssigned ? role : chrole;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするRoleAddAddonsが作成されました", "RoleAddAddons");
        }
        public static RoleAddAddons Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned)
        {
            return new RoleAddAddons(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename);
        }
    }
}
/*
            var pc = Utils.GetPlayerById(playerId);
            CustomRoles? RoleNullable = pc?.GetCustomRole();
            if (RoleNullable == null) return;
            CustomRoles role = RoleNullable.Value;

            if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
*/