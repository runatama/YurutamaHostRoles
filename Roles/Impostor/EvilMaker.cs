using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class EvilMaker : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilMaker),
            player => new EvilMaker(player),
            CustomRoles.EvilMaker,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            4600,
            SetupOptionItem,
            "Em"
        );
    public EvilMaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        AbilityCooldown = OptionAbilityCoolDown.GetFloat();

        Used = false;
    }
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionAbilityCoolDown; static float AbilityCooldown;
    bool Used;
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = AbilityCooldown;
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        fall = false;

        if (Used) return;

        var target = Player.GetKillTarget(true);
        if (target == null) return;
        if ((target.GetCustomRole() is CustomRoles.SKMadmate || target.GetCustomRole().IsImpostor()) && !SuddenDeathMode.NowSuddenDeathMode) return;

        Used = true;
        resetkillcooldown = true;
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

        _ = new LateTask(() => Player.SetKillCooldown(target: target), Main.LagTime, "", true);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);
        UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true) + $"({UtilsRoleText.GetTrueRoleName(Player.PlayerId)})"));
        target.RpcSetCustomRole(CustomRoles.SKMadmate);
        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#ff1919");
        NameColorManager.Add(target.PlayerId, Player.PlayerId, "#ff1919");
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
        resetkillcooldown = true;
    }
    public override string GetAbilityButtonText() => GetString("Sidekick");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Used || Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonSideKick");
        return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
    }
}
