using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class GuardianAngel : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(GuardianAngel),
            player => new GuardianAngel(player),
            RoleTypes.GuardianAngel,
            null,
            assignInfo: new RoleAssignInfo(CustomRoles.GuardianAngel, CustomRoleTypes.Crewmate)
            {
                IsInitiallyAssignableCallBack =
                    () => false
            }
            , from: From.AmongUs
        );
    public GuardianAngel(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}
