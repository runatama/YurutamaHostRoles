using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Guarding
    {
        private static readonly int Id = 16800;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Guarding);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "ζ");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionGuard;
        public static int Guard;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Guarding, fromtext: "<color=#000000>From:</color><color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.Guarding, true, true, true, true);
            OptionGuard = FloatOptionItem.Create(Id + 50, "AddGuardCount", new(1, 10, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Guarding])
            .SetParentRole(CustomRoles.Guarding);
        }
        public static void Init()
        {
            playerIdList = new();
            Guard = OptionGuard.GetInt();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    }
}