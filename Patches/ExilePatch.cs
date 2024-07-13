using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Roles.Core;
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
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
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
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
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
            if (exiled != null)
            {
                var role = exiled.GetCustomRole();
                var info = role.GetRoleInfo();

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
                    Utils.NotifyRoles();
                }, 0.4f, "Res");
                _ = new LateTask(() =>
                {
                    foreach (var Player in Main.AllPlayerControls)
                    {
                        {
                            if (Player != PlayerControl.LocalPlayer)
                                foreach (var pc in Main.AllPlayerControls)
                                {
                                    var role = pc.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() ?? RoleTypes.Scientist;

                                    if (!pc.IsAlive())
                                        if (pc.GetCustomRole().IsImpostor() || ((pc.GetRoleClass() as Roles.Core.Interfaces.IKiller)?.CanUseSabotageButton() ?? false))
                                        {
                                            role = RoleTypes.ImpostorGhost;
                                        }
                                        else role = RoleTypes.CrewmateGhost;

                                    if (Player != pc && (pc.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false))
                                        role = !pc.IsAlive() ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate;

                                    if (pc.IsGorstRole()) role = RoleTypes.GuardianAngel;

                                    var IDesycImpostor = Player.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false;
                                    pc.RpcSetRoleDesync((IDesycImpostor && Player != pc) ? (!pc.IsAlive() ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate) : role, Player.GetClientId());
                                }
                            if (!Player.IsAlive()) Player.RpcExileV2();
                        }
                    }

                    _ = new LateTask(() =>
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            pc.ResetKillCooldown();
                        }
                        Utils.AfterMeetingTasks();
                        FallFromLadder.Reset();
                        Utils.CountAlivePlayers(true);

                        _ = new LateTask(() =>
                            {
                                Utils.NotifyRoles();
                                foreach (var kvp in PlayerState.AllPlayerStates)
                                {
                                    if (kvp.Value == null) continue;
                                    kvp.Value.IsBlackOut = false;
                                    Utils.MarkEveryoneDirtySettings();
                                }
                                Utils.SyncAllSettings();
                                foreach (var pc in Main.AllPlayerControls)
                                {
                                    if (pc)
                                    {
                                        pc.SetKillCooldown();
                                    }
                                }
                            }, 0.2f, "AfterMeetingNotifyRoles");

                    }, 0.2f, "");
                }, 0.7f, "");
            }

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

            if (RandomSpawn.IsRandomSpawn())
            {
                RandomSpawn.SpawnMap map;
                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 2:
                        map = new RandomSpawn.PolusSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 5:
                        map = new RandomSpawn.FungleSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }
            GameStates.task = true;
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
                        var player = Utils.GetPlayerById(x.Key);
                        var roleClass = CustomRoleManager.GetByPlayerId(x.Key);
                        var requireResetCam = player?.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true;
                        var state = PlayerState.GetByPlayerId(x.Key);
                        Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
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

            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            Logger.Info("タスクフェイズ開始", "Phase");
            foreach (var pc in Main.AllPlayerControls) pc.RpcResetAbilityCooldown();
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
