using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Amanojaku
    {
        private static readonly int Id = 19100;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Amanojaku);
        public static List<byte> playerIdList = new();
        public static OptionItem Amaday;
        public static OptionItem Seizon;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amanojaku);
            AmanojakuAssing.Create(Id + 10, CustomRoles.Amanojaku, true, true);
            Amaday = IntegerOptionItem.Create(Id + 50, "Amanojakut", new(1, 99, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amanojaku]).SetParentRole(CustomRoles.Amanojaku).SetValueFormat(OptionFormat.day);
            Seizon = BooleanOptionItem.Create(Id + 51, "AmanojakuSeizon", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amanojaku).SetParent(CustomRoleSpawnChances[CustomRoles.Amanojaku]);
        }

        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static bool CheckWin(PlayerControl pc, GameOverReason reason)
        {
            if (pc.IsRiaju()) return false;

            if (playerIdList.Contains(pc.PlayerId))
            {
                if (reason.Equals(GameOverReason.CrewmatesByTask) || reason.Equals(GameOverReason.CrewmatesByVote)) goto remove;
                if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveOpportunist.GetBool()) goto remove;
                if (!pc.IsAlive() && Seizon.GetBool()) goto remove;

                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Amanojaku);
                return true;
            }

            return false;

        remove:
            CustomWinnerHolder.IdRemoveLovers.Add(pc.PlayerId);
            return false;
        }
    }
}