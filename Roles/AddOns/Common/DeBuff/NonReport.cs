using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class NonReport
    {
        private static readonly int Id = 70000;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.NonReport);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｒ");
        public static List<byte> playerIdList = new();
        public static OptionItem OptionConvener;
        public static Convener Mode;
        public enum Convener
        {
            NotButton,
            NotReport,
            ConvenerAll,
        }
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.NonReport, fromtext: "<color=#000000>From:</color><color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.NonReport, true, true, true, true);
            OptionConvener = StringOptionItem.Create(50, "ConverMode", EnumHelper.GetAllNames<Convener>(), 0, TabGroup.Addons, false)
            .SetParentRole(CustomRoles.NonReport).SetParent(CustomRoleSpawnChances[CustomRoles.NonReport]);
        }
        public static void Init()
        {
            playerIdList = new();
            Mode = (Convener)OptionConvener.GetValue();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}