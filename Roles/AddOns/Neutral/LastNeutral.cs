/*using TownOfHost.Roles.Core;
using static TownOfHost.Options;
namespace TownOfHost.Roles.AddOns.Neutral
{
    public static class LastNeutral
    {
        private static readonly int Id = 82000;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        public static OptionItem GiveGuesser;
        public static OptionItem GiveOpportunist;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.LastNeutral, new(1, 1, 1));
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral])
                .SetValueFormat(OptionFormat.Seconds);
            GiveGuesser = BooleanOptionItem.Create(Id + 11, "GiveGuesser", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            GiveOpportunist = BooleanOptionItem.Create(Id + 12, "GiveOpportunist", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown()
        {
            if (currentId == byte.MaxValue) return;
            Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
        }
        public static bool CanBeLastNeutral(PlayerControl pc)
        {
            if (!pc.IsAlive() || pc.Is(CustomRoles.LastNeutral) || !pc.Is(CustomRoleTypes.Neutral))
            {
                return false;
            }
            return true;
        }
        public static void SetSubRole()
        {
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastNeutral.IsPresent() || Main.AliveNeutalCount != 1)
                return;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (CanBeLastNeutral(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastNeutral);
                    Add(pc.PlayerId);
                    SetKillCooldown();
                    pc.SyncSettings();
                    Utils.NotifyRoles();
                    if (GiveGuesser.GetBool())
                    {
                        pc.RpcSetCustomRole(CustomRoles.Guesser);
                    }
                    break;
                }
            }
        }
    }
}*/