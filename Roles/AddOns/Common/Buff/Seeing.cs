using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class seeing
    {
        private static readonly int Id = 17700;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.seeing);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "☯");
        public static List<byte> playerIdList = new();
        public static OptionItem CanSeeComms;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.seeing, fromtext: "<color=#000000>From:</color><color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.seeing, true, true, true, true);
            CanSeeComms = BooleanOptionItem.Create(Id + 50, "CanseeComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.seeing]).SetParentRole(CustomRoles.seeing);
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