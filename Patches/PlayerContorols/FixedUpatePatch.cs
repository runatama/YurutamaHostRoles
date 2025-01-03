using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using AmongUs.Data;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Patches.ISystemType;
using AmongUs.GameOptions;

namespace TownOfHost
{

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
                CustomSpawnEditor.FixedUpdate(__instance);
                return;
            }

            if (!GameStates.IsModHost) return;

            if (Main.RTAMode && GameStates.IsInTask && Main.introDestroyed)
            {
                if (Main.RTAPlayer != byte.MaxValue && Main.RTAPlayer == player.PlayerId)
                {
                    HudManagerPatch.LowerInfoText.enabled = true;
                    HudManagerPatch.LowerInfoText.text = HudManagerPatch.GetTaskBattleTimer();
                    if (HudManagerPatch.TaskBattleTimer != 0 || player.MyPhysics.Animations.IsPlayingRunAnimation())
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
                    else if (first.Value <= 2 && !VentDuringDisabling.ContainsKey(player.PlayerId) && (((roleclass as IKiller)?.CanUseImpostorVentButton() is false) || (roleclass?.CanClickUseVentButton == false)))
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
                                if (TaskBattle.TaskBattelShowAllTask.GetBool())
                                {
                                    var t1 = 0f;
                                    var t2 = 0;
                                    if (!TaskBattle.TaskBattleTeamMode.GetBool() && !TaskBattle.TaskBattleTeamWinType.GetBool())
                                    {
                                        foreach (var pc in PlayerCatch.AllPlayerControls)
                                        {
                                            t1 += pc.GetPlayerTaskState().AllTasksCount;
                                            t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                                        }
                                    }
                                    else
                                    {
                                        foreach (var t in TaskBattle.TaskBattleTeams.Values)
                                        {
                                            if (!t.Contains(seer.PlayerId)) continue;
                                            t1 = TaskBattle.TaskBattleTeamWinTaskc.GetFloat();
                                            foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                                t2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                        }
                                    }
                                    Mark.Append($"<color=yellow>({t2}/{t1})</color>");
                                }
                                if (TaskBattle.TaskBattleShowFastestPlayer.GetBool())
                                {
                                    var to = 0;
                                    if (!TaskBattle.TaskBattleTeamMode.GetBool() && !TaskBattle.TaskBattleTeamWinType.GetBool())
                                    {
                                        foreach (var pc in PlayerCatch.AllPlayerControls)
                                            if (pc.GetPlayerTaskState().CompletedTasksCount > to) to = pc.GetPlayerTaskState().CompletedTasksCount;
                                    }
                                    else
                                        foreach (var t in TaskBattle.TaskBattleTeams.Values)
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
                            if (TaskBattle.TaskBattelCanSeeOtherPlayer.GetBool())
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
}