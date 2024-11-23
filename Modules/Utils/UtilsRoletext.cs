using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using static TownOfHost.Translator;
using static TownOfHost.Utils;
using static TownOfHost.PlayerCatch;
using static TownOfHost.UtilsTask;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.Impostor;

namespace TownOfHost
{
    public static class UtilsRoleText
    {

        /// <summary>
        /// seerが自分であるときのseenのRoleName + ProgressText
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>RoleName + ProgressTextを表示するか、構築する色とテキスト(bool, Color, string)</returns>
        public static (bool enabled, string text) GetRoleNameAndProgressTextData(PlayerControl seer, PlayerControl seen = null, bool Mane = true)
        {
            var roleName = GetDisplayRoleName(seer, seen);
            var progressText = GetProgressText(seer, seen, Mane);
            var text = roleName + (roleName != "" ? " " : "") + progressText;
            return (text != "", text);
        }
        /// <summary>
        /// GetDisplayRoleNameDataからRoleNameを構築
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>構築されたRoleName</returns>
        public static string GetDisplayRoleName(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            var seerrole = seer.GetCustomRole();
            //デフォルト値
            bool enabled = seer == seen
                        || seen.Is(CustomRoles.GM)
                        || (Main.VisibleTasksCount && !seer.Is(CustomRoles.AsistingAngel) && !seer.IsAlive() && (!seer.IsGorstRole() || Options.GRCanSeeOtherRoles.GetBool()) && Options.GhostCanSeeOtherRoles.GetBool())
                        || (Lovers.LoversRole.GetBool() && seer.Is(CustomRoles.Lovers) && seen.Is(CustomRoles.Lovers))
                        || (Lovers.RedLoversRole.GetBool() && seer.Is(CustomRoles.RedLovers) && seen.Is(CustomRoles.RedLovers))
                        || (Lovers.YellowLoversRole.GetBool() && seer.Is(CustomRoles.YellowLovers) && seen.Is(CustomRoles.YellowLovers))
                        || (Lovers.BlueLoversRole.GetBool() && seer.Is(CustomRoles.BlueLovers) && seen.Is(CustomRoles.BlueLovers))
                        || (Lovers.GreenLoversRole.GetBool() && seer.Is(CustomRoles.GreenLovers) && seen.Is(CustomRoles.GreenLovers))
                        || (Lovers.WhiteLoversRole.GetBool() && seer.Is(CustomRoles.WhiteLovers) && seen.Is(CustomRoles.WhiteLovers))
                        || (Lovers.PurpleLoversRole.GetBool() && seer.Is(CustomRoles.PurpleLovers) && seen.Is(CustomRoles.PurpleLovers))
                        || (Options.InsiderMode.GetBool() && seerrole.IsImpostor())
                        || (Options.RoleImpostor.GetBool() && seerrole.IsImpostor() && seen.GetCustomRole().IsImpostor());

            //属性が見れるか
            bool addon = seer == seen
                        || seen.Is(CustomRoles.GM)
                        || (Main.VisibleTasksCount && !seer.Is(CustomRoles.AsistingAngel) && !seer.IsAlive() && (!seer.IsGorstRole() || Options.GRCanSeeOtherRoles.GetBool()) && Options.GhostCanSeeOtherRoles.GetBool())
                        || (Options.InsiderMode.GetBool() && seerrole.IsImpostor());

            var (roleColor, roleText) = GetTrueRoleNameData(seen.PlayerId, addon);
            var text = roleText;
            var ch = addon;

            //seen側による変更
            if (Amnesia.CheckAbility(seen))
                seen.GetRoleClass()?.OverrideDisplayRoleNameAsSeen(seer, ref enabled, ref roleColor, ref roleText, ref addon);
            if (text == roleText && !ch)//アドオンの上書きチェック
                (roleColor, roleText) = GetTrueRoleNameData(seen.PlayerId, addon);

            //seer側による変更
            if (Amnesia.CheckAbility(seer))
                seer.GetRoleClass()?.OverrideDisplayRoleNameAsSeer(seen, ref enabled, ref roleColor, ref roleText, ref addon);
            if (text == roleText && !ch)//アドオンの上書きチェック
                (roleColor, roleText) = GetTrueRoleNameData(seen.PlayerId, addon);

            return enabled ? ColorString(roleColor, roleText) : "";
        }
        /// <summary>
        /// 引数の指定通りのRoleNameを表示
        /// </summary>
        /// <param name="mainRole">表示する役職</param>
        /// <param name="subRolesList">表示する属性のList</param>
        /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
        public static (Color color, string text) GetRoleNameData(CustomRoles mainRole, List<CustomRoles> subRolesList, CustomRoles GhostRole, bool showSubRoleMarks = true)
        {
            string roleText = "";
            Color roleColor = Color.white;

            if (mainRole < CustomRoles.NotAssigned)
            {
                roleText = GetRoleName(mainRole);
                roleColor = GetRoleColor(mainRole);
            }

            if (subRolesList != null)
            {
                foreach (var subRole in subRolesList)
                {
                    if (subRole <= CustomRoles.NotAssigned) continue;
                    switch (subRole)
                    {
                        case CustomRoles.LastImpostor:
                            roleText = GetRoleString("Last-") + roleText;
                            break;
                        case CustomRoles.LastNeutral:
                            roleText = GetRoleString("Last-") + roleText;
                            break;
                        case CustomRoles.Amanojaku:
                            roleText = GetRoleString("Amanojaku") + roleText;
                            roleColor = GetRoleColor(CustomRoles.Amanojaku);
                            break;
                        case CustomRoles.OneLove:
                            if (showSubRoleMarks) roleText = ColorString(GetRoleColor(CustomRoles.OneLove), GetString("OneLove")) + $" {roleText}";
                            break;
                    }
                }
            }

            if (GhostRole != CustomRoles.NotAssigned) roleText = $"<size=60%>({roleText})</size><size=80%>{ColorString(GetRoleColor(GhostRole), GetString($"{GhostRole}"))}</size>";

            string subRoleMarks = showSubRoleMarks ? GetSubRoleMarks(subRolesList, mainRole) : "";
            if (roleText != "" && subRoleMarks != "")
                subRoleMarks = " " + subRoleMarks; //空じゃなければ空白を追加

            return (roleColor, roleText + subRoleMarks);
        }
        public static string GetSubRoleMarks(List<CustomRoles> subRolesList, CustomRoles main)
        {
            var sb = new StringBuilder(100);
            if (subRolesList != null)
            {
                foreach (var subRole in subRolesList.Distinct())
                {
                    if (subRole <= CustomRoles.NotAssigned) continue;
                    switch (subRole)
                    {
                        case CustomRoles.Guesser: sb.Append(Guesser.SubRoleMark); break;
                        case CustomRoles.Serial: sb.Append(Serial.SubRoleMark); break;
                        case CustomRoles.Connecting: if (main != CustomRoles.WolfBoy) sb.Append(Connecting.SubRoleMark); break;
                        case CustomRoles.watching: sb.Append(watching.SubRoleMark); break;
                        case CustomRoles.PlusVote: sb.Append(PlusVote.SubRoleMark); break;
                        case CustomRoles.Tiebreaker: sb.Append(Tiebreaker.SubRoleMark); break;
                        case CustomRoles.MagicHand: sb.Append(MagicHand.SubRoleMark); break;
                        case CustomRoles.Autopsy: sb.Append(Autopsy.SubRoleMark); break;
                        case CustomRoles.Revenger: sb.Append(Revenger.SubRoleMark); break;
                        case CustomRoles.Speeding: sb.Append(Speeding.SubRoleMark); break;
                        case CustomRoles.Guarding: sb.Append(Guarding.SubRoleMark); break;
                        case CustomRoles.Management: sb.Append(Management.SubRoleMark); break;
                        case CustomRoles.seeing: sb.Append(seeing.SubRoleMark); break;
                        case CustomRoles.Opener: sb.Append(Opener.SubRoleMark); break;
                        case CustomRoles.Lighting: sb.Append(Lighting.SubRoleMark); break;
                        case CustomRoles.Moon: sb.Append(Moon.SubRoleMark); break;
                        //デバフ
                        case CustomRoles.SlowStarter: sb.Append(SlowStarter.SubRoleMark); break;
                        case CustomRoles.NonReport: sb.Append(NonReport.SubRoleMark); break;
                        case CustomRoles.Transparent: sb.Append(Transparent.SubRoleMark); break;
                        case CustomRoles.Notvoter: sb.Append(Notvoter.SubRoleMark); break;
                        case CustomRoles.Water: sb.Append(Water.SubRoleMark); break;
                        case CustomRoles.Slacker: sb.Append(Slacker.SubRoleMark); break;
                        case CustomRoles.Clumsy: sb.Append(Clumsy.SubRoleMark); break;
                        case CustomRoles.Elector: sb.Append(Elector.SubRoleMark); break;
                        case CustomRoles.InfoPoor: sb.Append(InfoPoor.SubRoleMark); break;
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 対象のRoleNameを全て正確に表示
        /// </summary>
        /// <param name="playerId">見られる側のPlayerId</param>
        /// <returns>RoleNameを構築する色とテキスト(Color, string)</returns>
        private static (Color color, string text) GetTrueRoleNameData(byte playerId, bool showSubRoleMarks = true)
        {
            var state = PlayerState.GetByPlayerId(playerId);
            var player = GetPlayerById(playerId);
            if (state == null)
            {
                var (c, t) = (Color.white, "Error(´・ω・｀)");
                return (c, t);
            }
            var Subrole = new List<CustomRoles>(state.SubRoles);
            if (Subrole == null) Subrole.Add(CustomRoles.NotAssigned);
            if (state.MainRole != CustomRoles.NotAssigned && state != null && player != null)
                if (RoleAddAddons.GetRoleAddon(state.MainRole, out var data, player))
                {
                    if (data != null)
                        if (data.GiveAddons.GetBool())
                        {
                            if (data.GiveGuesser.GetBool()) Subrole.Add(CustomRoles.Guesser);
                            if (data.GiveWatching.GetBool()) Subrole.Add(CustomRoles.watching);
                            if (data.GivePlusVote.GetBool()) Subrole.Add(CustomRoles.PlusVote);
                            if (data.GiveTiebreaker.GetBool()) Subrole.Add(CustomRoles.Tiebreaker);
                            if (data.GiveAutopsy.GetBool()) Subrole.Add(CustomRoles.Autopsy);
                            if (data.GiveRevenger.GetBool()) Subrole.Add(CustomRoles.Revenger);
                            if (data.GiveSpeeding.GetBool()) Subrole.Add(CustomRoles.Speeding);
                            if (data.GiveGuarding.GetBool()) Subrole.Add(CustomRoles.Guarding);
                            if (data.GiveManagement.GetBool()) Subrole.Add(CustomRoles.Management);
                            if (data.Giveseeing.GetBool()) Subrole.Add(CustomRoles.seeing);
                            if (data.GiveOpener.GetBool()) Subrole.Add(CustomRoles.Opener);
                            if (!data.IsImpostor)
                            {
                                if (data.GiveLighting.GetBool()) Subrole.Add(CustomRoles.Lighting);
                                if (data.GiveMoon.GetBool()) Subrole.Add(CustomRoles.Moon);
                            }
                            if (data.GiveNotvoter.GetBool()) Subrole.Add(CustomRoles.Notvoter);
                            if (data.GiveElector.GetBool()) Subrole.Add(CustomRoles.Elector);
                            if (data.GiveInfoPoor.GetBool()) Subrole.Add(CustomRoles.InfoPoor);
                            if (data.GiveNonReport.GetBool()) Subrole.Add(CustomRoles.NonReport);
                            if (data.GiveTransparent.GetBool()) Subrole.Add(CustomRoles.Transparent);
                            if (data.GiveWater.GetBool()) Subrole.Add(CustomRoles.Water);
                            if (data.GiveClumsy.GetBool()) Subrole.Add(CustomRoles.Clumsy);
                            if (data.GiveSlacker.GetBool()) Subrole.Add(CustomRoles.Slacker);
                        }
                    if (state.SubRoles.Any(x => x is CustomRoles.LastImpostor))
                    {
                        if (LastImpostor.GiveAutopsy.GetBool()) Subrole.Add(CustomRoles.Autopsy);
                        if (LastImpostor.giveguesser) Subrole.Add(CustomRoles.Guesser);
                        if (LastImpostor.GiveManagement.GetBool()) Subrole.Add(CustomRoles.Management);
                        if (LastImpostor.Giveseeing.GetBool()) Subrole.Add(CustomRoles.seeing);
                        if (LastImpostor.GiveTiebreaker.GetBool()) Subrole.Add(CustomRoles.Tiebreaker);
                        if (LastImpostor.GiveWatching.GetBool()) Subrole.Add(CustomRoles.watching);
                    }
                    if (state.SubRoles.Any(x => x is CustomRoles.LastNeutral))
                    {
                        if (LastNeutral.GiveAutopsy.GetBool()) Subrole.Add(CustomRoles.Autopsy);
                        if (LastNeutral.GiveGuesser.GetBool()) Subrole.Add(CustomRoles.Guesser);
                        if (LastNeutral.GiveManagement.GetBool()) Subrole.Add(CustomRoles.Management);
                        if (LastNeutral.Giveseeing.GetBool()) Subrole.Add(CustomRoles.seeing);
                        if (LastNeutral.GiveTiebreaker.GetBool()) Subrole.Add(CustomRoles.Tiebreaker);
                        if (LastNeutral.GiveWatching.GetBool()) Subrole.Add(CustomRoles.watching);
                    }
                }
            var (color, text) = GetRoleNameData(state.MainRole, Subrole, state.GhostRole, showSubRoleMarks);

            if (Amnesia.CheckAbility(player))
                CustomRoleManager.GetByPlayerId(playerId)?.OverrideTrueRoleName(ref color, ref text);

            if (player != null)
            {
                var roleClass = player.GetRoleClass();
                if (player.Is(CustomRoles.Amnesia))
                {
                    var c = CustomRoles.Crewmate;
                    var roletype = state.MainRole.GetCustomRoleTypes();
                    if (roletype is CustomRoleTypes.Impostor) c = CustomRoles.Impostor;
                    (color, text) = GetRoleNameData(c, Subrole, state.GhostRole, showSubRoleMarks);
                    if (roletype is CustomRoleTypes.Neutral or CustomRoleTypes.Madmate)
                    {
                        text = GetString("Neutral");
                        color = Palette.DisabledGrey;
                    }
                }
                if (roleClass?.Jikaku() is not null and not CustomRoles.NotAssigned)
                {
                    (color, text) = GetRoleNameData(roleClass.Jikaku(), Subrole, state.GhostRole, showSubRoleMarks);
                }
                if (state.MainRole is CustomRoles.Amnesiac)
                {
                    if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                        (color, text) = GetRoleNameData(Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff, Subrole, state.GhostRole, showSubRoleMarks);
                }
            }
            return (color, text);
        }
        /// <summary>
        /// 対象のRoleNameを全て正確に表示
        /// </summary>
        /// <param name="playerId">見られる側のPlayerId</param>
        /// <returns>構築したRoleName</returns>
        public static string GetTrueRoleName(byte playerId, bool showSubRoleMarks = true)
        {
            var (color, text) = GetTrueRoleNameData(playerId, showSubRoleMarks);
            return ColorString(color, text);
        }
        public static string GetRoleName(CustomRoles role)
        {
            return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
        }
        public static Color GetRoleColor(CustomRoles role, bool MadmateOrange = false)
        {
            if (role.IsMadmate() && MadmateOrange) return ModColors.MadMateOrenge;
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = role.GetRoleInfo()?.RoleColorCode ?? "#cccccc";
            if (role is CustomRoles.Amnesiac && Amnesiac.iamwolf) hexColor = CustomRoles.WolfBoy.GetRoleInfo()?.RoleColorCode ?? "#727171";
            _ = ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = role.GetRoleInfo()?.RoleColorCode;
            if (role is CustomRoles.Amnesiac && Amnesiac.iamwolf) hexColor = CustomRoles.WolfBoy.GetRoleInfo()?.RoleColorCode ?? "#727171";
            return hexColor;
        }
        ///<summary>
        /// コンビネーション役職だと専用の名前を返す<br>
        /// それ以外は通常と同じ名前になる</br>
        ///</summary>
        public static string GetCombinationCName(this CustomRoles role, bool color = true)
        {
            var roleinfo = role.GetRoleInfo();
            if (roleinfo is null || roleinfo?.Combination is null)
            {
                if (color)
                    return GetRoleName(role).Color(GetRoleColor(role));
                else
                    return GetRoleName(role);
            }

            if (color)
                return roleinfo?.Combination == CombinationRoles.None ? GetRoleName(role).Color(GetRoleColor(role)) : GetString(roleinfo.Combination.ToString()).Color(GetRoleColor(role));
            return roleinfo?.Combination == CombinationRoles.None ? GetRoleName(role) : GetString(roleinfo.Combination.ToString());
        }
        public static string GetProgressText(byte playerId, bool comms = false, bool Mane = true, bool gamelog = false, bool hide = false)
        {
            var ProgressText = new StringBuilder();
            var State = PlayerState.GetByPlayerId(playerId);
            var player = GetPlayerById(playerId);
            if (State == null || player == null) return "";
            var role = State.MainRole;
            var roleClass = CustomRoleManager.GetByPlayerId(playerId);
            ProgressText.Append(GetTaskProgressText(playerId, comms, Mane, hide));
            if (!hide && roleClass != null && !player.Is(CustomRoles.Amnesia) && (roleClass?.Jikaku() is CustomRoles.NotAssigned or CustomRoles.FortuneTeller))
            {
                ProgressText.Append(roleClass.GetProgressText(comms, gamelog));
            }
            if (player.CanMakeMadmate()) ProgressText.Append(ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"[{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]"));
            return ProgressText.ToString();
        }
        public static string GetProgressText(PlayerControl seer, PlayerControl seen = null, bool Mane = true)
        {
            seen ??= seer;
            var comms = IsActive(SystemTypes.Comms);
            var seerisAlive = seer.IsAlive();
            bool enabled = seer == seen
                        || (Main.VisibleTasksCount && !seer.Is(CustomRoles.AsistingAngel) && !seerisAlive && (!seer.IsGorstRole() || Options.GRCanSeeOtherTasks.GetBool()) && Options.GhostCanSeeOtherTasks.GetBool());
            string text = GetProgressText(seen.PlayerId, comms, Mane, hide: seer != seen && seen.Is(CustomRoles.Fox));

            if (Options.GhostCanSeeNumberOfButtonsOnOthers.GetBool() && !seerisAlive && !seer.Is(CustomRoles.AsistingAngel) && (!seer.IsGorstRole() || Options.GRCanSeeNumberOfButtonsOnOthers.GetBool())) text += $"[{PlayerState.GetByPlayerId(seen.PlayerId).NumberOfRemainingButtons}]";

            //seer側による変更
            if (Amnesia.CheckAbility(seer))
                seer.GetRoleClass()?.OverrideProgressTextAsSeer(seen, ref enabled, ref text);

            var r = enabled ? text : "";
            if (SuddenDeathMode.NowSuddenDeathMode && seer == seen)
            {
                r += SuddenDeathMode.SuddenDeathProgersstext(seer);
            }
            return r;
        }
        public static string GetTaskProgressText(byte playerId, bool comms = false, bool Mane = true, bool hide = false)
        {
            var pc = GetPlayerById(playerId);
            if (pc == null) return "";
            var cr = pc.GetCustomRole();

            if (cr is CustomRoles.GM)
                if (Options.GhostCanSeeAllTasks.GetBool())
                    return AllTaskstext(false, false, false, comms, true);

            var roleClass = pc.GetRoleClass();
            var info = GetPlayerInfoById(playerId);
            var state = PlayerState.GetByPlayerId(playerId);
            var isalive = pc.IsAlive();
            if (state == null) return "";

            //隠すなら??表示
            if (hide) return $"<color=#5c5c5c>(??/??)</color>";
            Color TextColor = Color.yellow;
            var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(state.MainRole).ShadeColor(0.5f); //タスク完了後の色
            var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色

            if (pc.Is(CustomRoles.Amnesia))
            {
                CustomRoles role = (CustomRoles)cr.GetRoleTypes();
                if (role.IsImpostor() && cr.IsImpostor())
                {
                    TaskCompleteColor = GetRoleColor(state.MainRole).ShadeColor(0.5f);
                    NonCompleteColor = Color.white;
                }
                else
                {
                    TaskCompleteColor = Color.green;
                    NonCompleteColor = Color.yellow;
                }
            }

            if (roleClass?.Jikaku() is not null and not CustomRoles.NotAssigned)
            {
                var role = roleClass.Jikaku();

                if (role.IsCrewmate() && !pc.IsRiaju() && isalive)
                {
                    TaskCompleteColor = Color.green;
                    NonCompleteColor = Color.yellow;
                }
                else
                {
                    TaskCompleteColor = GetRoleColor(state.MainRole).ShadeColor(0.5f);
                    NonCompleteColor = Color.white;
                }
            }

            if (Workhorse.IsThisRole(playerId))
                NonCompleteColor = Workhorse.RoleColor;

            var NormalColor = state.taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
            if (state.taskState.GetNeedCountOrAll() <= state.taskState.CompletedTasksCount) NormalColor = TaskCompleteColor;

            TextColor = comms ? Color.gray : NormalColor;
            string Completed = comms ? "?" : $"{state.taskState.CompletedTasksCount}";
            var isjingai = state == null || state.taskState == null || !state.taskState.hasTasks;
            if (Mane && !pc.Is(CustomRoles.Amnesia) && !(roleClass?.Jikaku() is CustomRoles.NotAssigned or null))
            {
                if (!isalive && Options.GhostCanSeeAllTasks.GetBool() && !pc.Is(CustomRoles.AsistingAngel) && (!pc.IsGorstRole() || Options.GRCanSeeAllTasks.GetBool()))//死んでて霊界がタスク進捗を見れるがON
                {
                    if (isjingai) return AllTaskstext(false, false, false, comms, true);
                    else return ColorString(TextColor, $"({Completed}/{state.taskState.GetNeedCountOrAll()})") + AllTaskstext(false, false, false, comms, true);
                }
                //ラスポスの処理
                if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveManagement.GetBool()) return AllTaskstext(LastImpostor.PercentGage.GetBool(), LastImpostor.PonkotuPercernt.GetBool(), LastImpostor.Meeting.GetBool(), comms, LastImpostor.comms.GetBool());
                //ラスニュの処理
                else if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveManagement.GetBool()) return AllTaskstext(LastNeutral.PercentGage.GetBool(), LastNeutral.PonkotuPercernt.GetBool(), LastNeutral.Meeting.GetBool(), comms, LastNeutral.comms.GetBool());
                else//書く役職の処理
                if (RoleAddAddons.GetRoleAddon(cr, out var data, pc) && data.GiveAddons.GetBool() && data.GiveManagement.GetBool())
                {
                    if (isjingai) return AllTaskstext(data.PercentGage.GetBool(), data.PonkotuPercernt.GetBool(), data.Meeting.GetBool(), comms, data.comms.GetBool());
                    else return ColorString(TextColor, $"({Completed}/{state.taskState.GetNeedCountOrAll()})") + AllTaskstext(data.PercentGage.GetBool(), data.PonkotuPercernt.GetBool(), data.Meeting.GetBool(), comms, data.comms.GetBool());
                }
                //マネジメントの処理
                else if (pc.Is(CustomRoles.Management))
                {
                    if (isjingai) return AllTaskstext(Management.PercentGage, Management.PonkotuPercernt.GetBool(), Management.Meeting.GetBool(), comms, Management.comms);
                    else return ColorString(TextColor, $"({Completed}/{state.taskState.GetNeedCountOrAll()})") + AllTaskstext(Management.PercentGage, Management.PonkotuPercernt.GetBool(), Management.Meeting.GetBool(), comms, Management.comms);
                }
                if (isjingai) return "";
                else return ColorString(TextColor, $"({Completed}/{state.taskState.GetNeedCountOrAll()})");
            }
            if (isjingai) return "";
            else return ColorString(TextColor, $"({Completed}/{state.taskState.GetNeedCountOrAll()})");
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                case RoleTypes.Shapeshifter:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                default:
                    switch (hRole)
                    {
                        case CustomRoles.Crewmate:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case CustomRoles.HASFox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.HASTroll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }
        public static string GetSubRolesText(byte id, bool disableColor = false, bool amkesu = false, bool mark = false)
        {
            var state = PlayerState.GetByPlayerId(id);
            if (state == null) return "";
            var SubRoles = state.SubRoles;

            var player = GetPlayerById(id);
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role is CustomRoles.NotAssigned or
                            CustomRoles.LastImpostor or
                            CustomRoles.LastNeutral or
                            CustomRoles.Amanojaku
                ) continue;
                if (role is CustomRoles.Amnesia && amkesu) continue;
                if (role.IsAddOn() && mark) continue;
                if (role.IsRiaju() && mark) continue;

                var RoleText = disableColor ? GetRoleName(role) : ColorString(GetRoleColor(role), GetRoleName(role));
                sb.Append($"{ColorString(Color.gray, " + ")}{RoleText}");
            }
            if (mark)
            {
                var lover = player?.GetRiaju();
                if (lover?.IsRiaju() ?? false)
                {
                    if (lover != CustomRoles.OneLove)
                    {
                        sb.Append($"{ColorString(Color.gray, " + ")}");
                        sb.Append(ColorString(GetRoleColor((CustomRoles)lover), "♥"));
                    }
                }
                var Subrole = new List<CustomRoles>(state.SubRoles);
                if (Subrole == null) Subrole.Add(CustomRoles.NotAssigned);
                if (state.MainRole != CustomRoles.NotAssigned && state != null && player != null)
                    if (RoleAddAddons.GetRoleAddon(state.MainRole, out var data, player))
                    {
                        if (data != null)
                            if (data.GiveAddons.GetBool())
                            {
                                if (data.GiveGuesser.GetBool()) Subrole.Add(CustomRoles.Guesser);
                                if (data.GiveWatching.GetBool()) Subrole.Add(CustomRoles.watching);
                                if (data.GivePlusVote.GetBool()) Subrole.Add(CustomRoles.PlusVote);
                                if (data.GiveTiebreaker.GetBool()) Subrole.Add(CustomRoles.Tiebreaker);
                                if (data.GiveAutopsy.GetBool()) Subrole.Add(CustomRoles.Autopsy);
                                if (data.GiveRevenger.GetBool()) Subrole.Add(CustomRoles.Revenger);
                                if (data.GiveSpeeding.GetBool()) Subrole.Add(CustomRoles.Speeding);
                                if (data.GiveGuarding.GetBool()) Subrole.Add(CustomRoles.Guarding);
                                if (data.GiveManagement.GetBool()) Subrole.Add(CustomRoles.Management);
                                if (data.Giveseeing.GetBool()) Subrole.Add(CustomRoles.seeing);
                                if (data.GiveOpener.GetBool()) Subrole.Add(CustomRoles.Opener);
                                if (!data.IsImpostor)
                                {
                                    if (data.GiveLighting.GetBool()) Subrole.Add(CustomRoles.Lighting);
                                    if (data.GiveMoon.GetBool()) Subrole.Add(CustomRoles.Moon);
                                }
                                if (data.GiveNotvoter.GetBool()) Subrole.Add(CustomRoles.Notvoter);
                                if (data.GiveElector.GetBool()) Subrole.Add(CustomRoles.Elector);
                                if (data.GiveInfoPoor.GetBool()) Subrole.Add(CustomRoles.InfoPoor);
                                if (data.GiveNonReport.GetBool()) Subrole.Add(CustomRoles.NonReport);
                                if (data.GiveTransparent.GetBool()) Subrole.Add(CustomRoles.Transparent);
                                if (data.GiveWater.GetBool()) Subrole.Add(CustomRoles.Water);
                                if (data.GiveClumsy.GetBool()) Subrole.Add(CustomRoles.Clumsy);
                                if (data.GiveSlacker.GetBool()) Subrole.Add(CustomRoles.Slacker);
                            }
                        if (state.SubRoles.Any(x => x is CustomRoles.LastImpostor))
                        {
                            if (LastImpostor.GiveAutopsy.GetBool()) Subrole.Add(CustomRoles.Autopsy);
                            if (LastImpostor.giveguesser) Subrole.Add(CustomRoles.Guesser);
                            if (LastImpostor.GiveManagement.GetBool()) Subrole.Add(CustomRoles.Management);
                            if (LastImpostor.Giveseeing.GetBool()) Subrole.Add(CustomRoles.seeing);
                            if (LastImpostor.GiveTiebreaker.GetBool()) Subrole.Add(CustomRoles.Tiebreaker);
                            if (LastImpostor.GiveWatching.GetBool()) Subrole.Add(CustomRoles.watching);
                        }
                        if (state.SubRoles.Any(x => x is CustomRoles.LastNeutral))
                        {
                            if (LastNeutral.GiveAutopsy.GetBool()) Subrole.Add(CustomRoles.Autopsy);
                            if (LastNeutral.GiveGuesser.GetBool()) Subrole.Add(CustomRoles.Guesser);
                            if (LastNeutral.GiveManagement.GetBool()) Subrole.Add(CustomRoles.Management);
                            if (LastNeutral.Giveseeing.GetBool()) Subrole.Add(CustomRoles.seeing);
                            if (LastNeutral.GiveTiebreaker.GetBool()) Subrole.Add(CustomRoles.Tiebreaker);
                            if (LastNeutral.GiveWatching.GetBool()) Subrole.Add(CustomRoles.watching);
                        }

                    }
                var marks = GetSubRoleMarks(Subrole, PlayerState.GetByPlayerId(id).MainRole);
                sb.Append(marks.RemoveHtmlTags() == "" ? "" : $"{ColorString(Color.gray, " + ")}{marks}");
            }

            return sb.ToString();
        }
        public static string GetRoleColorAndtext(CustomRoles role) => ColorString(GetRoleColor(role), GetString($"{role}"));

