using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Crewmate;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
    class AddTasksFromListPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (!Options.DisableTasks.GetBool()) return;
            List<NormalPlayerTask> disabledTasks = new();
            for (var i = 0; i < unusedTasks.Count; i++)
            {
                var task = unusedTasks[i];
                if (task.TaskType == TaskTypes.SwipeCard && Options.DisableSwipeCard.GetBool()) disabledTasks.Add(task);//カードタスク
                if (task.TaskType == TaskTypes.SubmitScan && Options.DisableSubmitScan.GetBool()) disabledTasks.Add(task);//スキャンタスク
                if (task.TaskType == TaskTypes.UnlockSafe && Options.DisableUnlockSafe.GetBool()) disabledTasks.Add(task);//金庫タスク
                if (task.TaskType == TaskTypes.UploadData && Options.DisableUploadData.GetBool()) disabledTasks.Add(task);//アップロードタスク
                if (task.TaskType == TaskTypes.StartReactor && Options.DisableStartReactor.GetBool()) disabledTasks.Add(task);//リアクターの3x3タスク
                if (task.TaskType == TaskTypes.ResetBreakers && Options.DisableResetBreaker.GetBool()) disabledTasks.Add(task);//レバータスク
                if (task.TaskType == TaskTypes.CatchFish && Options.DisableCatchFish.GetBool()) disabledTasks.Add(task);//釣りタスク
                if (task.TaskType == TaskTypes.DivertPower && Options.DisableDivertPower.GetBool()) disabledTasks.Add(task);//送電タスク
                if (task.TaskType == TaskTypes.FuelEngines && Options.DisableFuelEngins.GetBool()) disabledTasks.Add(task);//給油タスク
                if (task.TaskType == TaskTypes.ExtractFuel && Options.DisableFuelEngins.GetBool()) disabledTasks.Add(task);//給油タスク(ファングル)
                if (task.TaskType == TaskTypes.InspectSample && Options.DisableInspectSample.GetBool()) disabledTasks.Add(task);//サンプル
                if (task.TaskType == TaskTypes.RebootWifi && Options.DisableRebootWifi.GetBool()) disabledTasks.Add(task);//WIFIタスク
                if (task.TaskType == TaskTypes.PlayVideogame && Options.DisableInseki.GetBool()) disabledTasks.Add(task);//隕石(ファングル)
                if (task.TaskType == TaskTypes.ClearAsteroids && Options.DisableInseki.GetBool()) disabledTasks.Add(task);//隕石
                if (task.TaskType == TaskTypes.CalibrateDistributor && Options.disableCalibrateDistributor.GetBool()) disabledTasks.Add(task);//アスタリスク
                if (task.TaskType == TaskTypes.VentCleaning && Options.disableVentCleaning.GetBool()) disabledTasks.Add(task);//ベント掃除
                if (task.TaskType == TaskTypes.HelpCritter && Options.disableHelpCritter.GetBool()) disabledTasks.Add(task);//卵
            }
            foreach (var task in disabledTasks)
            {
                //Logger.Msg("削除: " + task.TaskType.ToString(), "AddTask");
                unusedTasks.Remove(task);
            }
        }
    }

    [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
    class RpcSetTasksPatch
    {
        //タスクを割り当ててRPCを送る処理が行われる直前にタスクを上書きするPatch
        //バニラのタスク割り当て処理自体には干渉しない
        public static Il2CppSystem.Collections.Generic.List<byte> Soroeru = new();
        public static Il2CppSystem.Collections.Generic.Dictionary<byte, Il2CppStructArray<byte>> taskIds = new();
        public static bool HostFin;
        public static void Prefix(NetworkedPlayerInfo __instance, [HarmonyArgument(0)] ref Il2CppStructArray<byte> taskTypeIds)
        {
            //null対策
            if (Main.RealOptionsData == null)
            {
                Logger.Warn("警告:RealOptionsDataがnullです。", "RpcSetTasksPatch");
                return;
            }

            var pc = __instance.Object;
            CustomRoles? RoleNullable = pc?.GetCustomRole();
            if (RoleNullable == null) return;
            CustomRoles role = RoleNullable.Value;

            //デフォルトのタスク数
            bool hasCommonTasks = true;//trueになるのは
            int NumCommonTasks = Main.NormalOptions.NumCommonTasks;
            int NumLongTasks = Main.NormalOptions.NumLongTasks;
            int NumShortTasks = Main.NormalOptions.NumShortTasks;

            if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
            {
                hasCommonTasks = data.numCommonTasks.GetInt() == Main.NormalOptions.NumCommonTasks;
                NumCommonTasks = data.numCommonTasks.GetInt();
                // コモンタスク(通常タスク)を割り当てるかどうか
                // 割り当てる場合でも再割り当てはされず、他のクルーと同じコモンタスクが割り当てられる。
                NumLongTasks = data.numLongTasks.GetInt();      // 割り当てるロングタスクの数
                NumShortTasks = data.numShortTasks.GetInt();    // 割り当てるショートタスクの数
                                                                // ロングとショートは常時再割り当てが行われる。
            }
            if (pc.Is(CustomRoles.Workhorse))
                (hasCommonTasks, NumLongTasks, NumShortTasks) = Workhorse.TaskData;

            if (taskTypeIds.Count == 0) hasCommonTasks = false; //タスク再配布時はコモンを0に

            if (GameStates.IsOnlineGame && !Main.IsCs())
            {
                if (NumCommonTasks + NumLongTasks + NumShortTasks >= 255)
                {
                    hasCommonTasks = false;
                    NumCommonTasks = 85;
                    NumLongTasks = 84;
                    NumShortTasks = 85;
                    Logger.Error($"{pc?.name ?? "ｼｭﾄｸﾑﾘﾀﾞｯﾀ!"}のタスクが255を超えています", "TaskAssignPatch");
                }
            }
            if (!hasCommonTasks && NumLongTasks == 0 && NumShortTasks == 0 && NumCommonTasks == 0)
            {
                NumShortTasks = 1; //タスク0対策
                Main.FixTaskNoPlayer.Add(pc);
            }
            //変更点がない場合
            if (!(Options.CurrentGameMode == CustomGameMode.TaskBattle && Options.TaskSoroeru.GetBool()) &&
                !Options.CommnTaskResetAssing.GetBool() && hasCommonTasks && NumCommonTasks == Main.NormalOptions.NumCommonTasks && NumLongTasks == Main.NormalOptions.NumLongTasks && NumShortTasks == Main.NormalOptions.NumShortTasks)
            {
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                    if (!Main.IsroleAssigned)
                    {
                        taskIds[__instance.PlayerId] = taskTypeIds;
                        return;
                    }
                return;
            }
            //割り当て可能なタスクのIDが入ったリスト
            //本来のRpcSetTasksの第二引数のクローン
            Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
            foreach (var num in taskTypeIds)
                TasksList.Add(num);

            //参考:ShipStatus.Begin
            //不要な割り当て済みのタスクを削除する処理
            //コモンタスクを割り当てる設定ならコモンタスク以外を削除
            //コモンタスクを割り当てない設定ならリストを空にする
            int defaultCommonTasksNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumCommonTasks);
            if (hasCommonTasks && !Options.CommnTaskResetAssing.GetBool()) TasksList.RemoveRange(defaultCommonTasksNum, TasksList.Count - defaultCommonTasksNum);
            else TasksList.Clear();

            //割り当て済みのタスクが入れられるHashSet
            //同じタスクが複数割り当てられるのを防ぐ
            Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
            int start1 = Main.NormalOptions.NumCommonTasks;
            int start2 = 0;
            int start3 = 0;

            if ((Options.CommnTaskResetAssing.GetBool() && hasCommonTasks) || (!hasCommonTasks && NumCommonTasks != start1))
            {
                //コモンの再割り当て
                Il2CppSystem.Collections.Generic.List<NormalPlayerTask> CommnTasks = new();
                foreach (var task in ShipStatus.Instance.CommonTasks)
                    CommnTasks.Add(task);
                Shuffle<NormalPlayerTask>(CommnTasks);
                ShipStatus.Instance.AddTasksFromList(
                    ref start1,
                    NumCommonTasks,
                    TasksList,
                    usedTaskTypes,
                    CommnTasks
                );
            }

            //割り当て可能なロングタスクのリスト
            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
            foreach (var task in ShipStatus.Instance.LongTasks)
                LongTasks.Add(task);
            Shuffle<NormalPlayerTask>(LongTasks);

            //割り当て可能なショートタスクのリスト
            Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
            foreach (var task in ShipStatus.Instance.ShortTasks)
                ShortTasks.Add(task);
            Shuffle<NormalPlayerTask>(ShortTasks);

            //実際にAmong Us側で使われているタスクを割り当てる関数を使う。
            ShipStatus.Instance.AddTasksFromList(
                ref start2,
                NumLongTasks,
                TasksList,
                usedTaskTypes,
                LongTasks
            );
            ShipStatus.Instance.AddTasksFromList(
                ref start3,
                NumShortTasks,
                TasksList,
                usedTaskTypes,
                ShortTasks
            );

            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                if (Options.TaskSoroeru.GetBool())
                {
                    if (!HostFin)
                    {
                        Soroeru = TasksList;
                        HostFin = true;
                    }
                    else
                    {
                        TasksList = Soroeru;
                    }
                }
            //タスクのリストを配列(Il2CppStructArray)に変換する
            taskTypeIds = new Il2CppStructArray<byte>(TasksList.Count);
            for (int i = 0; i < TasksList.Count; i++)
            {
                taskTypeIds[i] = TasksList[i];
            }

            if (Options.CurrentGameMode == CustomGameMode.Standard)
                if (!Main.IsroleAssigned)
                {
                    taskIds[__instance.PlayerId] = taskTypeIds;
                    return;
                }
        }

        public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                T obj = list[i];
                int rand = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[rand];
                list[rand] = obj;
            }
        }
    }
}
