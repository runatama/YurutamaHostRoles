using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Viper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Viper),
            player => new Viper(player),
            RoleTypes.Viper,
            SetUpCustomOption
            , from: From.AmongUs
        );
    public Viper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ViperDissolveTime = OptViperDissolveTime.GetFloat();
    }
    public static OptionItem OptViperDissolveTime; static float ViperDissolveTime;
    public static void SetUpCustomOption()
    {
        OptViperDissolveTime = FloatOptionItem.Create(RoleInfo, 3, StringNames.ViperDissolveTime, new(0, 180, 1), 15, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ViperDissolveTime = ViperDissolveTime;
    }
}