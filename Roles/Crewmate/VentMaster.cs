using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class VentMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentMaster),
            player => new VentMaster(player),
            CustomRoles.VentMaster,
            () => CanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            12600,
            (9, 4),
            SetUpOptionItem,
            "vm",
            "#ff6666",
            introSound: () => GetIntroSound(RoleTypes.Noisemaker)
        );
    public VentMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
    }
    static OptionItem CanUseVent;
    static void SetUpOptionItem()
    {
        CanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        var user = physics.myPlayer;
        if (!user.Is(CustomRoles.VentMaster))
        {
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (seer.Is(CustomRoles.VentMaster) && seer.PlayerId != user.PlayerId)
                {
                    if (seer.IsAlive() && GameStates.IsInTask)
                        seer.KillFlash();
                }
            }
        }
        return true;
    }
}