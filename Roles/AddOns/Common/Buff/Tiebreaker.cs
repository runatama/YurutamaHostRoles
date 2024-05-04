using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Tiebreaker
    {
        private static readonly int Id = 75600;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Tiebreaker);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "т");
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Tiebreaker, fromtext: "<color=#ffffff>From:<color=#ff0000>The Other Roles</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.Tiebreaker, true, true, true, true);
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