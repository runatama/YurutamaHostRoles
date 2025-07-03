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
        public static List<byte> playerIdList = new();
        public static OptionItem AssingDay;
        public static OptionItem SurvivetoWin;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amanojaku);
            AmanojakuAssing.Create(Id + 10, CustomRoles.Amanojaku, true, true);
            AssingDay = IntegerOptionItem.Create(Id + 50, "AmanojakuAssingDay", new(1, 99, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amanojaku]).SetParentRole(CustomRoles.Amanojaku).SetValueFormat(OptionFormat.day);
            SurvivetoWin = BooleanOptionItem.Create(Id + 51, "AmanojakuSurvivetoWin", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amanojaku).SetParent(CustomRoleSpawnChances[CustomRoles.Amanojaku]);
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
            if (pc.IsLovers()) return false;

            if (playerIdList.Contains(pc.PlayerId))
            {
                if (reason.Equals(GameOverReason.CrewmatesByTask) || reason.Equals(GameOverReason.CrewmatesByVote)) goto remove;
                if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveOpportunist.GetBool()) goto remove;
                if (!pc.IsAlive() && SurvivetoWin.GetBool()) goto remove;

                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Amanojaku);
                return true;
            }

            return false;

        remove:
            CustomWinnerHolder.CantWinPlayerIds.Add(pc.PlayerId);
            return false;
        }
    }
}