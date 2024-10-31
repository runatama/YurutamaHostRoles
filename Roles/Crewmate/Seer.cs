using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;
public sealed class Seer : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20500,
            SetupOptionItem,
            "se",
            "#61b26c",
            from: From.TheOtherRoles
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanseeComms = OptioACanSeeComms.GetBool();
    }
    private static bool CanseeComms;
    private static OptionItem OptioACanSeeComms;
    enum OptionName
    {
        CanseeComms,
    }
    private static void SetupOptionItem()
    {
        OptioACanSeeComms = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanseeComms, true, false);
    }
    public bool CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        return !Utils.IsActive(SystemTypes.Comms) || CanseeComms;
    }
}