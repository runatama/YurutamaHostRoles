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
            () => CanAddMad.GetBool() ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
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
    bool SKMad;
    public static void SetupCustomOption()
    {
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
        //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
        int livingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role != CustomRoles.Mafia && role.IsImpostor()) livingImpostorsNum++;
        }

        return livingImpostorsNum <= 0;
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
        Main.SKMadmateNowCount++;
        Utils.MarkEveryoneDirtySettings();
        Utils.NotifyRoles();
        Main.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(Utils.GetRoleColor(target.GetCustomRole()), Translator.GetString($"{target.GetCustomRole()}")) + "</b>" + Utils.GetSubRolesText(target.PlayerId);
    }
}