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
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            if (Main.NormalOptions.NumImpostors == 0 && GameStates.IsOnlineGame) Main.NormalOptions.NumImpostors = 1;
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
            foreach (var pc in Main.AllPlayerControls)
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

            if (AmongUsClient.Instance.AmHost && GameStates.InGame)
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

                        if (data.Character.Is(CustomRoles.ALovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.ALoversPlayers.ToArray())
                            {
                                Main.isALoversDead = true;
                                Main.ALoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.ALovers);
                            }
                        if (data.Character.Is(CustomRoles.BLovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.BLoversPlayers.ToArray())
                            {
                                Main.isBLoversDead = true;
                                Main.BLoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.BLovers);
                            }
                        if (data.Character.Is(CustomRoles.CLovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.CLoversPlayers.ToArray())
                            {
                                Main.isCLoversDead = true;
                                Main.CLoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.CLovers);
                            }
                        if (data.Character.Is(CustomRoles.DLovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.DLoversPlayers.ToArray())
                            {
                                Main.isDLoversDead = true;
                                Main.DLoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.DLovers);
                            }
                        if (data.Character.Is(CustomRoles.ELovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.ELoversPlayers.ToArray())
                            {
                                Main.isELoversDead = true;
                                Main.ELoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.ELovers);
                            }
                        if (data.Character.Is(CustomRoles.FLovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.FLoversPlayers.ToArray())
                            {
                                Main.isFLoversDead = true;
                                Main.FLoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.FLovers);
                            }
                        if (data.Character.Is(CustomRoles.GLovers) && !data.Character.Data.IsDead)
                            foreach (var lovers in Main.GLoversPlayers.ToArray())
                            {
                                Main.isGLoversDead = true;
                                Main.GLoversPlayers.Remove(lovers);
                                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.GLovers);
                            }
                        if (data.Character.Is(CustomRoles.MaLovers) && !data.Character.Data.IsDead)
                            foreach (var Mlovers in Main.MaMaLoversPlayers.ToArray())
                            {
                                Main.isMaLoversDead = true;
                                Main.MaMaLoversPlayers.Remove(Mlovers);
                                PlayerState.GetByPlayerId(Mlovers.PlayerId).RemoveSubRole(CustomRoles.MaLovers);
                            }
                        var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                        if (state.DeathReason == CustomDeathReason.etc) //死因が設定されていなかったら
                        {
                            state.DeathReason = CustomDeathReason.Disconnected;
                            Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Die]　{data.PlayerName} {GetString("DeathReason.Disconnected")}";
                        }
                        state.SetDead();
                        AntiBlackout.OnDisconnect(data.Character.Data);
                        PlayerGameOptionsSender.RemoveSender(data.Character);
                        Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true));
                        Utils.NotifyRoles(isForMeeting: true, NoCache: true);
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
                _ = new LateTask(() => CheckPingPatch.Check = true, 10f, "Start Ping Check");
                OptionItem.SyncAllOptions();
                _ = new LateTask(() =>
                {
                    if (client.Character == null) return;
                    if (AmongUsClient.Instance.IsGamePublic) Utils.SendMessage(string.Format(GetString("Message.AnnounceTOH-K"), Main.PluginVersion), client.Character.PlayerId);
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);
                }, 3f, "Welcome Message");
                if (Options.AutoDisplayLastResult.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null && !Main.AssignSameRoles)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowLastResult(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayLastRoles");
                }
                if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                {
                    _ = new LateTask(() =>
                    {
                        if (!GameStates.IsInGame && client.Character != null)
                        {
                            Main.isChatCommand = true;
                            Utils.ShowKillLog(client.Character.PlayerId);
                        }
                    }, 3f, "DisplayKillLog");
                }
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
