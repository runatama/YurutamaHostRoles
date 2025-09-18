using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Crewmate;

public sealed class Merlin : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Merlin),
            player => new Merlin(player),
            CustomRoles.Merlin,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            15900,
            null,
            "mer",
            "#8cc2ff",
            (2, 1),
            combination: CombinationRoles.AssassinandMerlin,
            from: From.TownOfHost_K

        );
    public Merlin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public override void Add()
    {
        foreach (var impostor in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor) || player.GetCustomRole() is CustomRoles.Egoist))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, "#ff1919");
        }
        Assassin.MarlinIds.Add(Player.PlayerId);
    }
}