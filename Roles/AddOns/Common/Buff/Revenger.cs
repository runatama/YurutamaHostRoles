using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Revenger
    {
        private static readonly int Id = 17600;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Revenger);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "я");
        public static List<byte> playerIdList = new();
        public static OptionItem Imp;
        public static OptionItem Crew;
        public static OptionItem Mad;
        public static OptionItem Neu;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Revenger, fromtext: "<color=#000000>From:</color><color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.Revenger, true, true, true, true);
            Imp = BooleanOptionItem.Create(Id + 50, "NekoKabochaImpostorsGetRevenged", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Revenger).SetParent(CustomRoleSpawnChances[CustomRoles.Revenger]);
            Crew = BooleanOptionItem.Create(Id + 51, "NekomataCanCrew", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Revenger).SetParent(CustomRoleSpawnChances[CustomRoles.Revenger]);
            Mad = BooleanOptionItem.Create(Id + 52, "NekoKabochaMadmatesGetRevenged", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Revenger).SetParent(CustomRoleSpawnChances[CustomRoles.Revenger]);
            Neu = BooleanOptionItem.Create(Id + 53, "NekomataCanNeu", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Revenger).SetParent(CustomRoleSpawnChances[CustomRoles.Revenger]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}