using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class EvilAddoer : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(EvilAddoer),
                player => new EvilAddoer(player),
                CustomRoles.EvilAddoer,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                12800,
                SetupOptionItem,
                "EA"
            );
        public EvilAddoer(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
        }
        private static void SetupOptionItem()
        {
            RoleAddAddons.Create(RoleInfo, 5);
        }
    }
}