        public static string GetExpelledText(byte expelledid, bool istie, bool isskip)
        {
            if (istie) return GetString("votetie");
            if (isskip) return GetString("voteskip");

            if (expelledid == byte.MaxValue) return GetString("voteskip");

            var playername = GetPlayerColor(expelledid, true);
            var role = PlayerState.GetByPlayerId(expelledid)?.MainRole ?? CustomRoles.NotAssigned;

            if (Options.ShowVoteResult.GetBool() && role is not CustomRoles.NotAssigned)
            {
                switch (Options.ShowVoteJudgments[Options.ShowVoteJudgment.GetValue()])
                {
                    case "Impostor":
                        return string.Format(GetString("fortuihourole"), playername, GetRoleColorAndtext(CustomRoles.Impostor),
                            role.IsImpostor() ? GetString("fortuihouisrole") : GetString("fortuihouisnotrole"));
                    case "Neutral":
                        return string.Format(GetString("fortuihourole"), playername, $"<color=#cccccc>{GetString("Neutral")}</color>",
                            role.IsNeutral() ? GetString("fortuihouisrole") : GetString("fortuihouisnotrole"));
                    case "CrewMate(Mad)":
                        return string.Format(GetString("fortuihourole"), playername, GetRoleColorAndtext(CustomRoles.Crewmate),
                            role.IsCrewmate() || role.IsMadmate() ? GetString("fortuihouisrole") : GetString("fortuihouisnotrole"));
                    case "Crewmate":
                        return string.Format(GetString("fortuihourole"), playername, GetRoleColorAndtext(CustomRoles.Crewmate),
                            role.IsCrewmate() ? GetString("fortuihouisrole") : GetString("fortuihouisnotrole"));
                    case "Role":
                        return string.Format(GetString("fortuihourole"), playername, GetRoleColorAndtext(role), GetString("fortuihouisrole"));
                }
            }

            return string.Format(GetString("fortuihou"), playername);
        }
    }
}