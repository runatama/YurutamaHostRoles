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
using static TownOfHost.Utils;
using static TownOfHost.UtilsGameLog;
using static TownOfHost.UtilsShowOption;
using static TownOfHost.UtilsRoleText;
using System.Text.Json;
using AmongUs.GameOptions;
using TownOfHost.Modules;
using static TownOfHost.PlayerCatch;
using TownOfHost.Roles.Impostor;
using Rewired;

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
            if (__instance.quickChatField.Visible) return true;

            // 入力欄に何も書かれてなければブロック
            if (__instance.freeChatField.textArea.text == "")
            {
                return false;
            }
            if (UrlFinder.TryFindUrl(__instance.freeChatField.textArea.text.ToCharArray(), out int _, out int _))
            {
                __instance.AddChatWarning(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FreeChatLinkWarning));
                __instance.timeSinceLastMessage = 3f;
                __instance.freeChatField.textArea.Clear();
                return false;
            }
            var text = __instance.freeChatField.textArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Logger.Info(text, "SendChat");
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
            if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) canceled = true;

            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    UtilsOutputLog.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") SendMessage(version_text, PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/voice":
                case "/vo":
                    canceled = true;
                    if (!Main.UseYomiage.Value) break;
                    var voc = 0;
                    byte vo0id = PlayerControl.LocalPlayer.PlayerId;
                    if ((args.Length < 2 ? "" : args[1]) == "set" && (args.Length < 3 ? "" : args[2]) != "")
                        if (byte.TryParse(args[2], out vo0id))
                            voc += 2;
                    subArgs = args.Length < 2 ? "" : args[voc + 1];
                    string subArgs2 = args.Length < 3 ? "" : args[voc + 2];
                    string subArgs3 = args.Length < 4 ? "" : args[voc + 3];
                    string subArgs4 = args.Length < 5 ? "" : args[voc + 4];
                    if (subArgs is "get" or "g" && Main.UseYomiage.Value)
                    {
                        StringBuilder sb = new();
                        foreach (var r in GetvoiceListAsync(true).Result)
                            sb.Append($"{r.Key}: {r.Value}\n");
                        SendMessage(sb.ToString(), PlayerControl.LocalPlayer.PlayerId);
                    }
                    else if (subArgs != "" && subArgs2 != "" && subArgs3 != "" && subArgs4 != "")
                    {
                        if (VoiceList is null) GetvoiceListAsync().Wait();
                        if (int.TryParse(subArgs, out int vid) && VoiceList.Count > vid)
                        {
                            var vopc = GetPlayerById(vo0id);
                            YomiageS[vopc.Data.DefaultOutfit.ColorId] = $"{subArgs} {subArgs2} {subArgs3} {subArgs4}";
                            if (AmongUsClient.Instance.AmHost) RPC.SyncYomiage();
                            if (vo0id != PlayerControl.LocalPlayer.PlayerId)
                                SendMessage($"{vopc.name}の声設定を変更しました。", PlayerControl.LocalPlayer.PlayerId);
                        }
                        else
                        {
                            StringBuilder sb = new();
                            foreach (var r in GetvoiceListAsync().Result)
                                sb.Append($"{r.Key}: {r.Value}\n");
                            SendMessage(sb.ToString(), PlayerControl.LocalPlayer.PlayerId);
                        }
                    }
                    else SendMessage("使用方法:\n/vo 音質 音量 速度 音程\n/vo set プレイヤーid 音質 音量 速度 音程\n\n音質の一覧表示:\n /vo get\n /vo g", PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/devban":
                    canceled = true;
                    if (!DebugModeManager.AuthBool(Main.ExplosionKeyAuth, Main.ExplosionKeyInput.Value)) break;
                    var writerban = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DevExplosion, SendOption.None);
                    writerban.Write(Main.ExplosionKeyInput.Value);
                    AmongUsClient.Instance.FinishRpcImmediately(writerban);
                    break;
                default:
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
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
                            case "crew":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
                                foreach (var player in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Crewmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, false);
                                break;
                            case "impostor":
                            case "imp":
                            case "インポスター":
                            case "インポ":
                            case "インポス":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
                                foreach (var player in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;
                            case "none":
                            case "全滅":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;
                            case "jackal":
                            case "ジャッカル":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;
                            case "廃村":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                break;
                            default:
                                if (GetRoleByInputName(subArgs, out var role, true))
                                {
                                    CustomWinnerHolder.WinnerTeam = (CustomWinner)role;
                                    CustomWinnerHolder.WinnerRoles.Add(role);
                                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorByKill, false);
                                    break;
                                }
                                __instance.AddChat(PlayerControl.LocalPlayer, "次の中から勝利させたい陣営を選んでね\ncrewmate\nクルー\nクルーメイト\nimpostor\nインポスター\njackal\nジャッカル\nnone\n全滅\n廃村");
                                cancelVal = "/sw ";
                                break;
                        }
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        ShowLastResult();
                        break;

                    case "/kl":
                    case "/killlog":
                        canceled = true;
                        ShowKillLog();
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
                                        ShowActiveRoles(PlayerControl.LocalPlayer.PlayerId);
                                        break;
                                    default:
                                        ShowActiveRoles();
                                        break;
                                }
                                break;
                            case "set":
                            case "s":
                            case "setting":
                                ShowSetting();
                                break;
                            case "my":
                            case "m":
                                ShowActiveSettings(PlayerControl.LocalPlayer.PlayerId);
                                break;
                            default:
                                ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/dis":
                        canceled = true;
                        if (!GameStates.InGame) break;
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
                                        SendMessage(GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"), playerh);
                                        break;

                                    default:
                                        SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)", playerh);
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
                                        SendMessage(GetString("HideAndSeekInfo"), playerh);
                                        break;

                                    case "タスクバトル":
                                    case "taskbattle":
                                    case "tbm":
                                        SendMessage(GetString("TaskBattleInfo"), playerh);
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        SendMessage(GetString("NoGameEndInfo"), playerh);
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        SendMessage(GetString("SyncButtonModeInfo"), playerh);
                                        break;

                                    case "インサイダーモード":
                                    case "insiderMode":
                                    case "im":
                                        SendMessage(GetString("InsiderModeInfo"));
                                        break;

                                    case "ランダムマップモード":
                                    case "randommapsmode":
                                    case "rmm":
                                        SendMessage(GetString("RandomMapsModeInfo"), playerh);
                                        break;
                                    case "サドンデスモード":
                                    case "SuddenDeath":
                                    case "Sd":
                                        SendMessage(GetString("SuddenDeathInfo"), playerh);
                                        break;
                                    default:
                                        SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm), taskbattle(tbm), InsiderMode(im),SuddenDeath(sd)", playerh);
                                        break;
                                }
                                break;

                            case "n":
                            case "now":
                                ShowActiveSettingsHelp(playerh);
                                break;

                            default:
                                ShowHelp(PlayerControl.LocalPlayer.PlayerId);
                                break;
                        }
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        if (GameStates.IsInGame)
                        {
                            var role = PlayerControl.LocalPlayer.GetCustomRole();
                            var roleClass = PlayerControl.LocalPlayer.GetRoleClass();
                            if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
                                role = PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                            if (roleClass?.Jikaku() != CustomRoles.NotAssigned && roleClass != null) role = roleClass.Jikaku();

                            if (role is CustomRoles.Amnesiac)
                            {
                                if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                                    role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
                            }
                            var hRoleTextData = GetRoleColorCode(role);
                            string hRoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                            string hRoleInfoTitle = $"<color={hRoleTextData}>{hRoleInfoTitleString}";
                            if (role is CustomRoles.Crewmate or CustomRoles.Impostor)//バーニラならこっちで
                            {
                                SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(PlayerControl.LocalPlayer.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{PlayerControl.LocalPlayer.GetRoleInfo(true)}", PlayerControl.LocalPlayer.PlayerId, hRoleInfoTitle);
                            }
                            else
                                SendMessage(role.GetRoleInfo()?.Description?.FullFormatHelp ?? $"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(PlayerControl.LocalPlayer.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{PlayerControl.LocalPlayer.GetRoleInfo(true)}", PlayerControl.LocalPlayer.PlayerId, hRoleInfoTitle);
                            GetAddonsHelp(PlayerControl.LocalPlayer);

                            subArgs = args.Length < 2 ? "" : args[1];
                            switch (subArgs)
                            {
                                case "a":
                                case "all":
                                case "allplayer":
                                case "ap":
                                    foreach (var player in PlayerCatch.AllPlayerControls.Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                                    {
                                        role = player.GetCustomRole();
                                        roleClass = player.GetRoleClass();
                                        if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                                        if (roleClass?.Jikaku() != CustomRoles.NotAssigned && roleClass != null) role = roleClass.Jikaku();

                                        if (role is CustomRoles.Amnesiac)
                                        {
                                            if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                                                role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
                                        }

                                        var RoleTextData = GetRoleColorCode(role);
                                        string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                                        string RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";

                                        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                                        {
                                            SendMessage("<b><line-height=2.0pic><size=150%>" + GetString(role.ToString()).Color(player.GetRoleColor()) + "\n</b><size=90%><line-height=1.8pic>" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                                        }
                                        else
                                        if (role.GetRoleInfo()?.Description is { } description)
                                        {

                                            SendMessage(description.FullFormatHelp, player.PlayerId, RoleInfoTitle);
                                        }
                                        // roleInfoがない役職
                                        else
                                        {
                                            SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(player.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{player.GetRoleInfo(true)}", player.PlayerId, RoleInfoTitle);
                                        }
                                        GetAddonsHelp(player);

                                        if (player.IsGorstRole())
                                            SendMessage(GetAddonsHelp(PlayerState.GetByPlayerId(player.PlayerId).GhostRole), player.PlayerId);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;

                    case "/impstorchat":
                    case "/impct":
                    case "/ic":
                        canceled = true;
                        if (Options.ImpostorHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && (PlayerControl.LocalPlayer.GetCustomRole().IsImpostor() || PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.Egoist))
                        {
                            if ((PlayerControl.LocalPlayer.GetRoleClass() as Amnesiac)?.omoidasita == false) break;
                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }
                            Logger.Info($"{PlayerControl.LocalPlayer.Data.PlayerName} : {send}", "impostorsChat");
                            foreach (var imp in PlayerCatch.AllAlivePlayerControls)
                            {
                                if ((imp.GetRoleClass() as Amnesiac)?.omoidasita == false) continue;
                                if (imp && ((imp?.GetCustomRole().IsImpostor() ?? false) || imp?.GetCustomRole() is CustomRoles.Egoist))
                                {
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.Reliable);
                                    writer.StartMessage(imp.GetClientId());
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write($"<line-height=-18%>\n<color=#ff1919>☆{GetPlayerColor(PlayerControl.LocalPlayer)}☆</color></line-height>")
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                    .Write(send.Mark(Palette.ImpostorRed))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(PlayerControl.LocalPlayer.Data.PlayerName)
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                        break;

                    case "/jackalchat":
                    case "/jacct":
                    case "/jc":
                        canceled = true;
                        if (Options.ImpostorHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien)
                        {
                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }
                            Logger.Info($"{PlayerControl.LocalPlayer.Data.PlayerName} : {send}", "jackalChat");
                            foreach (var jac in PlayerCatch.AllAlivePlayerControls)
                            {
                                if (jac && (jac?.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien))
                                {
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.Reliable);
                                    writer.StartMessage(jac.GetClientId());
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write($"<line-height=-18%>\n<color=#00b4eb>Φ{GetPlayerColor(PlayerControl.LocalPlayer)}Φ</color></line-height>")
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                    .Write(send.Mark(ModColors.JackalColor))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(PlayerControl.LocalPlayer.Data.PlayerName)
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                        break;

                    case "/loverschat":
                    case "/loverchat":
                    case "/lc":
                        canceled = true;
                        if (Options.LoversHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.IsRiaju())
                        {
                            var l = PlayerControl.LocalPlayer.GetRiaju();

                            if (l is CustomRoles.NotAssigned or CustomRoles.OneLove || !l.IsRiaju()) break;

                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }
                            Logger.Info($"{PlayerControl.LocalPlayer.Data.PlayerName} : {send}", "loversChat");
                            foreach (var lover in PlayerCatch.AllAlivePlayerControls)
                            {
                                if (lover && (lover.GetRiaju() == l))
                                {
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.Reliable);
                                    writer.StartMessage(lover.GetClientId());
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(ColorString(GetRoleColor(l), $"<line-height=-18%>\n♥{GetPlayerColor(PlayerControl.LocalPlayer)}♥</line-height>"))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                    .Write(send.Mark(GetRoleColor(l)))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(PlayerControl.LocalPlayer.Data.PlayerName)
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else SendMessage($"{GetString("ForExample")}:\n{args[0]} test", PlayerControl.LocalPlayer.PlayerId);
                        break;
                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && float.TryParse(args[1], out float sec))
                        {
                            Main.MessageWait.Value = sec;
                            SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                        canceled = true;
                        if (args.Length > 1)
                            SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/settask":
                    case "/stt":
                        canceled = true;
                        var chc = "";
                        if (!GameStates.IsLobby) break;
                        if (args.Length > 1 && int.TryParse(args[1], out var cot))
                            if (ch(cot))
                            {
                                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.NumCommonTasks, cot);
                                chc += $"通常タスクを{cot}にしました!\n";
                            }
                        if (args.Length > 2 && int.TryParse(args[2], out var lot))
                            if (ch(lot))
                            {
                                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.NumLongTasks, lot);
                                chc += $"ロングタスクを{lot}にしました!\n";
                            }
                        if (args.Length > 3 && int.TryParse(args[3], out var sht))
                            if (ch(sht))
                            {
                                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.NumShortTasks, sht);
                                chc += $"ショートタスクを{sht}にしました!\n";
                            }
                        if (chc == "")
                        {
                            chc = "/settask(/stt) Common Long Short";
                            SendMessage(chc, PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        GameOptionsSender.RpcSendOptions();
                        SendMessage($"<size=70%>{chc}</size>");

                        static bool ch(int n)
                        {
                            if (n > 99) return false;
                            if (0 > n) return false;
                            return true;
                        }
                        break;
                    case "/kc":
                        canceled = true;
                        if (!GameStates.IsLobby) break;
                        if (args.Length > 1 && float.TryParse(args[1], out var fl))
                        {
                            if (fl <= 0) fl = 0.00000000000000001f;
                            Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetFloat(FloatOptionNames.KillCooldown, fl);
                        }
                        GameOptionsSender.RpcSendOptions();
                        break;
                    case "/exile":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        GetPlayerById(id2)?.RpcMurderPlayer(GetPlayerById(id2), true);
                        break;

                    case "/allplayertp":
                    case "/apt":
                        canceled = true;
                        if (!GameStates.IsLobby) break;
                        foreach (var tp in PlayerCatch.AllPlayerControls)
                        {
                            Vector2 position = new(0.0f, 0.0f);
                            tp.RpcSnapToForced(position);
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
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            sendchatid = $"{sendchatid}{pc.PlayerId}:{pc.name}\n";
                        }
                        __instance.AddChat(PlayerControl.LocalPlayer, sendchatid);
                        break;

                    case "/forceend":
                    case "/fe":
                        canceled = true;
                        if (GameStates.InGame)
                            SendMessage(GetString("ForceEndText"));
                        GameManager.Instance.enabled = false;
                        CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                        break;

                    case "/w":
                        canceled = true;
                        ShowLastWins();
                        break;

                    case "/timer":
                    case "/tr":
                        canceled = true;
                        if (!GameStates.IsInGame)
                            ShowTimer();
                        break;
                    case "/kf":
                        canceled = true;
                        if (GameStates.InGame)
                            AllPlayerKillFlash();
                        break;
                    case "/MeeginInfo":
                    case "/mi":
                    case "/day":
                        canceled = true;
                        if (GameStates.InGame)
                        {
                            SendMessage(MeetingHudPatch.Send, title: MeetingHudPatch.Title);
                        }
                        break;

                    case "/find":
                        canceled = true;
                        GameOptionsMenuUpdatePatch.find = args.Length < 2 ? "" : args[1];
                        break;

                    case "/at":
                        canceled = true;
                        //Roles.Neutral.NoName.OnCommand(PlayerControl.LocalPlayer.PlayerId, int.TryParse(args.Length < 2 ? "" : args[1], out var coin) ? coin : null);
                        break;

                    case "/yaminabe":
                    case "/yami":
                    case "/ym":
                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            canceled = true;
                            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned && (!Event.IsE(option.Key) || Event.Special)))
                            {
                                var r = option.Key;
                                if (r.IsImpostor() || r.IsCrewmate() || r.IsMadmate() || r.IsNeutral()) option.Value.SetValue(10);
                            }
                        }
                        break;
                    case "/superyaminabe":
                    case "/superyami":
                    case "/sym":
                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            canceled = true;
                            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned && (!Event.IsE(option.Key) || Event.Special)))
                            {
                                option.Value.SetValue(10);
                            }
                        }
                        break;
                    case "/rolereset":
                    case "/resetrole":
                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            canceled = true;
                            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned))
                            {
                                option.Value.SetValue(0);
                            }
                        }
                        break;

                    case "/cr":
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            canceled = true;
                            subArgs = args.Length < 2 ? "" : args[1];
                            var pc = PlayerControl.LocalPlayer;
                            if (args.Length > 2 && int.TryParse(args[2], out var taisho))
                            {
                                pc = GetPlayerById(taisho);
                                if (pc == null) pc = PlayerControl.LocalPlayer;
                            }
                            if (GetRoleByInputName(subArgs, out var role, true))
                            {
                                if (GameStates.InGame)
                                {
                                    NameColorManager.RemoveAll(pc.PlayerId);
                                    pc.RpcSetCustomRole(role, true, true);
                                }
                                else
                                {
                                    if (role.IsAddOn() || role.IsGorstRole() || role.IsRiaju()) break;
                                    Main.HostRole = role;
                                    var r = ColorString(GetRoleColor(role), GetString($"{role}"));
                                    SendMessage($"ホストの役職を{r}にするよっ!!", PlayerControl.LocalPlayer.PlayerId);
                                }
                            }
                            else
                            {
                                if (Main.HostRole == CustomRoles.NotAssigned) SendMessage("役職変更に失敗したよ(´・ω・｀)", PlayerControl.LocalPlayer.PlayerId);
                                else
                                {
                                    Main.HostRole = CustomRoles.NotAssigned;
                                    SendMessage("役職固定をリセットしたよっ!", PlayerControl.LocalPlayer.PlayerId);
                                }
                            }
                        }
                        break;
                    case "/dgm":
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            canceled = true;
                            if (!GameStates.InGame)
                            {
                                SendMessage($"ロビーでは変更出来ないよっ");
                                break;
                            }
                            Main.DontGameSet = !Main.DontGameSet;
                            SendMessage($"ゲームを終了しない設定を{Main.DontGameSet}にしたよっ!!");
                        }
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
                                    Logger.seeingame($"AntiBlockOut:{Main.DebugAntiblackout}");
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
                                    foreach (var pc in PlayerCatch.AllPlayerControls)
                                        sb.Append(pc.PlayerId + ": " + pc.name + " => " + pc.GetCustomRole() + "\n");
                                    SendMessage(sb.ToString(), PlayerControl.LocalPlayer.PlayerId);
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
                                        var pc = GetPlayerById(pcid);
                                        var seer = GetPlayerById(seerid);
                                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, seer.GetClientId());
                                        writer.WriteNetObject(pc);
                                        writer.Write((int)ExtendedPlayerControl.SuccessFlags);
                                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                                    }
                                    break;
                                case "gettbteam":
                                    var tbstring = "";
                                    foreach (var t in TaskBattle.TaskBattleTeams)
                                    {
                                        foreach (var idt in t)
                                            tbstring += idt + "\n";
                                        tbstring += "\n";
                                    }
                                    SendMessage(tbstring, PlayerControl.LocalPlayer.PlayerId);
                                    break;
                                case "resetcam":
                                    if (args.Length < 2 || !int.TryParse(args[2], out int id3)) break;
                                    GetPlayerById(id3)?.ResetPlayerCam(1f);
                                    break;
                                case "resetdoorE":
                                    AirShipElectricalDoors.Initialize();
                                    break;
                                case "GetVoice":
                                    foreach (var r in GetvoiceListAsync().Result)
                                        Logger.Info(r.Value, "VoiceList");
                                    break;
                                case "rev":
                                    if (!byte.TryParse(args[2], out byte idr)) break;
                                    var revplayer = GetPlayerById(idr);
                                    revplayer.Data.IsDead = false;
                                    PlayerControl.LocalPlayer.SetDirtyBit(0b_1u << idr);
                                    AmongUsClient.Instance.SendAllStreamedObjects();
                                    break;
                            }
                            break;
                        }
                        break;

                    default:
                        if (args[0].StartsWith("/"))
                        {
                            canceled = true;
                            break;
                        }
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(cancelVal);
            }
            if (AmongUsClient.Instance.AmHost && GameStates.IsLobby && !canceled)
            {
                SendMessage(text, title: Main.nickName == "" ? PlayerControl.LocalPlayer.name : Main.nickName, rob: true);
                __instance.freeChatField.textArea.Clear();
                return false;
            }
            return !canceled;
        }

        public static async Task Yomiage(int color, string text = "")
        {
            var te = text;
            text = text.RemoveHtmlTags();//Html消す
            if (ChatManager.CommandCheck(text)) return;// /から始まってるならスルー
            if (text != te) return;//htmlタグが入ってる場合は発言じゃないのでスルー
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
                Logger.seeingame("エラーが発生したため、読み上げが無効になりました");
                Main.UseYomiage.Value = false;
            }
        }
        public static Dictionary<int, string> VoiceList;
        public static async Task<Dictionary<int, string>> GetvoiceListAsync(bool forced = false)
        {
            if (VoiceList is null || VoiceList.Count is 0 || forced)
            {
                try
                {
                    string result;
                    ClientOptionsManager.CheckOptions();
                    using (HttpClient client = new())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-K Updater");
                        using var response = await client.GetAsync(new Uri($"http://localhost:{ClientOptionsManager.YomiagePort}/getvoicelist"), HttpCompletionOption.ResponseContentRead);
                        if (!response.IsSuccessStatusCode || response.Content == null)
                        {
                            Logger.Error($"ステータスコード: {response.StatusCode}", "GetVoiceList");
                            return null;
                        }
                        result = await response.Content.ReadAsStringAsync();
                    }
                    var voice = JsonSerializer.Deserialize<Voice>(result)?.voiceList;

                    VoiceList = new();
                    for (var i = 0; i < voice.Count; i++)
                        VoiceList.Add(i, voice[i].name);
                    return VoiceList;

                }
                catch (HttpRequestException e)
                {
                    // エラーが発生した場合はエラーメッセージを表示
                    Logger.Info($"Error: {e.Message}", "yomiage");
                    Logger.seeingame("エラーが発生したため、読み上げが無効になりました");
                    Main.UseYomiage.Value = false;
                }
                return null;
            }
            return VoiceList;
        }
        public class Voice
        {
            public List<Namev> voiceList { get; set; }

            public class Namev
            {
                public string name { get; set; }
            }
        }

        public static string LastL = "N";
        public static void GetRolesInfo(string role, byte player = 255)
        {
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && player != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), player);
                return;
            }
            // 初回のみ処理
            if (roleCommands == null)
            {
#pragma warning disable IDE0028  // Dictionary初期化の簡素化をしない
                roleCommands = new Dictionary<CustomRoles, string>();

                // GM
                roleCommands.Add(CustomRoles.GM, "gm");

                // Impostor役職
                roleCommands.Add((CustomRoles)(-1), $"【=== {GetString("Impostor")} ===】");  // 区切り用
                roleCommands.Add(CustomRoles.Shapeshifter, "She");
                //roleCommands.Add(CustomRoles.Phantom, "Pha");//アプデ対応用 仮
                ConcatCommands(CustomRoleTypes.Impostor);

                // Madmate役職
                roleCommands.Add((CustomRoles)(-2), $"【=== {GetString("Madmate")} ===】");  // 区切り用
                ConcatCommands(CustomRoleTypes.Madmate);
                roleCommands.Add(CustomRoles.SKMadmate, "sm");

                // Crewmate役職
                roleCommands.Add((CustomRoles)(-3), $"【=== {GetString("Crewmate")} ===】");  // 区切り用
                roleCommands.Add(CustomRoles.Engineer, "Eng");
                roleCommands.Add(CustomRoles.Scientist, "Sci");
                roleCommands.Add(CustomRoles.Tracker, "Trac");
                roleCommands.Add(CustomRoles.Noisemaker, "Nem");//アプデ対応用 仮

                ConcatCommands(CustomRoleTypes.Crewmate);

                // Neutral役職
                roleCommands.Add((CustomRoles)(-4), $"【=== {GetString("Neutral")} ===】");  // 区切り用
                ConcatCommands(CustomRoleTypes.Neutral);
                // 属性
                roleCommands.Add((CustomRoles)(-5), $"【=== {GetString("Addons")} ===】");  // 区切り用
                //ラスト
                roleCommands.Add(CustomRoles.Workhorse, "wh");
                roleCommands.Add(CustomRoles.LastNeutral, "ln");
                roleCommands.Add(CustomRoles.LastImpostor, "li");
                //バフ
                roleCommands.Add(CustomRoles.watching, "wat");
                roleCommands.Add(CustomRoles.Speeding, "sd");
                roleCommands.Add(CustomRoles.Guarding, "gi");
                roleCommands.Add(CustomRoles.Guesser, "Gr");
                roleCommands.Add(CustomRoles.Moon, "Mo");
                roleCommands.Add(CustomRoles.Lighting, "Li");
                roleCommands.Add(CustomRoles.Management, "Dr");
                roleCommands.Add(CustomRoles.Connecting, "Cn");
                roleCommands.Add(CustomRoles.Serial, "Se");
                roleCommands.Add(CustomRoles.PlusVote, "Pv");
                roleCommands.Add(CustomRoles.Opener, "Oe");
                roleCommands.Add(CustomRoles.Revenger, "Re");
                roleCommands.Add(CustomRoles.seeing, "Se");
                roleCommands.Add(CustomRoles.Autopsy, "Au");
                roleCommands.Add(CustomRoles.Tiebreaker, "tb");
                roleCommands.Add(CustomRoles.MagicHand, "MaH");

                //デバフ
                roleCommands.Add(CustomRoles.SlowStarter, "sl");
                roleCommands.Add(CustomRoles.NonReport, "Nr");
                roleCommands.Add(CustomRoles.Notvoter, "nv");
                roleCommands.Add(CustomRoles.Water, "wt");
                roleCommands.Add(CustomRoles.Transparent, "tr");
                roleCommands.Add(CustomRoles.Slacker, "sl");
                roleCommands.Add(CustomRoles.Clumsy, "lb");
                roleCommands.Add(CustomRoles.Elector, "El");
                roleCommands.Add(CustomRoles.Amnesia, "am");
                roleCommands.Add(CustomRoles.InfoPoor, "IP");
                //第三
                roleCommands.Add(CustomRoles.Lovers, "lo");
                roleCommands.Add(CustomRoles.OneLove, "ol");
                roleCommands.Add(CustomRoles.MadonnaLovers, "Ml");
                roleCommands.Add(CustomRoles.Amanojaku, "Ama");

                roleCommands.Add((CustomRoles)(-7), $"== {GetString("GhostRole")} ==");  // 区切り用
                //幽霊
                roleCommands.Add(CustomRoles.Ghostbuttoner, "Bbu");
                roleCommands.Add(CustomRoles.GhostNoiseSender, "NiS");
                roleCommands.Add(CustomRoles.GhostReseter, "Res");
                roleCommands.Add(CustomRoles.GuardianAngel, "Gan");
                roleCommands.Add(CustomRoles.DemonicCrusher, "DCr");
                roleCommands.Add(CustomRoles.DemonicTracker, "DTr");
                roleCommands.Add(CustomRoles.DemonicVenter, "Dve");
                roleCommands.Add(CustomRoles.AsistingAngel, "AsA");

                // HAS
                roleCommands.Add((CustomRoles)(-6), $"== {GetString("HideAndSeek")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.HASFox, "hfo");
                roleCommands.Add(CustomRoles.HASTroll, "htr");
                LastL = "N";
#pragma warning restore IDE0028
            }

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            switch (role)
            {
                case "i":
                case "I":
                case "imp":
                case "インポスター":
                case "impostor":
                case "impostors":
                case "インポス":
                    rolemsg = $"{GetString("h_r_impostor").Color(Palette.ImpostorRed)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsImpostor()))
                    {
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "c":
                case "C":
                case "crew":
                case "crewmate":
                case "クルー":
                case "クルーメイト":
                    rolemsg = $"{GetString("h_r_crew").Color(Color.blue)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsCrewmate()))
                    {
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "n":
                case "N":
                case "Neu":
                case "Neutral":
                case "第三":
                case "第三陣営":
                case "ニュートラル":
                    rolemsg = $"{GetString("h_r_Neutral").Color(Palette.DisabledGrey)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsNeutral()))
                    {
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "m":
                case "Mad":
                case "マッド":
                case "狂人":
                case "M":
                    rolemsg = $"{GetString("h_r_MadMate").Color(ModColors.MadMateOrenge)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsMadmate()))
                    {
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "a":
                case "A":
                case "Addon":
                case "アドオン":
                case "属性":
                case "モディフィア":
                case "重複役職":
                    rolemsg = $"{GetString("h_r_Addon").Color(ModColors.AddonsColor)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsAddOn() || r.Key is CustomRoles.Amanojaku))
                    {
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "g":
                case "G":
                case "Ghost":
                case "幽霊":
                case "幽霊役職":
                    rolemsg = $"{GetString("h_r_GhostRole").Color(ModColors.GhostRoleColor)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsGorstRole()))
                    {
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
            }
            foreach (var r in roleCommands)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (string.Compare(role, roleName, true) == 0 || string.Compare(role, roleShort, true) == 0)
                {
                    var roleInfo = r.Key.GetRoleInfo();
                    if (roleInfo != null && roleInfo.Description != null)
                    {
                        SendMessage(roleInfo.Description.FullFormatHelp, sendTo: player);
                    }
                    // RoleInfoがない役職は従来の処理
                    else
                    {
                        if (r.Key.IsAddOn() || r.Key.IsRiaju() || r.Key == CustomRoles.Amanojaku || r.Key.IsGorstRole()) SendMessage(GetAddonsHelp(r.Key), sendTo: player);
                        else SendMessage(ColorString(GetRoleColor(r.Key), "<b><line-height=2.0pic><size=150%>" + GetString(roleName) + "\n<line-height=1.8pic><size=90%>" + GetString($"{roleName}Info")) + "\n<line-height=1.3pic></b><size=60%>\n" + GetString($"{roleName}InfoLong"), sendTo: player);
                    }
                    return;
                }
            }

            if (GetRoleByInputName(role, out var hr, true))
            {
                if (hr is CustomRoles.Crewmate or CustomRoles.Impostor) SendMessage(msg, player);
                var roleInfo = hr.GetRoleInfo();
                if (roleInfo != null && roleInfo.Description != null)
                {
                    SendMessage(roleInfo.Description.FullFormatHelp, sendTo: player);
                }
                // RoleInfoがない役職は従来の処理
                else
                {
                    if (hr.IsAddOn() || hr.IsRiaju() || hr == CustomRoles.Amanojaku || hr.IsGorstRole()) SendMessage(GetAddonsHelp(hr), sendTo: player);
                    else SendMessage(ColorString(GetRoleColor(hr), "<b><line-height=2.0pic><size=150%>" + GetString($"{hr}") + "\n<line-height=1.8pic><size=90%>" + GetString($"{hr}Info")) + "\n<line-height=1.3pic></b><size=60%>\n" + GetString($"{hr}InfoLong"), sendTo: player);
                }
                return;
            }
            msg += rolemsg;
            if (player == byte.MaxValue) player = 0;
            SendMessage(msg, player);
        }
        /// <summary>
        /// 複数登録するor特別な奴以外はしなくてよい。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string FixRoleNameInput(string text)
        {
            return text switch
            {
                "GM" or "gm" or "ゲームマスター" => GetString("GM"),

                //インポスター
                "ボマー" or "爆弾魔" => GetString("Bomber"),
                "大狼" or "たいろう" or "大老" => GetString("Tairou"),
                "吸血鬼" or "ヴァンパイア" => GetString("Vampire"),
                "魔女" or "ウィッチ" => GetString("Witch"),

                //マッドメイト
                "サイドキックマッドメイト" => GetString("SKMadmate"),

                //クルーメイト
                "ぽんこつ占い師" or "ポンコツ占い師" => GetString("PonkotuTeller"),
                "エンジニア" => GetString("Engineer"),
                "科学者" => GetString("Scientist"),
                "トラッカー" => GetString("Tracker"),
                "ノイズメーカー" => GetString("Noisemaker"),//アプデ対応用  よくわからない
                "巫女" or "みこ" or "ふじょ" => GetString("ShrineMaiden"),
                "クルー" or "クルーメイト" => GetString("Crewmate"),
                "狼少年" or "オオカミ少年" or "おおかみ少年" => GetString("WolfBoy"),

                //第3陣営
                "ラバーズ" or "リア充" or "恋人" => GetString("Lovers"),
                "片思い" or "片想い" => GetString("OneLove"),
                "シュレディンガーの猫" or "シュレ猫" => GetString("SchrodingerCat"),
                "Eシュレディンガーの猫" or "Eシュレ猫" => GetString("EgoSchrodingerCat"),
                "Jシュレディンガーの猫" or "Jシュレ猫" => GetString("JSchrodingerCat"),
                "ジャッカルドール" => GetString("Jackaldoll"),
                "きつね" or "ようこ" or "妖狐" => GetString("Fox"),
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
                if (input == GuessManager.ChangeNormal2Vanilla(role))
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
                roleCommands[role.RoleName] = role.ChatCommand;
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
            if (text.RemoveHtmlTags() != text) return;//システムメッセージなら処理しない
            if (player.PlayerId != 0)
            {
                ChatManager.SendMessage(player, text);
            }
            if (GuessManager.GuesserMsg(player, text)) { canceled = true; return; }

            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    canceled = true;
                    ShowLastResult(player.PlayerId);
                    break;

                case "/kl":
                case "/killlog":
                    canceled = true;
                    ShowKillLog(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            ShowActiveRoles(player.PlayerId);
                            break;
                        case "set":
                        case "s":
                        case "setting":
                            ShowSetting(player.PlayerId);
                            break;
                        default:
                            ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/h":
                case "/help":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            ShowActiveSettingsHelp(player.PlayerId);
                            break;

                        case "r":
                        case "roles":
                            subArgs = args.Length < 3 ? "" : args[2];
                            GetRolesInfo(subArgs, player.PlayerId);
                            break;
                        default:
                            ShowHelp(player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    if (GameStates.IsInGame)
                    {
                        canceled = true;
                        var role = player.GetCustomRole();
                        var roleclass = player.GetRoleClass();
                        if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                        if (roleclass?.Jikaku() != CustomRoles.NotAssigned && roleclass != null) role = roleclass.Jikaku();

                        if (role is CustomRoles.Amnesiac)
                        {
                            if (roleclass is Amnesiac amnesiac && !amnesiac.omoidasita)
                                role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
                        }

                        var RoleTextData = GetRoleColorCode(role);
                        string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                        string RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";

                        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                        {
                            SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(player.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{player.GetRoleInfo(true)}" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                        }
                        else
                        if (role.GetRoleInfo()?.Description is { } description)
                        {

                            SendMessage(description.FullFormatHelp, player.PlayerId, RoleInfoTitle);
                        }
                        // roleInfoがない役職
                        else
                        {
                            SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(player.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{player.GetRoleInfo(true)}", player.PlayerId, RoleInfoTitle);
                        }
                        GetAddonsHelp(player);
                    }
                    break;

                case "/t":
                case "/template":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                case "/timer":
                case "/tr":
                    canceled = true;
                    if (!GameStates.IsInGame)
                        ShowTimer(player.PlayerId);
                    break;

                case "/tp":
                    if (!GameStates.IsLobby || !Options.sotodererukomando.GetBool()) break;
                    canceled = true;
                    subArgs = args[1];
                    switch (subArgs)
                    {
                        case "o":
                            Vector2 position = new(3.0f, 0.0f);
                            player.RpcSnapToForced(position);
                            break;
                        case "i":
                            Vector2 position2 = new(0.0f, 0.0f);
                            player.RpcSnapToForced(position2);
                            break;

                    }
                    break;

                case "/kf":
                    canceled = true;
                    if (GameStates.InGame)
                        player.KillFlash(kiai: true);
                    break;

                case "/MeeginInfo":
                case "/mi":
                    canceled = true;
                    if (GameStates.InGame)
                    {
                        SendMessage(MeetingHudPatch.Send, player.PlayerId, title: MeetingHudPatch.Title);
                    }
                    break;

                case "/voice":
                case "/vo":
                    if (!Main.UseYomiage.Value) break;
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    string subArgs2 = args.Length < 3 ? "" : args[2];
                    string subArgs3 = args.Length < 4 ? "" : args[3];
                    string subArgs4 = args.Length < 5 ? "" : args[4];
                    if (subArgs is "get" or "g" && Main.UseYomiage.Value)
                    {
                        StringBuilder sb = new();
                        foreach (var r in GetvoiceListAsync().Result)
                            sb.Append($"{r.Key}: {r.Value}");
                        SendMessage(sb.ToString(), player.PlayerId);
                    }
                    else if (subArgs != "" && subArgs2 != "" && subArgs3 != "" && subArgs4 != "")
                    {
                        if (VoiceList is null) GetvoiceListAsync().Wait();
                        if (int.TryParse(subArgs, out int vid) && VoiceList.Count > vid)
                        {
                            YomiageS[player.Data.DefaultOutfit.ColorId] = $"{subArgs} {subArgs2} {subArgs3} {subArgs4}";
                            RPC.SyncYomiage();
                        }
                        else
                        {
                            StringBuilder sb = new();
                            foreach (var r in GetvoiceListAsync().Result)
                                sb.Append($"{r.Key}: {r.Value}");
                            SendMessage(sb.ToString(), player.PlayerId);
                        }
                    }
                    else SendMessage("使用方法:\n/vo 音質(id) 音量 速度 音程\n\n音質の一覧表示:\n /vo get\n /vo g", player.PlayerId);
                    break;
                case "/impstorchat":
                case "/impct":
                case "/ic":
                    if (Options.ImpostorHideChat.GetBool() && player.IsAlive() && (player.GetCustomRole().IsImpostor() || player.GetCustomRole() is CustomRoles.Egoist))
                    {
                        if ((player.GetRoleClass() as Amnesiac)?.omoidasita == false)
                        {
                            canceled = true;
                            break;
                        }
                        if (GameStates.Tuihou)
                        {
                            canceled = true;
                            break;
                        }
                        var send = "";
                        foreach (var ag in args)
                        {
                            if (ag.StartsWith("/")) continue;
                            send += ag;
                        }
                        Logger.Info($"{player.Data.PlayerName} : {send}", "ImpostorChat");
                        foreach (var imp in AllPlayerControls)
                        {
                            if ((imp.GetRoleClass() as Amnesiac)?.omoidasita == false && imp.IsAlive()) continue;
                            if (imp && ((imp.GetCustomRole().IsImpostor() || imp.GetCustomRole() is CustomRoles.Egoist) || (!imp.IsAlive() && PlayerControl.LocalPlayer.PlayerId == imp?.PlayerId)) && imp.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = imp.GetClientId();
                                    if (clientid == -1) continue;
                                    var writer = CustomRpcSender.Create("ImpostorChatSend", SendOption.Reliable);
                                    writer.StartMessage(clientid);
                                    writer.StartRpc(imp.NetId, (byte)RpcCalls.SetName)
                                    .Write(imp.Data.NetId)
                                    .Write($"<align=\"left\"><line-height=-18%>\n<color=#ff1919>☆{GetPlayerColor(player)}☆</color></line-height>")
                                    .EndRpc();
                                    writer.StartRpc(imp.NetId, (byte)RpcCalls.SendChat)
                                    .Write($"<align=\"left\">{send.Mark(Palette.ImpostorRed)}")
                                    .EndRpc();
                                    writer.StartRpc(imp.NetId, (byte)RpcCalls.SetName)
                                    .Write(imp.Data.NetId)
                                    .Write(imp.Data.PlayerName)
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                    }
                    canceled = true;
                    break;

                case "/jackalchat":
                case "/jacct":
                case "/jc":
                    if (Options.JackalHideChat.GetBool() && player.IsAlive() && player.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien)
                    {
                        var send = "";
                        if (GameStates.Tuihou)
                        {
                            canceled = true;
                            break;
                        }
                        foreach (var ag in args)
                        {
                            if (ag.StartsWith("/")) continue;
                            send += ag;
                        }
                        Logger.Info($"{player.Data.PlayerName} : {send}", "JackalChat");
                        foreach (var jac in AllPlayerControls)
                        {
                            if (jac && ((jac.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien) || (PlayerControl.LocalPlayer.PlayerId == jac?.PlayerId && !jac.IsAlive())) && jac.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = jac.GetClientId();
                                    if (clientid == -1) continue;
                                    var writer = CustomRpcSender.Create("JacalSendChat", SendOption.Reliable);
                                    writer.StartMessage(clientid);
                                    writer.StartRpc(jac.NetId, (byte)RpcCalls.SetName)
                                    .Write(jac.Data.NetId)
                                    .Write($"<align=\"left\"><line-height=-18%>\n<color=#00b4eb>Φ{GetPlayerColor(player)}Φ</color></line-height>")
                                    .EndRpc();
                                    writer.StartRpc(jac.NetId, (byte)RpcCalls.SendChat)
                                    .Write($"<align=\"left\">{send.Mark(ModColors.JackalColor)}")
                                    .EndRpc();
                                    writer.StartRpc(jac.NetId, (byte)RpcCalls.SetName)
                                    .Write(jac.Data.NetId)
                                    .Write(jac.Data.PlayerName)
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                    }
                    canceled = true;
                    break;
                case "/loverschat":
                case "/loverchat":
                case "/lc":
                    if (Options.LoversHideChat.GetBool() && player.IsAlive() && player.IsRiaju())
                    {
                        var l = player.GetRiaju();

                        if (GameStates.Tuihou)
                        {
                            canceled = true;
                            break;
                        }
                        if (l is CustomRoles.NotAssigned or CustomRoles.OneLove || !l.IsRiaju()) break;

                        var send = "";
                        foreach (var ag in args)
                        {
                            if (ag.StartsWith("/")) continue;
                            send += ag;
                        }
                        Logger.Info($"{player.Data.PlayerName} : {send}", "LoversChat");
                        foreach (var lover in AllPlayerControls)
                        {
                            if (lover && (lover.GetRiaju() == l || (PlayerControl.LocalPlayer.PlayerId == lover?.PlayerId && !lover.IsAlive())) && lover.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = lover.GetClientId();
                                    if (clientid == -1) continue;
                                    var writer = CustomRpcSender.Create("LoverChatSend", SendOption.Reliable);
                                    writer.StartMessage(clientid);
                                    writer.StartRpc(lover.NetId, (byte)RpcCalls.SetName)
                                    .Write(lover.Data.NetId)
                                    .Write(ColorString(GetRoleColor(l), $"<align=\"left\"><line-height=-18%>\n♥{GetPlayerColor(player)}♥</line-height>"))
                                    .EndRpc();
                                    writer.StartRpc(lover.NetId, (byte)RpcCalls.SendChat)
                                    .Write($"<align=\"left\">{send.Mark(GetRoleColor(l))}")
                                    .EndRpc();
                                    writer.StartRpc(lover.NetId, (byte)RpcCalls.SetName)
                                    .Write(lover.Data.NetId)
                                    .Write(lover.Data.PlayerName)
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                    }
                    canceled = true;
                    break;

                default:
                    if (args[0].StartsWith("/"))
                    {
                        canceled = true;
                        if (GameStates.Tuihou)
                        {
                            ChatManager.SendPreviousMessagesToAll(); break;
                        }
                        break;
                    }
                    canceled = false;
                    /*
                    if (!player.IsAlive() && GameStates.Tuihou && AntiBlackout.IsCached)
                    {
                        ChatManager.SendPreviousMessagesToAll();
                        break;
                    }*/

                    if (!Options.ExHideChatCommand.GetBool()) break;

                    if (GameStates.Meeting && GameStates.IsMeeting && !AntiBlackout.IsCached && !canceled)
                    {
                        if (GameStates.Tuihou) break;

                        if (!player.IsAlive()) break;
                        _ = new LateTask(() =>
                        {
                            if (AmongUsClient.Instance.AmHost)
                            {
                                List<PlayerControl> sendplayers = new();
                                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                                {
                                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId || pc.IsModClient() ||
                                    player.PlayerId == PlayerControl.LocalPlayer.PlayerId || player.IsModClient() ||
                                    pc.PlayerId == player.PlayerId) continue;
                                    player.Data.IsDead = false;

                                    var sender = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                    sender.StartMessage(pc.GetClientId());

                                    MeetingHudPatch.StartPatch.Serialize = true;

                                    sender.Write((wit) =>
                                    {
                                        wit.StartMessage(1); //0x01 Data
                                        {
                                            wit.WritePacked(player.Data.NetId);
                                            player.Data.Serialize(wit, false);
                                        }
                                        wit.EndMessage();
                                    }, true);
                                    MeetingHudPatch.StartPatch.Serialize = false;
                                    sender.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                                            .Write(text)
                                            .EndRpc();
                                    player.Data.IsDead = true;
                                    MeetingHudPatch.StartPatch.Serialize = true;

                                    sender.Write((wit) =>
                                    {
                                        wit.StartMessage(1); //0x01 Data
                                        {
                                            wit.WritePacked(player.Data.NetId);
                                            player.Data.Serialize(wit, false);
                                        }
                                        wit.EndMessage();
                                    }, true);
                                    sender.EndMessage();
                                    sender.SendMessage();
                                    MeetingHudPatch.StartPatch.Serialize = false;
                                }
                                player.Data.IsDead = false;
                            }
                        }, Main.LagTime, "", true);
                    }
                    break;
            }
            canceled &= Options.ExHideChatCommand.GetBool();
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
            var player = AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            var sendtopc = GetPlayerById(sendTo);
            int clientId = sendTo == byte.MaxValue ? -1 : sendtopc.GetClientId();
            if (sendTo != byte.MaxValue && sendtopc == null)
            {
                Logger.Info($"{sendTo}がnullだから弾いたぞ!", "ChatUpdatePatch");
                return;
            }
            if (GameStates.IsLobby)
            {
                _ = new LateTask(() => aftersystemmeg = true, 0.24f, "", true);
            }
            var name = player.Data.PlayerName;
            if (Options.ExHideChatCommand.GetBool())
            {
                if (clientId == -1)
                {
                    if (player.PlayerId != 0)
                    {
                        foreach (var pc in AllPlayerControls)
                        {
                            var sender = pc;
                            if (!pc.IsAlive()) sender = player;

                            clientId = pc.GetClientId();
                            var Nwriter = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            Nwriter.StartMessage(clientId);
                            Nwriter.StartRpc(sender.NetId, (byte)RpcCalls.SetName)
                            .Write(sender.Data.NetId)
                            .Write(title)
                            .EndRpc();
                            Nwriter.StartRpc(sender.NetId, (byte)RpcCalls.SendChat)
                            .Write(msg)
                            .EndRpc();
                            Nwriter.StartRpc(sender.NetId, (byte)RpcCalls.SetName)
                            .Write(sender.Data.NetId)
                            .Write(sender.Data.PlayerName)
                            .EndRpc();
                            Nwriter.EndMessage();
                            Nwriter.SendMessage();
                            if (GameStates.Meeting)
                                _ = new LateTask(() =>
                                    {
                                        foreach (var seer in AllPlayerControls)
                                        {
                                            var seenName = player.GetRealName(isMeeting: true);
                                            seenName = seenName.ApplyNameColorData(seer, player, true);

                                            player.RpcSetNamePrivate(seenName, true, seer, true);
                                        }
                                        DoBlockChat = false;
                                    }, Main.LagTime, "Setname", true);
                        }
                        __instance.timeSinceLastMessage = 0f;

                        return;
                    }
                    else
                    {
                        player.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                        player.SetName(name);
                    }
                }
                if (player.PlayerId != 0 && clientId != byte.MaxValue)
                {
                    clientId = sendtopc.GetClientId();
                    var Nwriter = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                    Nwriter.StartMessage(clientId);
                    Nwriter.StartRpc(sendtopc.NetId, (byte)RpcCalls.SetName)
                    .Write(sendtopc.Data.NetId)
                    .Write(title)
                    .EndRpc();
                    Nwriter.StartRpc(sendtopc.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                    Nwriter.StartRpc(sendtopc.NetId, (byte)RpcCalls.SetName)
                    .Write(sendtopc.Data.NetId)
                    .Write(sendtopc.Data.PlayerName)
                    .EndRpc();
                    Nwriter.EndMessage();
                    Nwriter.SendMessage();
                    __instance.timeSinceLastMessage = 0f;
                    return;
                }
            }
            else
            if (clientId == -1)
            {
                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);
            }
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.NetId)
            .Write(title)
            .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.NetId)
            .Write(player.Data.PlayerName)
            .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            if (GameStates.Meeting)
                _ = new LateTask(() =>
                    {
                        foreach (var seer in AllPlayerControls)
                        {
                            var seenName = player.GetRealName(isMeeting: true);
                            seenName = seenName.ApplyNameColorData(seer, player, true);

                            player.RpcSetNamePrivate(seenName, true, seer, true);
                        }
                        DoBlockChat = false;
                    }, Main.LagTime, "Setname", true);
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