using AmongUs.GameOptions;

using TownOfHost.Modules;
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
            () => CanmakeSidekickMadMate.GetBool() && Options.CanMakeMadmateCount.GetInt() != 0 ? RoleTypes.Phantom : RoleTypes.Impostor,
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
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount ? 200f : 1f;
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
    bool IUsePhantomButton.IsPhantomRole => SKMad && Options.CanMakeMadmateCount.GetInt() > PlayerCatch.SKMadmateNowCount;
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        resetkillcooldown = false;
        fall = true;
        if (!SKMad || Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount) return;
        var target = Player.GetKillTarget(true);
        if (target == null || target.GetCustomRole() is CustomRoles.King or CustomRoles.Merlin || (target.Is(CustomRoleTypes.Impostor) && !SuddenDeathMode.NowSuddenDeathTemeMode)) return;

        SKMad = false;
        if (SuddenDeathMode.NowSuddenDeathTemeMode)
        {
            if (SuddenDeathMode.TeamRed.Contains(Player.PlayerId))
            {
                SuddenDeathMode.TeamRed.Add(target.PlayerId);
                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
            }
            if (SuddenDeathMode.TeamBlue.Contains(Player.PlayerId))
            {
                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                SuddenDeathMode.TeamBlue.Add(target.PlayerId);
                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
            }
            if (SuddenDeathMode.TeamYellow.Contains(Player.PlayerId))
            {
                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                SuddenDeathMode.TeamYellow.Add(target.PlayerId);
                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
            }
            if (SuddenDeathMode.TeamGreen.Contains(Player.PlayerId))
            {
                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                SuddenDeathMode.TeamGreen.Add(target.PlayerId);
                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
            }
            if (SuddenDeathMode.TeamPurple.Contains(Player.PlayerId))
            {
                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                SuddenDeathMode.TeamPurple.Add(target.PlayerId);
            }
        }
        Player.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);
        UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true)));
        target.RpcSetCustomRole(CustomRoles.SKMadmate);
        target.RpcSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, true);
        if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);
        PlayerCatch.SKMadmateNowCount++;
        UtilsOption.MarkEveryoneDirtySettings();
        UtilsNotifyRoles.NotifyRoles();
        UtilsGameLog.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(target.GetCustomRole()), GetString($"{target.GetCustomRole()}")) + "</b>" + UtilsRoleText.GetSubRolesText(target.PlayerId);
    }
    public override string GetAbilityButtonText() => GetString("Sidekick");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Impostor_Side";
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !SKMad || Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonSideKick");
        return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
    }
}