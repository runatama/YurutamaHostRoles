using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class TimeThief : RoleBase, IMeetingTimeAlterable, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(TimeThief),
                player => new TimeThief(player),
                CustomRoles.TimeThief,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                2400,
                SetupOptionItem,
                "tt",
                from: From.TownOfHost
            );
        public TimeThief(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            DecreaseMeetingTime = OptionDecreaseMeetingTime.GetInt();
            ReturnStolenTimeUponDeath = OptionReturnStolenTimeUponDeath.GetBool();
        }
        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionDecreaseMeetingTime;
        private static OptionItem OptionReturnStolenTimeUponDeath;
        enum OptionName
        {
            TimeThiefDecreaseMeetingTime,
            TimeThiefReturnStolenTimeUponDeath
        }
        public static float KillCooldown;
        public static int DecreaseMeetingTime;
        public static bool ReturnStolenTimeUponDeath;

        public bool RevertOnDie => ReturnStolenTimeUponDeath;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionDecreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 1), 20, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 12, OptionName.TimeThiefReturnStolenTimeUponDeath, true, false);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public int CalculateMeetingTimeDelta()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return 0;
            var sec = -(DecreaseMeetingTime * MyState.GetKillCount(true));
            return sec;
        }
        public override string GetProgressText(bool comms = false)
        {
            var time = CalculateMeetingTimeDelta();
            return time < 0 ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"{time}s") : "";
        }
    }
}