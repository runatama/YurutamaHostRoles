using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Bait : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bait),
            player => new Bait(player),
            CustomRoles.Bait,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20060,
            SetupOptionItem,
            "ba",
            "#00f7ff",
            from: From.TheOtherRoles
        );
    public Bait(PlayerControl player)
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
        Kakuseitask,
        BaitComms
    }
    static OptionItem Kakusei;
    static OptionItem Task;
    static OptionItem Comms;
    bool kakusei;
    int ta;
    private static void SetupOptionItem()
    {
        Comms = BooleanOptionItem.Create(RoleInfo, 9, OptionName.BaitComms, true, false);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.TaskKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 11, OptionName.Kakuseitask, new(0f, 10f, 1f), 5f, false, Kakusei);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.Bait) && !info.IsSuicide && !info.IsFakeSuicide && (!Comms.GetBool() || !Utils.IsActive(SystemTypes.Comms)))
            _ = new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished || MyTaskState.CompletedTasksCount >= ta) kakusei = true;
        return true;
    }
}