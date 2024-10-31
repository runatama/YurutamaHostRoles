using System;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public static class UtilsTask
    {
        public static bool HasTasks(NetworkedPlayerInfo p, bool ForRecompute = true)
        {
            if (GameStates.IsLobby) return false;
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;
            if (p.Disconnected) return false;

            var hasTasks = true;
            var States = PlayerState.GetByPlayerId(p.PlayerId);
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                if (States.MainRole is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
            }
            else
            {
                // 死んでいて，死人のタスク免除が有効なら確定でfalse
                if (p.IsDead && Options.GhostIgnoreTasks.GetBool())
                {
                    return false;
                }
                var role = States.MainRole;
                var roleClass = CustomRoleManager.GetByPlayerId(p.PlayerId);
                if (roleClass != null)
                {
                    switch (roleClass.HasTasks)
                    {
                        case HasTask.True:
                            hasTasks = true;
                            break;
                        case HasTask.False:
                            hasTasks = false;
                            break;
                        case HasTask.ForRecompute:
                            hasTasks = !ForRecompute;
                            break;
                    }
                }
                switch (role)
                {
                    case CustomRoles.GM:
                    case CustomRoles.SKMadmate:
                    case CustomRoles.Jackaldoll:
                        hasTasks = false;
                        break;
                    default:
                        if (role.IsImpostor()) hasTasks = false;
                        break;
                }

                foreach (var subRole in States.SubRoles)
                    switch (subRole)
                    {
                        //ラバーズはタスクを勝利用にカウントしない
                        case CustomRoles.Lovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.RedLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.YellowLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.BlueLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.GreenLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.WhiteLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.PurpleLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.MadonnaLovers: hasTasks &= !ForRecompute; break;
                        case CustomRoles.OneLove: hasTasks &= !ForRecompute; break;

                        case CustomRoles.Amanojaku: hasTasks &= !ForRecompute; break;
                        case CustomRoles.Amnesia:
                            {
                                var ch = false;
                                switch (role.GetRoleInfo()?.BaseRoleType.Invoke())
                                {
                                    case RoleTypes.Crewmate:
                                    case RoleTypes.Engineer:
                                    case RoleTypes.Scientist:
                                    case RoleTypes.Noisemaker:
                                    case RoleTypes.Tracker:
                                        ch = true;
                                        break;
                                }
                                hasTasks = role.IsCrewmate() && ch ? hasTasks : (!ForRecompute && !States.MainRole.IsImpostor());
                            }
                            break;
                    }

                if (States.GhostRole is CustomRoles.AsistingAngel) hasTasks = false;
            }
            return hasTasks;
        }
        public static string AllTaskstext(bool kakuritu, bool oomaka, bool meetingdake, bool comms, bool CanSeeComms)
        {
            float t1 = 0;
            float t2 = 0;
            float pa = 0;
            foreach (var p in PlayerCatch.AllPlayerControls)
            {
                var task = PlayerState.GetByPlayerId(p.PlayerId).taskState;
                if (task.hasTasks && HasTasks(p.Data))
                {
                    t1 += p.GetPlayerTaskState().AllTasksCount;
                    t2 += p.GetPlayerTaskState().CompletedTasksCount;
                    pa = t2 / t1;//intならぶっこわれる!
                }
            }
            float pas = pa * 100;//小数点考えない四捨五入
            double ret1 = Math.Round(pas);//小数点以下の四捨五入
            double ret = ret1 * 0.1f;//ぽんこつ用に0.1倍して
            double ret2 = Math.Round(ret);//四捨五入
            double ret3 = ret2 * 10;//10倍してぽんこつに。

            if ((!GameStates.Meeting && meetingdake) || (comms && !CanSeeComms)) return $"<color=#cee4ae>[??]</color>";
            else if (!kakuritu) return $"<color=#cee4ae>[{t2}/{t1}]</color>";
            else if (oomaka) return $"<color=#cee4ae>[{ret3}%]</color>";
            else return $"<color=#cee4ae>[{ret1}%]</color>";
        }
        public static bool TaskCh = true;
        public static void DelTask()
        {
            if (Main.FixTaskNoPlayer.Count == 0) return;
            TaskCh = false;
            try
            {
                foreach (var pc in Main.FixTaskNoPlayer)
                {
                    if (pc != null)
                    {
                        if (pc.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() is RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.Phantom) continue;

                        if (Main.AllPlayerTask.TryGetValue(pc.PlayerId, out var tasks))
                            foreach (var task in tasks)
                                pc.RpcCompleteTask(task);
                    }
                }
                Main.FixTaskNoPlayer.Clear();
            }
            catch
            {
                Logger.Error($"DelTask中にエラーがおこったよ！", "Deltask");
            }
            TaskCh = true;
        }
    }
}