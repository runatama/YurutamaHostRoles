using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;

public sealed class Monochromer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Monochromer),
            player => new Monochromer(player),
            CustomRoles.Monochromer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50800,
            SetupOptionItem,
            "Mc",
            "#808080"
        );
    public Monochromer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanseeKiller = OpCanseeKiller.GetBool();
        color = OpCanseeRoleColor.GetBool();
    }
    private static OptionItem Kurosiro;
    private static OptionItem HasImpostorVision;
    private static OptionItem OpCanseeKiller;
    private static OptionItem OpCanseeRoleColor;
    bool CanseeKiller;
    bool color;
    enum Option
    {
        MonochromerMonochro,
        MonochromerCanseeKiller,
        MonochromerMarkColor
    }
    private static void SetupOptionItem()
    {
        Kurosiro = BooleanOptionItem.Create(RoleInfo, 5, Option.MonochromerMonochro, false, false);
        HasImpostorVision = BooleanOptionItem.Create(RoleInfo, 6, GeneralOption.ImpostorVision, false, false);
        OpCanseeKiller = BooleanOptionItem.Create(RoleInfo, 7, Option.MonochromerCanseeKiller, true, false);
        OpCanseeRoleColor = BooleanOptionItem.Create(RoleInfo, 8, Option.MonochromerMarkColor, false, false, OpCanseeKiller);
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (!CanseeKiller || GameStates.Meeting) return "";
        if (seer.Is(CustomRoles.Monochromer) && (seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.Is(CustomRoles.WolfBoy) || seen.Is(CustomRoles.Sheriff) || seen.Is(CustomRoles.GrimReaper)))
        {
            var c = seen.GetRoleColor();
            if (seen.Is(CustomRoles.WolfBoy))
            {
                c = Utils.GetRoleColor(CustomRoles.Impostor);
            }
            return Utils.ColorString(color ? c : Palette.DisabledGrey, "★");
        }
        else return "";
    }
    public override void Colorchnge()
    {
        if (!Player.IsAlive()) return;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc == Player) continue;
            if (pc == null) continue;
            if (pc.Is(CustomRoles.UltraStar)) continue;
            var id = Camouflage.PlayerSkins[pc.PlayerId].ColorId;
            if (Kurosiro.GetBool())
            {
                if (id is 0 or 1 or 2 or 6 or 8 or 9 or 12 or 15 or 16)
                {
                    pc.RpcChColor(Player, 6, true);
                }
                else pc.RpcChColor(Player, 7, true);
            }
            else
                pc.RpcChColor(Player, 15, true);
        }
        Utils.NotifyRoles();
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            var id = Camouflage.PlayerSkins[pc.PlayerId].ColorId;
            pc.SetColor(id);
            Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true);
        }
    }
    public static bool CheckWin()
    {
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.Monochromer))
            {
                if (pc.IsAlive())
                {
                    Win();
                    return true;
                }
            }
        }
        return false;
    }
    private static void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Monochromer);
        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Monochromer);
    }
}