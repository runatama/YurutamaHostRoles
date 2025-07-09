using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Staff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Staff),
            player => new Staff(player),
            CustomRoles.Staff,
            () => CanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            12400,
            SetupOptionItem,
            "sf",
            "#00ffff",
            (9, 2),
            from: From.RevolutionaryHostRoles
        );
    public Staff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        EndedTaskInAlive = false;
        Awakened = !OptAwakening.GetBool() || OptAwakeningTask.GetInt() < 1; ;
    }

    public bool EndedTaskInAlive = false;
    static OptionItem CanUseVent;
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTask;
    bool Awakened;
    private static void SetupOptionItem()
    {
        CanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.TaskAwakening, false, false);
        OptAwakeningTask = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.AwakeningTaskcount, new(0f, 255f, 1f), 5f, false, OptAwakening);
    }

    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : (CanUseVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate);
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished && Player.IsAlive()) EndedTaskInAlive = true;

        //これはFinの外にしないとタスク数での覚醒上手くいないゼ。
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptAwakeningTask.GetInt()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }

        return true;
    }
}