using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using static TownOfHost.Utils;
using static TownOfHost.UtilsRoleText;
using static TownOfHost.PlayerCatch;
using TownOfHost.Modules;

namespace TownOfHost
{
    #region  OutputLog
    public static class UtilsOutputLog
    {
        public static DirectoryInfo GetLogFolder(bool auto = false)
        {
            var folder = Directory.CreateDirectory($"{Application.persistentDataPath}/TownOfHost_K/Logs");
            if (auto)
            {
                folder = Directory.CreateDirectory($"{folder.FullName}/AutoLogs");
            }
            return folder;
        }
        public static void DumpLog()
        {
            var logs = GetLogFolder();
            var filename = CopyLog(logs.FullName);
            OpenDirectory(filename);
            if (PlayerControl.LocalPlayer != null)
                SendMessage(GetString("Message.LogsSavedInLogsFolder"));
        }
        public static void SaveNowLog()
        {
            var logs = GetLogFolder(true);
            // 3日以上前のログを削除 /* 元は7だけど、7も保存しててもなので...*/
            logs.EnumerateFiles().Where(f => f.CreationTime < DateTime.Now.AddDays(-3)).ToList().ForEach(f => f.Delete());
            CopyLog(logs.FullName);
        }
        public static string CopyLog(string path)
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string subver = CredentialsPatch.Subver.RemoveHtmlTags();
            if (subver != "") subver = $"({subver})";
            string fileName = $"{path}/TownOfHost_K-v{Main.PluginVersion}{subver}-{t}.log";
            FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            var logFile = file.CopyTo(fileName);
            return logFile.FullName;
        }
        public static void OpenLogFolder()
        {
            var logs = GetLogFolder(true);
            OpenDirectory(logs.FullName);
        }
        public static void OpenDirectory(string path)
        {
            Process.Start("Explorer.exe", $"/select,{path}");
        }
    }
    #endregion
    #region  GameLog
    public static class UtilsGameLog
    {
        public static int day;
        public static string gamelog;
        public static Dictionary<byte, string> LastLog = new();
        public static Dictionary<byte, string> LastLogRole = new();
        public static Dictionary<byte, string> LastLogPro = new();
        public static Dictionary<byte, string> LastLogSubRole = new();
        public static string GetLogtext(byte pc)
        {
            var longestNameByteCount = Main.AllPlayerNames?.Values?.Select(name => name.GetByteCount())?.OrderByDescending(byteCount => byteCount)?.FirstOrDefault() ?? 10;
            var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
            var pos1 = pos + 4f;
            var pos2 = pos + 4f + (DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8f : 4.5f);

            var name = "('ω')";
            if (LastLog.ContainsKey(pc))
                name = LastLog[pc];
            var pro = "(;;)";
            if (LastLogPro.ContainsKey(pc))
                pro = LastLogPro[pc];
            var role = "_(:3 」∠)_";
            if (LastLogRole.ContainsKey(pc))
                role = LastLogRole[pc];
            var addon = "(´-ω-`)";
            addon = LastLogSubRole.TryGetValue(pc, out var m) ? m : GetSubRolesText(pc, mark: true);

            return name + $"<pos={pos}em>" + pro + $"<pos={pos1}em>" + " : " + GetVitalText(pc, true) + " " + $"<pos={pos2}em>" + role + addon;
        }
        public static string SummaryTexts(byte id)
        {
            var builder = new StringBuilder();
            // 全プレイヤー中最長の名前の長さからプレイヤー名の後の水平位置を計算する
            // 1em ≒ 半角2文字
            // 空白は0.5emとする
            // SJISではアルファベットは1バイト，日本語は基本的に2バイト
            var longestNameByteCount = Main.AllPlayerNames.Values.Select(name => name.GetByteCount()).OrderByDescending(byteCount => byteCount).FirstOrDefault();
            //最大11.5emとする(★+日本語10文字分+半角空白)
            var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f /* ★+末尾の半角空白 */ , 11.5f);
            builder.Append(ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id]));
            builder.AppendFormat("<pos={0}em>", pos).Append(GetProgressText(id, Mane: false, gamelog: true)).Append("</pos>");
            // "(00/00) " = 4em
            pos += 6f;
            builder.AppendFormat("<pos={0}em>", pos).Append(GetVitalText(id, true)).Append("</pos>");
            // "Lover's Suicide " = 8em
            // "回線切断 " = 4.5em
            pos += DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8.5f : 5f;
            builder.AppendFormat("<pos={0}em>", pos);
            var role = GetTrueRoleName(id);
            if (LastLogRole.ContainsKey(id))
                role = LastLogRole[id];
            role = Regex.Replace(role, "<b>", "");
            role = Regex.Replace(role, "</b>", "");
            builder.Append(role);
            builder.Append(LastLogSubRole.TryGetValue(id, out var m) ? m : GetSubRolesText(id, mark: true));
            builder.Append("</pos>");
            return builder.ToString();
        }
        public static string GetLastWinTeamtext()
        {
            string CustomWinnerText = "";
            var AdditionalWinnerText = new StringBuilder(32);
            string CustomWinnerColor = GetRoleColorCode(CustomRoles.Crewmate);
            Main.AssignSameRoles = false;

            var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
            if (winnerRole >= 0)
            {
                CustomWinnerText = GetRoleName(winnerRole);
                CustomWinnerColor = GetRoleColorCode(winnerRole);
            }
            switch (CustomWinnerHolder.WinnerTeam)
            {
                //通常勝利
                case CustomWinner.Crewmate: CustomWinnerColor = GetRoleColorCode(CustomRoles.Crewmate); break;
                case CustomWinner.OneLove: CustomWinnerText = ColorString(GetRoleColor(CustomRoles.OneLove), GetString("OneLoveWin")); break;
                case CustomWinner.TaskPlayerB:
                    if (Main.winnerList.Count is 0) break;
                    if (Main.winnerList.Count == 1)
                        if (Main.RTAMode)
                        {
                            CustomWinnerText = $"タイム: {HudManagerPatch.GetTaskBattleTimer().Replace(" : ", "：")}<size=0>";
                        }
                        else
                            CustomWinnerText = Main.AllPlayerNames[Main.winnerList[0]];
                    else
                    {
                        var n = 0;
                        foreach (var t in Main.TaskBattleTeams)
                        {
                            n++;
                            if (t.Contains(Main.winnerList[0]))
                                break;
                        }
                        CustomWinnerText = string.Format(GetString("Team2"), n);
                    }
                    break;
                //引き分け処理
                case CustomWinner.Draw:
                    CustomWinnerText = GetString("ForceEnd").Color(ModColors.Gray);
                    break;
                //全滅
                case CustomWinner.None:
                    CustomWinnerText = GetString("EveryoneDied").Color(ModColors.Gray);
                    break;
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.None and not CustomWinner.Draw)
                if (SuddenDeathMode.NowSuddenDeathMode)
                {
                    var winner = CustomWinnerHolder.WinnerIds.FirstOrDefault();
                    var color = Color.white;
                    if (Main.PlayerColors.TryGetValue(winner, out var co)) color = co;
                    CustomWinnerText = "Game Over".Color(StringHelper.ColorCode(color));
                }

            foreach (var role in CustomWinnerHolder.AdditionalWinnerRoles)
            {
                AdditionalWinnerText.Append('＆').Append(ColorString(GetRoleColor(role), GetRoleName(role)));
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.OneLove && !SuddenDeathMode.NowSuddenDeathMode)
            {
                CustomWinnerText = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
            }
            return CustomWinnerText;
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastresult"), PlayerId);
                return;
            }
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? GetRoleColor((CustomRoles)CustomWinnerHolder.WinnerTeam);

            sb.Append("""<align="center">""");
            sb.Append("<size=150%>").Append(GetString("LastResult")).Append("</size>");
            sb.Append('\n').Append(SetEverythingUpPatch.LastWinsText.Mark(winnerColor, false));
            sb.Append("</align>");

            sb.Append("<size=65%>\n");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);

            foreach (var pc in cloneRoles) if (GetPlayerById(pc) == null) continue;

            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n★ ".Color(winnerColor)).Append(UtilsGameLog.GetLogtext(id));
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(UtilsGameLog.GetLogtext(id));
            }
            sb.Append("   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            sb.Append(string.Format(GetString("Result.Task"), Main.Alltask));
            SendMessage(sb.ToString(), PlayerId);
        }
        public static void ShowLastWins(byte PlayerId = byte.MaxValue)
        {
            SendMessage("<size=0>", PlayerId, $"{SetEverythingUpPatch.LastWinsText}");
        }
        public static void ShowKillLog(byte PlayerId = byte.MaxValue)
        {
            if (GameStates.IsInGame)
            {
                SendMessage(GetString("CantUse.killlog"), PlayerId);
                return;
            }
            SendMessage(EndGamePatch.KillLog, PlayerId);
        }

        public static void AddGameLog(string Name, string Meg) => gamelog += $"\n{DateTime.Now:HH.mm.ss} [{Name}]　" + Meg;
    }
    #endregion
    #region WebHook
    public static class UtilsWebHook
    {
        public static void WH_ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            StringBuilder sb;
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {

                sb = new StringBuilder("```cs\n").Append(GetString("Roles")).Append(':');
                sb.AppendFormat("\n# {0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString().RemoveHtmlTags());
                CustomRoleTypes? rr = null;//☆インポスターも表示させるため
                foreach (CustomRoles role in CustomRolesHelper.AllRoles)
                {
                    if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
                    if (rr == null && rr != role.GetCustomRoleTypes() && role.IsEnable())
                    {
                        rr = role.GetCustomRoleTypes();
                        if (role.IsSubRole())
                            sb.AppendFormat($"\n☆{GetString("Addons")}");
                        else
                        {
                            sb.AppendFormat($"\n☆{GetString($"{rr}")}");
                        }
                    }
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    var mark = "〇";
                    switch (role.GetCustomRoleTypes())
                    {
                        case CustomRoleTypes.Impostor: mark = "Ⓘ"; break;
                        case CustomRoleTypes.Crewmate: mark = "Ⓒ"; break;
                        case CustomRoleTypes.Madmate: mark = "Ⓜ"; break;
                        case CustomRoleTypes.Neutral: mark = "Ⓝ"; break;
                    }
                    if (role.IsSubRole())
                    {
                        if (role.IsBuffAddon()) mark = "Ⓐ";
                        else if (role.IsDebuffAddon()) mark = "Ⓓ";
                        else if (role.IsRiaju()) mark = "Ⓛ";
                        else if (role.IsGorstRole()) mark = "Ⓖ";
                        else mark = "〇";
                    }
                    if (role.IsEnable()) sb.AppendFormat($"\n {mark}" + "\"{0}\"   {1}×{2}", role.GetCombinationCName(false), $"{role.GetChance()}%", role.GetCount());
                }
            }
            else
            {
                sb = new StringBuilder("``` cs\n").Append(GetString(Options.CurrentGameMode.ToString()));
                sb.Append("\n\n").Append(GetString("TaskPlayerB") + ":");
                foreach (var pc in PlayerCatch.AllPlayerControls)
                    sb.Append("\n  " + pc.name);
            }
            sb.Append("\n```");
            Webhook.Send(sb.ToString());
        }
        public static void WH_ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? Palette.DisabledGrey;
            var tb = Options.CurrentGameMode == CustomGameMode.TaskBattle;
            sb.Append(GetString("LastResult"));
            if (!tb) sb.Append("\n## ").Append(SetEverythingUpPatch.LastWinsText).Append('\n');
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                var sr = GetSubRolesText(id).RemoveColorTags() != "";
                sb.Append("\n**★ ").Append(Main.AllPlayerNames[id] + "**");
                if (!tb)
                {
                    sb.Append("\n┣  ").Append(GetVitalText(id)).Append('　');
                    sb.Append(GetProgressText(id, Mane: false, gamelog: true).RemoveColorTags());
                    sb.Append(sr ? "\n┣  " : "\n┗   ").Append(GetTrueRoleName(id, false).RemoveColorTags());
                    if (sr) sb.Append("\n┗  ").Append(GetSubRolesText(id).RemoveColorTags());
                }
                else
                {
                    sb.Append('\n').Append($"{Main.AllPlayerNames[id]}{GetString("Win")}").Append('\n');
                    sb.Append('　').Append(GetProgressText(id, Mane: false, gamelog: true).RemoveColorTags());
                }
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                var sr = GetSubRolesText(id).RemoveColorTags() != "";
                sb.Append("\n〇 ").Append(Main.AllPlayerNames[id]);
                if (!tb)
                {
                    sb.Append("\n┣  ").Append(GetVitalText(id)).Append('　');
                    sb.Append(GetProgressText(id, Mane: false, gamelog: true).RemoveColorTags());
                    sb.Append(sr ? "\n┣  " : "\n┗   ").Append(GetTrueRoleName(id, false).RemoveColorTags());
                    if (sr) sb.Append("\n┗  ").Append(GetSubRolesText(id).RemoveColorTags());
                }
                else
                    sb.Append('　').Append(GetProgressText(id, gamelog: true).RemoveColorTags());
            }
            Webhook.Send(sb.ToString());
            Webhook.Send($"```\n{EndGamePatch.KillLog.RemoveHtmlTags()}\n```");
        }
    }
}
#endregion