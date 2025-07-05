using AmongUs.GameOptions;
using System.Linq;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;

public sealed class Workaholic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Workaholic),
            player => new Workaholic(player),
            CustomRoles.Workaholic,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            14700,
            (5, 3),
            SetupOptionItem,
            "wh",
            "#008b8b",
            from: From.TownOfHost_Y,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
        );
    public Workaholic(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => (CannotWinAtDeath && player.Data.IsDead) ? HasTask.False : HasTask.ForRecompute
    )
    {
        ventCooldown = OptionVentCooldown.GetFloat();
        CannotWinAtDeath = true;
    }
    private static OptionItem OptionCanVent;
    private static OptionItem OptionVentCooldown;
    enum OptionName
    {
        VentCooldown,
    }
    private static bool CannotWinAtDeath;
    private static float ventCooldown;
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.VentCooldown, new(0f, 180f, 2.5f), 0f, false, OptionCanVent)
                .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(RoleInfo, 20);
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
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Workaholic, Player.PlayerId, true))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }
        }
        return true;
    }
}
