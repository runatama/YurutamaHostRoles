using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Autopsy
    {
        private static readonly int Id = 71200;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Autopsy);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Å");
        public static List<byte> playerIdList = new();
        public static OptionItem CanSeeComms;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Autopsy, fromtext: "<color=#000000>From:</color><color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.Autopsy, true, true, true, true);
            CanSeeComms = BooleanOptionItem.Create(Id + 50, "CanseeComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]).SetParentRole(CustomRoles.Autopsy);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}