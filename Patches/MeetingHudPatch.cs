using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;
using static TownOfHost.Translator;

namespace TownOfHost;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId /* 投票した人 */ , [HarmonyArgument(1)] byte suspectPlayerId /* 投票された人 */ )
        {
            if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting)
            {
                MeetingVoteManager.Instance?.SetVote(srcPlayerId, 253);
                return true;
            }
            var voter = PlayerCatch.GetPlayerById(srcPlayerId);
            var votefor = PlayerCatch.GetPlayerById(suspectPlayerId);

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                var roleClass = pc.GetRoleClass();
                if (Balancer.Id != 255 && !(suspectPlayerId == srcPlayerId || suspectPlayerId == Balancer.target1 || suspectPlayerId == Balancer.target2) && !pc.Is(CustomRoles.Balancer) && Balancer.OptionCanMeetingAbility.GetBool()) continue;
                if (Amnesia.CheckAbilityreturn(pc)) roleClass = null;

                if (roleClass?.CheckVoteAsVoter(suspectPlayerId, voter) == false || (!votefor.IsAlive() && suspectPlayerId != 253 && suspectPlayerId != 254))
                {
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole().RemoveHtmlTags()} は投票しない！ => {srcPlayerId}", nameof(CastVotePatch));
                    return false;
                }
                else
                if (voter.Is(CustomRoles.Elector) && suspectPlayerId == 253 || (RoleAddAddons.GetRoleAddon(voter.GetCustomRole(), out var da, voter, subrole: CustomRoles.Elector) && da.GiveElector.GetBool() && suspectPlayerId == 253))
                {
                    Utils.SendMessage("君はイレクターなんだよ。\nスキップできない属性でね。\n誰かに投票してね。", voter.PlayerId);
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole().RemoveHtmlTags()} イレクター発動 => {srcPlayerId}", nameof(CastVotePatch));
                    return false;
                }
            }
            MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
            return true;
        }
    }
    public static string Oniku = "";
    public static string Send = "";
    public static string Title = "";
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public class StartPatch
    {
        public static bool Serialize = false;
        public static void Prefix()
        {
            Logger.Info($"------------会議開始　day:{UtilsGameLog.day}------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            MeetingStates.Sending = true;
            GameStates.task = false;
            GameStates.AlreadyDied |= !PlayerCatch.IsAllAlive;
            PlayerCatch.OldAlivePlayerControles.Clear();
            var Sender = CustomRpcSender.Create("MeetingSet", Hazel.SendOption.Reliable);
            Sender.StartMessage();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                ReportDeadBodyPatch.WaitReport[pc.PlayerId].Clear();

                if (!pc.IsAlive())
                {
                    if (AntiBlackout.OverrideExiledPlayer()) continue;
                    Sender.StartRpc(pc.NetId, RpcCalls.Exiled)
                    .EndRpc();
                    Sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.CrewmateGhost)
                    .Write(true)
                    .EndRpc();
                }//  会議時に生きてたぜリスト追加
                else
                {
                    PlayerCatch.OldAlivePlayerControles.Add(pc);
                    if (AntiBlackout.OverrideExiledPlayer()) continue;
                    Sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.Crewmate)
                    .Write(true)
                    .EndRpc();
                }
            }
            Sender.EndMessage();
            Sender.SendMessage();
            ReportDeadBodyPatch.DontReport.Clear();
            MeetingStates.MeetingCalled = true;
            GameStates.Tuihou = false;

            if (Options.ExHideChatCommand.GetBool())
            {
                _ = new LateTask(() =>
                {
                    var count = 0;
                    Dictionary<byte, bool> State = new();
                    foreach (var player in PlayerCatch.AllAlivePlayerControls)
                    {
                        State.TryAdd(player.PlayerId, player.Data.IsDead);
                    }
                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                    {
                        if (!Main.IsCs() && Options.ExRpcWeightR.GetBool()) count++;

                        if (!State.ContainsKey(pc.PlayerId)) continue;
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                        if (pc.IsModClient()) continue;

                        _ = new LateTask(() =>
                        {
                            foreach (PlayerControl tg in PlayerCatch.AllAlivePlayerControls)
                            {
                                if (tg.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                if (tg.IsModClient()) continue;
                                tg.Data.IsDead = true;
                            }
                            pc.Data.IsDead = false;
                            Serialize = true;
                            RPC.RpcSyncAllNetworkedPlayer(pc.GetClientId());
                            Serialize = false;
                        }, count * 0.1f, "SetDienoNaka", true);
                    }
                    _ = new LateTask(() =>
                    {
                        foreach (PlayerControl player in PlayerCatch.AllAlivePlayerControls)
                        {
                            player.Data.IsDead = State.TryGetValue(player.PlayerId, out var data) && data;
                        }
                    }, count * 0.1f, "SetDienoNaka", true);
                }, 4f, "SetDie");
            }
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;

            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            var p = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
            var a = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);
            var list = p.ToArray().AddRangeToArray(a.ToArray());

            HudManagerPatch.LowerInfoText.text = myRole?.GetLowerText(PlayerControl.LocalPlayer, isForMeeting: true, isForHud: true) ?? "";
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia)) HudManagerPatch.LowerInfoText.text = "";
            if (myRole?.Jikaku() != CustomRoles.NotAssigned) HudManagerPatch.LowerInfoText.text = "";

            HudManagerPatch.LowerInfoText.enabled = HudManagerPatch.LowerInfoText.text != "";

            foreach (var pva in __instance.playerStates)
            {
                var pc = PlayerCatch.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;

                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.PlayerIcon.transform);
                roleTextMeeting.transform.localPosition = new Vector3(3.25f, 1.02f, -5f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = UtilsRoleText.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, pc, PlayerControl.LocalPlayer == pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                //見る側が双子で相方が双子の場合
                if (Twins.TwinsList.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var targetid))
                {
                    if (targetid == pc.PlayerId) roleTextMeeting.text = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Twins) + roleTextMeeting.text;
                }

                var suffixTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                suffixTextMeeting.transform.SetParent(pva.PlayerIcon.transform);
                suffixTextMeeting.transform.localPosition = new Vector3(3.25f, 0.02f, 0f);
                suffixTextMeeting.fontSize = 1.5f;
                suffixTextMeeting.gameObject.name = "suffixTextMeeting";
                suffixTextMeeting.enableWordWrapping = false;
                suffixTextMeeting.enabled = false;

                // NameTextにSetParentすると後に作ったのにも付いてきちゃうからこっちに
                var MeetingInfo = UnityEngine.Object.Instantiate(pva.NameText);
                MeetingInfo.transform.SetParent(pva.PlayerIcon.transform);
                MeetingInfo.transform.localPosition = new Vector3(3.13f, 1.71f, 0f);
                MeetingInfo.fontSize = 1.8f;
                MeetingInfo.gameObject.name = "MeetingInfo";
                MeetingInfo.enableWordWrapping = false;
                MeetingInfo.enabled = false;

                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    if (Amnesia.CheckAbility(PlayerControl.LocalPlayer))
                        suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                // suffixが0文字じゃなくて　　　　タグ、空白をきったら空にならない時は
                if (suffixBuilder.Length > 0 && suffixBuilder.ToString().RemoveHtmlTags().Trim(' ').Trim('　') != "")
                {
                    //下にSuffixを表示
                    suffixTextMeeting.text = suffixBuilder.ToString();
                    suffixTextMeeting.enabled = true;
                }
                else
                {
                    //そうじゃない時、上側ロールはなんか好まないので下に
                    roleTextMeeting.enabled = false;
                    suffixTextMeeting.text = roleTextMeeting.text;
                    suffixTextMeeting.enabled = true;
                }
                if (list[0] != null)
                    if (list[0].PlayerId == pc.PlayerId)
                    {
                        MeetingInfo.enabled = true;
                        MeetingInfo.text = $"<color=#ffffff><line-height=95%>" + $"Day.{UtilsGameLog.day}".Color(Palette.Orange) + $"\n{UtilsNotifyRoles.MeetingMoji}";
                        if (CustomRolesHelper.CheckGuesser() || PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)))
                        {
                            MeetingInfo.text = $"<size=50%>\n </size>{MeetingInfo.text}\n<size=50%><color=#999900>{GetString("GuessInfo")}</color></size>";
                        }
                        MeetingInfo.text += "<line-height=0%>\n</line-height></line-height><line-height=300%>\n</line-height></color> ";
                    }
            }
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());
            SlowStarter.OnStartMeeting();
            Send = "<size=80%>";
            Title = "";

            if (!Options.firstturnmeeting || !MeetingStates.FirstMeeting) Title += "<b>" + string.Format(GetString("Message.Day"), UtilsGameLog.day).Color(Palette.Orange) + "</b>\n";
            else Title += "<b>" + GetString("Message.first").Color(Palette.Orange) + "</b>\n";

            foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
            {
                var RoleText = roleClass.MeetingMeg();
                if (RoleText != "") Send += RoleText + "\n\n";
            }
            var Ghostrumortext = GhostRumour.SendMes();
            if (Ghostrumortext != "")
            {
                Send += Ghostrumortext + "\n";
            }
            if (Oniku != "")
            {
                Send += "<color=#001e43>※" + Oniku + "</color>\n";
            }
            if (Options.SyncButtonMode.GetBool())
            {
                Send += "<color=#006e54>★" + string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "</color>\n";
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer())
            {
                Send += "<color=#640125>！" + GetString("Warning.OverrideExiledPlayer") + "</color>\n";
            }
            if (!SelfVoteManager.Canuseability())
            {
                Send += "<color=#998317>◇" + GetString("Warning.CannotUseAbility") + "</color>\n";
            }
            if (MeetingVoteManager.Voteresult != "")
            {
                if (Send.RemoveHtmlTags() != "") Send += "\n";
                Send += "<size=120%>【" + GetString("LastMeetingre") + "】\n</size>" + MeetingVoteManager.Voteresult;
            }
            Send += $"\n{GetString("MeetingHelp")}";
            TemplateManager.SendTemplate("OnMeeting", noErr: true);
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            if (Send != "") Utils.SendMessage(Send, title: Title);
            foreach (var pva in __instance.playerStates)
            {
                var pc = PlayerCatch.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                if (MeetingStates.FirstMeeting) UtilsShowOption.SendRoleInfo(pc);
                if (Utils.RoleSendList.Contains(pva.TargetPlayerId)) UtilsShowOption.SendRoleInfo(pc);
            }
            MeetingVoteManager.Voteresult = "";
            Oniku = "";
            Utils.RoleSendList.Clear();
            if (AmongUsClient.Instance.AmHost)
            {
                //エアシなら始まった瞬間に展望いるならうるさいからワープさせる
                if (Main.NormalOptions.MapId == 4)
                {
                    foreach (var pl in PlayerCatch.AllPlayerControls)
                    {
                        if (pl.IsModClient()) continue;
                        Vector2 poji = pl.transform.position;
                        if (poji.y <= -13.6f) pl.RpcSnapToForced(new Vector2(poji.x, -13f));
                        if (poji.x >= 4.3f && poji.y <= -13.6f) pl.RpcSnapToForced(new Vector2(7.6f, -10.6f));
                    }
                }
                _ = new LateTask(() =>
                {
                    MeetingStates.Sending = false;
                    ChatUpdatePatch.DoBlockChat = false;
                }, 2f, "Send to Chat", true);
                _ = new LateTask(() => NameColorManager.RpcMeetingColorName(), 5f, "SetName", true);
                _ = new LateTask(() => NameColorManager.RpcMeetingColorName(), 10f, "SetName", true);
            }
            Main.NowSabotage =
                Utils.IsActive(SystemTypes.Reactor)
                || Utils.IsActive(SystemTypes.Electrical)
                || Utils.IsActive(SystemTypes.Laboratory)
                || Utils.IsActive(SystemTypes.Comms)
                || Utils.IsActive(SystemTypes.LifeSupp)
                || Utils.IsActive(SystemTypes.HeliSabotage);

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = PlayerCatch.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();
                var fsb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"<size=75%>({Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})</size>");

                if (Amnesia.CheckAbility(seer))
                    sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                //相手のサブロール処理
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    if (subRole is not CustomRoles.OneLove && subRole.IsRiaju() && (seer.GetRiaju() == subRole || seer.Data.IsDead))
                    {
                        sb.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(subRole), "♥"));
                        continue; ;
                    }
                    switch (subRole)
                    {
                        case CustomRoles.Connecting:
                            if ((seer.Is(CustomRoles.Connecting) && !seer.Is(CustomRoles.WolfBoy)) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Connecting), "Ψ"));
                            continue;
                    }
                }

                //本人のsubrole処理
                foreach (var subRole in seer.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Guesser:
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            continue;
                        case CustomRoles.LastImpostor:
                            if (!LastImpostor.giveguesser) continue;
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            continue;
                        case CustomRoles.LastNeutral:
                            if (!LastNeutral.GiveGuesser.GetBool()) continue;
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            continue;
                        case CustomRoles.OneLove:
                            if (target == seer) continue;
                            if (Lovers.OneLovePlayer.Ltarget == target.PlayerId || target.Is(CustomRoles.OneLove))
                                sb.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡"));
                            continue;
                    }
                }
                if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer, subrole: CustomRoles.Guesser) && data.GiveGuesser.GetBool())
                {
                    if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                        fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                }

                if (Options.CanSeeNextRandomSpawn.GetBool() && seer == target)
                {
                    if (RandomSpawn.SpawnMap.NextSpornName.TryGetValue(seer.PlayerId, out var r))
                        pva.NameText.text += $"<size=40%><color=#9ae3bd>〔{r}〕</size></color>";
                }

                //名前の適応　　　　　ゲッサー番号等　　名前　　　　　　　　　ラバー等のマーク
                pva.NameText.text = fsb.ToString() + pva.NameText.text + sb.ToString();

                if (list.LastOrDefault() != null)
                    if (list.LastOrDefault() == target)
                    {
                        var team = seer.GetCustomRole().GetCustomRoleTypes();
                        if (Options.CanSeeTimeLimit.GetBool() && DisableDevice.optTimeLimitDevices)
                        {
                            var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                            if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                            || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seer.IsAlive())
                                if (info != "")
                                {
                                    var Name = info.RemoveText() + "\n" + pva.NameText.text + "\n" + info;
                                    pva.NameText.text = Name;
                                }
                        }
                    }
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = PlayerCatch.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), Utils.GetPlayerColor(player, true)));
                    UtilsGameLog.AddGameLog("Executed", string.Format(GetString("Message.Executed"), Utils.GetPlayerColor(player, true)));
                    Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();

                    if (Options.ExHideChatCommand.GetBool())
                    {
                        StartPatch.Serialize = true;
                        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (pc == player) continue;
                            pc.Data.IsDead = false;
                        }
                        RPC.RpcSyncAllNetworkedPlayer(player.GetClientId());
                        StartPatch.Serialize = false;
                    }
                });
            }
            if (Balancer.Id != 255)
            {
                if (!PlayerCatch.GetPlayerById(Balancer.target1).IsAlive()
                    || !PlayerCatch.GetPlayerById(Balancer.target2).IsAlive())
                    MeetingVoteManager.Instance.EndMeeting(false);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                foreach (var p in SelfVoteManager.CheckVote)
                    SelfVoteManager.CheckVote[p.Key] = false;
                foreach (var pc in PlayerCatch.AllPlayerControls)
                    (pc.GetRoleClass() as IUseTheShButton)?.ResetS(pc);
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        string log = "";
        playerIds.Do(id => log += $"({id})");
        Logger.Info($"{log}を{deathReason}で会議後に処理するぜ!", "TryAddAfterMeetingDeathPlayers");

        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
            {
                ReportDeadBodyPatch.Musisuruoniku[playerId] = false;
                AddedIdList.Add(playerId);
                if (deathReason == CustomDeathReason.Revenge)
                {
                    MeetingVoteManager.Voteresult += "\n<size=60%>" + Utils.GetPlayerColor(PlayerCatch.GetPlayerById(playerId)) + GetString("votemi");
                    UtilsGameLog.AddGameLog("Revenge", Utils.GetPlayerColor(PlayerCatch.GetPlayerById(playerId)) + GetString("votemi"));
                }
            }

        //投票の道連れ処理は他でしてるのでここではしない。
        if (deathReason != CustomDeathReason.Vote) CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            if (CustomRoles.Lovers.IsPresent() && !Lovers.isLoversDead && Lovers.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.LoversSuicide(playerId, true);
            if (CustomRoles.RedLovers.IsPresent() && !Lovers.isRedLoversDead && Lovers.RedLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.RedLoversSuicide(playerId, true);
            if (CustomRoles.YellowLovers.IsPresent() && !Lovers.isYellowLoversDead && Lovers.YellowLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.YellowLoversSuicide(playerId, true);
            if (CustomRoles.BlueLovers.IsPresent() && !Lovers.isBlueLoversDead && Lovers.BlueLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.BlueLoversSuicide(playerId, true);
            if (CustomRoles.GreenLovers.IsPresent() && !Lovers.isGreenLoversDead && Lovers.GreenLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.GreenLoversSuicide(playerId, true);
            if (CustomRoles.WhiteLovers.IsPresent() && !Lovers.isWhiteLoversDead && Lovers.WhiteLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.WhiteLoversSuicide(playerId, true);
            if (CustomRoles.PurpleLovers.IsPresent() && !Lovers.isPurpleLoversDead && Lovers.PurpleLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.PurpleLoversSuicide(playerId, true);
            if (CustomRoles.MadonnaLovers.IsPresent() && !Lovers.isMadonnaLoversDead && Lovers.MaMadonnaLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.MadonnLoversSuicide(playerId, true);
            if (CustomRoles.OneLove.IsPresent() && !Lovers.isOneLoveDead)
                Lovers.OneLoveSuicide(playerId, true);
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = PlayerCatch.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, playerId);
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, CustomDeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();

        if (deathReason != CustomDeathReason.Vote) return null;

        if (Amnesia.CheckAbility(exiledplayer))
            if (exiledplayer.GetRoleClass() is INekomata nekomata)
            {
                // 道連れしない状態ならnull
                if (!nekomata.DoRevenge(deathReason))
                {
                    return null;
                }
                TargetList = PlayerCatch.AllAlivePlayerControls.Where(candidate => candidate != exiledplayer && !Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId) && nekomata.IsCandidate(candidate)).ToList();
            }
            else
            {
                var role = exiledplayer.GetCustomRole();
                var isMadmate =
                    role.IsMadmate() ||
                    // マッド属性化時に削除
                    (exiledplayer.GetRoleClass() is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
                foreach (var candidate in PlayerCatch.AllAlivePlayerControls)
                {
                    if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                    switch (role)
                    {
                        // ここにINekomata未適用の道連れ役職を追加
                        default:
                            if (RoleAddAddons.GetRoleAddon(role, out var data, exiledplayer, subrole: CustomRoles.Revenger))
                            {
                                if (deathReason == CustomDeathReason.Vote && data.GiveRevenger.GetBool())

                                {
                                    if ((candidate.Is(CustomRoleTypes.Impostor) && data.Imp.GetBool()) ||
                                        (candidate.Is(CustomRoleTypes.Neutral) && data.Neu.GetBool()) ||
                                        (candidate.Is(CustomRoleTypes.Crewmate) && data.Crew.GetBool()) ||
                                        (candidate.Is(CustomRoleTypes.Madmate) && data.Mad.GetBool()))
                                        TargetList.Add(candidate);
                                }
                            }
                            else
                                if (isMadmate && deathReason == CustomDeathReason.Vote && Options.MadmateRevengeCrewmate.GetBool())
                            {
                                if ((candidate.Is(CustomRoleTypes.Impostor) && Options.MadNekomataCanImp.GetBool()) ||
                                (candidate.Is(CustomRoleTypes.Neutral) && Options.MadNekomataCanNeu.GetBool()) ||
                                (candidate.Is(CustomRoleTypes.Crewmate) && Options.MadNekomataCanCrew.GetBool()) ||
                                (candidate.Is(CustomRoleTypes.Madmate) && Options.MadNekomataCanMad.GetBool()))
                                    TargetList.Add(candidate);
                            }
                            else
                                foreach (var subRole in exiledplayer.GetCustomSubRoles())
                                {
                                    switch (subRole)
                                    {
                                        case CustomRoles.Revenger:
                                            if (exiledplayer.Is(CustomRoles.Revenger) && deathReason == CustomDeathReason.Vote)
                                            {
                                                if (
                                                (candidate.Is(CustomRoleTypes.Impostor) && Revenger.Imp.GetBool()) ||
                                                (candidate.Is(CustomRoleTypes.Neutral) && Revenger.Neu.GetBool()) ||
                                                (candidate.Is(CustomRoleTypes.Crewmate) && Revenger.Crew.GetBool()) ||
                                                (candidate.Is(CustomRoleTypes.Madmate) && Revenger.Mad.GetBool()))
                                                    TargetList.Add(candidate);
                                            }
                                            break;
                                    }
                                }
                            break;
                    }
                }
            }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}
