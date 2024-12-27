using System.Collections.Generic;
using TownOfHost;
using TownOfHost.Roles.Core;

class TaskBattle
{
    public static bool IsTaskBattleTeamMode;
    public static List<List<byte>> TaskBattleTeams = new();

    public static void Init()
    {
        IsTaskBattleTeamMode = Options.TaskBattleTeamMode.GetBool();
    }
    public static void TaskBattleCompleteTask(PlayerControl pc, TaskState taskState)
    {
        if (pc.Is(CustomRoles.TaskPlayerB) && taskState.IsTaskFinished)
        {
            if (IsTaskBattleTeamMode)
            {
                foreach (var team in TaskBattleTeams)
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
    }
}