using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Ghost;
using Rewired;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        public static bool DoDisable => Options.DisableDevices.GetBool() || Options.IsStandardHAS;
        private static List<byte> DesyncComms = new();
        private static int frame = 0;
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

            if (player.Is(CustomRoles.InfoPoor) ||
                            (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) &&
                            data.GiveAddons.GetBool() && data.GiveInfoPoor.GetBool()))
                return i == null;

            //ここから
            if (player.Is(CustomRoles.MassMedia)) return i != null ? false : true;

            return (bool)(i != null ? i : false);
        }
        public static void FixedUpdate()
        {
            frame = frame == 3 ? 0 : ++frame;
            if (frame != 0) return;

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
                                if (Options.DisableSkeldAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                if (Options.DisableSkeldCamera.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                                if (AdminUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= UsableDistance();
                                if (LogAndCamUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= UsableDistance();
                                break;
                            case 1:
                                if (Options.DisableMiraHQAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                if (Options.DisableMiraHQDoorLog.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                                if (AdminUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= UsableDistance();
                                if (LogAndCamUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= UsableDistance();
                                break;
                            case 2:
                                if (Options.DisablePolusAdmin.GetBool())
                                {
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance() && (PlayerPos.y < -19.8f);
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance() && (PlayerPos.y < -19.8f);
                                }
                                if (Options.DisablePolusCamera.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                                if (Options.DisablePolusVital.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                                if (AdminUsecheck(pc))
                                {
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= UsableDistance() && (PlayerPos.y < -19.8f);
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= UsableDistance() && (PlayerPos.y < -19.8f);
                                }
                                if (LogAndCamUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= UsableDistance();
                                if (VitealUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= UsableDistance();
                                break;
                            case 4:
                                if (Options.DisableAirshipCockpitAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                if (Options.DisableAirshipRecordsAdmin.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                                if (Options.DisableAirshipCamera.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                                if (Options.DisableAirshipVital.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                                if (AdminUsecheck(pc))
                                {
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= UsableDistance();
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= UsableDistance();
                                }
                                if (LogAndCamUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= UsableDistance();
                                if (VitealUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= UsableDistance();
                                break;
                            case 5:
                                if (Options.DisableFungleVital.GetBool())
                                    doComms |= Vector2.Distance(PlayerPos, DevicePos["FungleVital"]) <= UsableDistance();
                                if (VitealUsecheck(pc))
                                    RoleDisable |= Vector2.Distance(PlayerPos, DevicePos["FungleVital"]) <= UsableDistance();
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
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    class PCloseCam
    {
        public static void Postfix(PlanetSurveillanceMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();
        }
    }
    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    class SCloseCam
    {
        public static void Postfix(SurveillanceMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();
        }
    }

    [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
    class CloseLog
    {
        public static void Postfix(SecurityLogGame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();
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
            }
        }
    }
}