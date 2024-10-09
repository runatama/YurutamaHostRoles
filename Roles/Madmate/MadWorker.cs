using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadWorker : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadWorker),
            player => new MadWorker(player),
            CustomRoles.MadWorker,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            10600,
            SetupOptionItem,
            "mw"
        );
    public MadWorker(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => (CannotWinAtDeath && player.Data.IsDead) ? HasTask.False : HasTask.ForRecompute
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();

        canVent = OptionCanVent.GetBool();
        ventCooldown = OptionVentCooldown.GetFloat();
        CannotWinAtDeath = true;
    }
    private static OptionItem OptionCanVent;
    private static OptionItem OptionVentCooldown;
    enum OptionName
    {
        VentCooldown
    }
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static bool canVent;
    private static bool CannotWinAtDeath;
    private static float ventCooldown;
    private static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.VentCooldown, new(0f, 180f, 0.5f), 0f, false, OptionCanVent)
                .SetValueFormat(OptionFormat.Seconds);
        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = ventCooldown;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished && !(CannotWinAtDeath && !Player.IsAlive()))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            if (!AmongUsClient.Instance.AmHost) return true;
            GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
        }
        return true;
    }
    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
}
