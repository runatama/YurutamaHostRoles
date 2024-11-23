using System;
using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            if (Main.NormalOptions.NumImpostors == 0 && GameStates.IsOnlineGame)
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.NumImpostors, 1);

            ResolutionManager.SetResolution(Screen.width, Screen.height, Screen.fullScreen);
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            CheckPingPatch.Check = false;
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            ChatUpdatePatch.DoBlockChat = false;
            GameStates.InGame = false;
            Main.FeColl = 0;
            GameStates.canmusic = true;
            ErrorText.Instance.Clear();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc == null) continue;
                Logger.Info($"FriendCore:{pc.FriendCode},Puid:{pc.GetClient()?.GetHashedPuid()}", "Session");
            }
            if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
            {
                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetBool(BoolOptionNames.ConfirmImpostor, false);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.TaskBarMode, 2);
                foreach (var option in OptionItem.AllOptions)
                {
                    if (Event.OptionLoad.Contains(option.Name) && !Event.Special) option.SetValue(0);
                }
            }
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
    class DisconnectInternalPatch
    {
        public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
        {
            Logger.Info($"切断(理由:{reason}:{stringReason}, ping:{__instance.Ping})", "Session");

            if (GameStates.IsFreePlay && Main.EditMode)
            {
                CustomSpawnSaveandLoadManager.Save();
                Main.EditMode = false;
            }

            if (AmongUsClient.Instance.AmHost && GameStates.InGame && reason is not DisconnectReasons.Destroy)
            {
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                LastGameSave.CreateIfNotExists(oti: true);//落ちでも保存
            }
            Main.AssignSameRoles = false;
            CustomRoleManager.Dispose();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})(FriendCode:{client.FriendCode})(PuiD:{client.GetHashedPuid()})が参加", "Session");
            if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.seeingame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
            }
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
            }
            BanManager.CheckBanPlayer(client);
            BanManager.CheckDenyNamePlayer(client);
            RPC.RpcVersionCheck();
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.RpcSyncRoomTimer();
                RPC.SyncYomiage();
                //_ = new LateTask(() => client.Character.RpcSetNamePrivate($"<line-height=1000%><pos=30><size=2.8><color=green>host:<{Main.ModColor}>{Main.ModName} v{Main.PluginVersion}</color>\n</color><size=2.5>{client.PlayerName}\n</line-height>\n<line-height=30%>", true, force: true), 1.5f);
                string name = DataManager.player.Customization.Name;
                var playerName = client.PlayerName;
                if (playerName == name || playerName == Main.nickName)
                {
                    List<string> names = new() { name, Main.nickName };
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                        if (pc != PlayerControl.LocalPlayer && pc != __instance) names.Add(pc.Data.PlayerName);
                    for (int index1 = 1; index1 < 100; ++index1)
                    {
                        playerName = client.PlayerName + " " + index1.ToString();
                        if (!names.Contains(playerName))
                            break;
                    }
                    _ = new LateTask(() => client.Character.RpcSetName(playerName), 1.5f, "Fix Name");
                }
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        static void Prefix([HarmonyArgument(0)] ClientData data)
        {
            if (CustomRoles.Executioner.IsPresent())
                Executioner.ChangeRoleByTarget(data.Character.PlayerId);
        }
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            var isFailure = false;

            try
            {
                if (data == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のClientDataがnull", nameof(OnPlayerLeftPatch));
                }
                else if (data.Character == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のPlayerControlがnull", nameof(OnPlayerLeftPatch));
                }
                else if (data.Character.Data == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のPlayerInfoがnull", nameof(OnPlayerLeftPatch));
                }
                else
                {
                    if (GameStates.IsInGame)
                    {
                        if (!SelectRolesPatch.Disconnected.Contains(data.Character.PlayerId))
                            SelectRolesPatch.Disconnected.Add(data.Character.PlayerId);

                        Lovers.LoverDisconnected(data.Character);
                        var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                        if (state.DeathReason == CustomDeathReason.etc) //死因が設定されていなかったら
                        {
                            state.DeathReason = CustomDeathReason.Disconnected;
                            UtilsGameLog.AddGameLog("Disconnected", data.PlayerName + GetString("DeathReason.Disconnected"));
                        }
                        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                        {
                            role?.OnLeftPlayer(data.Character);
                        }
                        state.SetDead();
                        AntiBlackout.OnDisconnect(data.Character.Data);
                        PlayerGameOptionsSender.RemoveSender(data.Character);
                        PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true));
                        UtilsNotifyRoles.NotifyRoles(NoCache: true);
                    }
                    Main.playerVersion.Remove(data.Character.PlayerId);
                    Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
                }
            }
            catch (Exception e)
            {
                Logger.Warn("切断処理中に例外が発生", nameof(OnPlayerLeftPatch));
                Logger.Exception(e, nameof(OnPlayerLeftPatch));
                isFailure = true;
            }

            if (isFailure)
            {
                Logger.Warn($"正常に完了しなかった切断 - 名前:{(data == null || data.PlayerName == null ? "(不明)" : data.PlayerName)}, 理由:{reason}, ping:{AmongUsClient.Instance.Ping}", "Session");
                ErrorText.Instance.AddError(AmongUsClient.Instance.GameState is InnerNetClient.GameStates.Started ? ErrorCode.OnPlayerLeftPostfixFailedInGame : ErrorCode.OnPlayerLeftPostfixFailedInLobby);
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (client?.Character?.PlayerId == 0)
                    _ = new LateTask(() => CheckPingPatch.Check = true, 10f, "Start Ping Check", true);
                OptionItem.SyncAllOptions();

                _ = new LateTask(() =>
                {
                    if (client.Character == null) return;
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);

                    if (client.Character == null) return;
                    var varsion = Main.PluginShowVersion;
                    var text = $"<size=80%>この部屋では\n<color={Main.ModColor}><size=180%><u><b>{Main.ModName}</color></b></size> v.{varsion}</u>\nを導入しております。<size=40%>\n\n</size>現在AmongUsでは、<color=#fc8803>公開ルームでのMod利用はできません</color><size=40%>\n\n";
                    var text2 = "</size><color=red>公開ルームからMod部屋へ勧誘/誘導をするのは<b>禁止</b>です</color>。<size=40%>\n<color=red>勧誘/誘導行為</color>にあった場合はスクリーンショット等と一緒に開発者にお知らせください。<color=red>Mod使えなくします</color>。";
                    var text3 = "</size>\n<size=60%>\n☆参加型配信を行ったり、オープンチャットやTwitterで募集するのは?\n<size=50%>→<color=#352ac9>全然大丈夫です!!やっちゃってください!!</color>\n　<color=#fc8803>バニラAmongUsの公開ルーム</color>での<color=red>宣伝/勧誘/誘導</color>がダメなのです!!</size>";
                    var text4 = "\n☆開発者から許可貰ってるって言ってる?　　\n<size=50%>→<color=#c9145a>個々で許可を出しておりません</color>!!大噓つきですよ!!</size>\n☆公開ルームに参加し、コード宣伝して「来てね～」って言うのは?\n<size=50%>→<color=red>勧誘/誘導</color>に当たるのでダメです。迷惑考えてくださいよ!!</size>";
                    Utils.SendMessage($"{text}{text2}{text3}{text4}", client.Character.PlayerId, $"<color={Main.ModColor}>【This Room Use \"Town Of Host-K\"】");

                    if (Options.AutoDisplayLastResult.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null && !Main.AssignSameRoles)
                        {
                            Main.isChatCommand = true;
                            UtilsGameLog.ShowLastResult(client.Character.PlayerId);
                        }
                    }
                    if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                    {
                        if (!GameStates.IsInGame && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            UtilsGameLog.ShowKillLog(client.Character.PlayerId);
                        }
                    }
                }, 3.0f, "Welcome Meg");
            }
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Update))]
    class CheckPingPatch
    {
        public static bool Check;
        public static void Postfix(InnerNetClient __instance)
        {
            if (Check)
                if (__instance.Ping >= 750)
                {
                    Logger.Warn($"接続が不安定", "Ping");
                    ErrorText.Instance.AddError(ErrorCode.CommunicationisUnstable);
                }
        }
    }
}
