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
            40300,
            null,
            "mer",
            "#8cc2ff",
            combination: CombinationRoles.AssassinandMerlin
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
        foreach (var impostor in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, impostor.GetRoleColorCode());
        }
        Assassin.MarlinIds.Add(Player.PlayerId);
    }
    //もし設定など入れたい場合は
    //あああ = Assassin.設定名
    //って感じにこっちに持ってくればよい(←わかりずらい)
}