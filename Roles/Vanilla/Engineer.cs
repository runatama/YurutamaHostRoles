using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Engineer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Engineer),
            player => new Engineer(player),
            RoleTypes.Engineer,
            SetUpCustomOption,
            "#8cffff"
        );
    public Engineer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public static OptionItem EngineerCooldown;
    public static OptionItem EngineerInVentMaxTime;
    public static void SetUpCustomOption()
    {
        EngineerCooldown = FloatOptionItem.Create(RoleInfo, 203, "EngineerCooldown", new(0f, 180f, 2.5f), 15f, false)
        .SetValueFormat(OptionFormat.Seconds);
        EngineerInVentMaxTime = FloatOptionItem.Create(RoleInfo, 204, "EngineerInVentMaxTime", new(0f, 180f, 2.5f), 5f, false, infinity: true)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = EngineerCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = EngineerInVentMaxTime.GetFloat();
    }
}
