using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Serial
    {
        private static readonly int Id = 17800;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Serial);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∂");
        public static List<byte> playerIdList = new();
        public static OptionItem KillCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Serial);
            AddOnsAssignDataOnlyKiller.Create(Id + 10, CustomRoles.Serial, true, true, true, true);
            KillCooldown = FloatOptionItem.Create(Id + 50, "KillCooldown", new(0f, 180f, 0.5f), 25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Serial])
                .SetValueFormat(OptionFormat.Seconds).SetParentRole(CustomRoles.Serial);
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