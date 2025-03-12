using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.AddOns.Common;
using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.UtilsRoleText;

namespace TownOfHost
{
    #region  ShowOption
    public static class UtilsShowOption
    {

        public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
        {
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"), PlayerId);
                if (CustomRoles.HASFox.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASFox) + GetString("HASFoxInfoLong"), PlayerId); }
                if (CustomRoles.HASTroll.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASTroll) + GetString("HASTrollInfoLong"), PlayerId); }
            }
            else
            {
                if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
                if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
                if (Options.InsiderMode.GetBool()) { SendMessage(GetString("InsiderModeInfo"), PlayerId); }
                if (Options.SuddenDeathMode.GetBool()) { SendMessage(GetString("SuddenDeathInfo"), PlayerId); }
                if (Options.IsStandardHAS) { SendMessage(GetString("StandardHASInfo"), PlayerId); }
                if (Options.EnableGM.GetBool()) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong"), PlayerId); }
                foreach (var role in CustomRolesHelper.AllStandardRoles)
                {
                    if (role.IsEnable())
                    {
                        if (role.GetRoleInfo()?.Description is { } description)
                        {
                            SendMessage(description.FullFormatHelp, PlayerId);
                        }
                        // RoleInfoがない役職は従来処理
                        else
                        {
                            SendMessage(GetRoleName(role) + "\n\n" + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
                        }
                    }
                }
                foreach (var role in CustomRolesHelper.AllAddOns)
                    if (role.IsEnable())
                    {
                        SendMessage(GetAddonsHelp(role), PlayerId);
                    }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var mapId = Main.NormalOptions.MapId;
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var sb = new StringBuilder();

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.TaskBattle:
                    sb.Append($"<size=30%>{GetString("TaskBattleInfo")}</size>\n");
                    break;
                case CustomGameMode.HideAndSeek:
                    sb.Append($"<size=30%>{GetString("HideAndSeekInfo")}</size>\n");
                    break;
                case CustomGameMode.Standard:
                    if (Options.SuddenDeathMode.GetBool())
                    {
                        sb.Append($"<size=30%>{GetString("SuddenDeathInfo")}</size>\n");
                    }
                    if (Options.StandardHAS.GetBool())
                    {
                        sb.Append($"<size=30%>{GetString("StandardHASInfo")}</size>\n");
                    }
                    break;
            }

            sb.AppendFormat("<line-height={0}>", "45%");

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                sb.Append(GetString("Roles")).Append(':');
                if (CustomRoles.HASFox.IsEnable()) sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) sb.AppendFormat("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(sb.ToString(), PlayerId);
                sb.Clear().Append(GetString("Settings")).Append(':');
                sb.Append(GetString("HideAndSeek"));
            }
            else
            {
                sb.AppendFormat("<size={0}>", ActiveSettingsSize);
                sb.Append("<size=100%>").Append(GetString("Settings")).Append('\n').Append("</size>");

                var nowcount = GetRoleTypesCountInt();

                sb.AppendFormat("\n【{0}: {1}】", RoleAssignManager.OptionAssignMode.GetName(true), RoleAssignManager.OptionAssignMode.GetString());

                var (impcheck, impmax, impmin) = RoleAssignManager.CheckRoleTypeCount(CustomRoleTypes.Impostor);
                var (madcheck, madmax, madmin) = RoleAssignManager.CheckRoleTypeCount(CustomRoleTypes.Madmate);
                var (crewcheck, crewmax, crewmin) = RoleAssignManager.CheckRoleTypeCount(CustomRoleTypes.Crewmate);
                var (neucheck, neumax, neumin) = RoleAssignManager.CheckRoleTypeCount(CustomRoleTypes.Neutral);
                if (nowcount.imp > 0) sb.Append(ColorString(Palette.ImpostorRed, "\n<u>☆Impostors☆" + $"({nowcount.imp})" + (impcheck ? $"　[Min : {impmin}|Max : {impmax} ]" : "") + "</u>\n"));
                if (nowcount.mad > 0) sb.Append(ColorString(ModColors.MadMateOrenge, "\n<u>☆MadMates☆" + $"({nowcount.mad})" + (madcheck ? $"　[Min : {madmin}|Max : {madmax} ]" : "") + "</u>\n"));
                if (nowcount.crew > 0) sb.Append(ColorString(Palette.Blue, "\n<u>☆CrewMates☆" + $"({nowcount.crew})" + (crewcheck ? $"　[Min : {crewmin}|Max : {crewmax} ]" : "") + "</u>\n"));
                if (nowcount.neutral > 0) sb.Append(ColorString(ModColors.Gray, "\n<u>☆Neutrals☆" + $"({nowcount.neutral})" + (neucheck ? $"　[Min : {neumin}|Max : {neumax} ]" : "") + "</u>\n"));
                if (nowcount.lovers > 0) sb.Append(ColorString(ModColors.Pink, "\n<u>☆Lovers☆" + $"({nowcount.lovers})" + "</u>\n"));
                if (nowcount.ghost > 0) sb.Append(ColorString(ModColors.GhostRoleColor, "\n<u>☆GhostRole☆" + $"({nowcount.ghost})" + "</u>\n"));
                if (nowcount.addon > 0) sb.Append(ColorString(ModColors.AddonsColor, "\n<u>☆Add-Ons☆" + $"({nowcount.addon})" + "</u>\n"));

                sb.Append("\n");
                foreach (var roleop in Options.CustomRoleCounts)
                {
                    var role = roleop.Key;
                    if (role is not CustomRoles.Jackaldoll || JackalDoll.sidekick.GetFloat() is 0 || CustomRoles.Jackaldoll.GetChance() == 0)
                        if (!role.IsEnable()) continue;
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;

                    var mark = "";
                    if (role.IsCrewmate()) mark = ColorString(Palette.Blue, "<u>Ⓒ");
                    if (role.IsImpostor()) mark = ColorString(Palette.ImpostorRed, "<u>Ⓘ");
                    if (role.IsNeutral()) mark = ColorString(ModColors.Gray, "<u>Ⓝ");
                    if (role.IsMadmate()) mark = ColorString(ModColors.MadMateOrenge, "<u>Ⓜ");
                    if (role.IsAddOn() || role is CustomRoles.Amanojaku or CustomRoles.Twins) mark = ColorString(ModColors.AddonsColor, "<u>Ⓐ");
                    if (role.IsGhostRole()) mark = ColorString(ModColors.GhostRoleColor, "<u>Ⓖ");
                    if (role.IsRiaju()) mark = ColorString(ModColors.Pink, "<u>Ⓛ");

                    sb.Append($"\n<size=90%>{mark}{UtilsRoleText.GetCombinationCName(role)}</size><size=70%>×{role.GetCount()}</size></u>\n\n");
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, 1);
                    CheckPageChange(PlayerId, sb);
                }
                //sb.Append("</line-hight><line-height=55%>");
                foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
                {
                    if (opt.Name is "RandomSpawn")
                    {
                        foreach (var randomOpt in opt.Children)
                        {
                            var Id = randomOpt.Id / 100;
                            //マップIDor6(カススポ)
                            if (Id % 10 != mapId && Id % 100 != 5 && Id % 10 != 9) continue;
                            //現在のマップのみ表示する
                            if (randomOpt.GetBool())
                            {
                                //Onの時は頭に改ページを入れる
                                CheckPageChange(PlayerId, sb, true);
                                sb.Append($"\n<size=65%>【{opt.GetName(false)}】</size>");
                                sb.Append($"\n {randomOpt.GetName(true)}: {randomOpt.GetString().RemoveSN()}\n");

                                ShowChildrenSettings(randomOpt, ref sb, 1, getbool: true);
                            }
                            else
                            {
                                //オフならそのままで大丈夫
                                sb.Append($"\n<size=85%><b>☆{opt.GetName(false)}</b></size>");
                                sb.Append($"\n {randomOpt.GetName(false)}: {randomOpt.GetString().RemoveSN()}\n");
                            }
                        }
                        CheckPageChange(PlayerId, sb);
                    }
                    else
                    {
                        var s = new StringBuilder();
                        ShowChildrenSettings(opt, ref s, 1, getbool: true);
                        if (s.ToString().RemoveHtmlTags() == "" && opt.Children.Count != 0) continue;
                        if (opt.Name is "RoleAssigningAlgorithm" or "LimitMeetingTime" or "LowerLimitVotingTime")
                            sb.Append($"\n<size=65%><b>▶{opt.GetName(false)}</b>: {opt.GetString().RemoveSN()}\n</size>");
                        else
                        if (opt.Name is "KillFlashDuration")
                            sb.Append($"\n<size=65%>◇{opt.GetName(true)}: {opt.GetString().RemoveSN()}\n</size>");
                        else
                        if (opt.Name is "KickModClient" or "KickPlayerFriendCodeNotExist" or "ApplyDenyNameList" or "ApplyBanList")
                            sb.Append($"\n<size=65%>◆{opt.GetName(true)}\n</size>");
                        else if (opt.Name is "TaskBattleSet" or "ONspecialMode" or "ExperimentalMode" or "MadmateOption" or "GRRoleOptions"
                                or "MapModification" or "Sabotage" or "RandomMapsMode" or "GhostOptions" or "MeetingAndVoteOpt" or "DevicesOption" or "ConvenientOptions")
                            sb.Append($"\n<size=85%><b>■{opt.GetName(false)}</b>\n</size>");
                        else sb.Append($"\n<size=65%><b>・{opt.GetName(false)}</b>\n</size>");
                        ShowChildrenSettings(opt, ref sb, 1, getbool: true);
                        CheckPageChange(PlayerId, sb);
                    }
                }
            }
            if (sb.ToString() != "")
                SendMessage(sb.ToString(), PlayerId);
        }
        private static void CheckPageChange(byte PlayerId, StringBuilder sb, bool force = false, string title = "")
        {
            if (sb.ToString().RemoveHtmlTags() == "") return;

            //2Byte文字想定で1000byt越えるならページを変える
            if (force || sb.Length > 750)
            {
                SendMessage(sb.ToString(), PlayerId, title);
                sb.Clear();
                sb.AppendFormat("<line-height=45%><size={0}>", ActiveSettingsSize);
            }
        }
        public static void CopyCurrentSettings()
        {
            var sb = new StringBuilder();
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && !AmongUsClient.Instance.AmHost)
            {
                ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
                return;
            }
            sb.Append($"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━");
            foreach (var role in Options.CustomRoleCounts)
            {
                if (!role.Key.IsEnable()) continue;
                sb.Append($"\n【{UtilsRoleText.GetCombinationCName(role.Key)}×{role.Key.GetCount()}】\n");
                ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref sb);
                var text = sb.ToString();
                sb.Clear().Append(text.RemoveHtmlTags());
            }
            sb.Append($"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━");
            foreach (var opt in OptionItem.AllOptions.Where(x => x.GetBool() && x.Parent == null && x.Id >= 80000 && !x.IsHiddenOn(Options.CurrentGameMode)))
            {
                if (opt.Name == "KillFlashDuration")
                    sb.Append($"\n【{opt.GetName(true)}: {opt.GetString().RemoveSN()}】\n");
                else
                    sb.Append($"\n【{opt.GetName(true)}】\n");
                ShowChildrenSettings(opt, ref sb);
                var text = sb.ToString();
                sb.Clear().Append(text.RemoveHtmlTags());
            }
            sb.Append($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            ClipboardHelper.PutClipboardString(sb.ToString());
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var m = GetActiveRoleText(PlayerId);
            if (m.RemoveHtmlTags() != "")
                SendMessage(m, PlayerId);
        }
        public static string tmpRole()
        {
            var sb = new StringBuilder().AppendFormat("<line-height={0}>", ActiveSettingsLineHeight);
            sb.AppendFormat("<size={0}>", "70%");
            sb.AppendFormat("\n◆{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString());
            sb.Append("\n<size=100%>\n").Append(GetString("Roles")).Append("</size>");
            CustomRoles[] roles = null;
            CustomRoles[] addons = null;
            if (Options.CurrentGameMode == CustomGameMode.Standard) roles = CustomRolesHelper.AllStandardRoles;
            if (Options.CurrentGameMode == CustomGameMode.Standard) addons = CustomRolesHelper.AllAddOns;
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) roles = CustomRolesHelper.AllHASRoles;
            if (roles != null)
            {
                foreach (CustomRoles role in roles)
                {//Roles
                    if (role.IsEnable())
                    {
                        var longestNameByteCount = roles.Select(x => x.GetCombinationCName().Length).OrderByDescending(x => x).FirstOrDefault();
                        var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
                        var co = role.IsImpostor() ? ColorString(Palette.ImpostorRed, "●") : (role.IsCrewmate() ? ColorString(Palette.CrewmateBlue, "●") : (role.IsMadmate() ? "<color=#ff7f50>●</color>" : ColorString(Palette.DisabledGrey, "●")));
                        sb.AppendFormat("<line-height=82%>\n" + co + "<line-height=0%>\n○</line-height>{0}<pos={1}em>:{2}x{3}", role.GetCombinationCName(), pos, $"{role.GetChance()}%", role.GetCount() + "</line-height>");
                    }
                }
            }
            if (addons != null)
            {
                sb.Append("\n<size=100%>\n").Append(GetString("Addons")).Append("</size>");
                foreach (CustomRoles Addon in addons)
                {
                    var longestNameByteCount = addons.Select(x => x.GetCombinationCName().Length).OrderByDescending(x => x).FirstOrDefault();
                    var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
                    if (Addon.IsEnable()) sb.AppendFormat("<line-height=82%>\n★{0}<pos={1}em>:{2}x{3}", GetRoleName(Addon).Color(GetRoleColor(Addon)), pos, $"{Addon.GetChance()}%", Addon.GetCount() + "</line-height>");
                }
            }
            return sb.ToString();
        }
        public static string GetActiveRoleText(byte pc)
        {
            var sb = new StringBuilder().AppendFormat("<line-height={0}>", ActiveSettingsLineHeight);
            sb.AppendFormat("<size={0}>", "70%");
            sb.AppendFormat("\n◆{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString());
            sb.Append("\n<size=100%>\n").Append(GetString("Roles")).Append("</size>");
            CustomRoles[] roles = null;
            CustomRoles[] addons = null;
            if (Options.CurrentGameMode == CustomGameMode.Standard) roles = CustomRolesHelper.AllStandardRoles;
            if (Options.CurrentGameMode == CustomGameMode.Standard) addons = CustomRolesHelper.AllAddOns;
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) roles = CustomRolesHelper.AllHASRoles;
            if (roles != null)
            {
                var roleType = CustomRoleTypes.Impostor;
                var farst = true;
                var (imp, mad, crew, neu, addon, lover, gorst) = GetRoleTypesCountInt();
                foreach (CustomRoles role in roles)
                {
                    //Roles
                    if (role.IsEnable())
                    {
                        if (farst && role.IsImpostor())
                        {
                            var maxtext = $"({imp})";
                            var (che, max, min) = RoleAssignManager.CheckRoleTypeCount(role.GetCustomRoleTypes());
                            if (che)
                            {
                                maxtext += $"　[Min : {min}|Max : {max} ]";
                            }
                            sb.Append(ColorString(Palette.ImpostorRed, "\n\n<u>☆Impostors☆" + maxtext + "</u>\n"));
                        }
                        farst = false;
                        if (role.GetCustomRoleTypes() != roleType && role.GetCustomRoleTypes() != CustomRoleTypes.Impostor)
                        {
                            var s = "";
                            var c = 0;
                            var cor = Color.white;
                            switch (role.GetCustomRoleTypes())
                            {
                                case CustomRoleTypes.Crewmate: s = "☆CrewMates☆"; c = crew; cor = Palette.Blue; break;
                                case CustomRoleTypes.Madmate: s = "☆MadMates☆"; c = mad; cor = StringHelper.CodeColor("#ff7f50"); break;
                                case CustomRoleTypes.Neutral: s = "☆Neutrals☆"; c = neu; cor = Palette.DisabledGrey; break;
                            }
                            var maxtext = $"({c})";
                            var (che, max, min) = RoleAssignManager.CheckRoleTypeCount(role.GetCustomRoleTypes());
                            if (che)
                            {
                                maxtext += $"　[Min : {min}|Max : {max} ]";
                            }
                            sb.Append(ColorString(cor, $"\n\n<u>{s + maxtext}</u>\n"));
                            roleType = role.GetCustomRoleTypes();
                        }
                        var longestNameByteCount = roles.Select(x => x.GetCombinationCName().Length).OrderByDescending(x => x).FirstOrDefault();
                        var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
                        var co = role.IsImpostor() ? ColorString(Palette.ImpostorRed, "Ⓘ") : (role.IsCrewmate() ? ColorString(Palette.Blue, "Ⓒ") : (role.IsMadmate() ? "<color=#ff7f50>Ⓜ</color>" : ColorString(Palette.DisabledGrey, "Ⓝ")));
                        sb.AppendFormat("<line-height=82%>\n<b>" + co + "</b></line-height>{0}<pos={1}em>:{2}x{3}", role.GetCombinationCName(), pos, $"{role.GetChance()}%", role.GetCount() + "</line-height>");
                        P(sb);
                    }
                }
            }
            if (addons != null && addons?.Length != 0)
            {
                if (addons.Any(add => add.IsEnable()))
                {
                    sb.Append("\n<size=100%>\n").Append(GetString("Addons")).Append("</size>");
                    foreach (CustomRoles Addon in addons)
                    {
                        var m = AdditionalWinnerMark;
                        if (Addon.IsRiaju()) m = ColorString(GetRoleColor(CustomRoles.Lovers), "♥");
                        if (Addon.IsDebuffAddon()) m = ColorString(Palette.DisabledGrey, "☆");
                        if (Addon.IsGhostRole()) m = "<color=#8989d9>■</color>";
                        var longestNameByteCount = addons.Select(x => x.GetCombinationCName().Length).OrderByDescending(x => x).FirstOrDefault();
                        var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
                        if (Addon.IsEnable()) sb.AppendFormat("<line-height=82%>\n" + m + "{0}<pos={1}em>:{2}x{3}", GetRoleName(Addon).Color(GetRoleColor(Addon)), pos, $"{Addon.GetChance()}%", Addon.GetCount() + "</line-height>");
                        if (Addon.IsEnable()) P(sb);
                    }
                }
            }
            return sb.ToString();

            void P(StringBuilder sb)
            {
                var n = sb.ToString().Split("\n").Length;
                if (n >= 60)
                {
                    if (sb.ToString().RemoveHtmlTags() != "")
                    {
                        n = 0;
                        SendMessage(sb.ToString(), pc);
                        sb.Clear();
                        sb.Append("<size=70%><line-height=55%>");
                    }
                }
            }
        }
        public static void ShowSetting(byte PlayerId = byte.MaxValue)
        {
            var sb = new StringBuilder();
            if (RoleAssignManager.OptionAssignMode.GetBool())
            {
                sb.Append(GetString("AssignMode") + "<line-height=1.5pic><size=70%>\n");
                ShowChildrenSettings(RoleAssignManager.OptionAssignMode, ref sb);
                sb.Append('\n');
            }
            sb.Append("</line-height></size>" + GetString("Settings") + "\n<line-height=60%><size=70%>");
            sb.Append($"Mod:<color={Main.ModColor}>" + $"{Main.ModName} v.{Main.PluginShowVersion} {(Main.DebugVersion ? $"☆{GetString("Debug")}☆" : "")}</color>\n");
            sb.Append($"Map:{Constants.MapNames[Main.NormalOptions.MapId]}\n");
            sb.Append($"{GetString(StringNames.GameNumImpostors)}:{Main.NormalOptions.NumImpostors.ToString()}\n");
            sb.Append($"{GetString(StringNames.GameNumMeetings)}:{Main.NormalOptions.NumEmergencyMeetings.ToString()}\n");
            sb.Append($"{GetString(StringNames.GameEmergencyCooldown)}:{Main.NormalOptions.EmergencyCooldown.ToString()}s\n");
            if (!GameStates.IsLobby) sb.Append($"{GetString(StringNames.GameDiscussTime)}:{Main.Time.Item1.ToString()}s\n");
            if (Main.Time.Item1 != Main.NormalOptions.DiscussionTime || GameStates.IsLobby) sb.Append($"{GetString("NowTime")}{GetString(StringNames.GameDiscussTime)}:{Main.NormalOptions.DiscussionTime.ToString()}s\n");
            if (!GameStates.IsLobby) sb.Append($"{GetString(StringNames.GameVotingTime)}:{Main.Time.Item2.ToString()}s\n");
            if (Main.Time.Item2 != Main.NormalOptions.VotingTime || GameStates.IsLobby) sb.Append($"{GetString("NowTime")}{GetString(StringNames.GameVotingTime)}:{Main.NormalOptions.VotingTime.ToString()}s\n");
            sb.Append($"{GetString(StringNames.GamePlayerSpeed)}:{Main.NormalOptions.PlayerSpeedMod.ToString()}x\n");
            sb.Append($"{GetString(StringNames.GameCrewLight)}:{Main.NormalOptions.CrewLightMod.ToString()}x\n");
            sb.Append($"{GetString(StringNames.GameImpostorLight)}:{Main.NormalOptions.ImpostorLightMod.ToString()}x\n");
            sb.Append($"{GetString(StringNames.GameKillCooldown)}:{Main.NormalOptions.KillCooldown.ToString()}s\n");
            sb.Append($"{GetString(StringNames.GameCommonTasks)}:{Main.NormalOptions.NumCommonTasks.ToString()}\n");
            sb.Append($"{GetString(StringNames.GameLongTasks)}:{Main.NormalOptions.NumLongTasks.ToString()}\n");
            sb.Append($"{GetString(StringNames.GameShortTasks)}:{Main.NormalOptions.NumShortTasks.ToString()}\n");

            SendMessage(sb.ToString(), PlayerId);
        }

        public static void ShowChildrenSettings(OptionItem option, ref StringBuilder sb, int deep = 0, bool Askesu = false, PlayerControl pc = null, bool getbool = false)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (!opt.Value.GetBool())
                {
                    switch (opt.Value.Name)
                    {
                        case "GiveGuesser": continue;
                        case "GiveWatching": continue;
                        case "GiveManagement": continue;
                        case "Giveseeing": continue;
                        case "GiveAutopsy": continue;
                        case "GiveTiebreaker": continue;
                        case "GiveMagicHand": continue;
                        case "GivePlusVote": continue;
                        case "GiveRevenger": continue;
                        case "GiveOpener": continue;
                        case "GiveAntiTeleporter": continue;
                        case "GiveLighting": continue;
                        case "GiveMoon": continue;
                        case "GiveElector": continue;
                        case "GiveInfoPoor": continue;
                        case "GiveNonReport": continue;
                        case "GiveTransparent": continue;
                        case "GiveNotvoter": continue;
                        case "GiveWater": continue;
                        case "GiveSpeeding": continue;
                        case "GiveGuarding": continue;
                        case "GiveClumsy": continue;
                        case "GiveSlacker": continue;
                        default: if (getbool) continue; break;
                    }
                }
                if (!Options.IsActiveSkeld)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisableSkeldDevices": continue;
                        case "SkeldReactorTimeLimit": continue;
                        case "SkeldO2TimeLimit": continue;
                    }
                }
                if (!Options.IsActiveMiraHQ)
                {
                    switch (opt.Value.Name)
                    {
                        case "MiraReactorTimeLimit": continue;
                        case "MiraO2TimeLimit": continue;
                        case "DisableMiraHQDevices": continue;
                    }
                }
                if (!Options.IsActivePolus)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisablePolusDevices": continue;
                        case "PolusReactorTimeLimit": continue;
                    }
                }
                if (!Options.IsActiveAirship)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisableAirshipDevices": continue;
                        case "AirshipReactorTimeLimit": continue;
                        case "AirShipVariableElectrical": continue;
                        case "DisableAirshipMovingPlatform": continue;
                        case "DisableAirshipViewingDeckLightsPanel": continue;
                        case "DisableAirshipCargoLightsPanel": continue;
                        case "DisableAirshipGapRoomLightsPanel": continue;
                    }
                }
                if (!Options.IsActiveFungle)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisableFungleDevices": continue;
                        case "FungleReactorTimeLimit": continue;
                        case "FungleMushroomMixupDuration": continue;
                        case "DisableFungleSporeTrigger": continue;
                        case "CantUseZipLineTotop": continue;
                        case "CantUseZipLineTodown": continue;
                    }
                }
                if (opt.Value.Name is "Maximum" or "FixedRole") continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveFungle || Options.IsActiveAirship || Options.IsActivePolus)) continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveSkeld || Options.IsActiveMiraHQ || Options.IsActiveAirship || Options.IsActivePolus)) continue;
                if (Askesu && opt.Value.Name == "%roleTypes%Maximum") continue;

                if (pc != null)
                {
                    if (!pc.Is(CustomRoleTypes.Crewmate))
                    {
                        if (opt.Value == Guesser.Crewmateset) continue;
                        if (opt.Value == Guesser.CCanGuessVanilla) continue;
                        if (opt.Value == Guesser.CCanGuessNakama) continue;
                        if (opt.Value == Guesser.CCanWhiteCrew) continue;
                    }
                    if (!pc.Is(CustomRoleTypes.Impostor))
                    {
                        if (opt.Value == Guesser.impset) continue;
                        if (opt.Value == Guesser.ICanGuessVanilla) continue;
                        if (opt.Value == Guesser.ICanGuessNakama) continue;
                        if (opt.Value == Guesser.ICanGuessTaskDoneSnitch) continue;
                        if (opt.Value == Guesser.ICanWhiteCrew) continue;
                    }
                    if (!pc.Is(CustomRoleTypes.Madmate))
                    {
                        if (opt.Value == Guesser.Madset) continue;
                        if (opt.Value == Guesser.MCanGuessVanilla) continue;
                        if (opt.Value == Guesser.MCanGuessNakama) continue;
                        if (opt.Value == Guesser.MCanGuessTaskDoneSnitch) continue;
                        if (opt.Value == Guesser.MCanWhiteCrew) continue;
                    }
                    if (!pc.Is(CustomRoleTypes.Neutral))
                    {
                        if (opt.Value == Guesser.Neuset) continue;
                        if (opt.Value == Guesser.NCanGuessVanilla) continue;
                        if (opt.Value == Guesser.NCanGuessTaskDoneSnitch) continue;
                        if (opt.Value == Guesser.NCanWhiteCrew) continue;
                    }
                }

                if (deep > 0)
                {
                    sb.Append(string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0))));
                    sb.Append(opt.Index == option.Children.Count ? "┗ " : "┣ ");
                }
                sb.Append($"{opt.Value.GetName(true)}: {opt.Value.GetString().RemoveSN()}\n");
                if (opt.Value.GetBool()) ShowChildrenSettings(opt.Value, ref sb, deep + 1);
            }
        }
        public static string ShowAddonSet(OptionItem option, int deep = 0)
        {
            var sb = "";
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "FixedRole") continue;
                if (opt.Value.Name == "%roleTypes%Maximum") continue;

                if (deep > 0)
                {
                    sb += string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0)));
                    sb += opt.Index == option.Children.Count ? "┗ " : "┣ ";
                }
                sb += $"{opt.Value.GetName(true).RemoveHtmlTags()}: {opt.Value.GetString().RemoveSN()}\n";
                if (opt.Value.GetBool()) ShowAddonSet(opt.Value, deep + 1);
            }
            return sb;
        }
        public static void SendRoleInfo(PlayerControl player)
        {
            var roleclas = player.GetRoleClass();
            var role = player.GetCustomRole();
            if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
            if (roleclas?.Jikaku() != CustomRoles.NotAssigned && roleclas != null)
                role = roleclas.Jikaku();

            if (role is CustomRoles.Amnesiac)
            {
                if (roleclas is Amnesiac amnesiac && !amnesiac.omoidasita)
                    role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
            }

            if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            {
                var RoleTextData = GetRoleColorCode(role);
                //var SendRoleInfo = "";
                string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                string RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";
                {
                    SendMessage("<b><line-height=2.0pic><size=150%>" + GetString(role.ToString()).Color(player.GetRoleColor()) + "</b>\n<size=60%><line-height=1.8pic>" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                }
                //addon(一回これで応急手当。)
                GetAddonsHelp(player);
                return;
            }

            if (role.GetRoleInfo()?.Description is { } description)
            {
                var RoleTextData = GetRoleColorCode(role);
                string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                string RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";
                SendMessage(description.FullFormatHelp, player.PlayerId, title: RoleInfoTitle);
                GetAddonsHelp(player);
                return;
            }
            else
            {
                var RoleTextData = GetRoleColorCode(role);
                //var SendRoleInfo = "";
                string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                string RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";
                {
                    SendMessage("<b><line-height=2.0pic><size=150%>" + GetString(role.ToString()).Color(player.GetRoleColor()) + "</b>\n<size=60%><line-height=1.8pic>" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                }
                //addon(一回これで応急手当。)
                GetAddonsHelp(player);
            }
        }
        public static void GetAddonsHelp(PlayerControl player)
        {
            var roleclas = player.GetRoleClass();
            var AddRoleTextData = GetRoleColorCode(player.GetCustomRole());
            if (player.Is(CustomRoles.Amnesia)) AddRoleTextData = player.Is(CustomRoleTypes.Crewmate) ? "#8cffff" : (player.Is(CustomRoleTypes.Neutral) ? "#cccccc" : "#ff1919");
            if (roleclas?.Jikaku() != CustomRoles.NotAssigned && roleclas != null)
                AddRoleTextData = GetRoleColorCode(roleclas.Jikaku());

            var AddRoleInfoTitleString = $"{GetString("AddonInfoTitle")}";
            var AddRoleInfoTitle = $"<color={AddRoleTextData}>{AddRoleInfoTitleString}";
            var s = new StringBuilder();

            var k = "<line-height=2.0pic><size=100%>~~~~~~~~~~~~~~~~~~~~~~~~\n\n<size=150%><b>";
            //バフ
            if (player.Is(CustomRoles.Guesser)) s.Append(k + AddonInfo(CustomRoles.Guesser, "∮", From.TheOtherRoles, player));
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Serial)) s.Append(k + AddonInfo(CustomRoles.Serial, "∂", pc: player));
            if (player.Is(CustomRoles.MagicHand)) s.Append(k + AddonInfo(CustomRoles.MagicHand, "ж", pc: player));
            if ((player.Is(CustomRoles.Connecting) && !player.Is(CustomRoles.WolfBoy)) || (player.Is(CustomRoles.Connecting) && !player.IsAlive()))
                s.Append(k + AddonInfo(CustomRoles.Connecting, "Ψ", pc: player) + "\n");
            if (player.Is(CustomRoles.watching)) s.Append(k + AddonInfo(CustomRoles.watching, "∑", From.TOR_GM_Edition, pc: player) + "\n");
            if (player.Is(CustomRoles.PlusVote)) s.Append(k + AddonInfo(CustomRoles.PlusVote, "р", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Tiebreaker)) s.Append(k + AddonInfo(CustomRoles.Tiebreaker, "т", From.TheOtherRoles, pc: player) + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Autopsy)) s.Append(k + AddonInfo(CustomRoles.Autopsy, "Å", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Revenger)) s.Append(k + AddonInfo(CustomRoles.Revenger, "Я", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Speeding)) s.Append(k + AddonInfo(CustomRoles.Speeding, "∈", pc: player) + "\n");
            if (player.Is(CustomRoles.Guarding)) s.Append(k + AddonInfo(CustomRoles.Guarding, "ζ", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Management)) s.Append(k + AddonInfo(CustomRoles.Management, "θ", From.TownOfHost_Y, pc: player) + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Opener)) s.Append(k + AddonInfo(CustomRoles.Opener, "п") + "\n");
            //if (player.Is(CustomRoles.AntiTeleporter)) s.Append(k + AddonInfo(CustomRoles.AntiTeleporter, "t", From.RevolutionaryHostRoles, pc: player) + "\n");
            if (player.Is(CustomRoles.seeing)) s.Append(k + AddonInfo(CustomRoles.seeing, "☯", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Lighting)) s.Append(k + AddonInfo(CustomRoles.Lighting, "＊", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Moon)) s.Append(k + AddonInfo(CustomRoles.Moon, "э", pc: player) + "\n");

            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            //デバフ
            if (player.Is(CustomRoles.SlowStarter)) s.Append(k + AddonInfo(CustomRoles.SlowStarter, "Ｓs", pc: player) + "\n");
            if (player.Is(CustomRoles.Notvoter)) s.Append(k + AddonInfo(CustomRoles.Notvoter, "Ｖ", pc: player) + "\n");
            if (player.Is(CustomRoles.Elector)) s.Append(k + AddonInfo(CustomRoles.Elector, "Ｅ", pc: player) + "\n");
            if (player.Is(CustomRoles.InfoPoor)) s.Append(k + AddonInfo(CustomRoles.InfoPoor, "Ｉ", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.NonReport)) s.Append(k + AddonInfo(CustomRoles.NonReport, "Ｒ", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Transparent)) s.Append(k + AddonInfo(CustomRoles.Transparent, "Ｔ", pc: player) + "\n");
            if (player.Is(CustomRoles.Water)) s.Append(k + AddonInfo(CustomRoles.Water, "Ｗ", pc: player) + "\n");
            if (player.Is(CustomRoles.Clumsy)) s.Append(k + AddonInfo(CustomRoles.Clumsy, "Ｃ", From.TownOfHost_Y, pc: player) + "\n");
            if (player.Is(CustomRoles.Slacker)) s.Append(k + AddonInfo(CustomRoles.Slacker, "ＳＬ", pc: player) + "\n");

            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            //第三
            var lover = player.GetRiaju();
            if (lover != CustomRoles.NotAssigned) s.Append(k + AddonInfo(lover, "♥", lover is not CustomRoles.Lovers ? From.None : From.Love_Couple_Mod, pc: player) + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            //ラスト系
            if (player.Is(CustomRoles.LastImpostor)) s.Append(k + AddonInfo(CustomRoles.LastImpostor, from: From.TownOfHost, pc: player) + "\n");
            if (player.Is(CustomRoles.LastNeutral)) s.Append(k + AddonInfo(CustomRoles.LastNeutral, pc: player) + "\n");
            if (player.Is(CustomRoles.Workhorse)) s.Append(k + AddonInfo(CustomRoles.Workhorse, from: From.TownOfHost, pc: player) + "\n");
            CheckPageChange(player.PlayerId, s, title: AddRoleInfoTitle);
            if (player.Is(CustomRoles.Twins)) s.Append(k + AddonInfo(CustomRoles.Twins) + "\n");

            if (s.ToString().RemoveHtmlTags() != "" && s.Length != 0)
                SendMessage(s.ToString(), player.PlayerId, AddRoleInfoTitle);

            GetGhostRolesInfo(player);
        }
        public static void GetGhostRolesInfo(PlayerControl player)
        {
            if (player.IsAlive()) return;
            if (player == null) return;
            if (!player.IsGhostRole()) return;

            SendMessage(GetAddonsHelp(PlayerState.GetByPlayerId(player.PlayerId).GhostRole), player.PlayerId, $"<color=#8989d9>{GetString("GhostRolesIntoTitle")}</color>");
        }
        public static string GetAddonsHelp(CustomRoles role)
        {
            if (!(role.IsAddOn() || role.IsGhostRole() || role.IsRiaju() || role is CustomRoles.Amanojaku || role is CustomRoles.Twins)) return "";
            var s = "";
            var k = "<line-height=2.0pic><size=150%>";

            return (s += k) + (role switch
            {
                CustomRoles.Twins => AddonInfo(role, ""),
                //バフ
                CustomRoles.Guesser => AddonInfo(role, "∮", From.TheOtherRoles),
                CustomRoles.Serial => AddonInfo(role, "∂"),
                CustomRoles.MagicHand => AddonInfo(role, "ж"),
                CustomRoles.Connecting => AddonInfo(role, "Ψ"),
                CustomRoles.watching => AddonInfo(role, "∑", From.TOR_GM_Edition),
                CustomRoles.PlusVote => AddonInfo(role, "р", From.TownOfHost_Y),
                CustomRoles.Tiebreaker => AddonInfo(role, "т", From.TheOtherRoles),
                CustomRoles.Autopsy => AddonInfo(role, "Å", From.TownOfHost_Y),
                CustomRoles.Revenger => AddonInfo(role, "Я", From.TownOfHost_Y),
                CustomRoles.Speeding => AddonInfo(role, "∈"),
                CustomRoles.Guarding => AddonInfo(role, "ζ", From.TownOfHost_Y),
                CustomRoles.Management => AddonInfo(role, "θ", From.TownOfHost_Y),
                CustomRoles.Opener => AddonInfo(role, "п"),
                //CustomRoles.AntiTeleporter => AddonInfo(role, "t", From.RevolutionaryHostRoles),
                CustomRoles.seeing => AddonInfo(role, "☯", From.TownOfHost_Y),
                CustomRoles.Lighting => AddonInfo(role, "＊", From.TownOfHost_Y),
                CustomRoles.Moon => AddonInfo(role, "э"),
                //デバフ
                CustomRoles.Amnesia => AddonInfo(role),
                CustomRoles.SlowStarter => AddonInfo(role, "Ｓs"),
                CustomRoles.Notvoter => AddonInfo(role, "Ｖ"),
                CustomRoles.Elector => AddonInfo(role, "Ｅ"),
                CustomRoles.InfoPoor => AddonInfo(role, "Ｉ", From.TownOfHost_Y),
                CustomRoles.NonReport => AddonInfo(role, "Ｒ", From.TownOfHost_Y),
                CustomRoles.Transparent => AddonInfo(role, "Ｔ"),
                CustomRoles.Water => AddonInfo(role, "Ｗ"),
                CustomRoles.Clumsy => AddonInfo(role, "Ｃ", From.TownOfHost_Y),
                CustomRoles.Slacker => AddonInfo(role, "ＳＬ"),
                //第三属性
                CustomRoles.Amanojaku => AddonInfo(role),
                CustomRoles.Lovers or CustomRoles.RedLovers or CustomRoles.BlueLovers or CustomRoles.YellowLovers or CustomRoles.GreenLovers
                or CustomRoles.WhiteLovers or CustomRoles.PurpleLovers => AddonInfo(role, "♥", role != CustomRoles.Lovers ? From.None : From.Love_Couple_Mod),
                CustomRoles.OneLove => AddonInfo(role),
                //ラスト系
                CustomRoles.LastImpostor => AddonInfo(role, from: From.TownOfHost),
                CustomRoles.LastNeutral => AddonInfo(role),
                CustomRoles.Workhorse => AddonInfo(role, from: From.TownOfHost),
                //幽霊役職
                CustomRoles.Ghostbuttoner => AddonInfo(role),
                CustomRoles.GhostNoiseSender => AddonInfo(role),
                CustomRoles.GhostReseter => AddonInfo(role),
                CustomRoles.GhostRumour => AddonInfo(role),
                CustomRoles.GuardianAngel => AddonInfo(role),
                CustomRoles.DemonicTracker => AddonInfo(role),
                CustomRoles.DemonicCrusher => AddonInfo(role),
                CustomRoles.DemonicVenter => AddonInfo(role),
                CustomRoles.AsistingAngel => AddonInfo(role),

                _ => $"{role.GetRoleInfo()?.ConfigId ?? -334}...?"
            });
        }
        public static string AddonInfo(CustomRoles role, string Mark = "", From from = From.None, PlayerControl pc = null)
        {
            var m = "\n<size=90%><line-height=1.8pic>";
            var f = $"\n{UtilsOption.GetFrom(from)}\n";
            var set = new StringBuilder();
            var s = "";
            if (Mark != "") Mark = $" {Mark}";
            if (Options.CustomRoleSpawnChances.TryGetValue(role, out var op)) ShowChildrenSettings(op, ref set, Askesu: true, pc: pc);
            if (set.ToString().RemoveHtmlTags() != "") s = $"\n\n<size=90%>{GetString("Settings")}\n<size=60%>{set}";
            if (UtilsOption.GetFrom(from).RemoveHtmlTags() == "") f = "";

            var info = ColorString(GetRoleColor(role), GetString($"{role}") + Mark + m + GetString($"{role}Info"));
            if (Mark == "") info = $"<b>{info}</b>";

            return info + $"\n</b></color><size=60%>{f}<line-height=1.3pic>" + GetString($"{role}InfoLong") + s;
        }
        public static string GetRoleTypesCount(bool shouryaku = true)
        {
            if (Options.CurrentGameMode != CustomGameMode.Standard) return "";
            var text = "";
            var (i, m, c, n, a, l, g) = (0, 0, 0, 0, 0, 0, 0);
            foreach (var r in RoleAssignManager.GetCandidateRoleList(10, true).OrderBy(x => Guid.NewGuid()))
            {
                if (r.IsImpostor()) i++;
                if (r.IsMadmate()) m++;
                if (r.IsCrewmate()) c++;
                if (r.IsNeutral()) n++;
            }
            List<CustomRoles> loverch = new();
            foreach (var subRole in CustomRolesHelper.AllAddOns)
            {
                var chance = subRole.GetChance();
                var count = subRole.GetCount();
                if (chance == 0) continue;
                if (subRole.IsAddOn() || subRole is CustomRoles.Amanojaku or CustomRoles.Twins) a += count;
                if (subRole.IsRiaju() && !loverch.Contains(subRole)) l++;
                if (subRole.IsGhostRole()) g += count;
                if (!loverch.Contains(subRole)) loverch.Add(subRole);
            }
            if (shouryaku)
            {
                if (i != 0) text += $"<color=#ff1919>I:{i}  </color>";
                if (m != 0) text += $"<color=#ff7f50>M:{m}  </color>";
                if (c != 0) text += $"<color=#8cffff>C:{c}  </color>";
                if (n != 0) text += $"<color=#cccccc>N:{n}  </color>";
                if (a != 0) text += $"<color=#028760>A:{a}  </color>";
                if (l != 0) text += $"<color=#ff6be4>L:{l}  </color>";
                if (g != 0) text += $"<color=#8989d9>G:{g}  </color>";
            }
            else
            {
                if (i != 0) text += $"<color=#ff1919>Imp:{i}   </color>";
                if (m != 0) text += $"<color=#ff7f50>Mad:{m}   </color>";
                if (c != 0) text += $"<color=#8cffff>Crew:{c}   </color>";
                if (n != 0) text += $"<color=#cccccc>Neu:{n}   </color>";
                if (a != 0) text += $"<color=#028760>Add:{a}   </color>";
                if (l != 0) text += $"<color=#ff6be4>Love:{l}   </color>";
                if (g != 0) text += $"<color=#8989d9>Gost:{g}   </color>";
            }
            return text;
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="shouryaku"></param>
        /// <returns>(imp , mad , crew , neutral , addon , lovers , ghost)</returns>
        public static (int imp, int mad, int crew, int neutral, int addon, int lovers, int ghost) GetRoleTypesCountInt()
        {
            if (Options.CurrentGameMode != CustomGameMode.Standard) return (0, 0, 0, 0, 0, 0, 0);
            var (i, m, c, n, a, l, g) = (0, 0, 0, 0, 0, 0, 0);
            foreach (var r in RoleAssignManager.GetCandidateRoleList(10, true).OrderBy(x => Guid.NewGuid()))
            {
                if (r.IsImpostor()) i++;
                if (r.IsMadmate()) m++;
                if (r.IsCrewmate()) c++;
                if (r.IsNeutral()) n++;
            }
            List<CustomRoles> loverch = new();
            foreach (var subRole in CustomRolesHelper.AllAddOns)
            {
                var chance = subRole.GetChance();
                var count = subRole.GetCount();
                if (chance == 0) continue;
                if (subRole.IsAddOn() || subRole is CustomRoles.Amanojaku or CustomRoles.Twins) a += count;
                if (subRole.IsRiaju() && !loverch.Contains(subRole)) l++;
                if (subRole.IsGhostRole()) g += count;
                if (!loverch.Contains(subRole)) loverch.Add(subRole);
            }
            return (i, m, c, n, a, l, g);
        }
        private const string ActiveSettingsSize = "50%";
        private const string ActiveSettingsLineHeight = "55%";
    }
    #endregion
    #region  Option
    public static class UtilsOption
    {
        public static string GetFrom(SimpleRoleInfo info) => GetFrom(info.From, info.RoleName);
        public static string GetFrom(From from, CustomRoles role = CustomRoles.NotAssigned)
        {
            string Fromtext = "<color=#000000>From:</color>";
            switch (from)
            {
                case From.None: Fromtext = ""; break;
                case From.AmongUs: Fromtext += "<color=#ff1919>Among Us</color>"; break;
                case From.TheOtherRoles: Fromtext += $"<color=#ff0000>TheOtherRoles</color>"; break;
                case From.TOR_GM_Edition: Fromtext += $"<color=#ff0000>TOR GM Edition</color>"; break;
                case From.TOR_GM_Haoming_Edition: Fromtext += $"<color=#ff0000>TOR GM Haoming</color>"; break;
                case From.SuperNewRoles: Fromtext += "<color=#ffa500>Super</color><color=#ff0000>New</color><color=#00ff00>Roles</color>"; break;
                case From.ExtremeRoles: Fromtext += $"<color=#d3d3d3>{from}</color>"; break;
                case From.NebulaontheShip: Fromtext += $"<color=#191970>{from}</color>"; break;
                case From.au_libhalt_net: Fromtext += $"<color=#ffc0cb>au libhalt net</color>"; break;
                case From.FoolersMod: Fromtext += $"<color=#006400>{from}</color>"; break;
                case From.SheriffMod: Fromtext += $"<color=#f8cd46>{from}</color>"; break;
                case From.Jester: Fromtext += $"<color=#ec62a5>{from}</color>"; break;
                case From.TownOfUs: Fromtext += $"<color=#daa520>{from}</color>"; break;
                case From.TownOfHost: Fromtext += $"<color=#00bfff>{from}</color>"; break;
                case From.TownOfHost_Y: Fromtext += $"<color=#dddd00>TownOfHost_Y</color>"; break;
                case From.TownOfHost_for_E: Fromtext += $"<color=#18e744>TownOfHost for E</color>"; break;
                case From.Speyrp: Fromtext = $"<color=#7fffbf>From:Yoran★</color>"; break;
                case From.TownOfHost_E: Fromtext += $"<color=#ffc0cb>TownOfHost E</color>"; break;
                case From.RevolutionaryHostRoles: Fromtext += $"<color=#3cb371>RevolutionaryHostRoles</color>"; break;
                case From.Love_Couple_Mod: Fromtext += "<color=#ff6be4>Love Couple Mod</color>"; break;
            }
            if (role is CustomRoles.MadSuicide) Fromtext += "  <color=#000000>(<color=#ff1919>崇拝者</color>)</color>";
            return Fromtext;
        }
        public static void SetVision(this IGameOptions opt, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    opt.GetFloat(FloatOptionNames.ImpostorLightMod));
                if (IsActive(SystemTypes.Electrical))
                {
                    opt.SetFloat(
                    FloatOptionNames.CrewLightMod,
                    opt.GetFloat(FloatOptionNames.CrewLightMod) * AURoleOptions.ElectricalCrewVision);
                }
                return;
            }
            else
            {
                opt.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    opt.GetFloat(FloatOptionNames.CrewLightMod));
                if (IsActive(SystemTypes.Electrical))
                {
                    opt.SetFloat(
                    FloatOptionNames.ImpostorLightMod,
                    opt.GetFloat(FloatOptionNames.ImpostorLightMod) / AURoleOptions.ElectricalCrewVision);
                }
                return;
            }
        }
        public static void MarkEveryoneDirtySettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
        }
        public static void SyncAllSettings()
        {
            PlayerGameOptionsSender.SetDirtyToAll();
            GameOptionsSender.SendAllGameOptions();
        }
    }
    #endregion
}