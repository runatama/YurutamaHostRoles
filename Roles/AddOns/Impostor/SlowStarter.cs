using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class SlowStarter
    {
        private static readonly int Id = 75800;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.SlowStarter);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｓs");
        public static List<byte> playerIdList = new();
        private static OptionItem CanKill;
        private static OptionItem CanKillDay;
        public static bool cankill;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.SlowStarter, new(1, 3, 1));
            AddOnsAssignData.Create(Id + 10, CustomRoles.SlowStarter, false, false, true, false);
            CanKill = FloatOptionItem.Create(Id + 50, "MafiaCankill", new(1, 3, 1), 2, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.SlowStarter]);
            CanKillDay = FloatOptionItem.Create(Id + 51, "MafiaCanKillDay", new(0, 30, 1), 0, TabGroup.Addons, false, infinity: null).SetParent(CustomRoleSpawnChances[CustomRoles.SlowStarter]).SetValueFormat(OptionFormat.day);
        }
        public static void Init()
        {
            cankill = false;
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool CanUseKill()
        {
            if (PlayerState.AllPlayerStates == null) return false;
            int livingImpostorsNum = 0;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc == null) continue;
                var role = pc.GetCustomRole();
                if (role.IsImpostor()) livingImpostorsNum++;
            }

            return livingImpostorsNum <= CanKill.GetFloat() || cankill;
        }
        public static void OnStartMeeting()
        {
            if (CanKillDay.GetFloat() == 0) return;

            if (CanKillDay.GetFloat() <= Main.day) cankill = true;
        }
    }
}