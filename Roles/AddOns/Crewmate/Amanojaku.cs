using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Amanojaku
    {
        private static readonly int Id = 79500;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Amanojaku);
        public static List<byte> playerIdList = new();
        public static OptionItem Amaday;
        public static OptionItem Seizon;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amanojaku);
            AmanojakuAssing.Create(Id + 10, CustomRoles.Amanojaku, true, true);
            Amaday = IntegerOptionItem.Create(Id + 50, "Amanojakut", new(1, 99, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amanojaku]).SetValueFormat(OptionFormat.day);
            Seizon = BooleanOptionItem.Create(Id + 51, "AmanojakuSeizon", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amanojaku]);
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