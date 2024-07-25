using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class SlowStarter
    {
        private static readonly int Id = 75800;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.SlowStarter);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｓs");
        public static List<byte> playerIdList = new();
        private static OptionItem CanKill;
        public static int Guard;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.SlowStarter, new(1, 3, 1));
            AddOnsAssignData.Create(Id + 10, CustomRoles.SlowStarter, false, false, true, false);
            CanKill = FloatOptionItem.Create(Id + 50, "MafiaCankill", new(1, 3, 1), 2, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.SlowStarter]);
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
        public static bool CanUseKill()
        {
            if (PlayerState.AllPlayerStates == null) return false;
            int livingImpostorsNum = 0;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var role = pc.GetCustomRole();
                if (role.IsImpostor()) livingImpostorsNum++;
            }

            return livingImpostorsNum <= (CanKill.GetFloat() - 1);
        }
    }
}