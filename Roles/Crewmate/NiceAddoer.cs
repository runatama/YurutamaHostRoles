using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class NiceAddoer : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(NiceAddoer),
                player => new NiceAddoer(player),
                CustomRoles.NiceAddoer,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                20000,
                SetupOptionItem,
                "NA",
                "#87cefa"
            );
        public NiceAddoer(PlayerControl player)
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