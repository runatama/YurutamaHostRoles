using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
namespace TownOfHost.Roles.Crewmate;
public sealed class Staff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Staff),
            player => new Staff(player),
            CustomRoles.Staff,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            74827,
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
        kakusei = !Kakusei.GetBool();
        ta = Task.GetInt();
    }
    enum OptionName
    {
        BaitKakusei,
        Kakuseitask
    }

    public bool EndedTaskInAlive = false;

    static OptionItem Kakusei;
    static OptionItem Task;
    bool kakusei;
    int ta;
    private static void SetupOptionItem()
    {
        Kakusei = BooleanOptionItem.Create(RoleInfo, 10, OptionName.BaitKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 11, OptionName.Kakuseitask, new(0f, 10f, 1f), 5f, false, Kakusei);
    }

    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished)
        {
            if (Player.IsAlive()) EndedTaskInAlive = true;
        }
        //これはFinの外にしないとタスク数での覚醒上手くいないゼ。
        if (MyTaskState.CompletedTasksCount >= ta) kakusei = true;

        return true;
    }
}