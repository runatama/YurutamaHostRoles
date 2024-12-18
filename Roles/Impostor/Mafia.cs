using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Mafia : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mafia),
            player => new Mafia(player),
            CustomRoles.Mafia,
            () => CanmakeSidekickMadMate.GetBool() && Options.CanMakeMadmateCount.GetInt() != 0 ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            8600,
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
        SKMad = CanmakeSidekickMadMate.GetBool();
        canusekill = false;
    }
    static OptionItem CanmakeSidekickMadMate;
    static OptionItem OptionKillCoolDown;
    static OptionItem CanKillImpostorCount;
    static OptionItem CanKillDay;
    bool SKMad;
    bool canusekill;
    enum Option
    {
        MafiaCankill,
        MafiaCanKillDay
    }
    public static void SetupCustomOption()
    {
        CanKillImpostorCount = IntegerOptionItem.Create(RoleInfo, 9, Option.MafiaCankill, new(1, 3, 1), 2, false).SetValueFormat(OptionFormat.Players);
        CanKillDay = FloatOptionItem.Create(RoleInfo, 12, Option.MafiaCanKillDay, new(0, 30, 1), 0, false, infinity: null).SetValueFormat(OptionFormat.day);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false).SetValueFormat(OptionFormat.Seconds);
        CanmakeSidekickMadMate = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanCreateSideKick, false, false);
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = 1f;
    public bool CanUseKillButton()
    {
        if (PlayerState.AllPlayerStates == null) return false;
        int livingImpostorsNum = 0;
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role.IsImpostor()) livingImpostorsNum++;
        }

        return (livingImpostorsNum <= CanKillImpostorCount.GetFloat()) || canusekill;
    }
    public override void OnStartMeeting()
    {
        if (CanKillDay.GetFloat() == 0) return;
        if (!Player.IsAlive()) return;

        if (CanKillDay.GetFloat() <= UtilsGameLog.day) canusekill = true;
    }
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        resetkillcooldown = false;
        fall = true;
        if (!SKMad || Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount) return;
        var target = Player.GetKillTarget();
        if (target == null || target.Is(CustomRoles.King) || target.Is(CustomRoleTypes.Impostor)) return;

        SKMad = false;
        Player.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);
        UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true) + $"({UtilsRoleText.GetTrueRoleName(Player.PlayerId)})"));
        target.RpcSetCustomRole(CustomRoles.SKMadmate);
        if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);
        foreach (var pl in PlayerCatch.AllPlayerControls)
        {
            if (pl == PlayerControl.LocalPlayer)
                target.StartCoroutine(target.CoSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, Main.SetRoleOverride));
            else
                target.RpcSetRoleDesync(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, pl.GetClientId());
        }
        PlayerCatch.SKMadmateNowCount++;
        UtilsOption.MarkEveryoneDirtySettings();
        UtilsNotifyRoles.NotifyRoles();
        UtilsGameLog.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(target.GetCustomRole()), GetString($"{target.GetCustomRole()}")) + "</b>" + UtilsRoleText.GetSubRolesText(target.PlayerId);
    }
}