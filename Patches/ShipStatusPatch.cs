using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Main.introDestroyed)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    UtilsOption.MarkEveryoneDirtySettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.Info("キル能力解禁", "HideAndSeek");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
    class ShipStatusUpdateSystemPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            if (systemType != SystemTypes.Sabotage)
            {
                Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole().RemoveHtmlTags() + ", amount: " + amount, "UpdateSystem");
            }
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                Logger.seeingame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole().RemoveHtmlTags() + ", amount: " + amount);
            }

            _ = new LateTask(() =>
            {
                if (Main.sabotagetime > 0.3f)
                    if (Main.NowSabotage)
                    {
                        if (!Utils.IsActive(Main.SabotageType))
                        {
                            var sb = Translator.GetString($"sb.{Main.SabotageType}");

                            if (systemType == SystemTypes.MushroomMixupSabotage)
                                UtilsGameLog.AddGameLog($"MushroomMixup", string.Format(Translator.GetString("Log.FixSab"), sb));
                            else UtilsGameLog.AddGameLog($"{systemType}", string.Format(Translator.GetString("Log.FixSab"), sb));
                            Main.NowSabotage = false;
                            Main.sabotagetime = 0;

                            foreach (var role in Roles.Core.CustomRoleManager.AllActiveRoles.Values)
                            {
                                role.AfterSabotage(Main.SabotageType);
                            }
                        }
                    }
            }, 0.2f, "Check Sabotage");
        }
        public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
        {
            var Ids = new List<int>();
            for (var i = min; i <= max; i++)
            {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
        {
            if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Doors, (byte)id);
                }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;

            return !Options.AllowCloseDoors.GetBool();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();
            Logger.Info("-----------ゲーム開始-----------", "Phase");
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                Main.CustomSpawnPosition.TryAdd(AmongUsClient.Instance.TutorialMapId, new List<Vector2>());
                _ = new LateTask(() =>
                {
                    PlayerControl.LocalPlayer.StartCoroutine(PlayerControl.LocalPlayer.CoSetRole(AmongUs.GameOptions.RoleTypes.Shapeshifter, false));
                    if (PlayerControl.AllPlayerControls.Count < 10)
                    {
                        //SNR参考 https://github.com/SuperNewRoles/SuperNewRoles/blob/master/SuperNewRoles/Modules/BotManager.cs
                        byte id = 0;
                        foreach (var p in PlayerControl.AllPlayerControls)
                            id++;
                        for (var i = 0; PlayerControl.AllPlayerControls.Count < 10; i++)
                        {
                            var dummy = GameObject.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                            dummy.isDummy = true;
                            dummy.PlayerId = id;
                            AmongUsClient.Instance.Spawn(GameData.Instance.AddDummy(dummy));
                            AmongUsClient.Instance.Spawn(dummy);
                            dummy.NetTransform.enabled = true;
                            dummy.SetColor(6);
                            id++;
                        }
                    }
                    //Mark
                    var mark = UtilsSprite.LoadSprite("TownOfHost.Resources.SpawnMark.png", 300f);
                    foreach (var p in PlayerControl.AllPlayerControls.ToArray().Where(p => p.PlayerId > 1))
                    {
                        _ = new LateTask(() =>
                        {
                            var nametext = p.transform.Find("Names/NameText_TMP");
                            nametext.transform.position -= new Vector3(0, nametext.gameObject.activeSelf ? 0.3f : -0.3f);
                            nametext.gameObject.SetActive(true);
                        }, 0.5f);
                        GameObject.Destroy(p.transform.Find("Names/ColorblindName_TMP").gameObject);
                        p.transform.Find("BodyForms").gameObject.active = false;
                        var hand = p.transform.Find("BodyForms/Seeker/SeekerHand");
                        var Mark = GameObject.Instantiate(hand, hand.transform.parent.parent.parent);
                        Mark.transform.localPosition = new Vector2(0, 0);
                        Mark.GetComponent<SpriteRenderer>().sprite = mark;
                        Component.Destroy(Mark.GetComponent<PowerTools.SpriteAnimNodeSync>());
                        Mark.name = "Mark";
                        Mark.gameObject.SetActive(true);
                    }
                }, 0.2f);
                return;
            }
            if (GameStates.IsModHost && Main.UseWebHook.Value) UtilsWebHook.WH_ShowActiveRoles();
            PlayerCatch.CountAlivePlayers(true);
            Main.RTAMode = Options.CurrentGameMode == CustomGameMode.TaskBattle && PlayerCatch.AllPlayerControls.Count() == (Options.EnableGM.GetBool() ? 2 : 1);
            //RTAモードじゃないならLateTaskを作らない
            if (!Main.RTAMode) return;
            _ = new LateTask(() =>
            {
                var playerRTA = Options.EnableGM.GetBool() ? PlayerCatch.AllAlivePlayerControls.Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId).First() : PlayerControl.LocalPlayer;
                if (playerRTA == null)
                {
                    Logger.Warn("[TR] プレイヤーがnullです", "TaskBattle RTA");
                    return;
                }
                Main.RTAPlayer = playerRTA.PlayerId;
                HudManagerPatch.TaskBattlep = playerRTA.transform.position;
                HudManagerPatch.TaskBattleTimer = 0f;
            }, 1f, "TaskBattle TimerReset");
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(ShipStatus __instance, PlayerControl reporter, NetworkedPlayerInfo target)
        {
            MeetingStates.ReportTarget = target;
            MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        }
        public static void Postfix()
        {
            // 全プレイヤーを湧いてない状態にする
            foreach (var state in PlayerState.AllPlayerStates.Values)
            {
                state.HasSpawned = false;
                state.TeleportedWithAntiBlackout = false;
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            //ホストの役職初期設定はここで行うべき？
        }
    }
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    class CheckTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}