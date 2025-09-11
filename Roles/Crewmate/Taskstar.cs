using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class TaskStar : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TaskStar),
            player => new TaskStar(player),
            CustomRoles.TaskStar,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10000,
            SetupOptionItem,
            "ts",
            "#FFD700",
            (4, 1),
            from: From.TownOfHost_K
        );
    public TaskStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    private static void SetupOptionItem()
    {
        OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (IsTaskFinished)
            enabled = true;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished)
        {
            Player.MarkDirtySettings();
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        }

        return true;
    }
}