using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public static class GhostNoiseSender
    {
        private static readonly int Id = 60500;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem Time;
        public static OptionItem NoisTime;
        public static Dictionary<byte, byte> Nois;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GhostNoiseSender);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.GhostNoiseSender, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "GhostButtonerCoolDown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostNoiseSender]);
            Time = FloatOptionItem.Create(Id + 3, "GhostNoiseSenderTime", new(1f, 180, 1), 7, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostNoiseSender]);
            NoisTime = FloatOptionItem.Create(Id + 4, "NoisemakerAlertDuration", new(0, 180, 1), 5, TabGroup.GhostRoles, false, true)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostNoiseSender]);
        }
        public static void Init()
        {
            playerIdList = new();
            Nois = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GhostNoiseSender))
            {
                if (!Nois.ContainsKey(pc.PlayerId))
                {
                    Nois[pc.PlayerId] = target.PlayerId;
                    _ = new LateTask(() => Nois.Remove(pc.PlayerId), Time.GetFloat(), "GhostNoiseSender");
                }
            }
        }
    }
}