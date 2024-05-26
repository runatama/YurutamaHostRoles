using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class InSender : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(InSender),
            player => new InSender(player),
            CustomRoles.InSender,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20080,
            SetupOptionItem,
            "in",
            "#eee8aa",
            from: From.RevolutionaryHostRoles
        );
    public InSender(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        kakusei = !Kakusei.GetBool();
        ta = Task.GetInt();
    }
    enum OptionName
    {
        BaitKakusei,
        Kakuseitask
    }
    static OptionItem Kakusei;
    static OptionItem Task;
    bool kakusei;
    int ta;
    private static void SetupOptionItem()
    {
        Kakusei = BooleanOptionItem.Create(RoleInfo, 10, OptionName.BaitKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 11, OptionName.Kakuseitask, new(0f, 10f, 1f), 5f, false, Kakusei);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.InSender) && !target.Is(CustomRoles.Transparent) && !info.IsSuicide)
            _ = new LateTask(() =>
            {
                ReportDeadBodyPatch.DieCheckReport(target, target, false);
            }, 0.15f, "InSender Self Report");
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished || MyTaskState.CompletedTasksCount >= ta) kakusei = true;
        return true;
    }
}