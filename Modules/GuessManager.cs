/*using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TownOfHost.Roles.Core;
using TownOfHost.Attributes;
using TownOfHost.Roles.AddOns.Common;
using UnityEngine;
using static TownOfHost.Translator;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost;

public static class GuessManager
{

    public static Dictionary<byte, int> GuesserGuessed = new();

    [GameModuleInitializer]
    public static void Guessreset()
    {
        Logger.Info("全員のゲスをリセットするぜ!!", "Guesser");
        GuesserGuessed.Clear();
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    public static byte GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "赤|レッド|red")) return 0;
        if (ComfirmIncludeMsg(msg, "青|ブルー|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "緑|グリーン|green")) return 2;
        if (ComfirmIncludeMsg(msg, "桃|ピンク|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橙|オレンジ|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|イエロー|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黒|ブラック|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|ホワイト|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|パープル|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "茶|ブラウン|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "水|シアン|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄緑|ライム|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "栗|マルーン|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "薔薇|ローズ|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "ﾊﾞﾅﾅ|バナナ|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|グレー|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "ﾀﾝ|タン|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|コーラル|coral")) return 17;
        else return byte.MaxValue;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Length; i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }

    public static bool NotGueesFlag = false;
    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Guesser)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id")) operate = 1;
        else if (CheckCommond(ref msg, "bt", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessDead"));
            return true;
        }
        if (operate == 2)
        {
            if (
            pc.Is(CustomRoles.Guesser) && (Guesser.TryHideMsg.GetBool() || LastImpostor.GiveGuesser.GetBool() || LastNeutral.GiveGuesser.GetBool())
            )
            {
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner && !isUI) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                if (!isUI) Utils.SendMessage(error, pc.PlayerId);
                else pc.ShowPopUp(error);
                return true;
            }

            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!GuesserGuessed.ContainsKey(pc.PlayerId)) GuesserGuessed.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.Guesser) && GuesserGuessed[pc.PlayerId] >= Guesser.CanGuessTime.GetInt())
                {
                    Utils.SendMessage("ゲッサーの上限に達しているよ。無駄撃ちはやめな", pc.PlayerId, Utils.ColorString(Color.green, "【===弾数確認はした方がいいぜ===】"));
                    return true;
                }
                if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
                {
                    Utils.SendMessage("ゲームマスターを打つことはできないよ。他にしな。", pc.PlayerId, Utils.ColorString(Color.blue, "【===ホストがそんなに醜いのかい?===】"));
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !Guesser.CanGuessTaskDoneSnitch.GetBool() && !pc.Is(CustomRoleTypes.Crewmate))
                {
                    Utils.SendMessage("君は人外?タスク終わってるスニッチを打つことはできないよ。", pc.PlayerId, Utils.ColorString(Color.black, "【===ﾁｮｯﾄﾏﾃﾁｮﾄﾏﾃｸﾙｰｻﾝ!!===】"));
                    return true;
                }

                if (role.IsAddOn())
                {
                    return true;
                }
                if (pc.Is(CustomRoleTypes.Impostor) && role == CustomRoles.Egoist)
                {
                    return true;
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage("自分ゲスしてどないしたん?\n嫌なことあったんやったらワイが話聞くで?", pc.PlayerId, Utils.ColorString(Color.cyan, "【===君は何をしているんだい?===】"));
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoleTypes.Crewmate) && role.IsCrewmate() && !Guesser.CanGuessNakama.GetBool() && !pc.Is(CustomRoles.Madmate))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoleTypes.Impostor) &&
                        role.IsImpostor() && !Guesser.CanGuessNakama.GetBool() ||
                        (role.IsWhiteCrew() && !Guesser.CanWhiteCrew.GetBool()))
                {
                    guesserSuicide = true;
                }
                else if (!pc.Is(CustomRoleTypes.Neutral) &&
                        role.IsWhiteCrew() && !Guesser.CanWhiteCrew.GetBool())
                {
                    guesserSuicide = true;
                }
                else if (CheckTargetRoles(target, role))
                {
                    guesserSuicide = true;
                }
                Logger.Info($"{pc.GetNameWithRole()} が {target.GetNameWithRole()} をゲス", "Guesser");

                var dp = guesserSuicide ? pc : target;
                var tempDeathReason = CustomDeathReason.Guess;
                if (Guesser.ChangeGuessDeathReason.GetBool()
                    )
                {
                    tempDeathReason = guesserSuicide ? CustomDeathReason.Misfire : CustomDeathReason.Guess;
                }

                target = dp;

                Logger.Info($"ゲッサー：{target.GetNameWithRole()} ", "Guesser");

                string Name = dp.GetRealName();

                GuesserGuessed[pc.PlayerId]++;

                _ = new LateTask(() =>
                {
                    var playerState = PlayerState.GetByPlayerId(dp.PlayerId);
                    playerState.DeathReason = tempDeathReason;
                    dp.SetRealKiller(pc);
                    RpcGuesserMurderPlayer(dp);

                    //死者检查
                    //Utils.AfterPlayerDeathTasks(dp, true);
                    CustomRoleManager.OnMurderPlayer(pc, target);

                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);

                    _ = new LateTask(() =>
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            Utils.SendMessage(Name + GetString("Meetingkill"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), GetString("MSKillTitle")));
                        }
                    }, 0.4f, "Guess Msg");

                }, 0.2f, "Guesser Kill");
            }
        }
        return true;
    }

    private static bool CheckTargetRoles(PlayerControl target, CustomRoles role)
    {
        bool result = !target.Is(role);

        switch (target.GetCustomRole())
        {
            case CustomRoles.Engineer:
                if (role == CustomRoles.Engineer) result = false;
                break;
            case CustomRoles.Scientist:
                if (role == CustomRoles.Scientist) result = false;
                break;
            case CustomRoles.Impostor:
                if (role == CustomRoles.Impostor) result = false;
                break;
            case CustomRoles.Shapeshifter:
                if (role == CustomRoles.Shapeshifter) result = false;
                break;
        }

        return result;
    }

    public static TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f) //ゲッサー用の殺し方
    {
        // DEATH STUFF //
        var amOwner = pc.AmOwner;
        pc.Data.IsDead = true;
        pc.RpcExileV2();
        var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
        playerState.SetDead();
        //Main.PlayerStates[pc.PlayerId].SetDead();
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        //pc.Die(DeathReason.Kill);
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("GuessHelp");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByInputName(msg, out role, true))
        {
            error = GetString("GuessHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string ChangeNormal2Vanilla(CustomRoles role)
    {
        //ノーマル系役職ならバニラに変える
        switch (role)
        {
            case CustomRoles.Engineer:
                role = CustomRoles.Engineer;
                break;
            case CustomRoles.Scientist:
                role = CustomRoles.Scientist;
                break;
            case CustomRoles.Impostor:
                role = CustomRoles.Impostor;
                break;
            case CustomRoles.Shapeshifter:
                role = CustomRoles.Shapeshifter;
                break;
        }

        return GetString(Enum.GetName(typeof(CustomRoles), role)).TrimStart('*').ToLower().Trim().Replace(" ", string.Empty).RemoveHtmlTags();
    }
    private static void SendRPC(int playerId, CustomRoles role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadInt32();
        CustomRoles role = (CustomRoles)reader.ReadByte();
        GuesserMsg(pc, $"/bt {PlayerId} {GetString(role.ToString())}", true);
    }
}*/
