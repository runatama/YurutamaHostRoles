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
        BaitComms, BaitChien, Baitsaidaichien
    }
    static OptionItem Kakusei;
    static OptionItem Task;
    static OptionItem Comms;
    static OptionItem Chien;
    static OptionItem Saiaichien;
    bool kakusei;
    int ta;
    private static void SetupOptionItem()
    {
        Comms = BooleanOptionItem.Create(RoleInfo, 9, OptionName.BaitComms, true, false);
        Chien = FloatOptionItem.Create(RoleInfo, 12, OptionName.BaitChien, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        Saiaichien = FloatOptionItem.Create(RoleInfo, 13, OptionName.Baitsaidaichien, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.TaskKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Kakuseitask, new(0f, 255f, 1f), 5f, false, Kakusei);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var tien = 0f;
        //小数対応
        if (Saiaichien.GetFloat() != 0)
        {
            int ti = IRandom.Instance.Next(0, (int)Saiaichien.GetFloat() * 10);
            tien = ti * 0.1f;
            Logger.Info($"{tien}sの追加遅延発生!!", "InSender");
        }
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.InSender) && !info.IsSuicide && !info.IsFakeSuicide && (!Comms.GetBool() || !Utils.IsActive(SystemTypes.Comms)))
            _ = new LateTask(() => ReportDeadBodyPatch.DieCheckReport(target, target.Data, false), 0.15f + Chien.GetFloat() + tien, "InSender Self Report");
    }

    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished || MyTaskState.CompletedTasksCount >= ta) kakusei = true;
        return true;
    }
}