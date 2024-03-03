using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using InnerNet;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();
        private static Dictionary<CustomRoles, string> roleCommands;
        public static Dictionary<int, string> YomiageS = new();
        public static bool Prefix(ChatController __instance)
        {
            __instance.timeSinceLastMessage = 3f;

            // クイックチャットなら横流し
            if (__instance.quickChatField.Visible)
            {
                if (Main.UseYomiage.Value && PlayerControl.LocalPlayer.IsAlive()) Yomiage(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId, __instance.quickChatField.text.text).Wait();
                return true;
            }
            // 入力欄に何も書かれてなければブロック
            if (__instance.freeChatField.textArea.text == "")
            {
                return false;
            }
            var text = __instance.freeChatField.textArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
            //if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) canceled = true;

            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    Utils.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                    break;
                case "/voice":
                case "/vo":
                    canceled = true;
                    var voc = 0;
                    byte vo0id = PlayerControl.LocalPlayer.PlayerId;
                    if ((args.Length < 2 ? "" : args[1]) == "set" && (args.Length < 3 ? "" : args[2]) != "")
                        if (byte.TryParse(args[2], out vo0id))
                            voc += 2;
                    subArgs = args.Length < 2 ? "" : args[voc + 1];
                    string subArgs2 = args.Length < 3 ? "" : args[voc + 2];
                    string subArgs3 = args.Length < 4 ? "" : args[voc + 3];
                    string subArgs4 = args.Length < 5 ? "" : args[voc + 4];
                    if (subArgs != "" && subArgs2 != "" && subArgs3 != "" && subArgs4 != "")
                    {
                        var vopc = Utils.GetPlayerById(vo0id);
                        YomiageS[vopc.Data.DefaultOutfit.ColorId] = $"{subArgs} {subArgs2} {subArgs3} {subArgs4}";
                        if (AmongUsClient.Instance.AmHost) RPC.SyncYomiage();
                        if (vo0id != PlayerControl.LocalPlayer.PlayerId) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{vopc.name}の声設定を変更しました。");
                    }
                    else Utils.SendMessage("使用方法:\n/vo 音質 音量 速度 音程\n/vo set プレイヤーid 音質 音量 速度 音程", PlayerControl.LocalPlayer.PlayerId);
                    break;
                default:
                    Main.isChatCommand = false;
                    if (Main.UseYomiage.Value && PlayerControl.LocalPlayer.IsAlive()) Yomiage(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId, text).Wait();
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                Main.isChatCommand = true;
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;
                    //勝者指定
                    case "/sw":
                        canceled = true;
                        if (!GameStates.IsInGame) break;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                            case "クルーメイト":
                            case "クルー":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Crewmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.HumansByTask);
                                break;
                            case "impostor":
                            case "インポスター":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            case "none":
                            case "全滅":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            case "jackal":
                            case "ジャッカル":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            case "廃村":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "次の中から勝利させたい陣営を選んでね\ncrewmate\nクルー\nクルーメイト\nimpostor\nインポスター\njackal\nジャッカル\nnone\n全滅\n廃村");
                                cancelVal = "/sw ";
                                break;
                        }
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        Utils.ShowLastResult();
                        break;

                    case "/kl":
                    case "/killlog":
                        canceled = true;
                        Utils.ShowKillLog();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        var name = string.Join(" ", args.Skip(1)).Trim();
                        if (string.IsNullOrEmpty(name))
                        {
                            Main.nickName = "";
                            break;
                        }
                        if (name.StartsWith(" ")) break;
                        Main.nickName = name;
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.HideName.text = Main.HideName.Value;
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {

                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "myplayer":
                                    case "mp":
                                    case "m":
                                        Utils.ShowActiveRoles(PlayerControl.LocalPlayer.PlayerId);
                                        break;
                                    default:
                                        Utils.ShowActiveRoles();
                                        break;
                                }
                                break;
                            case "my":
                            case "m":
                                Utils.ShowActiveSettings(PlayerControl.LocalPlayer.PlayerId);
                                break;
                            default:
                                Utils.ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "impostor":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        var suba1 = 0;
                        byte playerh = 255;
                        subArgs = args.Length < 2 + suba1 ? "" : args[1 + suba1];
                        if (subArgs is "m" or "my")
                        {
                            suba1++;
                            playerh = PlayerControl.LocalPlayer.PlayerId;
                            subArgs = args.Length < 2 + suba1 ? "" : args[1 + suba1];
                        }
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 + suba1 ? "" : args[2 + suba1];
                                GetRolesInfo(subArgs, playerh);
                                break;

                            case "a":
                            case "addons":
                                subArgs = args.Length < 3 + suba1 ? "" : args[2 + suba1];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(Utils.GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"), playerh);
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)", playerh);
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 + suba1 ? "" : args[2 + suba1];
                                switch (subArgs)
                                {
                                    case "hideandseek":
                                    case "has":
                                        Utils.SendMessage(GetString("HideAndSeekInfo"), playerh);
                                        break;

                                    case "taskbattle":
                                    case "tbm":
                                        Utils.SendMessage(GetString("TaskBattleInfo"), playerh);
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(GetString("NoGameEndInfo"), playerh);
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(GetString("SyncButtonModeInfo"), playerh);
                                        break;

                                    /*case "InsiderMode":
                                    case "im":
                                        Utils.SendMessage(GetString("InsiderModeInfo"));
                                        break;*/

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(GetString("RandomMapsModeInfo"), playerh);
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm), taskbattle(tbm), InsiderMode(im)", playerh);
                                        break;
                                }
                                break;

                            case "n":
                            case "now":
                                Utils.ShowActiveSettingsHelp(playerh);
                                break;
                            case "k":
                            case "tohk":
                                Utils.ShowHelpK();
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        if (GameStates.IsInGame)
                        {
                            var role = PlayerControl.LocalPlayer.GetCustomRole();
                            HudManager.Instance.Chat.AddChat(
                                PlayerControl.LocalPlayer,
                                role.GetRoleInfo()?.Description?.FullFormatHelp ??
                                // roleInfoがない役職
                                GetString(role.ToString()) + PlayerControl.LocalPlayer.GetRoleInfo(true));

                            subArgs = args.Length < 2 ? "" : args[1];
                            switch (subArgs)
                            {
                                case "a":
                                case "all":
                                case "allplayer":
                                case "ap":
                                    foreach (var p in Main.AllPlayerControls)
                                    {
                                        if (p.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                                        {
                                            var rolea = p.GetCustomRole();
                                            if (rolea.GetRoleInfo()?.Description is { } description)
                                            {
                                                Utils.SendMessage(description.FullFormatHelp, p.PlayerId, removeTags: false);
                                            }
                                            // roleInfoがない役職
                                            else
                                            {
                                                Utils.SendMessage(GetString(rolea.ToString()) + p.GetRoleInfo(true), p.PlayerId);
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{GetString("ForExample")}:\n{args[0]} test");
                        break;

                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && int.TryParse(args[1], out int sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                        canceled = true;
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/exile":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        Utils.GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2), true);
                        break;

                    case "/allplayertp":
                    case "/apt":
                        canceled = true;
                        if (!GameStates.IsLobby) break;
                        foreach (var tp in Main.AllPlayerControls)
                        {
                            Vector2 position = new(0.0f, 0.0f);
                            tp.RpcSnapTo(position);
                        }
                        break;

                    case "/revive":
                    case "/rev":
                        canceled = true;
                        PlayerControl.LocalPlayer.Revive();
                        PlayerControl.LocalPlayer.Data.IsDead = false;
                        break;

                    case "/id":
                        canceled = true;
                        var sendchatid = "";
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            sendchatid = $"{sendchatid}{pc.PlayerId}:{pc.name}\n";
                        }
                        __instance.AddChat(PlayerControl.LocalPlayer, sendchatid);
                        break;

                    case "/forceend":
                    case "/fe":
                        canceled = true;
                        Utils.SendMessage("ホストから強制終了コマンドが入力されました");
                        GameManager.Instance.enabled = false;
                        CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                        break;

                    case "/w":
                        canceled = true;
                        Utils.ShowLastWins();
                        break;

                    case "/timer":
                    case "/tr":
                        canceled = true;
                        if (!GameStates.IsInGame)
                            Utils.ShowTimer();
                        break;

                    case "/debug":
                        canceled = true;
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            subArgs = args.Length < 2 ? "" : args[1];
                            switch (subArgs)
                            {
                                case "noimp":
                                    Main.NormalOptions.NumImpostors = 0;
                                    break;
                                case "setimp":
                                    int d = 0;
                                    subArgs = subArgs.Length < 2 ? "0" : args[2];
                                    if (int.TryParse(subArgs, out d))
                                    {
                                        Logger.Info($"変換に成功-{d}", "setimp");
                                    }
                                    Main.NormalOptions.NumImpostors = d;
                                    break;
                                case "abo":
                                    if (Main.DebugAntiblackout)
                                        Main.DebugAntiblackout = false;
                                    else
                                        Main.DebugAntiblackout = true;
                                    Logger.SendInGame($"AntiBlockOut:{Main.DebugAntiblackout}");
                                    break;
                                case "winset":
                                    byte wid;
                                    subArgs = subArgs.Length < 2 ? "0" : args[2];
                                    if (byte.TryParse(subArgs, out wid))
                                    {
                                        Logger.Info($"変換に成功-{wid}", "winset");
                                    }
                                    CustomWinnerHolder.WinnerIds.Add(wid);
                                    break;
                                case "win":
                                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                    break;
                                case "nc":
                                    Main.nickName = "<size=0>";
                                    break;
                                case "getrole":
                                    StringBuilder sb = new();
                                    foreach (var pc in Main.AllPlayerControls)
                                        sb.Append(pc.PlayerId + ": " + pc.name + " => " + pc.GetCustomRole() + "\n");
                                    Utils.SendMessage(sb.ToString(), PlayerControl.LocalPlayer.PlayerId);
                                    break;
                                case "rr":
                                    var name2 = string.Join(" ", args.Skip(2)).Trim();
                                    if (string.IsNullOrEmpty(name2))
                                    {
                                        Main.nickName = "";
                                        break;
                                    }
                                    if (name2.StartsWith(" ")) break;
                                    name2 = Regex.Replace(name2, @"size=(\d+)", "<size=$1>");
                                    name2 = Regex.Replace(name2, @"pos=(\d+)", "<pos=$1em>");
                                    name2 = Regex.Replace(name2, @"space=(\d+)", "<space=$1em>");
                                    name2 = Regex.Replace(name2, @"line-height=(\d+)", "<line-height=$1%>");
                                    name2 = Regex.Replace(name2, @"space=(\d+)", "<space=$1em>");
                                    name2 = Regex.Replace(name2, @"color=(\w+)", "<color=$1>");

                                    name2 = name2.Replace("\\n", "\n").Replace("しかくうう", "■").Replace("/l-h", "</line-height>");
                                    Main.nickName = name2; //これは何かって..? 気にしちゃﾏｹだ！
                                    break;
                                case "kill":
                                    byte pcid;
                                    byte seerid;
                                    if (byte.TryParse(args[2], out pcid) && byte.TryParse(args[3], out seerid))
                                    {
                                        var pc = Utils.GetPlayerById(pcid);
                                        var seer = Utils.GetPlayerById(seerid);
                                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, seer.GetClientId());
                                        writer.WriteNetObject(pc);
                                        writer.Write((int)ExtendedPlayerControl.SuccessFlags);
                                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                                    }
                                    break;
                            }
                            break;
                        }
                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(cancelVal);
                //__instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static async Task Yomiage(int color, string text = "")
        {
            // HttpClientを作成
            using var httpClient = new HttpClient();
            try
            {
                ClientOptionsManager.CheckOptions();
                string url = $"http://localhost:{ClientOptionsManager.YomiagePort}/";
                if (YomiageS.ContainsKey(color))
                {
                    string[] args = YomiageS[color].Split(' ');
                    string y1 = args[0];
                    string y2 = args[1];
                    string y3 = args[2];
                    string y4 = args[3];
                    HttpResponseMessage response = await httpClient.GetAsync(url + "talk?text=" + text + "&voice=" + y1 + "&volume=" + y2 + "&speed=" + y3 + "&tone=" + y4);
                }
                else
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url + "talk?text=" + text);
                }
            }
            catch (HttpRequestException e)
            {
                // エラーが発生した場合はエラーメッセージを表示
                Logger.Info($"Error: {e.Message}", "yomiage");
                Logger.SendInGame("エラーが発生したため、読み上げが無効になりました");
                Main.UseYomiage.Value = false;
            }
        }

        public static string LastL = "N";
        public static void GetRolesInfo(string role, byte player = 255)
        {

            // 初回のみ処理
            if (roleCommands == null
                || (Main.ChangeSomeLanguage.Value && LastL != $"{TranslationController.Instance.currentLanguage.languageID}")
                || (LastL != "N" && !Main.ChangeSomeLanguage.Value))
            {
#pragma warning disable IDE0028  // Dictionary初期化の簡素化をしない
                roleCommands = new Dictionary<CustomRoles, string>();

                // GM
                roleCommands.Add(CustomRoles.GM, Main.ChangeSomeLanguage.Value ? GetString("gm") : "gm");

                // Impostor役職
                roleCommands.Add((CustomRoles)(-1), $"== {GetString("Impostor")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Impostor);

                // Madmate役職
                roleCommands.Add((CustomRoles)(-2), $"== {GetString("Madmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Madmate);
                roleCommands.Add(CustomRoles.SKMadmate, Main.ChangeSomeLanguage.Value ? GetString("SKMadmate") : "sm");

                // Crewmate役職
                roleCommands.Add((CustomRoles)(-3), $"== {GetString("Crewmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Crewmate);

                // Neutral役職
                roleCommands.Add((CustomRoles)(-4), $"== {GetString("Neutral")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Neutral);

                // 属性
                roleCommands.Add((CustomRoles)(-5), $"== {GetString("Addons")} ==");  // 区切り用
                //roleCommands.Add(CustomRoles.LastNeutral, Main.ChangeSomeLanguage.Value ? GetString("LastNeutral") : "ln");
                roleCommands.Add(CustomRoles.LastImpostor, Main.ChangeSomeLanguage.Value ? GetString("LastImpostor") : "li");
                roleCommands.Add(CustomRoles.Lovers, Main.ChangeSomeLanguage.Value ? GetString("Lovers") : "lo");
                roleCommands.Add(CustomRoles.Watcher, Main.ChangeSomeLanguage.Value ? GetString("Watcher") : "wat");
                roleCommands.Add(CustomRoles.Workhorse, Main.ChangeSomeLanguage.Value ? GetString("Workhorse") : "wh");
                //roleCommands.Add(CustomRoles.Speeding, Main.ChangeSomeLanguage.Value ? GetString("Speeding") : "sd");
                //roleCommands.Add(CustomRoles.Guesser, Main.ChangeSomeLanguage.Value ? GetString("Guesser") : "Gr");
                //roleCommands.Add(CustomRoles.Moon, Main.ChangeSomeLanguage.Value ? GetString("Moon") : "Mo");
                //roleCommands.Add(CustomRoles.NotConvener, Main.ChangeSomeLanguage.Value ? GetString("NotConvener") : "Nc");

                // HAS
                roleCommands.Add((CustomRoles)(-6), $"== {GetString("HideAndSeek")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.HASFox, Main.ChangeSomeLanguage.Value ? GetString("HASFox") : "hfo");
                roleCommands.Add(CustomRoles.HASTroll, Main.ChangeSomeLanguage.Value ? GetString("HASTroll") : "htr");
                LastL = Main.ChangeSomeLanguage.Value ? $"{TranslationController.Instance.currentLanguage.languageID}" : "N";
#pragma warning restore IDE0028
            }

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleCommands)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    var roleInfo = r.Key.GetRoleInfo();
                    if (roleInfo != null && roleInfo.Description != null)
                    {
                        Utils.SendMessage(roleInfo.Description.FullFormatHelp, sendTo: player, removeTags: false);
                    }
                    // RoleInfoがない役職は従来の処理
                    else
                    {
                        Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"), sendTo: player);
                    }
                    return;
                }

                var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
                if ((int)r.Key < 0)
                {
                    msg += rolemsg + "\n" + roleShort + "\n";
                    rolemsg = "";
                }
                else if ((rolemsg.Length + roleText.Length) > 40)
                {
                    msg += rolemsg + "\n";
                    rolemsg = roleText;
                }
                else
                {
                    rolemsg += roleText;
                }
            }
            msg += rolemsg;
            Utils.SendMessage(msg, player);
        }
        public static string FixRoleNameInput(string text)
        {
            return text switch
            {
                "GM" or "gm" or "ゲームマスター" => GetString("GM"),

                //インポスター
                "アンチレポーター" => GetString("AntiReporter"),
                "ボマー" or "爆弾魔" => GetString("Bomber"),
                "バウンティハンター" => GetString("BountyHunter"),
                "ダイスキラー" => GetString("Dicekiller"),
                "イビルギャンブラー" => GetString("Evilgambler"),
                "イビルハッカー" => GetString("EvilHacker"),
                "イビルトラッカー" => GetString("EvilTracker"),
                "花火職人" => GetString("FireWorks"),
                "インサイダー" => GetString("Insider"),
                "メアー" => GetString("Mare"),
                "ネコカボチャ" => GetString("Nekokabocha"),
                "ペンギン" => GetString("Penguin"),
                "パペッティア" => GetString("Puppeteer"),
                "シリアルキラー" => GetString("SerialKiller"),
                "シェイプマスター" => GetString("shapeMaster"),
                "スナイパー" => GetString("Sniper"),
                "ステルス" => GetString("Stealth"),
                "大狼" or "たいろう" or "大老" => GetString("Tairou"),
                "テレポートキラー" => GetString("TeleportKiller"),
                "タイムシーフ" => GetString("TimeThief"),
                "吸血鬼" or "ヴァンパイア" => GetString("Vampire"),
                "ウォーロック" => GetString("Warlock"),
                "魔女" or "ウィッチ" => GetString("Witch"),
                "マフィア" => GetString("Mafia"),
                "ラストインポスター" => GetString("LastImpostor"),
                "シェイプシフター" => GetString("NormalShapeshifter"),

                //マッドメイト
                "マッドメイト" => GetString("Madmate"),
                "マッドガーディアン" => GetString("MadGuardian"),
                "マッドジェスター" => GetString("MadJester"),
                "マッドテラー" => GetString("MadTeller"),
                "マッドスニッチ" => GetString("MadSnitch"),
                "サイドキックマッドメイト" => GetString("SKMadmate"),

                //クルーメイト
                "ベイト" => GetString("Bait"),
                "ディクテーター" => GetString("Dictator"),
                "ドクター" => GetString("Doctor"),
                "ライター" => GetString("Lighter"),
                "メイヤー" => GetString("Mayor"),
                "サボタージュマスター" => GetString("SabotageMaster"),
                "シーア" => GetString("Seer"),
                "シェリフ" => GetString("Sheriff"),
                "スニッチ" => GetString("Snitch"),
                "スピードブースター" => GetString("SpeedBooster"),
                "トラッパー" => GetString("Trapper"),
                "タイムマネージャー" => GetString("TimeManager"),
                "パン屋" => GetString("Bakery"),
                "占い師" => GetString("FortuneTeller"),
                "ぽんこつ占い師" or "ポンコツ占い師" => GetString("PonkotuTeller"),
                "エンジニア" => GetString("NormalEngineer"),
                "科学者" => GetString("NormalScientist"),
                "タスクスター" => GetString("Taskstar"),
                "ベントマスター" => GetString("VentMaster"),
                "トイレファン" => GetString("ToiletFan"),
                "ウルトラスター" => GetString("UltraStar"),

                //第3陣営
                "アーソニスト" => GetString("Arsonist"),
                "シェフ" => GetString("Chef"),
                "カウントキラー" => GetString("Countkiller"),
                "エゴイスト" => GetString("Egoist"),
                "エクスキューショナー" => GetString("Executioner"),
                "死神" => GetString("GrimReaper"),
                "ジャッカル" => GetString("Jackal"),
                "ジャッカルマフィア" => GetString("JackalMafia"),
                "ジェスター" => GetString("Jester"),
                "恋人" or "ラバーズ" => GetString("Lovers"),
                "オポチュニスト" => GetString("Opportunist"),
                "ペスト医師" => GetString("PlagueDoctor"),
                "リモートキラー" => GetString("Remotekiller"),
                "テロリスト" => GetString("Terrorist"),
                "シュレディンガーの猫" or "シュレ猫" => GetString("SchrodingerCat"),
                "Eシュレディンガーの猫" or "Eシュレ猫" => GetString("EgoSchrodingerCat"),
                "Jシュレディンガーの猫" or "Jシュレ猫" => GetString("JSchrodingerCat"),

                _ => text,
            };
        }
        public static bool GetRoleByInputName(string input, out CustomRoles output, bool includeVanilla = false)
        {
            output = new();
            input = Regex.Replace(input, @"[0-9]+", string.Empty);
            input = Regex.Replace(input, @"\s", string.Empty);
            input = Regex.Replace(input, @"[\x01-\x1F,\x7F]", string.Empty);
            input = input.ToLower().Trim().Replace("是", string.Empty);
            if (input == "" || input == string.Empty) return false;
            input = FixRoleNameInput(input).ToLower();
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (!includeVanilla && role.IsVanilla() && role != CustomRoles.GuardianAngel) continue;
                //if (input == GuessManager.ChangeNormal2Vanilla(role))
                {
                    output = role;
                    return true;
                }
            }
            return false;
        }
        private static void ConcatCommands(CustomRoleTypes roleType)
        {
            var roles = CustomRoleManager.AllRolesInfo.Values.Where(role => role.CustomRoleType == roleType);
            foreach (var role in roles)
            {
                if (role.ChatCommand is null) continue;
                roleCommands[role.RoleName] = Main.ChangeSomeLanguage.Value ? GetString($"{role.RoleName}") : role.ChatCommand;
            }
        }
        public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
        {
            if (player != null)
            {
                var tag = !player.Data.IsDead ? "SendChatAlive" : "SendChatDead";
            }

            canceled = false;

            if (!AmongUsClient.Instance.AmHost) return;
            string[] args = text.Split(' ');
            string subArgs = "";
            if (player.PlayerId != 0)
            {
                ChatManager.SendMessage(player, text);
            }
            //if (GuessManager.GuesserMsg(player, text)) { canceled = true; return; }

            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    Utils.ShowLastResult(player.PlayerId);
                    break;

                case "/kl":
                case "/killlog":
                    Utils.ShowKillLog(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            Utils.ShowActiveRoles(player.PlayerId);
                            break;

                        default:
                            Utils.ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/h":
                case "/help":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            Utils.ShowActiveSettingsHelp(player.PlayerId);
                            break;

                        case "r":
                        case "roles":
                            subArgs = args.Length < 3 ? "" : args[2];
                            GetRolesInfo(subArgs, player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    if (GameStates.IsInGame)
                    {
                        var role = player.GetCustomRole();
                        if (role.GetRoleInfo()?.Description is { } description)
                        {
                            Utils.SendMessage(description.FullFormatHelp, player.PlayerId, removeTags: false);
                        }
                        // roleInfoがない役職
                        else
                        {
                            Utils.SendMessage(GetString(role.ToString()) + player.GetRoleInfo(true), player.PlayerId);
                        }
                    }
                    break;

                case "/t":
                case "/template":
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                case "/timer":
                case "/tr":
                    if (!GameStates.IsInGame)
                        Utils.ShowTimer(player.PlayerId);
                    break;

                case "/tp":
                    if (!GameStates.IsLobby || !Options.sotodererukomando.GetBool()) break;
                    subArgs = args[1];
                    switch (subArgs)
                    {
                        case "o":
                            Vector2 position = new(3.0f, 0.0f);
                            player.RpcSnapTo(position);
                            break;
                        case "i":
                            Vector2 position2 = new(0.0f, 0.0f);
                            player.RpcSnapTo(position2);
                            break;

                    }
                    break;

                case "/voice":
                case "/vo":
                    subArgs = args.Length < 2 ? "" : args[1];
                    string subArgs2 = args.Length < 2 ? "" : args[2];
                    string subArgs3 = args.Length < 2 ? "" : args[3];
                    string subArgs4 = args.Length < 2 ? "" : args[4];
                    if (subArgs != "" && subArgs2 != "" && subArgs3 != "" && subArgs4 != "")
                    {
                        YomiageS[player.Data.DefaultOutfit.ColorId] = $"{subArgs} {subArgs2} {subArgs3} {subArgs4}";
                        RPC.SyncYomiage();
                    }
                    else Utils.SendMessage("使用方法:\n/vo 音質 音量 速度 音程", player.PlayerId);
                    break;

                default:
                    if (Main.UseYomiage.Value && player.IsAlive()) Yomiage(player.Data.DefaultOutfit.ColorId, text).Wait();
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
            if (DoBlockChat) return;
            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            var name = player.Data.PlayerName;
            if (clientId == -1)
            {
                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);
            }
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            __instance.timeSinceLastMessage = 0f;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(string chatText)
        {
            switch (chatText)
            {
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    class RpcSendChatPatch
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
            if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
                DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
}