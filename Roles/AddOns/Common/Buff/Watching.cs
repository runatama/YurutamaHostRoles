using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class watching
    {
        private static readonly int Id = 74900;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.watching);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∑");
        private static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.watching, fromtext: "<color=#000000>From:</color><color=#ff0000>TOR GM Edition</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.watching, true, true, true, true);
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