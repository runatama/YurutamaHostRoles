using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Detective : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Detective),
            player => new Detective(player),
            RoleTypes.Detective,
            SetUpOptionItem,
            "#986f3a",
            from: From.AmongUs
        );
    public Detective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        detectivesuspectlimit = DetectiveSuspectLimit.GetFloat();
    }
    static OptionItem DetectiveSuspectLimit; static float detectivesuspectlimit;
    static void SetUpOptionItem()
    {
        DetectiveSuspectLimit = FloatOptionItem.Create(RoleInfo, 3, StringNames.DetectiveNotesSuspectNumber, new(1, 4, 1), 2, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.DetectiveSuspectLimit = detectivesuspectlimit;
    }
}