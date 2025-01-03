using System;
using System.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost;
class TaskBattle
{
    public static bool IsTaskBattleTeamMode;
    /// <summary>key:チームid,value:List<プレイヤーid> </summary>
    public static Dictionary<byte, List<byte>> TaskBattleTeams = new();
    /// <summary>チャットで設定されたチーム情報 </summary>
    public static Dictionary<byte, byte> SelectedTeams = new();
    /// <summary>追加でタスクを付与した数</summary>
    public static Dictionary<byte, int> TaskAddCount = new();
    public static bool IsAdding;
    public static void Init()
    {
        IsAdding = false;
        TaskAddCount = new();
        IsTaskBattleTeamMode = TaskBattleTeamMode.GetBool();

        if (TaskAddMode.GetBool())
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                //GMはさいなら
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;

                TaskAddCount.TryAdd(pc.PlayerId, 0);
            }
        }
    }
    public static bool TaskBattleCompleteTask(PlayerControl pc, TaskState taskState)
    {
        if (pc.Is(CustomRoles.TaskPlayerB) && taskState.IsTaskFinished)
        {
            if (TaskAddMode.GetBool())
            {
                if (TaskAddCount.TryGetValue(pc.PlayerId, out var count))
                {
                    //タスクは続くよどこまでも
                    if (count < MaxAddCount.GetInt())
                    {
                        IsAdding = true;
                        TaskAddCount[pc.PlayerId]++;

                        taskState.AllTasksCount += NumCommonTasks.GetInt() + NumLongTasks.GetInt() + NumShortTasks.GetInt();

                        if (AmongUsClient.Instance.AmHost)
                        {
                            pc.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
                            pc.SyncSettings();
                        }
                        UtilsNotifyRoles.NotifyRoles();
                        UtilsGameLog.AddGameLog("TaskBattle", string.Format(Translator.GetString("TB"), Utils.GetPlayerColor(pc, true), taskState.CompletedTasksCount + "/" + taskState.AllTasksCount));
                        return false;
                    }
                }
            }

            if (IsTaskBattleTeamMode)
            {
                foreach (var team in TaskBattleTeams.Values)
                {
                    if (team.Contains(pc.PlayerId)) continue;
                    team.Do(playerId =>
                    {
                        PlayerCatch.GetPlayerById(playerId).RpcExileV2();
                        var playerState = PlayerState.GetByPlayerId(playerId);
                        playerState.SetDead();
                    });
                }
            }
            else
            {
                foreach (var otherPlayer in PlayerCatch.AllAlivePlayerControls)
                {
                    if (otherPlayer == pc || otherPlayer.AllTasksCompleted()) continue;
                    otherPlayer.RpcExileV2();
                    var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
                    playerState.SetDead();
                }
            }
        }

        UtilsNotifyRoles.NotifyRoles();
        UtilsGameLog.AddGameLog("TaskBattle", string.Format(Translator.GetString("TB"), Utils.GetPlayerColor(pc, true), taskState.CompletedTasksCount + "/" + taskState.AllTasksCount));
        return true;
    }
    public static OptionItem TaskBattleSet;
    public static OptionItem TaskBattleCanVent;
    public static OptionItem TaskBattleVentCooldown;
    public static OptionItem TaskBattelCanSeeOtherPlayer;
    public static OptionItem TaskBattelShowAllTask;
    public static OptionItem TaskBattleShowFastestPlayer;
    public static OptionItem TaskBattleTeamMode;
    public static OptionItem TaskBattleTeamCount;
    public static OptionItem TaskBattleTeamWinType;
    public static OptionItem TaskBattleTeamWinTaskc;
    public static OptionItem TaskSoroeru;

    public static OptionItem TaskAddMode;
    public static OptionItem NumCommonTasks;
    public static OptionItem NumLongTasks;
    public static OptionItem NumShortTasks;
    public static OptionItem MaxAddCount;
    public static void SetupOptionItem()
    {
        TaskBattleSet = BooleanOptionItem.Create(200317, "TaskBattleSet", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle)
            .SetHeader(true)
            .SetColorcode("#87cffa");
        TaskBattleCanVent = BooleanOptionItem.Create(200307, "TaskBattleCanVent", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleVentCooldown = FloatOptionItem.Create(200308, "TaskBattleVentCooldown", new(0f, 99f, 1f), 5f, TabGroup.MainSettings, false).SetParent(TaskBattleCanVent)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattelShowAllTask = BooleanOptionItem.Create(200309, "TaskBattelShowAllTask", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
        TaskBattelCanSeeOtherPlayer = BooleanOptionItem.Create(200311, "TaskBattelCanSeeOtherPlayer", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
        TaskBattleShowFastestPlayer = BooleanOptionItem.Create(200312, "TaskBattleShowFastestPlayer", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
        TaskBattleTeamMode = BooleanOptionItem.Create(200313, "TaskBattleTeamMode", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleTeamCount = FloatOptionItem.Create(200314, "TaskBattleTeamCount", new(1f, 15f, 1f), 2f, TabGroup.MainSettings, false).SetParent(TaskBattleTeamMode)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleTeamWinType = BooleanOptionItem.Create(200315, "TaskBattleTeamGameTaskComp", false, TabGroup.MainSettings, false).SetParent(TaskBattleTeamMode)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleTeamWinTaskc = FloatOptionItem.Create(200316, "TaskBattleTeamWinTaskc", new(1f, 999f, 1f), 20f, TabGroup.MainSettings, false).SetParent(TaskBattleTeamWinType)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskSoroeru = BooleanOptionItem.Create(200318, "TaskSoroeru", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);

        TaskAddMode = BooleanOptionItem.Create(200319, "TaskAddMode", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);
        NumCommonTasks = IntegerOptionItem.Create(200320, "WorkhorseNumCommonTasks", new(0, 99, 1), 1, TabGroup.MainSettings, false).SetParent(TaskAddMode)
            .SetGameMode(CustomGameMode.TaskBattle).SetValueFormat(OptionFormat.Pieces);
        NumLongTasks = IntegerOptionItem.Create(200321, "WorkhorseNumLongTasks", new(0, 99, 1), 1, TabGroup.MainSettings, false).SetParent(TaskAddMode)
            .SetGameMode(CustomGameMode.TaskBattle).SetValueFormat(OptionFormat.Pieces);
        NumShortTasks = IntegerOptionItem.Create(200322, "WorkhorseNumShortTasks", new(0, 99, 1), 1, TabGroup.MainSettings, false).SetParent(TaskAddMode)
            .SetGameMode(CustomGameMode.TaskBattle).SetValueFormat(OptionFormat.Pieces);
        MaxAddCount = IntegerOptionItem.Create(200323, "MaxAddCount", new(1, 99, 1), 1, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskAddMode);
    }
}