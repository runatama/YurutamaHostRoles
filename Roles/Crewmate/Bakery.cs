using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class Bakery : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bakery),
            player => new Bakery(player),
            CustomRoles.Bakery,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22006,
            null,
            "bak",
            "#e65151",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Bakery(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override void OnStartMeeting()
    {
        if (Player.IsAlive())
        {
            string BakeryTitle = $"{GetString("Message.BakeryTitle")}";
            Utils.SendMessage(GetString("Message.Bakery1"), title: "<color=#e65151>" + BakeryTitle);
        }
    }
}