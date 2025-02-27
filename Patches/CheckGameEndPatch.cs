using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Roles;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Ghost;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            //ゲーム終了判定済みなら中断
            if (predicate == null) return false;

            //ゲーム終了しないモードで廃村以外の場合は中断
            if (Main.DontGameSet && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            //廃村用に初期値を設定
            var reason = GameOverReason.ImpostorByKill;

            //ゲーム終了判定
            predicate.CheckForEndGame(out reason);

            //ゲーム終了時
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                //カモフラージュ強制解除
                PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

                if (Options.CurrentGameMode != CustomGameMode.Standard || !SuddenDeathMode.NowSuddenDeathMode)
                    switch (CustomWinnerHolder.WinnerTeam)
                    {
                        case CustomWinner.Crewmate:
                            if (Monochromer.CheckWin(reason)) break;

                            PlayerCatch.AllPlayerControls
                                .Where(pc => pc.Is(CustomRoleTypes.Crewmate) && !pc.GetCustomRole().IsRiaju()
                                && !pc.Is(CustomRoles.Amanojaku) && !pc.Is(CustomRoles.Jackaldoll) && !pc.Is(CustomRoles.SKMadmate)
                                && ((pc.Is(CustomRoles.Staff) && (pc.GetRoleClass() as Staff).EndedTaskInAlive) || !pc.Is(CustomRoles.Staff)))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                            foreach (var pc in PlayerCatch.AllPlayerControls)
                            {
                                if (pc.GetCustomRole() is CustomRoles.SKMadmate or CustomRoles.Jackaldoll ||
                                    pc.IsRiaju())
                                    CustomWinnerHolder.IdRemoveLovers.Add(pc.PlayerId);
                            }
                            break;
                        case CustomWinner.Impostor:
                            if (Egoist.CheckWin()) break;

                            PlayerCatch.AllPlayerControls
                                .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate) || pc.Is(CustomRoles.SKMadmate)) && (!pc.GetCustomRole().IsRiaju() || !pc.Is(CustomRoles.Jackaldoll)))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                            foreach (var pc in PlayerCatch.AllPlayerControls)
                            {
                                if (pc.GetCustomRole() is CustomRoles.Jackaldoll ||
                                    pc.IsRiaju())
                                    CustomWinnerHolder.IdRemoveLovers.Add(pc.PlayerId);
                            }
                            break;
                        default:
                            //ラバー勝利以外の時にラバーをしめt...勝利を剥奪する処理。
                            //どーせ追加なら追加勝利するやろし乗っ取りなら乗っ取りやし。
                            if (CustomWinnerHolder.WinnerTeam.IsRiaju())
                                break;
                            PlayerCatch.AllPlayerControls
                                .Where(p => p.IsRiaju())
                                .Do(p => CustomWinnerHolder.IdRemoveLovers.Add(p.PlayerId));
                            break;
                    }
                //チーム戦で勝者がチームじゃない時(単独勝利とかね)
                if (SuddenDeathMode.NowSuddenDeathTemeMode && !(CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathRed or CustomWinner.SuddenDeathBlue or CustomWinner.SuddenDeathGreen or CustomWinner.SuddenDeathYellow or CustomWinner.PurpleLovers))
                {
                    foreach (var wi in CustomWinnerHolder.WinnerIds)
                    {
                        if (SuddenDeathMode.TeamRed.Contains(wi))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathRed);
                            SuddenDeathMode.TeamRed.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                            break;
                        }
                        if (SuddenDeathMode.TeamBlue.Contains(wi))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathBlue);
                            SuddenDeathMode.TeamBlue.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                            break;
                        }
                        if (SuddenDeathMode.TeamYellow.Contains(wi))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathYellow);
                            SuddenDeathMode.TeamYellow.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                            break;
                        }
                        if (SuddenDeathMode.TeamGreen.Contains(wi))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathGreen);
                            SuddenDeathMode.TeamGreen.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                            break;
                        }
                        if (SuddenDeathMode.TeamPurple.Contains(wi))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathPurple);
                            SuddenDeathMode.TeamPurple.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                            break;
                        }
                    }
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
                {
                    if (!reason.Equals(GameOverReason.HumansByTask))
                    {
                        Lovers.LoversSoloWin(ref reason);
                    }
                    if (reason.Equals(GameOverReason.HumansByTask))//タスクの場合リア充敗北☆
                    {
                        PlayerCatch.AllPlayerControls
                            .Where(p => p.IsRiaju())
                            .Do(p => CustomWinnerHolder.IdRemoveLovers.Add(p.PlayerId));
                    }
                    Lovers.LoversAddWin();
                    //追加勝利陣営
                    foreach (var pc in PlayerCatch.AllPlayerControls.Where(pc => !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) || pc.Is(CustomRoles.PhantomThief) || pc.Is(CustomRoles.Turncoat) || pc.Is(CustomRoles.AsistingAngel)))
                    {
                        var isAlive = pc.IsAlive();
                        if (Amnesia.CheckAbility(pc))
                            if (pc.GetRoleClass() is IAdditionalWinner additionalWinner && !pc.Is(CustomRoles.PhantomThief) && !pc.Is(CustomRoles.Turncoat) && !pc.IsRiaju())
                            {
                                var winnerRole = pc.GetCustomRole();
                                if (additionalWinner.CheckWin(ref winnerRole))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                    CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                                    continue;
                                }
                            }
                        if (!pc.Is(CustomRoles.Terrorist) && !pc.Is(CustomRoles.Madonna) && pc.Is(CustomRoles.LastNeutral) && isAlive && LastNeutral.GiveOpportunist.GetBool() && !pc.IsRiaju())
                        {
                            if (reason.Equals(GameOverReason.HumansByTask) && !LastNeutral.CanNotTaskWin.GetBool()) continue;
                            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate && reason.Equals(GameOverReason.HumansByVote) && !reason.Equals(GameOverReason.HumansByTask) && !LastNeutral.CanNotCrewWin.GetBool()) continue;
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.LastNeutral);
                            continue;
                        }
                        if (pc.Is(CustomRoles.Amanojaku) && !reason.Equals(GameOverReason.HumansByTask) && !reason.Equals(GameOverReason.HumansByVote)
                        && (!pc.Is(CustomRoles.LastNeutral) || !LastNeutral.GiveOpportunist.GetBool()) && (isAlive || !Amanojaku.Seizon.GetBool()) && !pc.IsRiaju())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Amanojaku);
                            continue;
                        }
                        else if (pc.Is(CustomRoles.Amanojaku)) CustomWinnerHolder.IdRemoveLovers.Add(pc.PlayerId);

                        if (Amnesia.CheckAbility(pc))
                            if (pc.GetRoleClass() is IAdditionalWinner additionalWinner && pc.Is(CustomRoles.PhantomThief))
                            {
                                var winnerRole = pc.GetCustomRole();
                                if (additionalWinner.CheckWin(ref winnerRole))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                    CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                                    continue;
                                }
                            }

                        if (pc.Is(CustomRoles.AsistingAngel))
                        {
                            if (AsistingAngel.Asist != null)
                            {
                                if (CustomWinnerHolder.WinnerIds.Contains(AsistingAngel.Asist.PlayerId))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.AsistingAngel);
                                    continue;
                                }
                                else
                                    CustomWinnerHolder.IdRemoveLovers.Add(pc.PlayerId);
                            }
                            else
                                CustomWinnerHolder.IdRemoveLovers.Add(pc.PlayerId);
                        }

                        if (Amnesia.CheckAbility(pc))
                            if (pc.GetRoleClass() is IAdditionalWinner additionalWinner && pc.Is(CustomRoles.Turncoat))
                            {
                                var winnerRole = pc.GetCustomRole();
                                if (additionalWinner.CheckWin(ref winnerRole))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                    CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                                    continue;
                                }
                            }
                    }
                }
                Twins.CheckAddWin();
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Draw)
                {
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.CurseMaker))
                            foreach (var cm in CurseMaker.curseMakers)
                            {
                                if (cm.CanWin)
                                {
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CurseMaker);
                                    CustomWinnerHolder.WinnerIds.Add(cm.Player.PlayerId);
                                }
                            }
                        if (pc.GetRoleClass() is Fox fox)
                        {
                            if (fox.FoxCheckWin(ref reason)) break;
                        }
                    }
                }
                ShipStatus.Instance.enabled = false;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate && (reason.Equals(GameOverReason.HumansByTask) || reason.Equals(GameOverReason.HumansByVote)))
                    reason = GameOverReason.ImpostorByKill;
                if (Options.OutroCrewWinreasonchenge.GetBool() && (reason.Equals(GameOverReason.HumansByTask) || reason.Equals(GameOverReason.HumansByVote)))
                    reason = GameOverReason.ImpostorByVote;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason)
        {
            /*if (Options.UseCustomRpcSenderAtGameEnd.GetBool())
            {
                var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
                sender.StartMessage(-1); // 5: GameData
                MessageWriter writer = sender.stream;

                // バニラ画面でのアウトロを正しくするために色々
                var winner = CustomWinnerHolder.WinnerTeam;
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (winner == CustomWinner.Draw)
                    {
                        SetGhostRole(ToGhostImpostor: true);
                        continue;
                    }
                    bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                            CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                    bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
                    SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

                    void SetGhostRole(bool ToGhostImpostor)
                    {
                        var isDead = pc.Data.IsDead;
                        RoleTypes role = ToGhostImpostor ?
                            isDead ? RoleTypes.ImpostorGhost : RoleTypes.Impostor :
                            isDead ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate;

                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)role)
                            .Write(true)
                            .EndRpc();
                        pc.StartCoroutine(pc.CoSetRole(role, true));
                        Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: {role}に変更", "ResetRoleAndEndGame");
                    }
                }

                // CustomWinnerHolderの情報の同期
                sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
                CustomWinnerHolder.WriteTo(sender.stream);
                sender.EndRpc();

                sender.EndMessage();

                //Outroのテキストを名前に変換してバニラにも表示
                sender.StartMessage(-1);
                SetRoleSummaryText(sender);
                sender.EndMessage();

                // バニラ側のゲーム終了RPC
                writer.StartMessage(8); //8: EndGame
                {
                    writer.Write(AmongUsClient.Instance.GameId); //GameId
                    writer.Write((byte)reason); //GameoverReason
                    writer.Write(false); //showAd
                }
                writer.EndMessage();

                sender.SendMessage();
                _ = new LateTask(() =>
                {
                    //3s経ってもアウトロに届いてないから強制終了()
                    if (GameStates.InGame) GameManager.Instance.RpcEndGame(reason, false);
                }, 3f, "", true);

            }
            else*/
            AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
        }
        private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
        {
            GameStates.IsOutro = true;
            // サーバー側のパケットサイズ制限によりCustomRpcSenderが利用できないため，遅延を挟むことで順番の整合性を保つ．

            // バニラ画面でのアウトロを正しくするためのゴーストロール化
            List<byte> ReviveRequiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                        CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                canWin &= !CustomWinnerHolder.IdRemoveLovers.Contains(pc.PlayerId);
                bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
                SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

                void SetGhostRole(bool ToGhostImpostor)
                {
                    var isDead = pc.Data.IsDead;
                    if (!isDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.ImpostorGhost, false);
                    }
                    else
                    {
                        Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.CrewmateGhost, false);
                    }
                    // 蘇生までの遅延の間にオートミュートをかけられないように元に戻しておく
                    pc.Data.IsDead = isDead;
                }
            }

            // CustomWinnerHolderの情報の同期
            var winnerWriter = self.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
            CustomWinnerHolder.WriteTo(winnerWriter);
            self.FinishRpcImmediately(winnerWriter);

            // 蘇生を確実にゴーストロール設定の後に届けるための遅延
            yield return new WaitForSeconds(EndGameDelay);

            if (ReviveRequiredPlayerIds.Count > 0)
            {
                // 蘇生 パケットが膨れ上がって死ぬのを防ぐため，1送信につき1人ずつ蘇生する
                for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
                {
                    var playerId = ReviveRequiredPlayerIds[i];
                    var playerInfo = GameData.Instance.GetPlayerById(playerId);
                    // 蘇生
                    playerInfo.IsDead = false;
                    // 送信
                    playerInfo.MarkDirty();
                    AmongUsClient.Instance.SendAllStreamedObjects();
                }
                // ゲーム終了を確実に最後に届けるための遅延
                yield return new WaitForSeconds(EndGameDelay);
            }
            yield return new WaitForSeconds(EndGameDelay);
            //ちゃんとバニラに試合結果表示させるための遅延
            try
            {
                SetRoleSummaryText();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "SetRoleSummaryText");
                Logger.seeingame("非クライアントへのアウトロテキスト生成中にエラーが発生しました。");
            }
            yield return new WaitForSeconds(EndGameDelay);

            // ゲーム終了
            GameManager.Instance.RpcEndGame(reason, false);
        }
        private static void SetRoleSummaryText(CustomRpcSender sender = null)
        {
            var winners = new List<PlayerControl>(); //先に処理
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winners.Add(pc);
            }
            foreach (var team in CustomWinnerHolder.WinnerRoles)
            {
                winners.AddRange(PlayerCatch.AllPlayerControls.Where(p => p.Is(team) && !winners.Contains(p)));
            }
            foreach (var id in CustomWinnerHolder.IdRemoveLovers)
            {
                var pc = PlayerCatch.GetPlayerById(id);
                if (pc == null) continue;
                winners.Remove(pc);
            }

            List<byte> winnerList = new();
            if (winners.Count != 0)
                foreach (var pc in winners)
                {
                    if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;
                    if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) && winnerList.Contains(pc.PlayerId)) continue;
                    if (CustomWinnerHolder.IdRemoveLovers.Contains(pc.PlayerId)) continue;

                    winnerList.Add(pc.PlayerId);
                }

            var sb = new StringBuilder();
            string[] rtaStr = null;
            if (Main.RTAMode && Options.CurrentGameMode == CustomGameMode.TaskBattle && CustomWinnerHolder.WinnerTeam is not (CustomWinner.Draw or CustomWinner.None))
                rtaStr = UtilsGameLog.GetRTAText(winnerList).ToString().Split('\n');//RTA&廃村以外の時のみ取得/他はnull
            sb.Append("<align=left><ktag-voffset><pos=-44><size=1><color=white>" + (rtaStr == null ? Translator.GetString("RoleSummaryText") : rtaStr[0]) + "</voffset>");

            var (CustomWinnerText, CustomWinnerColor, _, _, _) = UtilsGameLog.GetWinnerText(winnerList: winnerList);
            var winnerSize = GetScale(CustomWinnerText.RemoveHtmlTags().Length, 2, 3.3);
            // フォントサイズを制限
            CustomWinnerText = $"<size={winnerSize}>{CustomWinnerText}</size>";

            static double GetScale(int input, double min, double max)
                => min + (max - min) * (1 - (double)(input - 1) / 13);//大きさ調整するやつ

            if (rtaStr == null)//nullならRTAではないので通常ログを追加
            {
                List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);

                if (winnerList.Count != 0)
                    foreach (var id in winnerList)
                    {
                        sb.Replace("<ktag-voffset>", "");
                        sb.Append($"\n<pos=-44><ktag-voffset><color={CustomWinnerColor}>★</color> </pos>").Append(Regex.Replace(UtilsGameLog.SummaryTexts(id), @"<pos=(\d+(\.\d+)?)em>", m => $"<pos={float.Parse(m.Groups[1].Value) - 41}em>") + "</voffset>");
                        cloneRoles.Remove(id);
                    }
                if (cloneRoles.Count != 0)
                    foreach (var id in cloneRoles)
                    {
                        sb.Replace("<ktag-voffset>", "");
                        sb.Append($"\n<pos=-44><ktag-voffset>　 </pos>").Append(Regex.Replace(UtilsGameLog.SummaryTexts(id), @"<pos=(\d+(\.\d+)?)em>", m => $"<pos={float.Parse(m.Groups[1].Value) - 41}em>") + "</voffset>");
                    }
            }
            else
            {
                rtaStr = rtaStr.Skip(1).ToArray();
                foreach (var text in rtaStr)
                {
                    sb.Replace("<ktag-voffset>", "");
                    sb.Append($"\n<pos=-44><ktag-voffset>{text}</pos></voffset>");
                }
            }
            sb.Replace("<ktag-voffset>", $"<voffset={15 - (1.5 * sb.ToString().Split('\n').Length * (0.2 * winnerSize))}>");
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (pc == null) continue;
                var target = (winnerList.Contains(pc.PlayerId) ? pc : (winnerList.Count == 0 ? pc : PlayerCatch.GetPlayerById(winnerList.OrderBy(pc => pc).FirstOrDefault()) ?? pc)) ?? pc;
                var targetname = Main.AllPlayerNames[target.PlayerId].Color(UtilsRoleText.GetRoleColor(target.GetCustomRole()));
                var text = sb.ToString() + $"\n</align><voffset=23>{CustomWinnerText}\n<voffset=45><size=1.75>{targetname}";
                if (sender == null)
                    target.RpcSetNamePrivate(text, true, pc, true);
                else
                {
                    sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName, pc.GetClientId())
                        .Write(pc.Data.NetId)
                        .Write(text)
                        .Write(true)
                        .EndRpc();
                }
            }
        }
        private const float EndGameDelay = 0.2f;

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();
        public static void SetPredicateToTaskBattle() => predicate = new TaskBattleGameEndPredicate();

        public static void SetPredicateToSadness() => predicate = new SadnessGameEndPredicate();
        class SadnessGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (checkplayer(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }
            public static bool checkplayer(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;


                if (!PlayerCatch.AllAlivePlayerControls.Any())
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                    return true;
                }
                else if (SuddenDeathMode.NowSuddenDeathTemeMode)
                {
                    if (PlayerCatch.AllAlivePlayerControls.All(pc => SuddenDeathMode.TeamRed.Contains(pc.PlayerId)))
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathRed);
                        SuddenDeathMode.TeamRed.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                        return true;
                    }
                    if (PlayerCatch.AllAlivePlayerControls.All(pc => SuddenDeathMode.TeamBlue.Contains(pc.PlayerId)))
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathBlue);
                        SuddenDeathMode.TeamBlue.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                        return true;
                    }
                    if (PlayerCatch.AllAlivePlayerControls.All(pc => SuddenDeathMode.TeamYellow.Contains(pc.PlayerId)))
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathYellow);
                        SuddenDeathMode.TeamYellow.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                        return true;
                    }
                    if (PlayerCatch.AllAlivePlayerControls.All(pc => SuddenDeathMode.TeamGreen.Contains(pc.PlayerId)))
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathGreen);
                        SuddenDeathMode.TeamGreen.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                        return true;
                    }
                    if (PlayerCatch.AllAlivePlayerControls.All(pc => SuddenDeathMode.TeamPurple.Contains(pc.PlayerId)))
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathPurple);
                        SuddenDeathMode.TeamPurple.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                        return true;
                    }
                }
                else if (PlayerCatch.AllAlivePlayersCount == 1)
                {
                    var winner = PlayerCatch.AllAlivePlayerControls.FirstOrDefault();

                    CustomWinnerHolder.ResetAndSetWinner((CustomWinner)winner.GetCustomRole());
                    CustomWinnerHolder.WinnerIds.Add(winner.PlayerId);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers)) ||
                 (Lovers.LoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.LoversPlayers.Count != 0 && Lovers.LoversPlayers.All(pc => pc.IsAlive()))) //ラバーズ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.RedLovers)) ||
                (Lovers.RedLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.RedLoversPlayers.Count != 0 && Lovers.RedLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedLovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.YellowLovers)) ||
                (Lovers.YellowLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.YellowLoversPlayers.Count != 0 && Lovers.YellowLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YellowLovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.BlueLovers)) ||
                (Lovers.BlueLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.BlueLoversPlayers.Count != 0 && Lovers.BlueLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueLovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.GreenLovers)) ||
                (Lovers.GreenLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.GreenLoversPlayers.Count != 0 && Lovers.GreenLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GreenLovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.WhiteLovers)) ||
                (Lovers.WhiteLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.WhiteLoversPlayers.Count != 0 && Lovers.WhiteLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.WhiteLovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PurpleLovers)) ||
                (Lovers.PurpleLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.PurpleLoversPlayers.Count != 0 && Lovers.PurpleLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PurpleLovers);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayersCount <= 2 && PlayerCatch.AllAlivePlayerControls.All(pc => pc.PlayerId == Lovers.OneLovePlayer.Ltarget || pc.PlayerId == Lovers.OneLovePlayer.OneLove)
                || (Lovers.OneLoveSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.OneLove)?.IsAlive() == true && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.Ltarget)?.IsAlive() == true))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.OneLove);
                    if (!Lovers.OneLovePlayer.doublelove) CustomWinnerHolder.WinnerIds.Add(Lovers.OneLovePlayer.Ltarget);
                    return true;
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.MadonnaLovers)) ||
                (Madonna.MaLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.MaMadonnaLoversPlayers.Count != 0 && Lovers.MaMadonnaLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MadonnaLovers);
                    return true;
                }
                return false;
            }
        }

        // ===== ゲーム終了条件 =====
        // 通常ゲーム用
        class NormalGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int Imp = 0;
                int Jackal = 0;
                int Crew = 0;
                int Remotekiller = 0;
                int GrimReaper = 0;
                int Fox = 0;
                int FoxAndCrew = 0;

                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    switch (pc.GetCountTypes())
                    {
                        case CountTypes.Crew: Crew++; FoxAndCrew++; break;
                        case CountTypes.Impostor: Imp++; break;
                        case CountTypes.Jackal: Jackal++; break;
                        case CountTypes.Remotekiller: Remotekiller++; break;
                        case CountTypes.GrimReaper: GrimReaper++; break;
                        case CountTypes.Fox:
                            if (pc.GetRoleClass() is Fox fox)
                            {
                                Fox++;
                                FoxAndCrew += fox.FoxCount();
                            }
                            break;
                    }
                }
                if (Jackal == 0 && (CustomRoles.Jackal.IsPresent() || CustomRoles.JackalMafia.IsPresent() || CustomRoles.JackalAlien.IsPresent()))
                    foreach (var player in PlayerCatch.AllAlivePlayerControls)
                    {
                        if (player.Is(CustomRoles.Jackaldoll) && JackalDoll.Oyabun.ContainsKey(player.PlayerId))
                        {
                            Jackal++;
                            Crew--;
                            FoxAndCrew--;
                            break;
                        }
                    }

                if (Imp == 0 && FoxAndCrew == 0 && Jackal == 0 && Remotekiller == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }/* ここら辺のLoversは全員時の処理のはずだから追加勝利考えない */
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers)) ||
                /* 設定がONで、　　　　　　　　　　　　　　　　3人以下で、　　　　　　　　　　　　　　　　ラバーが全員生きてたら */
                (Lovers.LoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.LoversPlayers.Count != 0 && Lovers.LoversPlayers.All(pc => pc.IsAlive()))) //ラバーズ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.RedLovers)) ||
                (Lovers.RedLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.RedLoversPlayers.Count != 0 && Lovers.RedLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.RedLovers);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.YellowLovers)) ||
                (Lovers.YellowLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.YellowLoversPlayers.Count != 0 && Lovers.YellowLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YellowLovers);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.BlueLovers)) ||
                (Lovers.BlueLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.BlueLoversPlayers.Count != 0 && Lovers.BlueLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.BlueLovers);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.GreenLovers)) ||
                (Lovers.GreenLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.GreenLoversPlayers.Count != 0 && Lovers.GreenLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GreenLovers);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.WhiteLovers)) ||
                (Lovers.WhiteLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.WhiteLoversPlayers.Count != 0 && Lovers.WhiteLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.WhiteLovers);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PurpleLovers)) ||
                (Lovers.PurpleLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.PurpleLoversPlayers.Count != 0 && Lovers.PurpleLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PurpleLovers);
                }
                else if (PlayerCatch.AllAlivePlayersCount <= 2 && PlayerCatch.AllAlivePlayerControls.All(pc => pc.PlayerId == Lovers.OneLovePlayer.Ltarget || pc.PlayerId == Lovers.OneLovePlayer.OneLove)
                || (Lovers.OneLoveSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.OneLove)?.IsAlive() == true && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.Ltarget)?.IsAlive() == true))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.OneLove);
                    if (!Lovers.OneLovePlayer.doublelove) CustomWinnerHolder.WinnerIds.Add(Lovers.OneLovePlayer.Ltarget);
                }
                else if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.MadonnaLovers)) ||
                (Madonna.MaLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.MaMadonnaLoversPlayers.Count != 0 && Lovers.MaMadonnaLoversPlayers.All(pc => pc.IsAlive())))
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MadonnaLovers);
                }
                else if (Imp == 1 && Crew == 0 && GrimReaper == 1)//死神勝利(1)
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GrimReaper);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                }
                else if (Jackal == 0 && Remotekiller == 0 && FoxAndCrew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0 && Remotekiller == 0 && FoxAndCrew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                }
                else if (Imp == 0 && Jackal == 0 && FoxAndCrew <= Remotekiller)
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Remotekiller);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Remotekiller);
                }
                else if (Jackal == 0 && Imp == 0 && GrimReaper == 1 && Remotekiller == 0)//死神勝利(2)
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GrimReaper);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                }
                else if (Jackal == 0 && Remotekiller == 0 && Imp == 0) //クルー勝利
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        // HideAndSeek用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int Imp = PlayerCatch.AlivePlayersCount(CountTypes.Impostor);
                int Crew = PlayerCatch.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Crew <= 0) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }
    }
    // ﾀｽﾊﾞﾄ用
    class TaskBattleGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

            if (CheckGameEndByLivingPlayers(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (Main.RTAMode)
            {
                var player = PlayerCatch.GetPlayerById(Main.RTAPlayer);
                if (player.GetPlayerTaskState().IsTaskFinished)
                {
                    reason = GameOverReason.HumansByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    Main.RTAPlayer = byte.MaxValue;
                }
            }
            else
            if (!TaskBattle.TaskBattleTeamWinType.GetBool())
            {
                int TaskPlayerB = PlayerCatch.AlivePlayersCount(CountTypes.TaskPlayer);
                bool win = TaskPlayerB <= 1;
                if (TaskBattle.IsTaskBattleTeamMode)
                {
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        if (pc.AllTasksCompleted())
                            win = true;
                    }
                }
                if (win)
                {
                    reason = GameOverReason.HumansByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                else return false; //勝利条件未達成
            }
            else
            {
                foreach (var t in TaskBattle.TaskBattleTeams.Values)
                {
                    if (t == null) continue;
                    var task = 0;
                    foreach (var id in t)
                        task += PlayerState.GetByPlayerId(id).taskState.CompletedTasksCount;
                    if (TaskBattle.TaskBattleTeamWinTaskc.GetFloat() <= task)
                    {
                        reason = GameOverReason.HumansByTask;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                        foreach (var id in t)
                            CustomWinnerHolder.WinnerIds.Add(id);
                    }
                }
            }

            return true;
        }
    }
    public abstract class GameEndPredicate
    {
        /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
        /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
        /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
        public abstract bool CheckForEndGame(out GameOverReason reason);

        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                reason = GameOverReason.HumansByTask;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                return true;
            }
            return false;
        }
        /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (ShipStatus.Instance.Systems == null) return false;

            // TryGetValueは使用不可
            var systems = ShipStatus.Instance.Systems;
            LifeSuppSystemType LifeSupp;
            if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
                (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
                LifeSupp.Countdown < 0f) // タイムアップ確認
            {
                // 酸素サボタージュ
                if (Options.Chcabowin.GetBool())
                {
                    var pc = PlayerCatch.GetPlayerById(Main.LastSab);
                    var role = pc.GetCustomRole();

                    switch (role)
                    {
                        case CustomRoles.Jackal:
                        case CustomRoles.JackalMafia:
                        case CustomRoles.JackalAlien:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                            break;
                        case CustomRoles.GrimReaper:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GrimReaper);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                            break;
                        case CustomRoles.Egoist:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                            break;
                        default:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                            break;
                    }
                    reason = GameOverReason.ImpostorBySabotage;
                    Main.NowSabotage = false;
                    LifeSupp.Countdown = 10000f;
                    return true;
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                Main.NowSabotage = false;
                reason = GameOverReason.ImpostorBySabotage;
                LifeSupp.Countdown = 10000f;
                return true;
            }

            ISystemType sys = null;
            if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
            else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
            else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];
            ICriticalSabotage critical;
            if (sys != null && // サボタージュ存在確認
                (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
                critical.Countdown < 0f) // タイムアップ確認
            {
                if (SuddenDeathMode.NowSuddenDeathMode)
                {
                    PlayerCatch.AllAlivePlayerControls.Do(p => p.RpcMurderPlayerV2(p));
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                    Main.NowSabotage = false;
                    reason = GameOverReason.ImpostorBySabotage;
                    critical.ClearSabotage();
                    return true;
                }
                // リアクターサボタージュ
                if (Options.Chcabowin.GetBool())
                {
                    var pc = PlayerCatch.GetPlayerById(Main.LastSab);
                    var role = pc.GetCustomRole();

                    switch (role)
                    {
                        case CustomRoles.Jackal:
                        case CustomRoles.JackalMafia:
                        case CustomRoles.JackalAlien:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                            break;
                        case CustomRoles.GrimReaper:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.GrimReaper);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                            break;
                        case CustomRoles.Egoist:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                            break;
                        default:
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                            break;
                    }
                    Main.NowSabotage = false;
                    reason = GameOverReason.ImpostorBySabotage;
                    critical.ClearSabotage();
                    return true;
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                Main.NowSabotage = false;
                reason = GameOverReason.ImpostorBySabotage;
                critical.ClearSabotage();
                return true;
            }

            return false;
        }
    }
}
//<align=left><pos=-44><size=1><color=white>ゲーム終了時の役職一覧:</voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>\n<pos=-44>　 </pos><color=#50f039ff>夜藍</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>\n<pos=-44>　 </pos><color=#50f039ff>夜藍</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>\n<pos=-44>　 </pos><color=#50f039ff>夜藍</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>\n<pos=-44>　 </pos><color=#50f039ff>夜藍</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>\n<pos=-44>　 </pos><color=#50f039ff>夜藍</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>
//\n<pos=-44><voffset=-4.010526315789473>　 </pos><color=#50f039ff>夜藍 1</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>
//<align=left><pos=-44><size=1><color=white>ゲーム終了時の役職一覧:</voffset>\n<pos=-44><color=#8cffff>★</color> </pos><color=#50f039ff>夜藍</color><pos=-36.5em><color=#ffeb04ff>(0/3)</color></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#8cffffff>クルーメイト</color></pos></voffset>
//\n<pos=-44><voffset=10.926315789473684>　 </pos><color=#5f1d2eff>夜藍 1</color><pos=-36.5em></pos><pos=-30.5em><color=#6180a3ff>生存</color></pos><pos=-25.5em><color=#00b4ebff>ジャッカルドール</color></pos></voffset>