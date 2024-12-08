using System.Collections.Generic;
using System.Linq;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost
{
    public class RoleAddAddons
    {
        public static Dictionary<CustomRoles, RoleAddAddons> AllData = new();
        public static Dictionary<CustomRoles, CustomRoles> chRoles = new(); //1人までしか対応していない、
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public bool IsImpostor;
        public OptionItem GiveAddons;
        //ゲッサー
        public OptionItem GiveGuesser;
        public OptionItem CanGuessTime; public OptionItem OwnCanGuessTime;
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
        public OptionItem GiveSpeeding;
        public OptionItem Speed;
        //ガーディング
        public OptionItem GiveGuarding;
        public OptionItem Guard;
        //イレクター
        public OptionItem GiveElector;
        //ノンレポート
        public OptionItem GiveNonReport;
        public OptionItem OptionConvener; public Convener mode = Convener.nullpo;
        public enum Convener { NotButton, NotReport, ConvenerAll, nullpo }
        public enum cop { NotButton, NotReport, ConvenerAll }
        //トランスパレント
        public OptionItem GiveTransparent;
        //ノットヴォウター
        public OptionItem GiveNotvoter;
        //インフォプアー
        public OptionItem GiveInfoPoor;

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
        public RoleAddAddons(int idStart, TabGroup tab, CustomRoles role, CustomRoles chrole = CustomRoles.NotAssigned, bool NeutralKiller = false, bool MadMate = false, bool DefaaultOn = false)
        {
            this.IsImpostor = role.IsImpostor();
            this.IdStart = idStart;
            this.Role = role;
            GiveAddons = BooleanOptionItem.Create(idStart++, "addaddons", DefaaultOn || NeutralKiller, tab, false).SetParent(Options.CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.None);
            GiveGuesser = BooleanOptionItem.Create(idStart++, "GiveGuesser", false, tab, false).SetParent(GiveAddons);
            CanGuessTime = FloatOptionItem.Create(idStart++, "CanGuessTime", new(1, 15, 1), 3, tab, false).SetParent(GiveGuesser)
                .SetValueFormat(OptionFormat.Players);
            AddTama = BooleanOptionItem.Create(idStart++, "Addtama", false, tab, false).SetParent(GiveGuesser);
            OwnCanGuessTime = FloatOptionItem.Create(idStart++, "OwnCanGuessTime", new(1, 15, 1), 1, tab, false).SetParent(GiveGuesser)
                    .SetValueFormat(OptionFormat.Players);
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
            GiveSpeeding = BooleanOptionItem.Create(idStart++, "GiveSpeeding", false, tab, false).SetParent(GiveAddons);
            Speed = FloatOptionItem.Create(idStart++, "Speed", new(0.5f, 10f, 0.25f), 2f, tab, false).SetParent(GiveSpeeding);
            GiveGuarding = BooleanOptionItem.Create(idStart++, "GiveGuarding", false, tab, false).SetParent(GiveAddons);
            Guard = FloatOptionItem.Create(idStart++, "AddGuardCount", new(1, 10, 1), 1, tab, false).SetParent(GiveGuarding);
            GiveManagement = BooleanOptionItem.Create(idStart++, "GiveManagement", false, tab, false).SetParent(GiveAddons);
            PercentGage = BooleanOptionItem.Create(idStart++, "PercentGage", false, tab, false).SetParent(GiveManagement);
            PonkotuPercernt = BooleanOptionItem.Create(idStart++, "PonkotuPercernt", false, tab, false).SetParent(PercentGage);
            comms = BooleanOptionItem.Create(idStart++, "CanseeComms", false, tab, false).SetParent(GiveManagement);
            Meeting = BooleanOptionItem.Create(idStart++, "CanseeMeeting", false, tab, false).SetParent(GiveManagement);
            Giveseeing = BooleanOptionItem.Create(idStart++, "Giveseeing", false, tab, false).SetParent(GiveAddons);
            SCanSeeComms = BooleanOptionItem.Create(idStart++, "CanseeComms", true, tab, false).SetParent(Giveseeing);
            GiveOpener = BooleanOptionItem.Create(idStart++, "GiveOpener", false, tab, false).SetParent(GiveAddons);
            if (!role.IsImpostor())
            {
                GiveLighting = BooleanOptionItem.Create(idStart++, "GiveLighting", NeutralKiller, tab, false).SetParent(GiveAddons);
                GiveMoon = BooleanOptionItem.Create(idStart++, "GiveMoon", NeutralKiller || MadMate, tab, false).SetParent(GiveAddons);
            }
            //デバフ
            GiveNotvoter = BooleanOptionItem.Create(idStart++, "GiveNotvoter", false, tab, false).SetParent(GiveAddons);
            GiveElector = BooleanOptionItem.Create(idStart++, "GiveElector", false, tab, false).SetParent(GiveAddons);
            GiveInfoPoor = BooleanOptionItem.Create(idStart++, "GiveInfoPoor", false, tab, false).SetParent(GiveAddons);
            GiveNonReport = BooleanOptionItem.Create(idStart++, "GiveNonReport", false, tab, false).SetParent(GiveAddons);
            OptionConvener = StringOptionItem.Create(idStart++, "ConverMode", EnumHelper.GetAllNames<cop>(), 0, tab, false).SetParent(GiveNonReport);

            GiveTransparent = BooleanOptionItem.Create(idStart++, "GiveTransparent", false, tab, false).SetParent(GiveAddons);
            GiveWater = BooleanOptionItem.Create(idStart++, "GiveWater", MadMate, tab, false).SetParent(GiveAddons);
            GiveClumsy = BooleanOptionItem.Create(idStart++, "GiveClumsy", MadMate, tab, false).SetParent(GiveAddons);
            GiveSlacker = BooleanOptionItem.Create(idStart++, "GiveSlacker", false, tab, false).SetParent(GiveAddons);

            role = chrole == CustomRoles.NotAssigned ? role : chrole;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするRoleAddAddonsが作成されました", "RoleAddAddons");
        }
        public static RoleAddAddons Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned, bool NeutralKiller = false, bool MadMate = false, bool DefaaultOn = false)
        {
            return new RoleAddAddons(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename, NeutralKiller, MadMate, DefaaultOn);
        }
        public static RoleAddAddons Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new RoleAddAddons(idStart, tab, role);
        }
        /// <summary>
        /// 役職付与の属性<br/>
        /// ストルナーとかが重いため必要な分だけ取り出す<br/>
        /// GiveAddonがfalseの場合、全てデフォルト値になるのでチェックは基本不要
        /// </summary>
        /// <param name="role">役職</param>
        /// <param name="data">返すデータ</param>
        /// <param name="player">付与されているプレイヤー</param>
        /// <param name="subrole">必要な役職</param>
        /// <returns></returns>
        public static bool GetRoleAddon(CustomRoles role, out RoleAddAddons data, PlayerControl player = null, params CustomRoles[] subrole)
        {
            var haveaddon = false;
            AllData.TryGetValue(CustomRoles.NotAssigned, out var nulldata);
            data = nulldata;

            switch (role)
            {
                case CustomRoles.Stolener:
                    if (player == null && AllData.TryGetValue(role, out data)) haveaddon = true;
                    else if ((player.GetRoleClass() as Stolener)?.ICanUseaddon == true && AllData.TryGetValue(role, out data) && data?.GiveAddons.GetBool() == true)
                        haveaddon = true;
                    break;
                default:
                    if (AllData.TryGetValue(role, out data) && data?.GiveAddons.GetBool() == true)
                        haveaddon = true;
                    break;
            }
            if (data is not null) data.mode = haveaddon ? (Convener)data.OptionConvener.GetValue() : Convener.nullpo;

            if (player != null)
            {
                if (Stolener.Killers.Contains(player.PlayerId) && AllData.TryGetValue(CustomRoles.Stolener, out var ovdata) && ovdata?.GiveAddons.GetBool() == true)
                {
                    if (haveaddon)
                    {
                        refdata(ref data, ovdata, subrole);
                        if (subrole.Contains(CustomRoles.NonReport))
                        {
                            var oldd = (Convener)data.OptionConvener.GetValue();
                            var newd = (Convener)ovdata.OptionConvener.GetValue();
                            if (oldd != newd)
                            {
                                switch (oldd)
                                {
                                    case Convener.NotButton:
                                        if (newd is Convener.ConvenerAll or Convener.NotReport)
                                            data.mode = Convener.ConvenerAll;
                                        break;
                                    case Convener.NotReport:
                                        if (newd is Convener.ConvenerAll or Convener.NotButton)
                                            data.mode = Convener.ConvenerAll;
                                        break;
                                }
                            }
                        }
                    }
                    else data = ovdata;

                    haveaddon = true;
                }
            }
            //持ってなかったりするならぬるぽの奴に変える
            if (data is null || !haveaddon)
            {
                data = nulldata;
            }

            return haveaddon;
        }
        static void refdata(ref RoleAddAddons olddata, RoleAddAddons newdata, CustomRoles[] subrole)
        {
            olddata.GiveAddons = olddata.GiveAddons.InfoGetBool() == false ? newdata.GiveAddons : olddata.GiveAddons;

            if (!olddata.IsImpostor && !newdata.IsImpostor)
            {
                olddata.GiveMoon = olddata.GiveMoon.InfoGetBool() == false ? newdata.GiveMoon : olddata.GiveMoon;
                olddata.GiveLighting = olddata.GiveLighting.InfoGetBool() == false ? newdata.GiveLighting : olddata.GiveLighting;
            }
            else
            if (olddata.IsImpostor && !newdata.IsImpostor)
            {
                olddata.IsImpostor = false;
                olddata.GiveMoon = newdata.GiveMoon;
                olddata.GiveLighting = newdata.GiveLighting;
            }

            //必要な時だけ変更する
            if (subrole.Contains(CustomRoles.NotAssigned))
            {
                foreach (var sub in subrole)
                    switch (sub)
                    {
                        case CustomRoles.Guesser:
                            olddata.GiveGuesser = olddata.GiveGuesser.InfoGetBool() == false ? newdata.GiveGuesser : olddata.GiveGuesser;
                            if (newdata.GiveGuesser.InfoGetBool())
                            {
                                olddata.CanGuessTime = olddata.CanGuessTime.GetInt() <= newdata.CanGuessTime.GetInt() ? newdata.CanGuessTime : olddata.CanGuessTime;
                                olddata.OwnCanGuessTime = olddata.OwnCanGuessTime.GetInt() <= newdata.OwnCanGuessTime.GetInt() ? newdata.OwnCanGuessTime : olddata.OwnCanGuessTime;
                                olddata.ICanGuessVanilla = olddata.ICanGuessVanilla.InfoGetBool() == false ? newdata.ICanGuessVanilla : olddata.ICanGuessVanilla;
                                olddata.ICanGuessNakama = olddata.ICanGuessNakama.InfoGetBool() == false ? newdata.ICanGuessNakama : olddata.ICanGuessNakama;
                                olddata.ICanGuessTaskDoneSnitch = olddata.ICanGuessTaskDoneSnitch.InfoGetBool() == false ? newdata.ICanGuessTaskDoneSnitch : olddata.ICanGuessTaskDoneSnitch;
                                olddata.ICanWhiteCrew = olddata.ICanWhiteCrew.InfoGetBool() == false ? newdata.ICanWhiteCrew : olddata.ICanWhiteCrew;
                                olddata.AddTama = olddata.AddTama.InfoGetBool() == false ? newdata.AddTama : olddata.AddTama;
                            }
                            break;
                        case CustomRoles.Management:
                            olddata.GiveManagement = olddata.GiveManagement.InfoGetBool() == false ? newdata.GiveManagement : olddata.GiveManagement;
                            if (newdata.GiveManagement.InfoGetBool())
                            {
                                olddata.comms = olddata.comms.InfoGetBool() == false ? newdata.comms : olddata.comms;
                                olddata.PercentGage = olddata.PercentGage.InfoGetBool() == false ? newdata.PercentGage : olddata.PercentGage;
                                olddata.Meeting = olddata.Meeting.InfoGetBool() == false ? newdata.Meeting : olddata.Meeting;
                                olddata.PonkotuPercernt = olddata.PonkotuPercernt.InfoGetBool() == false ? newdata.PonkotuPercernt : olddata.PonkotuPercernt;
                            }
                            break;
                        case CustomRoles.seeing:
                            olddata.Giveseeing = olddata.Giveseeing.InfoGetBool() == false ? newdata.Giveseeing : olddata.Giveseeing;
                            if (newdata.Giveseeing.InfoGetBool()) olddata.SCanSeeComms = olddata.SCanSeeComms.InfoGetBool() == false ? newdata.SCanSeeComms : olddata.SCanSeeComms;
                            break;
                        case CustomRoles.Autopsy:
                            olddata.GiveAutopsy = olddata.GiveAutopsy.InfoGetBool() == false ? newdata.GiveAutopsy : olddata.GiveAutopsy;
                            if (newdata.GiveAutopsy.InfoGetBool()) olddata.ACanSeeComms = olddata.ACanSeeComms.InfoGetBool() == false ? newdata.ACanSeeComms : olddata.ACanSeeComms;
                            break;
                        case CustomRoles.PlusVote:
                            olddata.GivePlusVote = olddata.GivePlusVote.InfoGetBool() == false ? newdata.GivePlusVote : olddata.GivePlusVote;
                            if (newdata.GivePlusVote.InfoGetBool()) olddata.AdditionalVote = olddata.AdditionalVote.GetInt() <= newdata.AdditionalVote.GetInt() ? newdata.AdditionalVote : olddata.AdditionalVote;
                            break;
                        case CustomRoles.Revenger:
                            olddata.GiveRevenger = olddata.GiveRevenger.InfoGetBool() == false ? newdata.GiveRevenger : olddata.GiveRevenger;
                            if (newdata.GiveRevenger.InfoGetBool())
                            {
                                olddata.Imp = olddata.Imp.InfoGetBool() == false ? newdata.Imp : olddata.Imp;
                                olddata.Crew = olddata.Crew.InfoGetBool() == false ? newdata.Crew : olddata.Crew;
                                olddata.Neu = olddata.Neu.InfoGetBool() == false ? newdata.Neu : olddata.Neu;
                                olddata.Mad = olddata.Mad.InfoGetBool() == false ? newdata.Mad : olddata.Mad;
                            }
                            break;
                        case CustomRoles.Speeding:
                            olddata.GiveSpeeding = olddata.GiveSpeeding.InfoGetBool() == false ? newdata.GiveSpeeding : olddata.GiveSpeeding;
                            if (newdata.GiveSpeeding.InfoGetBool()) olddata.Speed = olddata.Speed.GetFloat() <= newdata.Speed.GetFloat() ? newdata.Speed : olddata.Speed;
                            break;
                        case CustomRoles.Guarding:
                            olddata.GiveGuarding = olddata.GiveGuarding.InfoGetBool() == false ? newdata.GiveGuarding : olddata.GiveGuarding;
                            if (newdata.GiveGuarding.InfoGetBool()) olddata.Guard = olddata.Guard.GetFloat() <= newdata.Guard.GetFloat() ? newdata.Guard : olddata.Guard;
                            break;
                        case CustomRoles.NonReport:
                            olddata.GiveNonReport = olddata.GiveNonReport.InfoGetBool() == false ? newdata.GiveNonReport : olddata.GiveNonReport;
                            if (!olddata.GiveNonReport.InfoGetBool())
                            {
                                olddata.OptionConvener = newdata.OptionConvener;
                                break;
                            }
                            break;
                        case CustomRoles.watching: olddata.GiveWatching = olddata.GiveWatching.InfoGetBool() == false ? newdata.GiveWatching : olddata.GiveWatching; break;
                        case CustomRoles.Tiebreaker: olddata.GiveTiebreaker = olddata.GiveTiebreaker.InfoGetBool() == false ? newdata.GiveTiebreaker : olddata.GiveTiebreaker; break;
                        case CustomRoles.Opener: olddata.GiveOpener = olddata.GiveOpener.InfoGetBool() == false ? newdata.GiveOpener : olddata.GiveOpener; break;
                        case CustomRoles.Elector: olddata.GiveElector = olddata.GiveElector.InfoGetBool() == false ? newdata.GiveElector : olddata.GiveElector; break;
                        case CustomRoles.Transparent: olddata.GiveTransparent = olddata.GiveTransparent.InfoGetBool() == false ? newdata.GiveTransparent : olddata.GiveTransparent; break;
                        case CustomRoles.Notvoter: olddata.GiveNotvoter = olddata.GiveNotvoter.InfoGetBool() == false ? newdata.GiveNotvoter : olddata.GiveNotvoter; break;
                        case CustomRoles.Water: olddata.GiveWater = olddata.GiveWater.InfoGetBool() == false ? newdata.GiveWater : olddata.GiveWater; break;
                        case CustomRoles.Clumsy: olddata.GiveClumsy = olddata.GiveClumsy.InfoGetBool() == false ? newdata.GiveClumsy : olddata.GiveClumsy; break;
                        case CustomRoles.Slacker: olddata.GiveSlacker = olddata.GiveSlacker.InfoGetBool() == false ? newdata.GiveSlacker : olddata.GiveSlacker; break;
                        case CustomRoles.InfoPoor: olddata.GiveInfoPoor = olddata.GiveInfoPoor.InfoGetBool() == false ? newdata.GiveInfoPoor : olddata.GiveInfoPoor; break;
                    }
            }
            else
            {
                olddata.GiveGuesser = olddata.GiveGuesser.InfoGetBool() == false ? newdata.GiveGuesser : olddata.GiveGuesser;
                olddata.GiveManagement = olddata.GiveManagement.InfoGetBool() == false ? newdata.GiveManagement : olddata.GiveManagement;
                olddata.GiveWatching = olddata.GiveWatching.InfoGetBool() == false ? newdata.GiveWatching : olddata.GiveWatching;
                olddata.Giveseeing = olddata.Giveseeing.InfoGetBool() == false ? newdata.Giveseeing : olddata.Giveseeing;
                olddata.GiveAutopsy = olddata.GiveAutopsy.InfoGetBool() == false ? newdata.GiveAutopsy : olddata.GiveAutopsy;
                olddata.GiveTiebreaker = olddata.GiveTiebreaker.InfoGetBool() == false ? newdata.GiveTiebreaker : olddata.GiveTiebreaker;
                olddata.GivePlusVote = olddata.GivePlusVote.InfoGetBool() == false ? newdata.GivePlusVote : olddata.GivePlusVote;
                olddata.GiveRevenger = olddata.GiveRevenger.InfoGetBool() == false ? newdata.GiveRevenger : olddata.GiveRevenger;
                olddata.GiveOpener = olddata.GiveOpener.InfoGetBool() == false ? newdata.GiveOpener : olddata.GiveOpener;
                olddata.GiveSpeeding = olddata.GiveSpeeding.InfoGetBool() == false ? newdata.GiveSpeeding : olddata.GiveSpeeding;
                olddata.GiveGuarding = olddata.GiveGuarding.InfoGetBool() == false ? newdata.GiveGuarding : olddata.GiveGuarding;
                olddata.GiveElector = olddata.GiveElector.InfoGetBool() == false ? newdata.GiveElector : olddata.GiveElector;
                olddata.GiveNonReport = olddata.GiveNonReport.InfoGetBool() == false ? newdata.GiveNonReport : olddata.GiveNonReport;
                olddata.GiveTransparent = olddata.GiveTransparent.InfoGetBool() == false ? newdata.GiveTransparent : olddata.GiveTransparent;
                olddata.GiveNotvoter = olddata.GiveNotvoter.InfoGetBool() == false ? newdata.GiveNotvoter : olddata.GiveNotvoter;
                olddata.GiveWater = olddata.GiveWater.InfoGetBool() == false ? newdata.GiveWater : olddata.GiveWater;
                olddata.GiveClumsy = olddata.GiveClumsy.InfoGetBool() == false ? newdata.GiveClumsy : olddata.GiveClumsy;
                olddata.GiveSlacker = olddata.GiveSlacker.InfoGetBool() == false ? newdata.GiveSlacker : olddata.GiveSlacker;
                olddata.GiveInfoPoor = olddata.GiveInfoPoor.InfoGetBool() == false ? newdata.GiveInfoPoor : olddata.GiveInfoPoor;
            }
        }
    }
}