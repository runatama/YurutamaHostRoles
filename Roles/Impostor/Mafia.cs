using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Mafia : RoleBase, IImpostor, IUseTheShButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mafia),
            player => new Mafia(player),
            CustomRoles.Mafia,
            () => CanAddMad.GetBool() && Options.CanMakeMadmateCount.GetInt() != 0 ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            1600,
            SetupCustomOption,
            "mf",
            from: From.TheOtherRoles
        );
    public Mafia(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SKMad = CanAddMad.GetBool();
    }
    static OptionItem CanAddMad;
    static OptionItem OptionCankill;
    static OptionItem Cankill;
    bool SKMad;
    enum Option
    {
        MafiaCankill
    }
    public static void SetupCustomOption()
    {
        Cankill = IntegerOptionItem.Create(RoleInfo, 9, Option.MafiaCankill, new(1, 3, 1), 2, false).SetValueFormat(OptionFormat.Players);
        OptionCankill = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false).SetValueFormat(OptionFormat.Seconds);
        CanAddMad = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanCreateSideKick, false, false);
    }
    public float CalculateKillCooldown() => OptionCankill.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public bool CanUseKillButton()
    {
        if (PlayerState.AllPlayerStates == null) return false;
        int livingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role.IsImpostor()) livingImpostorsNum++;
        }

        return livingImpostorsNum <= (Cankill.GetFloat() - 1);
    }
    public void OnClick()
    {
        if (!SKMad || Options.CanMakeMadmateCount.GetInt() <= Main.SKMadmateNowCount) return;
        var target = Player.GetKillTarget();
        if (target == null || target.Is(CustomRoleTypes.Impostor)) return;

        SKMad = false;
        Player.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);
        Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Sidekick]　" + string.Format(Translator.GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({Utils.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true) + $"({Utils.GetTrueRoleName(Player.PlayerId)})");
        target.RpcSetCustomRole(CustomRoles.SKMadmate);
        foreach (var pl in Main.AllPlayerControls)
        {
            if (pl == PlayerControl.LocalPlayer)
                target.StartCoroutine(target.CoSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, Main.SetRoleOverride));
            else
                target.RpcSetRoleDesync(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, pl.GetClientId());
        }
        Main.SKMadmateNowCount++;
        Utils.MarkEveryoneDirtySettings();
        Utils.NotifyRoles();
        Main.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(Utils.GetRoleColor(target.GetCustomRole()), Translator.GetString($"{target.GetCustomRole()}")) + "</b>" + Utils.GetSubRolesText(target.PlayerId);
    }
}