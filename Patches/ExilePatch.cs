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
        public static float SpawnTimer = 0;
        public static bool AllSpawned = false;
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
            if (AntiBlackout.OverrideExiledPlayer)
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
                if (!AntiBlackout.OverrideExiledPlayer && info?.IsDesyncImpostor == true)
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
                }, 0.4f, "Res");//ラグを考慮して遅延入れる。
                _ = new LateTask(() =>
                {
                    foreach (var Player in PlayerCatch.AllPlayerControls)//役職判定を戻す。
                    {
                        if (Player != PlayerControl.LocalPlayer)
                            foreach (var pc in PlayerCatch.AllPlayerControls)
                            {
                                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool())
                                {
                                    pc.RpcSetRoleDesync(RoleTypes.Crewmate, Player.PlayerId);
                                }
                                var customrole = pc.GetCustomRole();
                                var roleinfo = customrole.GetRoleInfo();
                                var role = roleinfo?.BaseRoleType.Invoke() ?? RoleTypes.Scientist;
                                var isalive = pc.IsAlive();
                                if (!isalive)
                                    if (customrole.IsImpostor() || ((pc.GetRoleClass() as IKiller)?.CanUseSabotageButton() ?? false))
                                    {
                                        role = RoleTypes.ImpostorGhost;
                                    }
                                    else role = RoleTypes.CrewmateGhost;
                                if (Player != pc && (roleinfo?.IsDesyncImpostor ?? false))
                                    role = !isalive ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate;
                                if (pc.IsGorstRole()) role = RoleTypes.GuardianAngel;

                                var IDesycImpostor = Player.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false;
                                if (SuddenDeathMode.NowSuddenDeathMode) IDesycImpostor = true;

                                if (pc.Is(CustomRoles.Amnesia))
                                {
                                    if (roleinfo?.IsDesyncImpostor == true && !pc.Is(CustomRoleTypes.Impostor))
                                        role = RoleTypes.Crewmate;

                                    if (Amnesia.dontcanUseability)
                                    {
                                        if (pc.Is(CustomRoleTypes.Impostor))
                                            role = RoleTypes.Impostor;
                                        else role = RoleTypes.Crewmate;
                                    }
                                }

                                pc.RpcSetRoleDesync((IDesycImpostor && Player != pc) ? (!isalive ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate) : role, Player.GetClientId());
                            }

                        if (Player.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) Player.RpcExileV2();

                        Player.ResetKillCooldown();
                        Player.SyncSettings();
                        _ = new LateTask(() =>
                            {
                                Player.SetKillCooldown(kyousei: true, delay: true);
                                if (Player.IsAlive())
                                {
                                    var roleclass = Player.GetRoleClass();
                                    (roleclass as IUseTheShButton)?.ResetS(Player);
                                    (roleclass as IUsePhantomButton)?.Init(Player);
                                }
                                else
                                {
                                    Player.RpcExileV2();
                                    if (Player.IsGorstRole()) Player.RpcSetRole(RoleTypes.GuardianAngel, true);
                                }
                            }, Main.LagTime, "", true);
                    }
                    _ = new LateTask(() =>
                        {
                            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                            foreach (var kvp in PlayerState.AllPlayerStates)
                            {
                                kvp.Value.IsBlackOut = false;
                            }
                            UtilsOption.SyncAllSettings();
                            ExtendedPlayerControl.RpcResetAbilityCooldownAllPlayer();
                            if (Options.ExAftermeetingflash.GetBool()) Utils.AllPlayerKillFlash();
                        }, Main.LagTime * 2, "AfterMeetingNotifyRoles");
                }, 0.7f, "", true);

                //OFFならリセ
                /*if (!Options.AntiBlackOutSpawnVer.GetBool())
                {
                    foreach (var state in PlayerState.AllPlayerStates.Values)
                    {
                        if (state == null) continue;
                        state.TeleportedWithAntiBlackout = true;
                    }
                    AllSpawned = true;
                }*/

                if (Options.BlackOutwokesitobasu.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (!pc.IsModClient())
                            {
                                var role = pc.GetCustomRole().GetRoleTypes();
                                var pos = pc.transform.position;
                                pc.RpcSnapToDesync(pc, new UnityEngine.Vector2(9999, 9999));
                                pc.ResetPlayerCam();
                                _ = new LateTask(() =>
                                {
                                    if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship)
                                        RandomSpawn.AirshipSpawn(pc);
                                    else
                                        pc.RpcSnapToForced(pos);
                                    PlayerState.GetByPlayerId(pc.PlayerId).HasSpawned = true;
                                    pc.RpcSetRoleDesync(role, pc.GetClientId());
                                }, 0.65f);
                            }
                        }
                    }, 0.25f);
                }
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
            GameStates.task = true;
            FallFromLadder.Reset();
            PlayerCatch.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            UtilsOption.SyncAllSettings();
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
                    if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }
                }, 0.5f, "Restore IsDead Task");
                _ = new LateTask(() =>
                {
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
                }, 0.5f, "AfterMeetingDeathPlayers Task");
            }

            GameStates.AlreadyDied |= !PlayerCatch.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            Logger.Info("タスクフェイズ開始", "Phase");
            ExtendedPlayerControl.RpcResetAbilityCooldownAllPlayer();
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