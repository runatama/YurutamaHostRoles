using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        public static NetworkedPlayerInfo AntiBlackout_LastExiled;

        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.initData.networkedPlayer);
                }
                finally
                {
                    WrapUpFinalizer(__instance.initData.networkedPlayer);
                }
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.initData.networkedPlayer);
                }
                finally
                {
                    WrapUpFinalizer(__instance.initData.networkedPlayer);
                }
            }
        }
        static void WrapUpPostfix(NetworkedPlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer())
            {
                exiled = AntiBlackout_LastExiled;
            }

            var mapId = Main.NormalOptions.MapId;

            // エアシップではまだ湧かない
            if ((MapNames)mapId != MapNames.Airship)
            {
                foreach (var state in PlayerState.AllPlayerStates.Values)
                {
                    state.HasSpawned = true;
                }
            }

            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            //AntiBlackout.RestoreIsDead(doSend: false);
            //AntiBlackout.RestoreSetRole();
            if (exiled != null)
            {
                var role = exiled.GetCustomRole();
                var info = role.GetRoleInfo();
                //霊界用暗転バグ対処
                if (!AntiBlackout.OverrideExiledPlayer() && info?.IsDesyncImpostor == true)
                    exiled.Object?.ResetPlayerCam(3f);

                exiled.IsDead = true;
                PlayerState.GetByPlayerId(exiled.PlayerId).DeathReason = CustomDeathReason.Vote;

                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                {
                    roleClass.OnExileWrapUp(exiled, ref DecidedWinner);
                }

                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) PlayerState.GetByPlayerId(exiled.PlayerId).SetDead();
            }
            AfterMeetingTasks();
        }
        public static void AfterMeetingTasks()
        {
            //霊界用暗転バグ処置(移設)
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    AntiBlackout.RestoreIsDead(doSend: false);
                }, 0.2f, "Res");//ラグを考慮して遅延入れる。
                _ = new LateTask(() =>
                {
                    if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
                    foreach (var Player in PlayerCatch.AllPlayerControls)//役職判定を戻す。
                    {
                        if (Player.GetClient() is null)
                        {
                            Logger.Error($"{Player?.Data?.PlayerName ?? "???"}のclientがnull", "ExiledSetRole");
                            continue;
                        }
                        var sender = CustomRpcSender.Create("ExiledSetRole", Hazel.SendOption.Reliable);
                        sender.StartMessage(Player.GetClientId());
                        if (Player != PlayerControl.LocalPlayer)
                        {
                            foreach (var pc in PlayerCatch.AllPlayerControls)
                            {
                                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool())
                                {
                                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                                    .Write((ushort)RoleTypes.Crewmate)
                                    .Write(true)
                                    .EndRpc();
                                    continue;
                                }
                                var customrole = pc.GetCustomRole();
                                var roleinfo = customrole.GetRoleInfo();
                                var role = roleinfo?.BaseRoleType.Invoke() ?? RoleTypes.Scientist;
                                var isalive = pc.IsAlive();
                                if (!isalive)
                                {
                                    role = customrole.IsImpostor() || ((pc.GetRoleClass() as IKiller)?.CanUseSabotageButton() ?? false) ?
                                            RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost;
                                }

                                if (Player != pc && (roleinfo?.IsDesyncImpostor ?? false))
                                    role = !isalive ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate;

                                var IDesycImpostor = Player.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false;
                                IDesycImpostor |= SuddenDeathMode.NowSuddenDeathMode;

                                if (pc.Is(CustomRoles.Amnesia))
                                {
                                    if (roleinfo?.IsDesyncImpostor == true && !pc.Is(CustomRoleTypes.Impostor))
                                        role = RoleTypes.Crewmate;
                                    if (Amnesia.dontcanUseability)
                                    {
                                        role = pc.Is(CustomRoleTypes.Impostor) ? RoleTypes.Impostor : RoleTypes.Crewmate;
                                    }
                                }
                                var setrole = (IDesycImpostor && Player != pc) ? (!isalive ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate) : role;

                                sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                                .Write((ushort)setrole)
                                .Write(true)
                                .EndRpc();
                            }
                            sender.EndMessage();
                            sender.SendMessage();
                            Player.Revive();
                        }

                        Player.ResetKillCooldown();
                        Player.PlayerId.GetPlayerState().IsBlackOut = false;
                        Player.SyncSettings();
                        _ = new LateTask(() =>
                            {
                                Player.SetKillCooldown(kyousei: true, delay: true);
                                if (Player.IsAlive() && !(Player.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()))
                                {
                                    var roleclass = Player.GetRoleClass();
                                    (roleclass as IUseTheShButton)?.ResetS(Player);
                                    (roleclass as IUsePhantomButton)?.Init(Player);
                                }
                                else
                                {
                                    Player.RpcExileV2();
                                    if (Player.IsGhostRole()) Player.RpcSetRole(RoleTypes.GuardianAngel, true);
                                }
                            }, Main.LagTime, "", true);
                    }
                    _ = new LateTask(() =>
                        {
                            Twins.TwinsSuicide();
                            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
                            UtilsNotifyRoles.NotifyRoles(true, true);
                            ExtendedPlayerControl.RpcResetAbilityCooldownAllPlayer(false);
                            if (Options.ExAftermeetingflash.GetBool()) Utils.AllPlayerKillFlash();
                        }, Main.LagTime * 2, "AfterMeetingNotifyRoles");
                }, 0.4f, "", true);
            }

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                pc.ResetKillCooldown();
            }
            if (RandomSpawn.IsRandomSpawn())
            {
                RandomSpawn.SpawnMap map;
                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 2:
                        map = new RandomSpawn.PolusSpawnMap();
                        PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 5:
                        map = new RandomSpawn.FungleSpawnMap();
                        PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }
            FallFromLadder.Reset();
            PlayerCatch.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            if (Main.NormalOptions.MapId != 4)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    pc.GetRoleClass()?.OnSpawn();
                }
            }
        }

        static void WrapUpFinalizer(NetworkedPlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    AntiBlackout.SendGameData();
                }, 0.28f, "Restore IsDead Task");
                _ = new LateTask(() =>
                {
                    if (AntiBlackout.OverrideExiledPlayer() && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }
                    Main.AfterMeetingDeathPlayers.Do(x =>
                    {
                        var player = PlayerCatch.GetPlayerById(x.Key);
                        var roleClass = CustomRoleManager.GetByPlayerId(x.Key);
                        var requireResetCam = player?.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true;
                        var state = PlayerState.GetByPlayerId(x.Key);
                        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                        state.DeathReason = x.Value;
                        state.SetDead();
                        player?.RpcExileV2();
                        if (x.Value == CustomDeathReason.Suicide)
                            player?.SetRealKiller(player, true);
                        if (requireResetCam)
                            player?.ResetPlayerCam(1f);
                        if (roleClass is Executioner executioner && executioner.TargetId == x.Key)
                            Executioner.ChangeRoleByTarget(x.Key);
                    });
                    Main.AfterMeetingDeathPlayers.Clear();
                }, 0.34f, "AfterMeetingDeathPlayers Task");
            }

            UtilsGameLog.WriteGameLog();
            GameStates.AlreadyDied |= !PlayerCatch.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            GameStates.task = true;
            Logger.Info("タスクフェイズ開始", "Phase");
            //ExtendedPlayerControl.RpcResetAbilityCooldownAllPlayer();
            MeetingStates.First = false;
            _ = new LateTask(() => GameStates.Tuihou = false, 3f + Main.LagTime, "Tuihoufin");
        }
    }

    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class PolusExileHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
        }
    }
}