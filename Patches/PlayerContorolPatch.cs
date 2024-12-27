using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.Neutral;
using AmongUs.Data;
using static TownOfHost.Roles.Core.RoleBase;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Patches.ISystemType;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost || !Main.CanUseAbility) return false;

            Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole().RemoveHtmlTags() + "=>" + target.GetNameWithRole().RemoveHtmlTags(), "CheckProtect");

            if (__instance.IsGorstRole())
            {
                Ghostbuttoner.UseAbility(__instance);
                GhostNoiseSender.UseAbility(__instance, target);
                GhostReseter.UseAbility(__instance, target);
                GuardianAngel.UseAbility(__instance, target);
                DemonicTracker.UseAbility(__instance, target);
                DemonicCrusher.UseAbility(__instance);
                DemonicVenter.UseAbility(__instance, target);
                AsistingAngel.UseAbility(__instance, target);
                return false;
            }

            if (__instance.GetCustomRole() is CustomRoles.Sheriff or CustomRoles.WolfBoy or CustomRoles.SwitchSheriff)
            {
                if (__instance.Data.IsDead)
                {
                    Logger.Info("守護をブロックしました。", "CheckProtect");
                    return false;
                }
            }
            //全部falseしてもいい気はする
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
    {
        public static Dictionary<byte, float> TimeSinceLastKill = new();
        public static void Update()
        {
            for (byte i = 0; i < 15; i++)
            {
                if (TimeSinceLastKill.ContainsKey(i))
                {
                    TimeSinceLastKill[i] += Time.deltaTime;
                    if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
                }
            }
        }
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;

            if (!ExileControllerWrapUpPatch.AllSpawned && !MeetingStates.FirstMeeting) return false;
            // 処理は全てCustomRoleManager側で行う
            if (!CustomRoleManager.OnCheckMurder(__instance, target))
            {
                // キル失敗
                __instance.RpcMurderPlayer(target, false);
            }

            return false;
        }

        // 不正キル防止チェック
        public static bool CheckForInvalidMurdering(MurderInfo info, bool kantu = false)
        {
            (var killer, var target) = info.AttemptTuple;

            // Killerが既に死んでいないかどうか
            if (!kantu)
                if (!killer.IsAlive())
                {
                    Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()}は死亡しているためキャンセルされました。", "CheckMurder");
                    return false;
                }
            // targetがキル可能な状態か
            if (
                // PlayerDataがnullじゃないか確認
                target.Data == null ||
                // targetの状態をチェック
                target.inVent ||
                target.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
                target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
                target.inMovingPlat)
            {
                Logger.Info("targetは現在キルできない状態です。", "CheckMurder");
                return false;
            }
            // targetが既に死んでいないか
            if (!target.IsAlive())
            {
                Logger.Info("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            // 会議中のキルでないか
            if (MeetingHud.Instance != null)
            {
                Logger.Info("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            if (!kantu)
            {
                // 連打キルでないか
                float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
                //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
                //↓許可されない場合
                if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
                {
                    Logger.Info("前回のキルからの時間が早すぎるため、キルをブロックしました。", "CheckMurder");
                    return false;
                }
            }
            TimeSinceLastKill[killer.PlayerId] = 0f;

            // HideAndSeek_キルボタンが使用可能か
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            if (Options.CurrentGameMode == CustomGameMode.Standard && Options.firstturnmeeting && MeetingStates.FirstMeeting)
            {
                Logger.Info(killer.GetNameWithRole().RemoveHtmlTags() + "が強制会議前にキルしようとしたから弾いたぞ！", "CheckMurder");
                return false;
            }
            // キルが可能なプレイヤーか(遠隔は除く)
            if (!info.IsFakeSuicide && !killer.CanUseKillButton() && !killer.Is(CustomRoles.UltraStar))
            {
                Logger.Info(killer.GetNameWithRole().RemoveHtmlTags() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.MurderPlayer));
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags, ref bool __state /* 成功したキルかどうか */ )
        {
            logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}{(target.protectedByGuardianThisRound ? "(Protected)" : "")} ({resultFlags})");
            var isProtectedByClient = resultFlags.HasFlag(MurderResultFlags.DecisionByHost) && target.IsProtected();
            var isProtectedByHost = resultFlags.HasFlag(MurderResultFlags.FailedProtected);
            var isFailed = resultFlags.HasFlag(MurderResultFlags.FailedError);
            var isSucceeded = __state = !isProtectedByClient && !isProtectedByHost && !isFailed;
            if (isProtectedByClient)
            {
                logger.Info("守護されているため，キルは失敗します");
            }
            if (isProtectedByHost)
            {
                //logger.Info("守護されているため，キルはホストによってキャンセルされました");
            }
            if (isFailed)
            {
                logger.Info("キルはホストによってキャンセルされました");
            }

            if (isSucceeded)
            {
                if (target.shapeshifting)
                {
                    //シェイプシフトアニメーション中
                    //アニメーション時間を考慮して1s、加えてクライアントとのラグを考慮して+0.5s遅延する
                    _ = new LateTask(
                        () =>
                        {
                            if (GameStates.IsInTask)
                            {
                                target.RpcShapeshift(target, false);
                            }
                        },
                        1.5f, "RevertShapeshift");
                }
                else
                {
                    if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out var shapeshifting) && shapeshifting)
                    {
                        //シェイプシフト強制解除
                        target.RpcShapeshift(target, false);
                    }
                }
                if (!Camouflager.NowUse) Camouflage.RpcSetSkin(target, ForceRevert: true, RevertToDefault: true);
            }
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, bool __state)
        {
            // キルが成功していない場合，何もしない
            if (!__state)
            {
                return;
            }
            if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;
            //以降ホストしか処理しない
            // 処理は全てCustomRoleManager側で行う
            CustomRoleManager.OnMurderPlayer(__instance, target);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()} => {target?.GetNameWithRole().RemoveHtmlTags()}", "Shapeshift");

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

            if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
            {
                Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()}:Cancel Shapeshift.Prefix", "Shapeshift");
                return;
            }

            Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
            Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

            shapeshifter.GetRoleClass()?.OnShapeshift(target);

            if (!AmongUsClient.Instance.AmHost) return;

            _ = new LateTask(() =>
            {
                foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                {
                    role.Colorchnge();
                }
            }, 1.2f, "", true);

            if (!shapeshifting)
            {
                Camouflage.RpcSetSkin(shapeshifter);
                if (Options.Onlyseepet.GetBool())
                {
                    shapeshifter.OnlySeeMePet(shapeshifter.Data.DefaultOutfit.PetId);
                }
            }
            //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
            if (!shapeshifting)
            {
                _ = new LateTask(() =>
                {
                    UtilsNotifyRoles.NotifyRoles(NoCache: true);
                },
                1.2f, "ShapeShiftNotify");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
    public static class PlayerControlCheckShapeshiftPatch
    {
        private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckShapeshift));

        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
        {
            if (Main.EditMode && GameStates.IsFreePlay)
            {
                var mapid = AmongUsClient.Instance.TutorialMapId;
                if (target.PlayerId is 1)
                {
                    if (Main.page is 0)
                    {
                        if (Main.CustomSpawnPosition[mapid].Count < 8)
                            Main.CustomSpawnPosition[mapid].Add(__instance.transform.position);
                    }
                    else
                        __instance.NetTransform.SnapTo(Main.CustomSpawnPosition[mapid][Main.page - 2]);
                }
                else if (Main.page is not 0)
                {
                    if (target.PlayerId is 2)
                    {
                        Main.CustomSpawnPosition[mapid][Main.page - 2] = __instance.transform.position;
                    }
                    if (target.PlayerId is 3)
                    {
                        Main.CustomSpawnPosition[mapid].Remove(Main.CustomSpawnPosition[mapid][Main.page - 2]);
                    }
                    if (target.PlayerId is 4)
                    {
                        Minigame.Instance.ForceClose();
                        Main.page = 0;
                        _ = new LateTask(() => DestroyableSingleton<HudManager>.Instance.AbilityButton.DoClick(), 0.03f, "Open Menu");
                    }
                }
                else if (target.PlayerId <= Main.CustomSpawnPosition[mapid].Count + 1)
                {
                    Minigame.Instance.ForceClose();
                    _ = new LateTask(() =>
                    {
                        PlayerControl.AllPlayerControls[1].SetName(Translator.GetString("ED.Move"));
                        PlayerControl.AllPlayerControls[2].SetName(Translator.GetString("ED.Movehere"));
                        PlayerControl.AllPlayerControls[3].SetName(Translator.GetString("ED.delete"));
                        PlayerControl.AllPlayerControls[4].SetName(Translator.GetString("ED.back"));
                        foreach (var pc in PlayerControl.AllPlayerControls)
                            if (pc.PlayerId > 4) pc.SetName("<size=0>");
                        DestroyableSingleton<HudManager>.Instance.AbilityButton.DoClick();
                        Main.page = target.PlayerId;
                    }, 0.03f, "Open Menu");
                }
                __instance.RpcRejectShapeshift();
                return false;
            }

            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
            {
                return false;
            }

            var roleclass = __instance.GetRoleClass();

            // 無効な変身を弾く．これより前に役職等の処理をしてはいけない
            if ((!ExileControllerWrapUpPatch.AllSpawned && !MeetingStates.FirstMeeting) || !CheckInvalidShapeshifting(__instance, target, shouldAnimate))
            {
                __instance.RpcRejectShapeshift();
                return false;
            }

            var button = (roleclass as IUseTheShButton)?.CheckShapeshift(__instance, target);
            if (button.HasValue)
            {
                shouldAnimate = false;
                return button.Value;
            }

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;
            // 変身したとき一番近い人をマッドメイトにする処理
            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {
                var sidekickable = roleclass as ISidekickable;
                var targetRole = sidekickable?.SidekickTargetRole ?? CustomRoles.SKMadmate;

                //var targetm = shapeshifter.GetKillTarget();
                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (var p in PlayerCatch.AllAlivePlayerControls)
                {
                    if ((p.Data.Role.Role != RoleTypes.Shapeshifter || p.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() != RoleTypes.Shapeshifter) && !p.Is(CustomRoleTypes.Impostor) && !p.Is(targetRole))
                    {
                        dis = Vector2.Distance(shapeshifterPosition, p.transform.position);
                        mpdistance.Add(p, dis);
                    }
                }
                if (mpdistance.Count != 0)

                //if (targetm != null && !targetm.Is(targetRole) && !targetm.Is(CustomRoleTypes.Impostor))
                {
                    //shapeshifter.RpcShapeshift(shapeshifter, false);
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    if (!targetm.Is(CustomRoles.King))
                    {
                        UtilsGameLog.AddGameLog("SideKick", string.Format(Translator.GetString("log.Sidekick"), Utils.GetPlayerColor(targetm, true) + $"({UtilsRoleText.GetTrueRoleName(targetm.PlayerId)})", Utils.GetPlayerColor(shapeshifter, true) + $"({UtilsRoleText.GetTrueRoleName(shapeshifter.PlayerId)})"));
                        targetm.RpcSetCustomRole(targetRole);
                        Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                        PlayerCatch.SKMadmateNowCount++;
                        shapeshifter.RpcProtectedMurderPlayer(targetm);
                        targetm.RpcProtectedMurderPlayer(shapeshifter);
                        targetm.RpcProtectedMurderPlayer(targetm);

                        foreach (var pl in PlayerCatch.AllPlayerControls)
                        {
                            if (pl == PlayerControl.LocalPlayer)
                                targetm.StartCoroutine(targetm.CoSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, Main.SetRoleOverride));
                            else
                                targetm.RpcSetRoleDesync(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, pl.GetClientId());
                        }
                        NameColorManager.Add(targetm.PlayerId, shapeshifter.PlayerId, "#ff1919");
                        if (!Utils.RoleSendList.Contains(targetm.PlayerId)) Utils.RoleSendList.Add(targetm.PlayerId);
                        PlayerState.GetByPlayerId(targetm.PlayerId).SetCountType(CountTypes.Crew);
                        UtilsGameLog.LastLogRole[targetm.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(targetm.GetCustomRole()), Translator.GetString($"{targetm.GetCustomRole()}")) + "</b>" + UtilsRoleText.GetSubRolesText(targetm.PlayerId);
                        UtilsOption.MarkEveryoneDirtySettings();
                        UtilsNotifyRoles.NotifyRoles();
                        //shapeshifter.RpcRejectShapeshift();
                        //return false;
                    }
                }
            }
            // 役職の処理
            if (Amnesia.CheckAbility(shapeshifter))
                if (roleclass?.CheckShapeshift(target, ref shouldAnimate) == false)
                {
                    if (roleclass.CanDesyncShapeshift)
                    {
                        shapeshifter.RpcSpecificRejectShapeshift(target, shouldAnimate);
                    }
                    else
                    {
                        shapeshifter.RpcRejectShapeshift();
                    }
                    return false;
                }

            shapeshifter.RpcShapeshift(target, shouldAnimate);
            return false;
        }
        private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
        {
            logger.Info($"Checking shapeshift {instance.GetNameWithRole().RemoveHtmlTags()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole().RemoveHtmlTags())}");

            if (!target || target.Data == null)
            {
                logger.Info("targetがnullのため変身をキャンセルします");
                return false;
            }
            if (!instance.IsAlive())
            {
                logger.Info("変身者が死亡しているため変身をキャンセルします");
                return false;
            }
            if (instance.Is(CustomRoles.SKMadmate) || instance.Is(CustomRoles.Jackaldoll))
            {
                logger.Info("変身者がサイドキックされてるため変身をキャンセルします");
                return false;
            }
            // RoleInfoによるdesyncシェイプシフター用の判定を追加
            if (instance.Data.Role.Role != RoleTypes.Shapeshifter && instance.GetCustomRole().GetRoleInfo()?.BaseRoleType?.Invoke() != RoleTypes.Shapeshifter)
            {
                logger.Info("変身者がシェイプシフターではないため変身をキャンセルします");
                return false;
            }
            if (instance.Data.Disconnected)
            {
                logger.Info("変身者が切断済のため変身をキャンセルします");
                return false;
            }
            if (target.IsMushroomMixupActive() && animate)
            {
                logger.Info("キノコカオス中のため変身をキャンセルします");
                return false;
            }
            if (MeetingHud.Instance && animate)
            {
                logger.Info("会議中のため変身をキャンセルします");
                return false;
            }
            if (!Main.CanUseAbility)
            {
                logger.Info("CanUseAbilityがfalseなのでりたーん");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, bool> Musisuruoniku;
        public static Dictionary<byte, string> ChengeMeetingInfo;
        public static Dictionary<byte, List<NetworkedPlayerInfo>> WaitReport = new();
        //public static Dictionary<byte, Vector2> Pos = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            if (GameStates.IsMeeting) return false;

            Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target?.Object?.GetNameWithRole()?.RemoveHtmlTags() ?? "null"}", "ReportDeadBody");

            var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons <= 0 && target is null)
            {
                Logger.Info($"{__instance.name}君はもうボタン使ったでしょ!", "ReportDeadBody");
                return false;
            }

            GameStates.Meeting = true;
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.TaskBattle || Options.IsStandardHAS) return false;
            if (!CanReport[__instance.PlayerId])
            {
                GameStates.Meeting = false;
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole().RemoveHtmlTags()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");

                if (DontReport.TryGetValue(__instance.PlayerId, out var check))
                {
                    if (check.reason == DontReportreson.wait) return false;
                }
                if (!DontReport.TryAdd(__instance.PlayerId, (0, DontReportreson.wait))) DontReport[__instance.PlayerId] = (0, DontReportreson.wait);
                _ = new LateTask(() =>
                {
                    if (!GameStates.Meeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: __instance);
                }, 0.2f, "", true);
                return false;
            }

            //ホスト以外はこの先処理しない
            if (!AmongUsClient.Instance.AmHost) return true;

            PlayerControlRpcUseZiplinePatch.OnMeeting(__instance, target);

            if (!CheckMeeting(__instance, target)) return false;

            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================
            GameStates.task = false;
            //Pos.Clear();

            DisableDevice.StartMeeting();
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();
                if (pc == null) continue;

                foreach (var pl in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pl == null) continue;
                    if (pl.PlayerId == pc.PlayerId) continue;
                    pl.RpcSnapToDesync(pc, new Vector2(999f, 999f));
                }
            }

            UtilsOption.MarkEveryoneDirtySettings();

            AdminProvider.CalculateAdmin(true);

            if (target != null)
            {
                UtilsGameLog.AddGameLog("Meeting", Utils.GetPlayerColor(target, true) + Translator.GetString("Meeting.Report") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true)));
                var colorid = Camouflage.PlayerSkins[target.PlayerId].ColorId;
                var DieName = Palette.GetColorName(colorid);
                var check = false;
                var color = Palette.PlayerColors[colorid];
                if (ChengeMeetingInfo.TryGetValue(target.PlayerId, out var output))
                {
                    color = ModColors.NeutralGray;
                    check = true;
                    DieName = output;
                }
                MeetingHudPatch.Oniku = (check ? DieName : Utils.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
                UtilsNotifyRoles.MeetingMoji = "<i><u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[__instance.PlayerId].ColorId]) + "<color=#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</i></u></color>";
            }
            else
            {
                UtilsGameLog.AddGameLog("Meeting", Translator.GetString("Meeting.Button") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true)));
                MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
                UtilsNotifyRoles.MeetingMoji = "<i><u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[__instance.PlayerId].ColorId]) + "<color=#ffffff>" + Translator.GetString("MI.Bot") + "</i></u></color>";
            }

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (!pc) continue;
                var roleClass = pc.GetRoleClass();
                roleClass?.OnReportDeadBody(__instance, target);
            }

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (!pc) continue;
                if (!pc.IsAlive())
                {
                    pc.RpcExileV2();
                    pc.RpcSetRole(RoleTypes.CrewmateGhost, Main.SetRoleOverride);
                }

                Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true);
            }

            // var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons > 0 && target is null)
                State.NumberOfRemainingButtons--;

            MeetingTimeManager.OnReportDeadBody();

            UtilsNotifyRoles.NotifyMeetingRoles();

            UtilsOption.SyncAllSettings();

            //サボ関係多分なしに～
            //押したのなら強制で始める
            MeetingRoomManager.Instance.AssignSelf(__instance, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(__instance);
            __instance.RpcStartMeeting(target);
            return false;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time)
        {
            //async Taskじゃ警告出るから仕方ないよね。
            var revertName = PlayerControl.LocalPlayer.name;
            PlayerControl.LocalPlayer.RpcSetNameEx(name);
            await Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
        }
        /// <summary>
        /// 死者でもReportさせるやーつ
        /// </summary>
        /// <param name="repo">通報者</param>
        /// <param name="target">死体(null=button)</param>
        /// <param name="ch">属性等のチェック入れるか</param>
        public static void DieCheckReport(PlayerControl repo, NetworkedPlayerInfo target = null, bool? ch = true, string Meetinginfo = "", string colorcode = "#000000")
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (repo == null)
            {
                Logger.Error($"{repo?.Data.PlayerName ?? "???"} がnull!", "DieCheckReport");
            }
            if (GameStates.IsMeeting) return;

            var State = PlayerState.GetByPlayerId(repo.PlayerId);
            if (State.NumberOfRemainingButtons <= 0 && target is null && ch is not false) return;

            PlayerControlRpcUseZiplinePatch.OnMeeting(repo, target);

            if (ch is null or true)
                if (!CheckMeeting(repo, target, checkdie: ch is true)) return;

            if (!AmongUsClient.Instance.AmHost) return;
            GameStates.Meeting = true;
            GameStates.task = false;// Pos.Clear();

            DisableDevice.StartMeeting();
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();

                if (pc == null) continue;
                foreach (var pl in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pl == null) continue;
                    if (pl.PlayerId == pc.PlayerId) continue;
                    pl.RpcSnapToDesync(pc, new Vector2(999f, 999f));
                }
            }

            UtilsOption.MarkEveryoneDirtySettings();
            AdminProvider.CalculateAdmin(true);

            if (Meetinginfo == "")
            {
                if (target != null)
                {
                    UtilsGameLog.AddGameLog("Meeting", Utils.GetPlayerColor(target, true) + Translator.GetString("Meeting.Report") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true)));
                    var colorid = Camouflage.PlayerSkins[target.PlayerId].ColorId;
                    var DieName = Palette.GetColorName(colorid);
                    var check = false;
                    var color = Palette.PlayerColors[colorid];
                    if (ChengeMeetingInfo.TryGetValue(target.PlayerId, out var output))
                    {
                        color = ModColors.NeutralGray;
                        check = true;
                        DieName = output;
                    }
                    MeetingHudPatch.Oniku = (check ? DieName : Utils.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true));
                    UtilsNotifyRoles.MeetingMoji = "<i><u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[repo.PlayerId].ColorId]) + "<color=#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</i></u></color>";
                }
                else
                {
                    UtilsGameLog.AddGameLog("Meeting", Translator.GetString("Meeting.Button") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true)));
                    MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true));
                    UtilsNotifyRoles.MeetingMoji = "<i><u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[repo.PlayerId].ColorId]) + "<color=#ffffff>" + Translator.GetString("MI.Bot") + "</i></u></color>";
                }
            }
            else
            {
                MeetingHudPatch.Oniku = Meetinginfo;
                UtilsNotifyRoles.MeetingMoji = $"<color={colorcode}><i><u>★" + Meetinginfo + "</i></u></color>";
            }

            if (!Options.firstturnmeeting || !MeetingStates.FirstMeeting)
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var roleClass = pc.GetRoleClass();
                    roleClass?.OnReportDeadBody(repo, target);
                }

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (!pc) continue;
                if (!pc.IsAlive())
                {
                    pc.RpcExileV2();
                    pc.RpcSetRole(RoleTypes.CrewmateGhost, Main.SetRoleOverride);
                }
                Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true);
            }

            UtilsNotifyRoles.NotifyMeetingRoles();

            MeetingTimeManager.OnReportDeadBody();

            UtilsOption.SyncAllSettings();

            MeetingRoomManager.Instance.AssignSelf(repo, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(repo);
            repo.RpcStartMeeting(target);
        }
        public static Dictionary<byte, (float time, DontReportreson reason)> DontReport = new();
        public static string Dontrepomark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting) return "";

            if (seer == seen)
                if (DontReport.TryGetValue(seer.PlayerId, out var data))
                {
                    switch (data.reason)
                    {
                        case DontReportreson.wait: return "<size=120%><color=#91abbd>...</color></size>";
                        case DontReportreson.NonReport: return "<size=120%><color=#006666>×</color></size>";
                        case DontReportreson.Transparent: return "<size=120%><color=#7b7c7d>×</color></size>";
                        case DontReportreson.CantUseButton: return "<size=120%><color=#bdb091>×</color></size>";
                        case DontReportreson.Other: return "<size=120%><color=#bd9391>×</color></size>";
                    }
                }

            return "";
        }
        public static bool CheckMeeting(PlayerControl repoter, NetworkedPlayerInfo target, bool checkdie = true)
        {
            var c = false;
            if (target != null)
                if (repoter.GetRoleClass() is MassMedia massMedia)
                {
                    if (massMedia.Target == target.PlayerId)
                        c = true;
                }

            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                Logger.Info($"サドンデスモードなのにボタンを使おうと?", "ReportDeadBody");
                AddDontrepo(repoter, DontReportreson.CantUseButton);
                return false;
            }
            if (Main.NowSabotage && target == null)
            {
                AddDontrepo(repoter, DontReportreson.CantUseButton);
                Logger.Info($"サボタージュ中なのでキャンセル。{Main.SabotageType}", "ReportDeadBody");
                return false;
            }
            RoleAddAddons.GetRoleAddon(repoter.GetCustomRole(), out var da, repoter, subrole: CustomRoles.NonReport);
            var GiveNonReport = da.GiveNonReport.GetBool();
            var val = da.mode;
            if (target == null)
            {
                if (GiveNonReport && val is RoleAddAddons.Convener.ConvenerAll or RoleAddAddons.Convener.NotButton)
                {
                    Logger.Info($"役職でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                {
                    if (repoter.Is(CustomRoles.NonReport) && NonReport.Mode is NonReport.Convener.ConvenerAll or NonReport.Convener.NotButton)
                    {
                        Logger.Info($"属性でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                        GameStates.Meeting = false;
                        AddDontrepo(repoter, DontReportreson.NonReport);
                        return false;
                    }
                }
            }
            else if (!c)
            {
                if (GiveNonReport && val is RoleAddAddons.Convener.ConvenerAll or RoleAddAddons.Convener.NotReport)
                {
                    Logger.Info($"NonReportの設定が{val}だからキャンセル。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                if (repoter.Is(CustomRoles.NonReport) && NonReport.Mode is NonReport.Convener.ConvenerAll or NonReport.Convener.NotReport)
                {
                    Logger.Info($"属性でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                if (RoleAddAddons.GetRoleAddon(target?.Object.GetCustomRole() ?? CustomRoles.NotAssigned, out var d, target?.Object, subrole: CustomRoles.Transparent) && d.GiveTransparent.GetBool())
                {
                    GameStates.Meeting = false;
                    Logger.Info($"ターゲットが属性トランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.Transparent);
                    return false;
                }
                else
                if (target?.Object.Is(CustomRoles.Transparent) ?? false || Transparent.playerIdList.Contains(target.PlayerId))
                {
                    GameStates.Meeting = false;
                    Logger.Info($"ターゲットが属性トランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.Transparent);
                    return false;
                }
            }

            if (!AmongUsClient.Instance.AmHost) return true;

            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (repoter?.Data?.IsDead ?? false && checkdie)
            {
                GameStates.Meeting = false;
                Logger.Info($"通報者が死んでいるのでキャンセルする", "ReportDeadBody");
                return false;
            }

            var r = DontReportreson.None;
            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
            {
                if (role.CancelReportDeadBody(repoter, target, ref r))
                {
                    Logger.Info($"{role}によって会議はキャンセルされました。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, r);
                    return false;
                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    GameStates.Meeting = false;
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.CantUseButton);
                    return false;
                }
                else Options.UsedButtonCount++;
            }
            return true;

            void AddDontrepo(PlayerControl pc, DontReportreson repo)
            {
                if (DontReport.TryGetValue(pc.PlayerId, out var check))
                {
                    //同一の者ならスキップ
                    if (check.reason == repo) return;
                }
                if (!DontReport.TryAdd(pc.PlayerId, (0, repo))) DontReport[pc.PlayerId] = (0, repo);
                _ = new LateTask(() =>
                {
                    if (!GameStates.Meeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: pc);
                }, 0.2f, "", true);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static Dictionary<byte, int> VentDuringDisabling = new();
        //public static float test = 13.1f;
        public static float text = 0;
        public static void Postfix(PlayerControl __instance)
        {
            var player = __instance;

            if (Main.EditMode && GameStates.IsFreePlay)
            {
                var mapid = AmongUsClient.Instance.TutorialMapId;
                if (player.PlayerId is 1)
                {
                    player.SetName(Main.CustomSpawnPosition[mapid].Count > 7 ? Translator.GetString("ED.noadd") : Translator.GetString("ED.add"));
                    player.NetTransform.SnapTo(new Vector2(9999f, 9999f));
                }
                else if (player.PlayerId is not 0)
                {
                    var check = Main.CustomSpawnPosition[mapid].Count >= player.PlayerId - 1;
                    player.SetName(check ? $"{Translator.GetString("EDCustomSpawn")}{player.PlayerId - 1}" : "<size=0>");
                    player.NetTransform.SnapTo(check ? Main.CustomSpawnPosition[mapid][player.PlayerId - 2] : new Vector2(9999f, 9999f));
                }

                if (Minigame.Instance.IsDestroyedOrNull() && Main.page is not 0)
                    Main.page = 0;

                if (player.AmOwner)
                {
                    string name = DataManager.player.Customization.Name;
                    if (Main.nickName != "") name = Main.nickName;
                    player.SetName($"<color={Main.ModColor}>{name}</color>");
                }
                return;
            }

            if (!GameStates.IsModHost) return;

            if (Main.RTAMode && GameStates.IsInTask && Main.introDestroyed)
            {
                if (Main.RTAPlayer != byte.MaxValue)
                {
                    var playerRTA = PlayerCatch.GetPlayerById(Main.RTAPlayer);
                    HudManagerPatch.LowerInfoText.enabled = true;
                    HudManagerPatch.LowerInfoText.text = HudManagerPatch.GetTaskBattleTimer();
                    if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship && HudManagerPatch.TaskBattlep == new Vector2(-25f, 40f) && (Vector2)playerRTA.transform.position != new Vector2(-25f, 40f))
                    {
                        HudManagerPatch.TaskBattleTimer = 0.0f;
                        if (playerRTA == PlayerControl.LocalPlayer)
                            HudManagerPatch.TaskBattlep = (Vector2)playerRTA.transform.position;
                    }
                    else
                        if (HudManagerPatch.TaskBattlep != (Vector2)playerRTA.transform.position)
                        HudManagerPatch.TaskBattleTimer += Time.deltaTime;
                }
            }

            TargetArrow.OnFixedUpdate(player);
            GetArrow.OnFixedUpdate(player);

            CustomRoleManager.OnFixedUpdate(player);

            var roleclass = player.GetRoleClass();
            var isAlive = player.IsAlive();
            var roleinfo = __instance.GetCustomRole().GetRoleInfo();

            (roleclass as IUsePhantomButton)?.FixedUpdate(player);

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby)
                {
                    //非導入者が遥か彼方へ行かないように。
                    if (!player.IsModClient())
                    {
                        Vector2 c = new(0f, 0f);
                        Vector2 pj = player.transform.position;
                        if (pj.y < -8) player.RpcSnapToForced(c);
                        if (pj.y > 8) player.RpcSnapToForced(c);
                        if (pj.x < -8) player.RpcSnapToForced(c);
                        if (pj.x > 8) player.RpcSnapToForced(c);
                    }
                }
                if (__instance && GameStates.IsInTask)
                {
                    if (ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                    {
                        var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                        ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                        __instance.ReportDeadBody(info);
                    }

                    if (ReportDeadBodyPatch.DontReport.TryGetValue(__instance.PlayerId, out var data))
                    {
                        try
                        {
                            var time = data.time += Time.fixedDeltaTime;

                            if (4f <= time)
                            {
                                ReportDeadBodyPatch.DontReport.Remove(__instance.PlayerId);
                                _ = new LateTask(() =>
                                {
                                    if (!GameStates.Meeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: __instance);
                                }, 0.2f, "", true);
                            }
                            else
                                ReportDeadBodyPatch.DontReport[__instance.PlayerId] = (time, data.reason);
                        }
                        catch { Logger.Error($"{__instance.PlayerId}でエラー！", "DontReport"); }
                    }
                    //梯子ぼーんの奴
                    //Q.ジップラインどうするの？
                    //A.しらん。
                    if (isAlive)
                    {
                        var nowpos = __instance.GetTruePosition();
                        if (!Main.AllPlayerLastkillpos.TryGetValue(__instance.PlayerId, out var tppos))
                            tppos = new Vector2(0, 0);

                        if (!__instance.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                        {
                            switch ((MapNames)Main.NormalOptions.MapId)
                            {
                                case MapNames.Airship:
                                    if ((4.0 <= nowpos.x && nowpos.x <= 5.2 && 10.1 <= nowpos.y && nowpos.y <= 12.9)
                                    || (10.2 <= nowpos.x && nowpos.x <= 11.7 && 6.9 <= nowpos.y && nowpos.y <= 7.2)
                                    || (12.4 <= nowpos.x && nowpos.x <= 13.4 && -5.4 <= nowpos.y && nowpos.y <= -4.6)
                                    )
                                        __instance.RpcSnapToForced(tppos);
                                    break;
                                case MapNames.Fungle:
                                    if ((10.8 <= nowpos.x && nowpos.x <= 12.4 && -5.3 <= nowpos.y && nowpos.y <= -2.1)
                                    || (17.3 <= nowpos.x && nowpos.x <= 18.9 && -5.0 <= nowpos.y && nowpos.y <= -1.9)
                                    || (18.5 <= nowpos.x && nowpos.x <= 19.8 && 4.8 <= nowpos.y && nowpos.y <= 5.7)
                                    || (21.0 <= nowpos.x && nowpos.x <= 22.1 && 8.1 <= nowpos.y && nowpos.y <= 9.4)
                                    )
                                        __instance.RpcSnapToForced(tppos);
                                    break;
                            }
                        }
                        if (!__instance.inMovingPlat && (MapNames)Main.NormalOptions.MapId == MapNames.Airship)
                        {
                            if (6.3 <= nowpos.x && nowpos.x <= 9.3 && 7.8 <= nowpos.y && nowpos.y <= 9.1)
                                __instance.RpcSnapToForced(tppos);
                        }
                    }
                }

                DoubleTrigger.OnFixedUpdate(player);

                //ターゲットのリセット
                if (GameStates.IsInTask && isAlive && Options.LadderDeath.GetBool())
                {
                    FallFromLadder.FixedUpdate(player);
                }
                if (Options.CurrentGameMode == CustomGameMode.Standard && GameStates.IsInTask && Main.introDestroyed && isAlive && !player.IsModClient())
                {
                    Dictionary<int, float> Distance = new();
                    Vector2 position = player.transform.position;
                    foreach (var vent in ShipStatus.Instance.AllVents)
                        Distance.Add(vent.Id, Vector2.Distance(position, vent.transform.position));
                    var first = Distance.OrderBy(x => x.Value).First();

                    if (VentDuringDisabling.TryGetValue(player.PlayerId, out var ventId) && (first.Key != ventId || first.Value > 2))
                    {
                        ushort num = (ushort)(Patches.ISystemType.VentilationSystemUpdateSystemPatch.last_opId + 1U);
                        MessageWriter msgWriter = MessageWriter.Get(SendOption.Reliable);
                        msgWriter.Write(num);
                        msgWriter.Write((byte)VentilationSystem.Operation.StopCleaning);
                        msgWriter.Write((byte)ventId);
                        player.RpcDesyncUpdateSystem(SystemTypes.Ventilation, msgWriter);
                        msgWriter.Recycle();
                        VentDuringDisabling.Remove(player.PlayerId);
                        Patches.ISystemType.VentilationSystemUpdateSystemPatch.last_opId = num;
                    }
                    else if (first.Value <= 2 && !VentDuringDisabling.ContainsKey(player.PlayerId) && (((roleclass as IKiller)?.CanUseImpostorVentButton() is false or null) || (roleclass?.CanClickUseVentButton == false)))
                    {
                        ushort num = (ushort)(Patches.ISystemType.VentilationSystemUpdateSystemPatch.last_opId + 1U);
                        MessageWriter msgWriter = MessageWriter.Get(SendOption.Reliable);
                        msgWriter.Write(num);
                        msgWriter.Write((byte)VentilationSystem.Operation.StartCleaning);
                        msgWriter.Write((byte)first.Key);
                        player.RpcDesyncUpdateSystem(SystemTypes.Ventilation, msgWriter);
                        msgWriter.Recycle();
                        Patches.ISystemType.VentilationSystemUpdateSystemPatch.last_opId = num;
                        VentDuringDisabling[player.PlayerId] = first.Key;
                    }
                }

                Utils.ApplySuffix(__instance);
            }
            //LocalPlayer専用
            if (__instance.AmOwner)
            {
                if (GameStates.IsLobby && (Options.SuddenTeamOption.GetBool() || SuddenDeathMode.CheckTeamDoreka))
                {
                    if (SuddenDeathMode.CheckTeamDoreka && !Options.SuddenTeamOption.GetBool())
                    {
                        SuddenDeathMode.TeamReset();
                        return;
                    }
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        var pos = pc.GetTruePosition();

                        if (-3 <= pos.x && pos.x <= -1.1 && -1 <= pos.y && pos.y <= 0.4)
                        {
                            if (!SuddenDeathMode.TeamRed.Contains(pc.PlayerId))
                                SuddenDeathMode.TeamRed.Add(pc.PlayerId);
                            SuddenDeathMode.TeamBlue.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamYellow.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamGreen.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamPurple.Remove(pc.PlayerId);
                        }
                        else
                        if (-0.2 <= pos.x && pos.x <= 0.7 && -1.1 <= pos.y && pos.y <= 0.4)
                        {
                            SuddenDeathMode.TeamRed.Remove(pc.PlayerId);
                            if (!SuddenDeathMode.TeamBlue.Contains(pc.PlayerId))
                                SuddenDeathMode.TeamBlue.Add(pc.PlayerId);
                            SuddenDeathMode.TeamYellow.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamGreen.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamPurple.Remove(pc.PlayerId);
                        }
                        else
                        if (1.7 <= pos.x && pos.x <= 3 && -1.1 <= pos.y && pos.y <= 0.7 && Options.SuddenTeamYellow.GetBool())
                        {
                            SuddenDeathMode.TeamRed.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamBlue.Remove(pc.PlayerId);
                            if (!SuddenDeathMode.TeamYellow.Contains(pc.PlayerId))
                                SuddenDeathMode.TeamYellow.Add(pc.PlayerId);
                            SuddenDeathMode.TeamGreen.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamPurple.Remove(pc.PlayerId);
                        }
                        else
                        if (0.5f <= pos.x && pos.x <= 2.1 && 2.1 <= pos.y && pos.y <= 3.2 && Options.SuddenTeamGreen.GetBool())
                        {
                            SuddenDeathMode.TeamRed.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamBlue.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamYellow.Remove(pc.PlayerId);
                            if (!SuddenDeathMode.TeamGreen.Contains(pc.PlayerId))
                                SuddenDeathMode.TeamGreen.Add(pc.PlayerId);
                            SuddenDeathMode.TeamPurple.Remove(pc.PlayerId);
                        }
                        else
                        if (-2.9 <= pos.x && pos.x <= -1.2 && 2.2 <= pos.y && pos.y <= 3.0 && Options.SuddenTeamPurple.GetBool())
                        {
                            SuddenDeathMode.TeamRed.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamBlue.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamYellow.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamGreen.Remove(pc.PlayerId);
                            if (!SuddenDeathMode.TeamPurple.Contains(pc.PlayerId))
                                SuddenDeathMode.TeamPurple.Add(pc.PlayerId);
                        }
                        else if (!GameStates.IsCountDown && !GameStates.Intro)
                        {
                            SuddenDeathMode.TeamRed.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamBlue.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamYellow.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamGreen.Remove(pc.PlayerId);
                            SuddenDeathMode.TeamPurple.Remove(pc.PlayerId);
                        }
                    }

                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                        var color = "#ffffff";
                        if (SuddenDeathMode.TeamRed.Contains(pc.PlayerId)) color = ModColors.codered;
                        if (SuddenDeathMode.TeamBlue.Contains(pc.PlayerId)) color = ModColors.codeblue;
                        if (SuddenDeathMode.TeamYellow.Contains(pc.PlayerId)) color = ModColors.codeyellow;
                        if (SuddenDeathMode.TeamGreen.Contains(pc.PlayerId)) color = ModColors.codegreen;
                        if (SuddenDeathMode.TeamPurple.Contains(pc.PlayerId)) color = ModColors.codepurple;
                        foreach (var seer in PlayerCatch.AllPlayerControls)
                        {
                            if (pc.name != "Player(Clone)" && seer.name != "Player(Clone)" && seer.PlayerId != PlayerControl.LocalPlayer.PlayerId && !seer.IsModClient())
                                pc.RpcSetNamePrivate($"<color={color}>{pc.Data.PlayerName}", true, seer, false);
                        }
                    }
                    if (!SuddenDeathMode.CheckTeam && GameStates.IsCountDown)
                    {
                        GameStartManager.Instance.ResetStartState();
                        Utils.SendMessage(Translator.GetString("SuddendeathLobbyError"));
                    }
                }
                if (GameStates.InGame)
                {
                    List<byte> del = new();
                    foreach (var ventpc in CoEnterVentPatch.VentPlayers)
                    {
                        var pc = PlayerCatch.GetPlayerById(ventpc.Key);
                        if (pc == null) continue;

                        if (ventpc.Value > Options.MaxInVentTime.GetFloat())
                        {
                            if (!CoEnterVentPatch.VentPlayers.TryGetValue(ventpc.Key, out var a))
                            {
                                del.Add(ventpc.Key);
                                continue;
                            }
                            pc.MyPhysics.RpcBootFromVent(VentilationSystemUpdateSystemPatch.NowVentId.TryGetValue(ventpc.Key, out var r) ? r : 0);
                            del.Add(ventpc.Key);
                        }
                        CoEnterVentPatch.VentPlayers[ventpc.Key] += Time.fixedDeltaTime;
                    }
                    del.Do(id => CoEnterVentPatch.VentPlayers.Remove(id));
                    DisableDevice.FixedUpdate();
                    //情報機器制限
                    var nowuseing = true;
                    if (DisableDevice.optTimeLimitCamAndLog > 0 && DisableDevice.GameLogAndCamTimer > DisableDevice.optTimeLimitCamAndLog)
                        nowuseing = false;

                    if (DisableDevice.optTurnTimeLimitCamAndLog > 0 && DisableDevice.TurnLogAndCamTimer > DisableDevice.optTurnTimeLimitCamAndLog)
                        nowuseing = false;

                    if (DisableDevice.UseCount > 0)
                    {
                        if (nowuseing)
                        {
                            if (DisableDevice.optTimeLimitDevices)
                                DisableDevice.GameLogAndCamTimer += Time.fixedDeltaTime * DisableDevice.UseCount;
                            if (DisableDevice.optTurnTimeLimitDevice)
                                DisableDevice.TurnLogAndCamTimer += Time.fixedDeltaTime * DisableDevice.UseCount;
                        }
                        else
                        {
                            DisableDevice.UseCount = 0;
                        }
                    }
                    if (Main.NowSabotage) Main.sabotagetime += Time.fixedDeltaTime;

                    if (!GameStates.Meeting && PlayerControl.LocalPlayer.IsAlive())
                    {
                        if (Main.MessagesToSend.Count > 0)
                        {
                            var pc = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
                            if (pc != null)
                            {
                                (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
                                if (sendTo != byte.MaxValue)
                                {
                                    Main.MessagesToSend.RemoveAt(0);
                                    var sendpc = PlayerCatch.GetPlayerById(sendTo);
                                    int clientId = sendpc.GetClientId();
                                    if (sendpc == null) return;
                                    var name = pc.Data.PlayerName;
                                    if (clientId == -1)
                                    {
                                        pc.SetName(title);
                                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(pc, msg);
                                        pc.SetName(name);
                                    }
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                    writer.StartMessage(clientId);
                                    writer.StartRpc(pc.NetId, (byte)RpcCalls.SetName)
                                        .Write(player.Data.NetId)
                                        .Write(title)
                                        .EndRpc();
                                    writer.StartRpc(pc.NetId, (byte)RpcCalls.SendChat)
                                        .Write(msg)
                                        .EndRpc();
                                    writer.StartRpc(pc.NetId, (byte)RpcCalls.SetName)
                                        .Write(player.Data.NetId)
                                        .Write(pc.Data.PlayerName)
                                        .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                    }
                    //ラバーズ
                    Lovers.LoversSuicide();
                    Lovers.RedLoversSuicide();
                    Lovers.YellowLoversSuicide();
                    Lovers.BlueLoversSuicide();
                    Lovers.GreenLoversSuicide();
                    Lovers.WhiteLoversSuicide();
                    Lovers.PurpleLoversSuicide();
                    Lovers.MadonnLoversSuicide();
                    Lovers.OneLoveSuicide();

                    //サドンデスモード
                    if (SuddenDeathMode.NowSuddenDeathMode)
                    {
                        if (Options.SuddenDeathTimeLimit.GetFloat() > 0) SuddenDeathMode.SuddenDeathReactor();
                        if (Options.SuddenItijohoSend.GetBool()) SuddenDeathMode.ItijohoSend();
                    }

                    //ネームカラー
                    if (!(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && (roleinfo?.IsDesyncImpostor ?? false) && !__instance.Data.IsDead)
                        foreach (var p in PlayerCatch.AllPlayerControls)
                        {
                            if (!p || (p?.Data == null)) continue;
                            p.Data.Role.NameColor = Color.white;
                        }

                    //カモフラ
                    if (Camouflage.ventplayr.Count != 0)
                    {
                        var remove = new List<byte>();
                        foreach (var id in Camouflage.ventplayr)
                        {
                            var target = PlayerCatch.GetPlayerById(id);
                            if (target.inVent) continue;

                            if (Camouflage.IsCamouflage)
                            {
                                var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");
                                byte color = (byte)ModColors.PlayerColor.Gray;

                                target.SetColor(color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                                    .Write(target.Data.NetId)
                                    .Write(color)
                                    .EndRpc();

                                target.SetHat("", color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                                    .Write("")
                                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                                    .EndRpc();

                                target.SetSkin("", color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                                    .Write("")
                                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                                    .EndRpc();

                                target.SetVisor("", color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                                    .Write("")
                                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                                    .EndRpc();
                                sender.SendMessage();
                            }
                            else Camouflage.RpcSetSkin(target);

                            remove.Add(id);
                        }
                        if (Options.Onlyseepet.GetBool()) PlayerCatch.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
                        remove.ForEach(task => Camouflage.ventplayr.Remove(task));
                    }
                }

                var kiruta = GameStates.IsInTask && !GameStates.Intro && __instance.Is(CustomRoles.Amnesiac) && !(roleclass as Amnesiac).omoidasita;
                //キルターゲットの上書き処理
                if (GameStates.IsInTask && !GameStates.Intro && ((!(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && (roleinfo?.IsDesyncImpostor ?? false)) || kiruta) && !__instance.Data.IsDead)
                {
                    var target = __instance.killtarget();
                    if (!__instance.CanUseKillButton()) target = null;
                    HudManager.Instance.KillButton.SetTarget(target);
                }
            }

            if ((GameStates.InGame || GameStates.Intro) && PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Monochromer)))
            {
                if (!Camouflage.PlayerSkins.TryGetValue(__instance.PlayerId, out var outfit))
                {
                    __instance.Data.DefaultOutfit.ColorId = outfit.ColorId;
                    __instance.Data.DefaultOutfit.HatId = outfit.HatId;
                    __instance.Data.DefaultOutfit.SkinId = outfit.SkinId;
                    __instance.Data.DefaultOutfit.VisorId = outfit.VisorId;
                }
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                if (GameStates.IsLobby)
                {
                    if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                    {
                        if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                            __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                        else if (Main.version.CompareTo(ver.version) == 0)
                            __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                        else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                    }
                    else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;

                    if (Options.SuddenTeamOption.GetBool())
                    {
                        var color = "#ffffff";
                        if (SuddenDeathMode.TeamRed.Contains(__instance.PlayerId)) color = ModColors.codered;
                        if (SuddenDeathMode.TeamBlue.Contains(__instance.PlayerId)) color = ModColors.codeblue;
                        if (SuddenDeathMode.TeamYellow.Contains(__instance.PlayerId)) color = ModColors.codeyellow;
                        if (SuddenDeathMode.TeamGreen.Contains(__instance.PlayerId)) color = ModColors.codegreen;
                        if (SuddenDeathMode.TeamPurple.Contains(__instance.PlayerId)) color = ModColors.codepurple;

                        __instance.cosmetics.nameText.text = $"{__instance.cosmetics.nameText.text}<color={color}>★</color>";
                    }
                }
                if (GameStates.IsInGame)
                {

                    (RoleText.enabled, RoleText.text) = UtilsRoleText.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, __instance, PlayerControl.LocalPlayer == __instance);
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                    }

                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var seerRole = seer.GetRoleClass();
                    var seerSubrole = seer.GetCustomSubRoles();
                    var target = __instance;
                    string name = "";
                    bool nomarker = false;
                    string RealName;
                    Mark.Clear();
                    Suffix.Clear();

                    //名前を一時的に上書きするかのチェック
                    var TemporaryName = roleclass?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                    //名前変更
                    RealName = TemporaryName ? name : target.GetRealName();

                    //NameColorManager準拠の処理
                    RealName = RealName.ApplyNameColorData(seer, target, false);

                    //seerに関わらず発動するMark
                    Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        if (PlayerControl.LocalPlayer.PlayerId == __instance.PlayerId)
                        {
                            if (!Options.EnableGM.GetBool())
                            {
                                if (Options.TaskBattelShowAllTask.GetBool())
                                {
                                    var t1 = 0f;
                                    var t2 = 0;
                                    if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                                    {
                                        foreach (var pc in PlayerCatch.AllPlayerControls)
                                        {
                                            t1 += pc.GetPlayerTaskState().AllTasksCount;
                                            t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                                        }
                                    }
                                    else
                                    {
                                        foreach (var t in TaskBattle.TaskBattleTeams)
                                        {
                                            if (!t.Contains(seer.PlayerId)) continue;
                                            t1 = Options.TaskBattleTeamWinTaskc.GetFloat();
                                            foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                                t2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                        }
                                    }
                                    Mark.Append($"<color=yellow>({t2}/{t1})</color>");
                                }
                                if (Options.TaskBattleShowFastestPlayer.GetBool())
                                {
                                    var to = 0;
                                    if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                                    {
                                        foreach (var pc in PlayerCatch.AllPlayerControls)
                                            if (pc.GetPlayerTaskState().CompletedTasksCount > to) to = pc.GetPlayerTaskState().CompletedTasksCount;
                                    }
                                    else
                                        foreach (var t in TaskBattle.TaskBattleTeams)
                                        {
                                            var to2 = 0;
                                            foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                                to2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                            if (to2 > to) to = to2;
                                        }
                                    Mark.Append($"<color=#00f7ff>({to})</color>");
                                }
                            }
                        }
                        else
                        {
                            if (Options.TaskBattelCanSeeOtherPlayer.GetBool())
                                Mark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");
                        }
                    }
                    else
                    {
                        var targetlover = target.GetRiaju();
                        var seerisonelover = seerSubrole.Contains(CustomRoles.OneLove);
                        //ハートマークを付ける(会議中MOD視点)
                        if ((targetlover == seer.GetRiaju() && targetlover is not CustomRoles.OneLove and not CustomRoles.NotAssigned)
                        || (seer.Data.IsDead && target.IsRiaju() && targetlover != CustomRoles.OneLove))
                        {
                            Mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(targetlover), "♥"));
                        }
                        else
                        if ((Lovers.OneLovePlayer.Ltarget == target.PlayerId && target.PlayerId != seer.PlayerId && seerisonelover)
                        || (target.Is(CustomRoles.OneLove) && target.PlayerId != seer.PlayerId && seerisonelover)
                        || (seer.Data.IsDead && target.Is(CustomRoles.OneLove) && !seerisonelover))
                        {
                            Mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡"));
                        }

                        if ((target.Is(CustomRoles.Connecting) && seerSubrole.Contains(CustomRoles.Connecting)
                        && !target.Is(CustomRoles.WolfBoy) && seerRole is not WolfBoy)
                        || (target.Is(CustomRoles.Connecting) && seer.Data.IsDead))
                        {
                            Mark.Append($"<color={UtilsRoleText.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                        }

                        //seerに関わらず発動するLowerText
                        Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
                        //追放者
                        if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "" && seer.PlayerId == target.PlayerId)
                        {
                            Suffix.Append("<color=#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                        }
                        //seer役職が対象のSuffix
                        if (Amnesia.CheckAbility(player))
                        {
                            Mark.Append(seerRole?.GetMark(seer, target, false));

                            if (target.Is(CustomRoles.Workhorse))
                            {
                                if (((seerRole as Alien)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                                || ((seerRole as JackalAlien)?.modeProgresskiller == true && JackalAlien.ProgressWorkhorseseen)
                                || (seerRole is ProgressKiller) && ProgressKiller.ProgressWorkhorseseen)
                                {
                                    Mark.Append($"<color=blue>♦</color>");
                                }
                            }
                            Suffix.Append(seerRole?.GetSuffix(seer, target));
                        }

                        //seerに関わらず発動するSuffix
                        Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                        if ((Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                        || (seer.Is(CustomRoles.Monochromer) && seer.IsAlive())
                        || Camouflager.NowUse
                        || (SuddenDeathMode.SuddenCannotSeeName && !TemporaryName))
                            RealName = $"<size=0>{RealName}</size> ";
                    }
                    bool? canseedeathreasoncolor = seer.PlayerId.CanDeathReasonKillerColor() == true ? true : null;
                    string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"<size=75%>({Utils.GetVitalText(target.PlayerId, canseedeathreasoncolor)})</size>" : "";

                    //Mark・Suffixの適用
                    if (!seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false)
                        target.cosmetics.nameText.text = $"{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}";
                    else
                        target.cosmetics.nameText.text = $"<color=#ffffff>{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}</color>";

                    if (Suffix.ToString() != "" && (!TemporaryName || (TemporaryName && !nomarker)))
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();
                    }
                    else
                    {
                        //役職テキストの座標を初期値に戻す
                        RoleText.transform.SetLocalY(0.2f);
                    }
                }
                else
                {
                    if (PlayerControl.LocalPlayer.PlayerId == __instance.PlayerId)
                        PlayerControl.LocalPlayer.cosmetics.nameText.text = Main.lobbyname == "" ? DataManager.player.Customization.Name : Main.lobbyname;
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            roleText.transform.localScale = new(1f, 1f, 1f);
            roleText.fontSize = Main.RoleTextSize;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
    class SetColorPatch
    {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor)
        {
            //色変更バグ対策
            if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if (AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance, true);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static Dictionary<byte, float> VentPlayers = new();
        static bool MadBool = false;
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                var user = __instance.myPlayer;

                if (MadBool)
                {
                    MadBool = false;
                    return true;
                }

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                    __instance.RpcBootFromVent(id);

                if (user.Is(CustomRoles.DemonicVenter)) return true;

                var roleClass = user.GetRoleClass();
                var pos = __instance.transform.position;
                if (Amnesia.CheckAbilityreturn(user)) roleClass = null;

                if ((!roleClass?.OnEnterVent(__instance, id) ?? false) || !CanUse(__instance, id))
                {
                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle) return true;
                    //一番遠いベントに追い出す
                    var sender = CustomRpcSender.Create("Farthest Vent")
                        .StartMessage();
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == user || pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue; //本人とホストは別の処理
                        Dictionary<int, float> Distance = new();
                        Vector2 position = pc.transform.position;
                        //一番遠いベントを調べて送る
                        foreach (var vent in ShipStatus.Instance.AllVents)
                            Distance.Add(vent.Id, Vector2.Distance(position, vent.transform.position));
                        var ventid = Distance.OrderByDescending(x => x.Value).First().Key;
                        sender.AutoStartRpc(__instance.NetId, (byte)RpcCalls.BootFromVent, pc.GetClientId())
                            .Write(ventid)
                            .EndRpc();
                        __instance.myPlayer.RpcSnapToForced(pos);
                    }
                    sender.EndMessage();
                    sender.SendMessage(); //多分負荷あれだし、テープで無理やり戻した感じだから参考にしない方がいい、

                    /*MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);*/

                    _ = new LateTask(() =>
                    {
                        int clientId = user.GetClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                        __instance.myPlayer.RpcSnapToForced(pos);
                    }, __instance.myPlayer != PlayerControl.LocalPlayer ? 0.8f : 0.3f, "Fix DesyncImpostor Stuck", null);
                    return false;
                }

                //マッドでベント移動できない設定なら矢印を消す
                if ((!roleClass?.CantVentIdo(__instance, id) ?? false) ||
                    (user.GetCustomRole().IsMadmate() && !Options.MadmateCanMovedByVent.GetBool()))
                {
                    if (!MadBool && user.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        MadBool = true;
                    int clientId = user.GetClientId();
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.1f, "Vent- BootFromVent", true);
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.25f, "Vent- EnterVent", null);
                    //BootFromVentで255とかしてもできるが、タイミングがよくわかんないので上ので今のとこはおｋ
                }
                CustomRoleManager.OnEnterVent(__instance, id);
            }
            if (Options.MaxInVentMode.GetBool())
                VentPlayers.TryAdd(__instance.myPlayer.PlayerId, 0);
            return true;
        }
        static bool CanUse(PlayerPhysics pp, int id)
        {
            //役職処理はここで行ってしまうと色々とめんどくさくなるので上で。
            var user = pp.myPlayer;

            if (!(user.Data.Role.Role == RoleTypes.Engineer || user.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() == RoleTypes.Engineer))//エンジニアでなく
            {
                if (!user.CanUseImpostorVentButton()) //インポスターベントも使えない
                {
                    Logger.Info($"{pp.name}はエンジニアでもインポスターベントも使えないため弾きます。", "OnenterVent");
                    return false;
                }
            }
            if (Utils.CanVent)
            {
                Logger.Info($"{pp.name}がベントに入ろうとしましたがベントが無効化されているので弾きます。", "OnenterVent");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
    class ExitVentPatch
    {
        public static void Prefix(PlayerPhysics __instance)
        {
            CoEnterVentPatch.VentPlayers.Remove(__instance.myPlayer.PlayerId);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] uint taskid)
        {
            var pc = __instance;

            Logger.Info($"TaskComplete:{pc.GetNameWithRole().RemoveHtmlTags()} {taskid}", "CompleteTask");
            var taskState = pc.GetPlayerTaskState();
            taskState.Update(pc);

            var roleClass = pc.GetRoleClass();
            var roleinfo = pc.GetCustomRole().GetRoleInfo();
            var ret = true;

            //タスクバトルの処理　タスバトならこれ以外の処理いらない。
            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
            {
                TaskBattle.TaskBattleCompleteTask(pc, taskState);
                return ret;
            }

            if (roleClass != null)
            {
                if (Amnesia.CheckAbility(pc))
                    ret = roleClass.OnCompleteTask(taskid);
            }

            CustomRoleManager.onCompleteTaskOthers(__instance, ret);

            if (pc.Is(CustomRoles.Amnesia))
                if (Amnesia.TriggerTask.GetBool() && taskState.HasCompletedEnoughCountOfTasks(Amnesia.Task.GetInt()))
                {
                    if (!Utils.RoleSendList.Contains(pc.PlayerId)) Utils.RoleSendList.Add(pc.PlayerId);
                    Amnesia.Kesu(pc.PlayerId);

                    taskState.hasTasks = UtilsTask.HasTasks(pc.Data, false);

                    if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                        pc.RpcSetRoleDesync(roleinfo.BaseRoleType.Invoke(), pc.GetClientId());
                    else
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        if (roleinfo?.IsDesyncImpostor == true && roleinfo?.BaseRoleType.Invoke() is not null and not RoleTypes.Impostor)
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, roleinfo.BaseRoleType.Invoke());

                    pc.SyncSettings();
                    _ = new LateTask(() =>
                    {
                        pc.SetKillCooldown(Main.AllPlayerKillCooldown[pc.PlayerId], kyousei: true, delay: true);
                        pc.RpcResetAbilityCooldown(kousin: true);
                    }, 0.2f, "ResetAbility");
                }

            //属性クラスの扱いを決定するまで仮置き
            ret &= Workhorse.OnCompleteTask(pc);
            if (UtilsTask.TaskCh) UtilsNotifyRoles.NotifyRoles();

            if (ret && UtilsTask.TaskCh)
            {
                if (taskState.CompletedTasksCount < taskState.AllTasksCount) return ret;
                if (!UtilsTask.HasTasks(pc.Data)) return ret;
                UtilsGameLog.AddGameLog("Task", string.Format(Translator.GetString("Taskfin"), Utils.GetPlayerColor(pc, true)));
            }
            return ret;
        }
        public static void Postfix()
        {
            //人外のタスクを排除して再計算
            GameData.Instance.RecomputeTaskCounts();
            Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    class PlayerControlProtectPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (__instance == target) return;
            Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "ProtectPlayer");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
    class PlayerControlRemoveProtectionPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            //Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}", "RemoveProtection");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole().RemoveHtmlTags();
            Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                roleType = RoleTypes.Shapeshifter;
                return true;
            }
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                var targetIsKiller = target.GetRoleClass() is IKiller;
                var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
                foreach (var seer in PlayerCatch.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.GetRoleClass() is IKiller;

                    {
                        ghostRoles[seer] = RoleTypes.CrewmateGhost;
                    }
                }
                if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
                {
                    roleType = RoleTypes.CrewmateGhost;
                }
                else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
                {
                    roleType = RoleTypes.ImpostorGhost;
                }
                else
                {
                    foreach ((var seer, var role) in ghostRoles)
                    {
                        Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole().RemoveHtmlTags()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(role, seer.GetClientId());
                    }
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    public static class PlayerControlDiePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                // 死者の最終位置にペットが残るバグ対応
                __instance.RpcSetPet("");

                if (__instance.Is(CustomRoles.Amnesia))//アムネシア削除
                {
                    Amnesia.Kesu(__instance.PlayerId);
                }

                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
                {
                    if (!SuddenDeathMode.NowSuddenDeathMode)
                        if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId)//サボ可能役職のみインポスターゴーストにする
                            if (__instance.GetCustomRole().IsImpostor() || ((__instance.GetRoleClass() as IKiller)?.CanUseSabotageButton() ?? false))
                                _ = new LateTask(() =>
                                {
                                    if (!GameStates.Meeting)
                                        foreach (var Player in PlayerCatch.AllPlayerControls)
                                        {
                                            if (Player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                            __instance.RpcSetRoleDesync(RoleTypes.ImpostorGhost, Player.GetClientId());
                                        }
                                }, 1.4f, "Fix sabotage", true);

                    _ = new LateTask(() => GhostRoleAssingData.AssignAddOnsFromList(), 1.4f, "Fix sabotage", true);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MixUpOutfit))]
    public static class PlayerControlMixupOutfitPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!__instance.IsAlive())
            {
                return;
            }
            // 自分がDesyncインポスターで，バニラ判定ではインポスターの場合，バニラ処理で名前が非表示にならないため，相手の名前を非表示にする
            if (
                PlayerControl.LocalPlayer.Data.Role.IsImpostor &&  // バニラ判定でインポスター
                !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) &&  // Mod判定でインポスターではない
                PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)  // Desyncインポスター
            {
                // 名前を隠す
                __instance.cosmetics.ToggleNameVisible(false);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
    public static class PlayerControlCheckSporeTriggerPatch
    {
        public static bool Prefix()
        {
            if (Options.DisableFungleSporeTrigger.GetBool())
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPhantomPatch
    {
        [HarmonyPatch(nameof(PlayerControl.CheckVanish)), HarmonyPrefix]
        public static bool Prefix(PlayerControl __instance)
        {
            //Logger.Info($"{__instance?.Data?.PlayerName}", "CheckVanish and Appear");
            var resetkillcooldown = false;
            bool? fall = false;

            if (__instance.GetRoleClass() is IUsePhantomButton iusephantombutton && Main.CanUseAbility)
                iusephantombutton.CheckOnClick(ref resetkillcooldown, ref fall);

            float k = 0;
            IUsePhantomButton.IPPlayerKillCooldown.TryGetValue(__instance.PlayerId, out k);
            Main.AllPlayerKillCooldown.TryGetValue(__instance.PlayerId, out var killcool);
            /* 初手で、キルク修正がオフでキルクが10s以上で、キルが0回*/
            if (MeetingStates.FirstMeeting && !Options.FixFirstKillCooldown.GetBool() && killcool > 10 && PlayerState.GetByPlayerId(__instance.PlayerId)?.GetKillCount() is 0 or null)
                killcool = 10;
            float cooldown = killcool - k;
            if (cooldown <= 1) cooldown = 0.005f;

            if (!resetkillcooldown)
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] = cooldown < 10 ? cooldown : cooldown * 2;
                __instance.SyncSettings();
            }
            var writer = CustomRpcSender.Create("Phantom OneClick", SendOption.None);
            writer.StartMessage(__instance.GetClientId());

            /*モーションのせいで1.5秒位ベント、キル不可になるのが...
            writer.StartRpc(__instance.NetId, (byte)RpcCalls.StartVanish)
                .EndRpc();
            writer.StartRpc(__instance.NetId, (byte)RpcCalls.StartAppear)
                .Write(true)//falseにしたら動かない。多分ベント用
                .EndRpc();*/
            if (fall is not null)
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.Impostor)
                    .Write(true)
                    .EndRpc();
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.Phantom)
                    .Write(true)
                    .EndRpc();
            }
            if (!resetkillcooldown && !(cooldown < 10))
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.MurderPlayer)
                    .WriteNetObject(__instance)
                    .Write((int)MurderResultFlags.FailedProtected)
                    .EndRpc();
            }
            if (fall == false)
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.ProtectPlayer)
                    .WriteNetObject(__instance)
                    .Write(0)
                    .EndRpc();
            }
            writer.EndMessage();
            writer.SendMessage();
            __instance.ResetKillCooldown();

            return false;
        }
    }
}