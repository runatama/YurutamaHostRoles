using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class ShapeMaster : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShapeMaster),
            player => new ShapeMaster(player),
            CustomRoles.ShapeMaster,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1200,
            SetupOptionItem,
            "sha",
            from: From.TownOfHost
        );
    public ShapeMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        shapeshiftDuration = OptionShapeshiftDuration.GetFloat();
        anime = OptionShapeAnime.GetBool();
    }
    private static OptionItem OptionShapeshiftDuration;
    static OptionItem OptionShapeAnime;
    enum OptionName
    {
        ShapeMasterShapeshiftDuration,
        animate
    }
    private static float shapeshiftDuration;
    static bool anime;
    public static void SetupOptionItem()
    {
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 10, OptionName.ShapeMasterShapeshiftDuration, new(1, 1000, 1), 10, false);
        OptionShapeAnime = BooleanOptionItem.Create(RoleInfo, 11, OptionName.animate, false, false);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 0f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterDuration = shapeshiftDuration;
    }
    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        animate = anime;
        return true;
    }
}
