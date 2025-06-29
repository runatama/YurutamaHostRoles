using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Management
    {
        private static readonly int Id = 17200;
        public static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Management);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "θ");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionPercentGage;
        private static OptionItem Optioncomms;
        public static bool comms;
        public static bool PercentGage;
        public static OptionItem Meeting;
        public static OptionItem PonkotuPercernt;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Management, fromtext: "<color=#000000>From:</color><color=#ffff00>TownOfHost_Y</color></size>");
            AddOnsAssignData.Create(Id + 10, CustomRoles.Management, true, true, true, true);
            OptionPercentGage = BooleanOptionItem.Create(Id + 50, "PercentGage", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Management]).SetParentRole(CustomRoles.Management);
            PonkotuPercernt = BooleanOptionItem.Create(Id + 51, "PonkotuPercernt", true, TabGroup.Addons, false).SetParent(OptionPercentGage).SetParentRole(CustomRoles.Management);
            Optioncomms = BooleanOptionItem.Create(Id + 55, "CanseeComms", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Management]).SetParentRole(CustomRoles.Management);
            Meeting = BooleanOptionItem.Create(Id + 56, "CanseeMeeting", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Management]).SetParentRole(CustomRoles.Management);
        }
        public static void Init()
        {
            playerIdList = new();
            PercentGage = OptionPercentGage.GetBool();
            comms = Optioncomms.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}