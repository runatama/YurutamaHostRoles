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
            (9, 2),
            SetupOptionItem,
            "sf",
            "#00ffff",
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
        kakusei = !Kakusei.GetBool() || Task.GetInt() < 1; ;
        ta = Task.GetInt();
    }
    enum OptionName
    {
        Kakuseitask
    }

    public bool EndedTaskInAlive = false;
    static OptionItem CanUseVent;
    static OptionItem Kakusei;
    static OptionItem Task;
    bool kakusei;
    int ta;
    private static void SetupOptionItem()
    {
        CanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.TaskKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Kakuseitask, new(0f, 255f, 1f), 5f, false, Kakusei);
    }

    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : (CanUseVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate);
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished && Player.IsAlive()) EndedTaskInAlive = true;

        //これはFinの外にしないとタスク数での覚醒上手くいないゼ。
        if (MyTaskState.HasCompletedEnoughCountOfTasks(ta))
        {
            if (kakusei == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            kakusei = true;
        }

        return true;
    }
}