using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;
using System;
using AmongUs.GameOptions;

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
        ///0→/m
        ///1→/n
        ///2→/n r
        ///-1→次ページへ

        /// <summary>
        /// 2→役職処理  0→通常処理 1→Infomode 3→Infomode(2)
        /// </summary>
        public static Dictionary<byte, int> InfoMode = new();
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId /* 投票した人 */ , [HarmonyArgument(1)] byte suspectPlayerId /* 投票された人 */ )
        {
            var voter = Utils.GetPlayerById(srcPlayerId);
            var votefor = Utils.GetPlayerById(suspectPlayerId);

            Dictionary<byte, int> Menu1 = new();
            Dictionary<byte, int> Menu2 = new();

            if (Options.UseSelfVoteInfo.GetBool())
            {
                var i = 0;
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (!pc.IsAlive()) continue;
                    if (pc.PlayerId == srcPlayerId) continue;

                    Menu1.Add(pc.PlayerId, i + 1 == Main.AllAlivePlayerControls.Count() ? -1 : i);
                    if (!(i + 1 == Main.AllAlivePlayerControls.Count())) i++;
                }
                if (i <= 2)
                {
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (!pc.IsAlive()) continue;
                        if (pc.PlayerId == srcPlayerId) continue;
                        Menu2.Add(pc.PlayerId, i);
                        i++;
                    }
                }

                if (srcPlayerId == suspectPlayerId)
                {
                    if (InfoMode[srcPlayerId] == 0)//InfoMode発動
                    {
                        var s = GetString("InfoF") + "<size=70%>";
                        __instance.RpcClearVote(voter.GetClientId());
                        InfoMode[srcPlayerId] = 1;
                        foreach (var no in Menu1.Keys)
                        {
                            if (Menu1.ContainsKey(no))
                            {
                                if (Menu1[no] == 0)
                                    s += "\n" + Utils.GetPlayerById(no).name + GetString("InfoNo1");
                                if (Menu1[no] == 1)
                                    s += "\n" + Utils.GetPlayerById(no).name + GetString("InfoNo2");
                                if (Menu1[no] == 2)
                                    s += "\n" + Utils.GetPlayerById(no).name + GetString("InfoNo3");
                                if (Menu1[no] == -1)
                                    s += "\n" + Utils.GetPlayerById(no).name + GetString("InfoNext");
                            }
                        }
                        Utils.SendMessage(s, srcPlayerId);
                        return false;
                    }
                    if (InfoMode[srcPlayerId] == 1)//InfoMode解除(役職能力)
                    {
                        __instance.RpcClearVote(voter.GetClientId());
                        InfoMode[srcPlayerId] = 2;
                        Utils.SendMessage(GetString("InfoRole"), srcPlayerId);
                        return false;
                    }
                }

                if (InfoMode[srcPlayerId] == 1)
                {
                    __instance.RpcClearVote(voter.GetClientId());
                    var ch = false;
                    if (Menu1.ContainsKey(suspectPlayerId))
                    {
                        var p = Menu1[suspectPlayerId];
                        if (p == -1)
                        {
                            ch = true;
                            InfoMode[srcPlayerId] = 3;

                            Utils.SendMessage(GetString("InfoMenu2"), srcPlayerId);
                        }
                        if (p == 0)// /m
                        {
                            var role = voter.GetCustomRole();
                            if (voter.Is(CustomRoles.Amnesia)) role = voter.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                            if (role == CustomRoles.Braid) role = CustomRoles.Driver;
                            if (voter.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && voter.GetRoleClass() != null) role = voter.GetRoleClass().Jikaku();
                            if (voter.IsGorstRole()) role = PlayerState.GetByPlayerId(voter.PlayerId).GhostRole;
                            var RoleTextData = Utils.GetRoleColorCode(role);
                            string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                            string RoleInfoTitle = $"<color={RoleTextData}>{RoleInfoTitleString}";
                            if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                                Utils.SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(voter.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{voter.GetRoleInfo(true)}" + voter.GetRoleInfo(true), voter.PlayerId, RoleInfoTitle);
                            else if (role.GetRoleInfo()?.Description is { } description) Utils.SendMessage(description.FullFormatHelp, voter.PlayerId, RoleInfoTitle, removeTags: false);
                            else Utils.SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(voter.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{voter.GetRoleInfo(true)}", voter.PlayerId, RoleInfoTitle);
                            Utils.GetAddonsHelp(voter);
                        }
                        if (p == 1)// /n
                            Utils.ShowActiveSettings(srcPlayerId);
                        if (p == 2)// /n r
                            Utils.ShowActiveRoles(srcPlayerId);
                    }
                    if (!ch)
                    {
                        InfoMode[srcPlayerId] = 0;
                        Utils.SendMessage(GetString("InfoVote"), srcPlayerId);
                    }
                    return false;
                }
                if (InfoMode[srcPlayerId] == 3)
                {
                    __instance.RpcClearVote(voter.GetClientId());

                    var p = Menu2[suspectPlayerId];
                    if (Menu2.ContainsKey(suspectPlayerId))
                    {
                        if (p == 1)// /n
                            Utils.ShowActiveSettings(srcPlayerId);
                        if (p == 2)// /n r
                            Utils.ShowActiveRoles(srcPlayerId);
                    }
                    InfoMode[srcPlayerId] = 0;
                    Utils.SendMessage(GetString("InfoVote"), srcPlayerId);
                    return false;
                }
            }

            foreach (var pc in Main.AllPlayerControls)
                if (pc.GetRoleClass()?.CheckVoteAsVoter(suspectPlayerId, voter) == false || (!votefor.IsAlive() && suspectPlayerId != 253 && suspectPlayerId != 254))
                {
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole()} は投票しない！ => {srcPlayerId}", nameof(CastVotePatch));
                    InfoMode[srcPlayerId] = 2;
                    return false;
                }
                else
                if (voter.Is(CustomRoles.Elector) && suspectPlayerId == 253 || (RoleAddAddons.AllData.TryGetValue(voter.GetCustomRole(), out var da) && da.GiveAddons.GetBool() && da.GiveElector.GetBool() && suspectPlayerId == 253))
                {
                    Utils.SendMessage("君はイレクターなんだよ。\nスキップできない属性でね。\n誰かに投票してね。", voter.PlayerId);
                    __instance.RpcClearVote(voter.GetClientId());
                    InfoMode[srcPlayerId] = 2;
                    Logger.Info($"{voter.GetNameWithRole()} イレクター発動 => {srcPlayerId}", nameof(CastVotePatch));
                    return false;
                }

            MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
            return true;
        }
    }
    public static string Oniku = "";
    public static string Send = "";
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info("------------会議開始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            MeetingStates.MeetingCalled = true;

            if (!AntiBlackout.OverrideExiledPlayer)
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    foreach (var Player in Main.AllPlayerControls)
                    {
                        if (!Player.IsAlive() && (Player.GetCustomRole().IsImpostor() || (Player?.CanUseSabotageButton() ?? false)))
                            foreach (var pc in Main.AllPlayerControls)
                            {
                                if (pc == PlayerControl.LocalPlayer) continue;
                                Player.RpcSetRoleDesync(RoleTypes.CrewmateGhost, pc.GetClientId());
                            }
                    }
                }
            }
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (CastVotePatch.InfoMode.ContainsKey(pc.PlayerId))
                    CastVotePatch.InfoMode[pc.PlayerId] = 0;
            }

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;

            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = Utils.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, pc, PlayerControl.LocalPlayer == pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;

                // 役職とサフィックスを同時に表示する必要が出たら要改修
                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                if (suffixBuilder.Length > 0)
                {
                    roleTextMeeting.text = suffixBuilder.ToString();
                    roleTextMeeting.enabled = true;
                }
            }
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());
            Send = "";

            Send += "<size=110%><b>" + string.Format(GetString("Message.Day"), Main.day).Color(Palette.Orange) + "</b></size>\n";
            foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
            {
                var RoleText = roleClass.MeetingMeg();
                if (RoleText != "") Send += RoleText + "\n\n";
            }
            if (Oniku != "")
            {
                Send += "<color=#001e43><size=80%>※" + Oniku + "</size></color>\n";

            }
            if (Options.SyncButtonMode.GetBool())
            {
                Send += "<size=80%><color=#006e54>★" + string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "</size></color>\n";
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer)
            {
                Send += "<color=#640125><size=80%>！" + GetString("Warning.OverrideExiledPlayer") + "</size></color>\n";
            }
            if (MeetingVoteManager.Voteresult != "")
            {
                if (Send != "") Send += "\n";
                Send += "<size=120%>【" + GetString("LastMeetingre") + "】\n</size>" + MeetingVoteManager.Voteresult;
            }
            TemplateManager.SendTemplate("OnMeeting", noErr: true);
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            if (Send != "") Utils.SendMessage(Send);
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                if (Options.ShowRoleAtFirstMeeting.GetBool() && MeetingStates.FirstMeeting) Utils.SendRoleInfo(pc);
                if (Utils.OKure) Utils.SendRoleInfo(pc);
            }
            MeetingVoteManager.Voteresult = "";
            Oniku = "";
            Utils.OKure = false;
            if (AmongUsClient.Instance.AmHost)
            {
                //エアシなら始まった瞬間に展望いるならうるさいからワープさせる
                if (Main.NormalOptions.MapId == 4)
                {
                    foreach (var p in Main.AllPlayerControls)
                    {
                        if (p.IsModClient()) continue;
                        Vector2 poji = p.transform.position;
                        if (poji.y <= -13.6f) p.RpcSnapToForced(new Vector2(poji.x, -13f));
                        if (poji.x >= 4.3f && poji.y <= -13.6f) p.RpcSnapToForced(new Vector2(7.6f, -10.6f));
                    }
                }//名前のなんかの処理
                _ = new LateTask(() =>
                {
                    foreach (var seen in Main.AllPlayerControls)
                    {
                        var seenName = seen.GetRealName(isMeeting: true);
                        var coloredName = Utils.ColorString(seen.GetRoleColor(), seenName);
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            seen.RpcSetNamePrivate(
                                seer == seen ? coloredName : seenName,
                                true,
                                seer);
                        }
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();
                var fsb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})");

                sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                //相手のサブロール処理
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.ALovers:
                            if (seer.Is(CustomRoles.ALovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ALovers), "♥"));
                            break;
                        case CustomRoles.BLovers:
                            if (seer.Is(CustomRoles.BLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BLovers), "♥"));
                            break;
                        case CustomRoles.CLovers:
                            if (seer.Is(CustomRoles.CLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CLovers), "♥"));
                            break;
                        case CustomRoles.DLovers:
                            if (seer.Is(CustomRoles.DLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DLovers), "♥"));
                            break;
                        case CustomRoles.ELovers:
                            if (seer.Is(CustomRoles.ELovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.ELovers), "♥"));
                            break;
                        case CustomRoles.FLovers:
                            if (seer.Is(CustomRoles.FLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.FLovers), "♥"));
                            break;
                        case CustomRoles.GLovers:
                            if (seer.Is(CustomRoles.GLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.GLovers), "♥"));
                            break;
                        case CustomRoles.MaLovers:
                            if (seer.Is(CustomRoles.MaLovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.MaLovers), "♥"));
                            break;
                        case CustomRoles.Connecting:
                            if ((seer.Is(CustomRoles.Connecting) && !seer.Is(CustomRoles.WolfBoy)) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Connecting), "Ψ"));
                            break;
                    }
                }

                //本人のsubrole処理
                foreach (var subRole in seer.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Guesser:
                            if (!seer.Is(CustomRoles.Guesser)) break;
                            if (!seer.Data.IsDead && target == seer)
                                fsb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), $"<line-height=100%><size=50%>{GetString("GuessInfo")}</size>\n"));
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            break;
                        case CustomRoles.LastImpostor:
                            if (!LastImpostor.GiveGuesser.GetBool()) break;
                            if (!seer.Data.IsDead && target == seer)
                                fsb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), $"<line-height=100%><size=50%>{GetString("GuessInfo")}</size>\n"));
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            break;
                        case CustomRoles.LastNeutral:
                            if (!LastNeutral.GiveGuesser.GetBool()) break;
                            if (!seer.Data.IsDead && target == seer)
                                fsb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), $"<line-height=100%><size=50%>{GetString("GuessInfo")}</size>\n"));
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            break;
                    }
                }
                if (RoleAddAddons.AllData.TryGetValue(seer.GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GiveGuesser.GetBool())
                {
                    if (!seer.Data.IsDead && target == seer)
                        fsb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), $"<line-height=100%><size=50%>{GetString("GuessInfo")}</size>\n"));
                    if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                        fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                }

                var Info = "";
                //会議画面ではインポスター自身の名前にSnitchマークはつけません。
                var p = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                var a = Main.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);

                if (p.ToArray().AddRangeToArray(a.ToArray())[0] != null)
                    if (p.ToArray().AddRangeToArray(a.ToArray())[0] == target)
                    {
                        Info = $"<color=#ffffff><line-height=95%>" + $"Day.{Main.day}".Color(Palette.Orange) + $"\n{Utils.MeetingMoji}<line-height=0%>\n</line-height></line-height><line-height=300%>\n</line-height></color> ";
                    }
                pva.NameText.text = sb.ToString().RemoveText() + (Info == "" ? "" : "\n") + Info + fsb.ToString() + pva.NameText.text + sb.ToString() + fsb.ToString().RemoveText() + Info.RemoveText() + ((Info != "" && seer != target) ? "\n " : "");
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
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), Utils.GetPlayerColor(player, true)));
                    Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Executed]　" + string.Format(GetString("Message.Executed"), Utils.GetPlayerColor(player, true));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                });
            }
            if (Balancer.Id != 255)
            {
                if (!Utils.GetPlayerById(Balancer.target1).IsAlive()
                    || !Utils.GetPlayerById(Balancer.target2).IsAlive())
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
                foreach (var pc in Main.AllPlayerControls)
                    (pc.GetRoleClass() as IUseTheShButton)?.ResetS(pc);
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
            {
                ReportDeadBodyPatch.Musisuruoniku[playerId] = false;
                AddedIdList.Add(playerId);
                if (deathReason == CustomDeathReason.Revenge && Options.VRcanseemitidure.GetBool())
                    MeetingVoteManager.Voteresult += "\n<size=60%>" + Utils.GetPlayerColor(Utils.GetPlayerById(playerId)) + GetString("votemi");
                if (deathReason == CustomDeathReason.Revenge)
                    Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Revenge]　" + Utils.GetPlayerColor(Utils.GetPlayerById(playerId)) + GetString("votemi");
            }

        //投票の道連れ処理は他でしてるのでここではしない。
        if (deathReason != CustomDeathReason.Vote) CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            if (CustomRoles.ALovers.IsPresent() && !Main.isALoversDead && Main.ALoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.ALoversSuicide(playerId, true);
            if (CustomRoles.BLovers.IsPresent() && !Main.isBLoversDead && Main.BLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.BLoversSuicide(playerId, true);
            if (CustomRoles.CLovers.IsPresent() && !Main.isCLoversDead && Main.CLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.CLoversSuicide(playerId, true);
            if (CustomRoles.DLovers.IsPresent() && !Main.isDLoversDead && Main.DLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.DLoversSuicide(playerId, true);
            if (CustomRoles.ELovers.IsPresent() && !Main.isELoversDead && Main.ELoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.ELoversSuicide(playerId, true);
            if (CustomRoles.FLovers.IsPresent() && !Main.isFLoversDead && Main.FLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.FLoversSuicide(playerId, true);
            if (CustomRoles.GLovers.IsPresent() && !Main.isGLoversDead && Main.GLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.GLoversSuicide(playerId, true);
            //MaL
            if (CustomRoles.MaLovers.IsPresent() && !Main.isMaLoversDead && Main.MaMaLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.MadonnaLoversSuicide(playerId, true);
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
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

        if (exiledplayer.GetRoleClass() is INekomata nekomata)
        {
            // 道連れしない状態ならnull
            if (!nekomata.DoRevenge(deathReason))
            {
                return null;
            }
            TargetList = Main.AllAlivePlayerControls.Where(candidate => candidate != exiledplayer && !Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId) && nekomata.IsCandidate(candidate)).ToList();
        }
        else
        {
            var isMadmate =
                exiledplayer.Is(CustomRoleTypes.Madmate) ||
                // マッド属性化時に削除
                (exiledplayer.GetRoleClass() is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
            foreach (var candidate in Main.AllAlivePlayerControls)
            {
                if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                switch (exiledplayer.GetCustomRole())
                {
                    // ここにINekomata未適用の道連れ役職を追加
                    default:
                        if (RoleAddAddons.AllData.TryGetValue(exiledplayer.GetCustomRole(), out var data) && data.GiveAddons.GetBool())
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
