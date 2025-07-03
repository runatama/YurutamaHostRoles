using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class NonReport
    {
        private static readonly int Id = 18600;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.NonReport);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｒ");
        public static List<byte> playerIdList = new();
        public static OptionItem OptionNonReportMode;
        public static NonReportMode Mode;
        public enum NonReportMode
        {
            NotButton,
            NotReport,
            NonReportModeAll,
        }
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.NonReport, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.NonReport, true, true, true, true);
            OptionNonReportMode = StringOptionItem.Create(50, "ConverMode", EnumHelper.GetAllNames<NonReportMode>(), 0, TabGroup.Addons, false)
            .SetParentRole(CustomRoles.NonReport).SetParent(CustomRoleSpawnChances[CustomRoles.NonReport]);
        }
        public static void Init()
        {
            playerIdList = new();
            Mode = (NonReportMode)OptionNonReportMode.GetValue();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}