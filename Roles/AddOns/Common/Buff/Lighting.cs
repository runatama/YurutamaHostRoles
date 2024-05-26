using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Lighting
    {
        private static readonly int Id = 75300;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Lighting);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "＊");
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Lighting, fromtext: "<color=#ffffff>From:<color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignDataNotImp.Create(Id + 10, CustomRoles.Lighting, true, true, true);
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