using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Autopsy
    {
        private static readonly int Id = 16600;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Autopsy);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Å");
        public static List<byte> playerIdList = new();
        public static OptionItem CanUseActiveComms;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Autopsy, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.Autopsy, true, true, true, true);
            CanUseActiveComms = BooleanOptionItem.Create(Id + 50, "CanUseActiveComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Autopsy]).SetParentRole(CustomRoles.Autopsy);
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