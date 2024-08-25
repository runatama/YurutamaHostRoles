using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Ghost;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        public static bool DoDisable => Options.DisableDevices.GetBool() || Options.IsStandardHAS || Options.TimeLimitDevices.GetBool() || Options.TarnTimeLimitDevice.GetBool();
        private static List<byte> DesyncComms = new();
        private static int frame = 0;

        //検知
        private static Dictionary<byte, Vector2> AdminPoss = new();
        private static Dictionary<byte, Vector2> LogPoss = new();
        private static Dictionary<byte, Vector2> VitalPoss = new();
        //タイマー
        public static float GameAdminTimer;
        public static float GameLogAndCamTimer;
        public static float GameVitalTimer;
        //ターンでのタイマー
        public static float TarnAdminTimer;
        public static float TarnLogAndCamTimer;
        public static float TarnVitalTimer;
        //カメラ検知用
        public static int UseCount;

        public static void Reset()
        {
            AdminPoss.Clear();
            LogPoss.Clear();
            VitalPoss.Clear();
            GameAdminTimer = 0;
            GameLogAndCamTimer = 0;
            GameVitalTimer = 0;
            TarnAdminTimer = 0;
            TarnLogAndCamTimer = 0;
            TarnVitalTimer = 0;
            UseCount = 0;
        }
        public static void StartMeeting()
        {
            AdminPoss.Clear();
            LogPoss.Clear();
            VitalPoss.Clear();
            TarnAdminTimer = 0;
            TarnLogAndCamTimer = 0;
            TarnVitalTimer = 0;
            UseCount = 0;
        }
        public static readonly Dictionary<string, Vector2> DevicePos = new()
        {
            ["SkeldAdmin"] = new(3.48f, -8.62f),
            ["SkeldCamera"] = new(-13.06f, -2.45f),
            ["MiraHQAdmin"] = new(21.02f, 19.09f),
            ["MiraHQDoorLog"] = new(16.22f, 5.82f),
            ["PolusLeftAdmin"] = new(22.80f, -21.52f),
            ["PolusRightAdmin"] = new(24.66f, -21.52f),
            ["PolusCamera"] = new(2.96f, -12.74f),
            ["PolusVital"] = new(26.70f, -15.94f),
            ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
            ["AirshipRecordsAdmin"] = new(19.89f, 12.60f),
            ["AirshipCamera"] = new(8.10f, -9.63f),
            ["AirshipVital"] = new(25.24f, -7.94f),
            ["FungleVital"] = new(-2.765f, -9.819f)
        };
        public static float UsableDistance()
        {
            var Map = (MapNames)Main.NormalOptions.MapId;
            return Map switch
            {
                MapNames.Skeld => 1.8f,
                MapNames.Mira => 2.4f,
                MapNames.Polus => 1.8f,
                //MapNames.Dleks => 1.5f,
                MapNames.Airship => 1.8f,
                MapNames.Fungle => 1.8f,
                _ => 0.0f
            };
        }
        //役職によって使えるか異なるならこっちつかーう
        //意味わからない処理すぎて笑えてくる。
        public static bool AdminUsecheck(PlayerControl player, bool? i = null)
        {
            if (player == null) return false;
            if (!player.IsAlive() && player.PlayerId == 0) return true;
            else if (!player.IsAlive()) return false;

            if (DemonicCrusher.DemUseAbility) return i != null ? false : true;

            if (Options.TimeLimitAdmin.GetFloat() != 0 && GameAdminTimer > Options.TimeLimitAdmin.GetFloat()) return i != null ? false : true;

            if (Options.TarnTimeLimitAdmin.GetFloat() != 0 && TarnAdminTimer > Options.TarnTimeLimitAdmin.GetFloat()) return i != null ? false : true;

            if (player.Is(CustomRoles.InfoPoor) ||
                (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) &&
                data.GiveAddons.GetBool() && data.GiveInfoPoor.GetBool()))
                return i != null ? false : true;

            if (player.Is(CustomRoles.MassMedia)) return i != null ? false : true;

            return (bool)(i != null ? i : false);
        }
        public static bool VitealUsecheck(PlayerControl player, bool? i = null)
        {
            if (player == null) return false;
            if (!player.IsAlive() && player.PlayerId == 0) return true;
            else if (!player.IsAlive()) return false;

            if (DemonicCrusher.DemUseAbility) return i != null ? false : true;

            if (Options.TimeLimitVital.GetFloat() != 0 && GameVitalTimer > Options.TimeLimitVital.GetFloat()) return i != null ? false : true;

            if (Options.TarnTimeLimitVital.GetFloat() != 0 && TarnVitalTimer > Options.TarnTimeLimitVital.GetFloat()) return i != null ? false : true;

            if (player.Is(CustomRoles.InfoPoor) ||
                            (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) &&
                            data.GiveAddons.GetBool() && data.GiveInfoPoor.GetBool()))
                return i == null;

            if (player.Is(CustomRoles.MassMedia)) return i != null ? false : true;

            return (bool)(i != null ? i : false);
        }

        public static bool LogAndCamUsecheck(PlayerControl player, bool? i = null)
        {
            if (player == null) return false;
            if (!player.IsAlive() && player.PlayerId == 0) return true;
            else if (!player.IsAlive()) return false;

            if (DemonicCrusher.DemUseAbility) return i != null ? false : true;

            if (Options.TimeLimitCamAndLog.GetFloat() != 0 && GameLogAndCamTimer > Options.TimeLimitCamAndLog.GetFloat()) return i != null ? false : true;

            if (Options.TarnTimeLimitCamAndLog.GetFloat() != 0 && TarnLogAndCamTimer > Options.TarnTimeLimitCamAndLog.GetFloat()) return i != null ? false : true;

            if (player.Is(CustomRoles.InfoPoor) ||
                            (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) &&
                            data.GiveAddons.GetBool() && data.GiveInfoPoor.GetBool()))
                return i == null;

            //ここから
            if (player.Is(CustomRoles.MassMedia)) return i != null ? false : true;

            return (bool)(i != null ? i : false);
        }
        public static void AdminTimer(PlayerControl pc, Vector2 pos)
        {
            if (AdminPoss.TryGetValue(pc.PlayerId, out var p))
            {
                if (p == pos)
                {
                    if (Options.TimeLimitDevices.GetBool()) GameAdminTimer += Time.fixedDeltaTime;
                    if (Options.TarnTimeLimitDevice.GetBool()) TarnAdminTimer += Time.fixedDeltaTime;
                }
                else AdminPoss[pc.PlayerId] = pos;
            }
            else AdminPoss.TryAdd(pc.PlayerId, pos);
        }
        public static void LogTimer(PlayerControl pc, Vector2 pos)
        {
            if (LogPoss.TryGetValue(pc.PlayerId, out var p))
            {
                if (p == pos)
                {
                    if (Options.TimeLimitDevices.GetBool()) GameLogAndCamTimer += Time.fixedDeltaTime;
                    if (Options.TarnTimeLimitDevice.GetBool()) TarnLogAndCamTimer += Time.fixedDeltaTime;
                }
                else LogPoss[pc.PlayerId] = pos;
            }
            else LogPoss.TryAdd(pc.PlayerId, pos);
        }
        public static void VitalTimer(PlayerControl pc, Vector2 pos)
        {
            if (VitalPoss.TryGetValue(pc.PlayerId, out var p))
            {
                if (p == pos)
                {
                    if (Options.TimeLimitDevices.GetBool()) GameVitalTimer += Time.fixedDeltaTime;
                    if (Options.TarnTimeLimitDevice.GetBool()) TarnVitalTimer += Time.fixedDeltaTime;
                }
                else VitalPoss[pc.PlayerId] = pos;
            }
            else VitalPoss.TryAdd(pc.PlayerId, pos);
        }
        public static void FixedUpdate()
        {
            frame = frame == 3 ? 0 : ++frame;
            //if (frame != 0) return;

            //if (!DoDisable) return;
            foreach (var pc in Main.AllPlayerControls)
            {
                try
                {
                    if (pc.IsModClient()) continue;

                    bool doComms = false;
                    bool RoleDisable = false;
                    Vector2 PlayerPos = pc.GetTruePosition();
                    bool ignore = !DoDisable &&
                            ((Options.DisableDevicesIgnoreImpostors.GetBool() && pc.Is(CustomRoleTypes.Impostor)) ||
                            (Options.DisableDevicesIgnoreMadmates.GetBool() && pc.Is(CustomRoleTypes.Madmate)) ||
                            (Options.DisableDevicesIgnoreNeutrals.GetBool() && pc.Is(CustomRoleTypes.Neutral)) ||
                            (Options.DisableDevicesIgnoreCrewmates.GetBool() && pc.Is(CustomRoleTypes.Crewmate)) ||
                            (Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied));

                    if (pc.IsAlive() && !Utils.IsActive(SystemTypes.Comms))
                    {
                        switch (Main.NormalOptions.MapId)
                        {
                            case 0:
                                if (Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableSkeldAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableSkeldCamera.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                }
                                break;
                            case 1:
                                if (Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableMiraHQAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableMiraHQDoorLog.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) LogTimer(pc, PlayerPos);
                                }
                                break;
                            case 2:
                                if ((Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance() && (PlayerPos.y < -19.8f)) || (Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance() && (PlayerPos.y < -19.8f)))
                                {
                                    doComms |= Options.DisablePolusAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisablePolusCamera.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance() && (PlayerPos.y < -15.8f))
                                {
                                    doComms |= Options.DisablePolusVital.GetBool();
                                    RoleDisable |= VitealUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) VitalTimer(pc, PlayerPos);
                                }
                                break;
                            case 4:
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableAirshipCockpitAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableAirshipRecordsAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableAirshipCamera.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableAirshipVital.GetBool();
                                    RoleDisable |= VitealUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) VitalTimer(pc, PlayerPos);
                                }
                                break;
                            case 5:
                                if (Vector2.Distance(PlayerPos, DevicePos["FungleVital"]) <= UsableDistance())
                                {
                                    doComms |= Options.DisableFungleVital.GetBool();
                                    RoleDisable |= VitealUsecheck(pc);
                                    if (!pc.inVent && pc.CanMove && !doComms && !RoleDisable) VitalTimer(pc, PlayerPos);
                                }
                                break;
                        }
                    }
                    doComms &= !ignore;
                    if ((RoleDisable || doComms) && !pc.inVent && GameStates.IsInTask)
                    {
                        if (!DesyncComms.Contains(pc.PlayerId))
                            DesyncComms.Add(pc.PlayerId);

                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 128);
                    }
                    else if (!Utils.IsActive(SystemTypes.Comms) && DesyncComms.Contains(pc.PlayerId))
                    {
                        DesyncComms.Remove(pc.PlayerId);
                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 16);

                        if (Main.NormalOptions.MapId is 1 or 5)
                            pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 17);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "DisableDevice");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveDisableDevicesPatch
    {
        public static void Postfix()
        {
            if (GameStates.IsFreePlay && Main.EditMode)
                GameObject.FindObjectsOfType<SystemConsole>(true).DoIf(x => x.name == "TaskAddConsole", x => x.gameObject.SetActive(false));

            UpdateDisableDevices();
        }

        public static void UpdateDisableDevices(bool kyouseikousin = false)
        {
            var player = PlayerControl.LocalPlayer;
            bool ignore = player.Is(CustomRoles.GM) ||
                !player.IsAlive() ||
                !Options.DisableDevices.GetBool() ||
                (Options.DisableDevicesIgnoreImpostors.GetBool() && player.Is(CustomRoleTypes.Impostor)) ||
                (Options.DisableDevicesIgnoreMadmates.GetBool() && player.Is(CustomRoleTypes.Madmate)) ||
                (Options.DisableDevicesIgnoreNeutrals.GetBool() && player.Is(CustomRoleTypes.Neutral)) ||
                (Options.DisableDevicesIgnoreCrewmates.GetBool() && player.Is(CustomRoleTypes.Crewmate)) ||
                (Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool() && GameStates.AlreadyDied);
            var admins = GameObject.FindObjectsOfType<MapConsole>(true);
            var consoles = GameObject.FindObjectsOfType<SystemConsole>(true);
            if (admins == null || consoles == null) return;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    if (Options.DisableSkeldAdmin.GetBool() || DisableDevice.AdminUsecheck(player) || kyouseikousin)
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = DisableDevice.AdminUsecheck(player, ignore);
                    if (Options.DisableSkeldCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = DisableDevice.LogAndCamUsecheck(player, ignore));
                    break;
                case 1:
                    if (Options.DisableMiraHQAdmin.GetBool() || DisableDevice.AdminUsecheck(player) || kyouseikousin)
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = DisableDevice.AdminUsecheck(player, ignore);
                    if (Options.DisableMiraHQDoorLog.GetBool() || DisableDevice.LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = DisableDevice.LogAndCamUsecheck(player, ignore));
                    break;
                case 2:
                    if (Options.DisablePolusAdmin.GetBool() || DisableDevice.AdminUsecheck(player) || kyouseikousin)
                        admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = DisableDevice.AdminUsecheck(player, ignore));
                    if (Options.DisablePolusCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = DisableDevice.LogAndCamUsecheck(player, ignore));
                    if (Options.DisablePolusVital.GetBool() || DisableDevice.VitealUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = DisableDevice.VitealUsecheck(player, ignore));
                    break;
                case 4:
                    admins.Do(x =>
                    {
                        if (((Options.DisableAirshipCockpitAdmin.GetBool() || DisableDevice.AdminUsecheck(player) || kyouseikousin) && x.name == "panel_cockpit_map") ||
                            ((Options.DisableAirshipRecordsAdmin.GetBool() || DisableDevice.AdminUsecheck(player) || kyouseikousin) && x.name == "records_admin_map"))
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = DisableDevice.AdminUsecheck(player, ignore);
                    });
                    if (Options.DisableAirshipCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = DisableDevice.LogAndCamUsecheck(player, ignore));
                    if (Options.DisableAirshipVital.GetBool() || DisableDevice.VitealUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = DisableDevice.VitealUsecheck(player, ignore));
                    break;
                case 5:
                    if (Options.DisableFungleVital.GetBool() || DisableDevice.VitealUsecheck(player) || kyouseikousin)
                    {
                        consoles.DoIf(x => x.name == "VitalsConsole", x => x.GetComponent<Collider2D>().enabled = DisableDevice.VitealUsecheck(player, ignore));
                    }
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    class CloseVitals
    {
        public static void Postfix(VitalsMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();

                if (PlayerControl.LocalPlayer.IsAlive())
                {
                    var ch = true;
                    if (Options.TimeLimitVital.GetFloat() != 0 && DisableDevice.GameVitalTimer > Options.TimeLimitVital.GetFloat())
                    {
                        __instance.Close();
                        ch = false;
                    }

                    if (Options.TarnTimeLimitVital.GetFloat() != 0 && DisableDevice.TarnVitalTimer > Options.TarnTimeLimitVital.GetFloat())
                    {
                        __instance.Close();
                        ch = false;
                    }
                    if (ch)
                    {
                        if (Options.TimeLimitDevices.GetBool()) DisableDevice.GameVitalTimer += Time.fixedDeltaTime;
                        if (Options.TarnTimeLimitDevice.GetBool()) DisableDevice.TarnVitalTimer += Time.fixedDeltaTime;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    class PCloseCam
    {
        public static void Postfix(PlanetSurveillanceMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();
                if (PlayerControl.LocalPlayer.IsAlive() && __instance)
                {
                    if (Options.TimeLimitCamAndLog.GetFloat() != 0 && DisableDevice.GameLogAndCamTimer > Options.TimeLimitCamAndLog.GetFloat())
                    {
                        __instance.Close();
                    }

                    if (Options.TarnTimeLimitCamAndLog.GetFloat() != 0 && DisableDevice.TarnLogAndCamTimer > Options.TarnTimeLimitCamAndLog.GetFloat())
                    {
                        __instance.Close();
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    class SCloseCam
    {
        public static void Postfix(SurveillanceMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();

                if (PlayerControl.LocalPlayer.IsAlive() && __instance)
                {
                    if (Options.TimeLimitCamAndLog.GetFloat() != 0 && DisableDevice.GameLogAndCamTimer > Options.TimeLimitCamAndLog.GetFloat())
                        __instance.Close();

                    if (Options.TarnTimeLimitCamAndLog.GetFloat() != 0 && DisableDevice.TarnLogAndCamTimer > Options.TarnTimeLimitCamAndLog.GetFloat())
                        __instance.Close();
                }
            }
        }
    }

    [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
    class CloseLog
    {
        public static void Postfix(SecurityLogGame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();

                if (PlayerControl.LocalPlayer.IsAlive() && __instance)
                {
                    var ch = true;
                    if (Options.TimeLimitCamAndLog.GetFloat() != 0 && DisableDevice.GameLogAndCamTimer > Options.TimeLimitCamAndLog.GetFloat())
                    {
                        __instance.Close();
                        ch = false;
                    }

                    if (Options.TarnTimeLimitCamAndLog.GetFloat() != 0 && DisableDevice.TarnLogAndCamTimer > Options.TarnTimeLimitCamAndLog.GetFloat())
                    {
                        __instance.Close();
                        ch = false;
                    }
                    if (ch)
                    {
                        if (Options.TimeLimitDevices.GetBool()) DisableDevice.GameLogAndCamTimer += Time.fixedDeltaTime;
                        if (Options.TarnTimeLimitDevice.GetBool()) DisableDevice.TarnLogAndCamTimer += Time.fixedDeltaTime;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    class CloseAddmin
    {
        public static void Prefix(MapCountOverlay __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility)
                {
                    MapBehaviour.Instance.Close();
                    return;
                }
                if (PlayerControl.LocalPlayer.IsAlive() && MapBehaviour.Instance && __instance)
                {
                    var ch = true;
                    if (Options.TimeLimitAdmin.GetFloat() != 0 && DisableDevice.GameAdminTimer > Options.TimeLimitAdmin.GetFloat())
                    {
                        MapBehaviour.Instance.Close();
                        ch = false;
                    }

                    if (Options.TarnTimeLimitAdmin.GetFloat() != 0 && DisableDevice.TarnAdminTimer > Options.TarnTimeLimitAdmin.GetFloat())
                    {
                        MapBehaviour.Instance.Close();
                        ch = false;
                    }
                    if (ch)
                    {
                        if (Options.TimeLimitDevices.GetBool()) DisableDevice.GameAdminTimer += Time.fixedDeltaTime;
                        if (Options.TarnTimeLimitDevice.GetBool()) DisableDevice.TarnAdminTimer += Time.fixedDeltaTime;
                    }
                }
            }
        }
    }
}