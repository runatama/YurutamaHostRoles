using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;
public sealed class Efficient : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Efficient),
            player => new Efficient(player),
            CustomRoles.Efficient,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            26400,
            SetupOptionItem,
            "ef",
            "#a68b96"
        );
    public Efficient(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = 0f;
    }
    enum Option { EfficientCollectRect }
    static OptionItem CollectRect;
    float Cooldown;
    private static void SetupOptionItem()
    {
        CollectRect = FloatOptionItem.Create(RoleInfo, 10, Option.EfficientCollectRect, new(0, 100, 1), 15, false).SetValueFormat(OptionFormat.Percent);
        Options.OverrideTasksData.Create(RoleInfo, 11);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;

        Cooldown -= Time.fixedDeltaTime;
    }
    public override bool OnCompleteTask()
    {
        if (Cooldown > 0f) return true;

        int chance = IRandom.Instance.Next(1, 101);

        if (CollectRect.GetFloat() > chance)
        {
            var Task = new List<uint>();
            foreach (var task in Player.myTasks)
            {
                if (!task.IsComplete && !task.WasCollected) Task.Add(task.Id);
            }
            if (Task.Count() == 0) return true;
            var rand = IRandom.Instance;
            var FinTask = Task[rand.Next(0, Task.Count())];

            if (Cooldown > 0f) return true;

            Cooldown = 3;
            Player.RpcCompleteTask(FinTask);
            Player.RpcProtectedMurderPlayer();
            Logger.Info($"{Player.name} => 効率化成功!タスクを一個減らすぞ!", "Efficient");
        }
        return true;
    }
}