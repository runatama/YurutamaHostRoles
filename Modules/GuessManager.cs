using Hazel;
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
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Madmate;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;

namespace TownOfHost;

//参考→https://github.com/0xDrMoe/TownofHost-Enhanced/releases/tag/v1.0.1
//上手くいかない部分はこっちも参考にしました→https://github.com/AsumuAkaguma/TownOfHost_ForE/releases/tag/vBOMB!
public static class GuessManager
{

    public static Dictionary<byte, int> GuesserGuessed = new();
    public static Dictionary<byte, int> TGuess = new();

    [GameModuleInitializer]
    public static void Guessreset()
    {
        kakusu = false;
        Logger.Info("全員のゲスをリセットするぜ!!", "Guesser");
        GuesserGuessed.Clear();
        TGuess.Clear();//ゲスでターン終了したらリセットされないので
    }
    public static void Reset()
    {
        Logger.Info("会議制限を解除するぜ", "Guesser");
        TGuess.Clear();
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

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Length; i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }
    public static bool kakusu = false;
    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;

        int operate = 0;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "bt", false)) operate = 2;
        else return false;

        if (!pc.IsAlive()) return true;//本当は悪あがきはやめなって処理入れたかった(´・ω・｀)←入れてもいいけどめんどくs(((!(?)
        if (operate == 2)
        {
            if (!Options.ExHideChatCommand.GetBool())
            {
                if (pc.IsAlive()) ChatManager.SendPreviousMessagesToAll();
            }
            //Notゲッサーはここで処理を止める。
            if (!pc.Is(CustomRoles.Guesser)
                && !(pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool())
                && !(pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool())
                && !(RoleAddAddons.GetRoleAddon(pc.GetCustomRole(), out var op, pc) && op.GiveAddons.GetBool() && op.GiveGuesser.GetBool())
                )
            {
                Utils.SendMessage(GetString("NotGuesserError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("NotGuesserErrortitle")));
                return true;
            }
            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId, "<color=#e6b422>" + GetString("GuessErrortitle") + "</color>");
                return true;
            }
            if (MadAvenger.Skill)//マッドアベンジャー中は処理しないぜ★
            {
                Utils.SendMessage(GetString("GuessErrorMadAvenger"), pc.PlayerId, $"<color=#ff1919>{GetString("GuessErrorMadAvengerTitle")}</color>");
                return true;
            }
            if (!SelfVoteManager.Canuseability())
            {
                return true;
            }
            if (GameStates.Tuihou)
            {
                Utils.SendMessage(GetString("GuessErrorTuiho"), pc.PlayerId, $"<color=#ab80c2>{GetString("GuessErrorTuihoTitle")}</color>");
                return true;
            }
            if (Balancer.Id != 255 && !(Balancer.target1 == targetId || Balancer.target2 == targetId) && Balancer.OptionCanMeetingAbility.GetBool()) return true;

            var target = PlayerCatch.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!GuesserGuessed.ContainsKey(pc.PlayerId)) GuesserGuessed.Add(pc.PlayerId, 0);
                if (!TGuess.ContainsKey(pc.PlayerId)) TGuess.Add(pc.PlayerId, 0);

                if (target.GetRoleClass()?.CheckGuess(pc) == null) return true;

                //陣営事に区別する
                if (pc.Is(CustomRoleTypes.Impostor)) if (GuessCountImp(pc)) return true;
                if (pc.Is(CustomRoleTypes.Neutral)) if (GuessCountNeu(pc)) return true;
                if (pc.Is(CustomRoleTypes.Crewmate) || pc.Is(CustomRoleTypes.Madmate)) if (GuessCountCrewandMad(pc)) return true;
                //ｸﾙｰとﾏｯﾄﾞは処理一緒だからまとめる。

                if (role == CustomRoles.GM || target.Is(CustomRoles.GM)) return true;

                if (role.IsAddOn())
                    return true;

                if (role.IsGorstRole())
                    return true;

                if (pc.Is(CustomRoleTypes.Impostor) && role == CustomRoles.Egoist)
                    return true;
                if (pc.Is(CustomRoles.Egoist) && target.Is(CustomRoleTypes.Impostor) && Guesser.ICanGuessNakama.GetBool())
                    return true;

                //↓ここから成功/失敗の処理。
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("Guesssuicide"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("Guesssuicidetitle")));
                    guesserSuicide = true;
                }
                else if (role.IsRiaju(false) && target.IsRiaju(false))
                {//ラバーズって打っててどれかのラバーズなら破滅★
                    guesserSuicide = false;
                }
                else if (role is CustomRoles.OneLove && target.Is(CustomRoles.OneLove))
                {
                    guesserSuicide = false;
                }
                //ここから陣営事に処理決める
                if (pc.Is(CustomRoleTypes.Impostor))
                    if (ImpHantei(pc, target)) guesserSuicide = true;
                if (pc.Is(CustomRoleTypes.Crewmate))
                    if (CrewHantei(pc, target)) guesserSuicide = true;
                if (pc.Is(CustomRoleTypes.Madmate))
                    if (MadHantei(pc, target)) guesserSuicide = true;
                if (pc.Is(CustomRoleTypes.Neutral))
                    if (NeuHantei(pc, target)) guesserSuicide = true;

                if (target.GetRoleClass()?.CheckGuess(pc) == false)
                {
                    guesserSuicide = true;
                }

                //自殺が決まってないなら処理
                if (CheckTargetRoles(target, role) && !guesserSuicide)
                {
                    guesserSuicide = true;
                }
                Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()} が {target.GetNameWithRole().RemoveHtmlTags()} をゲス", "Guesser");

                var dp = guesserSuicide ? pc : target;
                var tempDeathReason = CustomDeathReason.Guess;
                tempDeathReason = guesserSuicide ? CustomDeathReason.Misfire : CustomDeathReason.Guess;
                var dareda = target;
                target = dp;

                Logger.Info($"ゲッサー：{target.GetNameWithRole().RemoveHtmlTags()} ", "Guesser");

                string Name = dp.GetRealName();

                GuesserGuessed[pc.PlayerId]++;
                TGuess[pc.PlayerId]++;

                _ = new LateTask(() =>
                {
                    var playerState = PlayerState.GetByPlayerId(dp.PlayerId);
                    playerState.DeathReason = tempDeathReason;
                    dp.SetRealKiller(pc);
                    RpcGuesserMurderPlayer(dp);

                    //死者检查
                    //Utils.AfterPlayerDeathTasks(dp, true);
                    if (!dp.IsModClient() && !dp.AmOwner) pc.RpcMeetingKill(dp);
                    CustomRoleManager.OnMurderPlayer(pc, target);

                    if (Options.ExHideChatCommand.GetBool())
                    {
                        MeetingHudPatch.StartPatch.Serialize = true;
                        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (pc == dp) continue;
                            pc.Data.IsDead = false;
                        }
                        RPC.RpcSyncAllNetworkedPlayer(dp.GetClientId());
                        MeetingHudPatch.StartPatch.Serialize = false;
                    }
                    //Main.LastLogVitel[dp.PlayerId] = GetString("DeathReason.Guess");
                    _ = new LateTask(() =>
                    {
                        foreach (var pl in PlayerCatch.AllPlayerControls)
                        {
                            Utils.SendMessage(Utils.GetPlayerColor(dp, true) + GetString("Meetingkill"), pl.PlayerId, GetString("MSKillTitle"));
                        }
                    }, 0.1f, "Guess Msg");
                }, Main.LagTime, "Guesser Kill");

                if (pc == dp)
                {
                    UtilsGameLog.AddGameLog("Guess", string.Format(GetString("guessfall"), Utils.GetPlayerColor(pc, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(pc.PlayerId, false)}</b>)", Utils.GetPlayerColor(dareda, true), GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role))));
                }
                else
                {
                    UtilsGameLog.AddGameLog("Guess", string.Format(GetString("guesssuccess"), Utils.GetPlayerColor(pc, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(pc.PlayerId, false)}</b>)", Utils.GetPlayerColor(dp, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(dp.PlayerId, false)}</b>)"));
                }

                _ = new LateTask(() =>
                {
                    string send = "";
                    if (pc == dp)
                    {
                        send = string.Format(GetString("Rgobaku"), Utils.GetPlayerColor(pc, true), Utils.GetPlayerColor(dareda, true), GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)));
                    }
                    else
                    {
                        send = string.Format(GetString("RMeetingKill"), Utils.GetPlayerColor(pc, true), Utils.GetPlayerColor(dp, true));
                    }
                    foreach (var spl in PlayerCatch.AllPlayerControls.Where(pc => !pc.IsAlive())) Utils.SendMessage(send, spl.PlayerId, GetString("RMSKillTitle"));
                }, 2.0f, "Reikai Guess Msg");
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
            case CustomRoles.Tracker:
                if (role == CustomRoles.Tracker) result = false;
                break;
            case CustomRoles.Noisemaker:
                if (role == CustomRoles.Noisemaker) result = false;
                break;
            case CustomRoles.Impostor:
                if (role == CustomRoles.Impostor) result = false;
                break;
            case CustomRoles.Shapeshifter:
                if (role == CustomRoles.Shapeshifter) result = false;
                break;
            case CustomRoles.Phantom:
                if (role == CustomRoles.Phantom) result = false;
                break;
        }

        return result;
    }

    public static TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f)
    {
        // DEATH STUFF //
        var amOwner = pc.AmOwner;
        pc.Data.IsDead = true;
        pc.RpcExileV2();
        var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
        playerState.SetDead();
        PlayerState.AllPlayerStates[pc.PlayerId].SetDead();
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        Utils.AllPlayerKillFlash();
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
            meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
            var voteAreaPlayer = PlayerCatch.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "GuessMeetingCheck");
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        Utils.AllPlayerKillFlash();
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
            meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
            meetingHud.ClearVote();
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
            var voteAreaPlayer = PlayerCatch.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, pc.PlayerId);
            meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
            meetingHud.ClearVote();
            playerVoteArea.UnsetVote();
        }
        _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "GuessMeetingCheck");
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
            error = GetString("GuessError1");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = PlayerCatch.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessError2");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByInputName(msg, out role, true))
        {
            error = GetString("GuessError1");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string ChangeNormal2Vanilla(CustomRoles role)
    {
        switch (role)
        {
            case CustomRoles.Engineer:
                role = CustomRoles.Engineer;
                break;
            case CustomRoles.Scientist:
                role = CustomRoles.Scientist;
                break;
            case CustomRoles.Tracker:
                role = CustomRoles.Tracker;
                break;
            case CustomRoles.Noisemaker:
                role = CustomRoles.Noisemaker;
                break;
            case CustomRoles.Impostor:
                role = CustomRoles.Impostor;
                break;
            case CustomRoles.Shapeshifter:
                role = CustomRoles.Shapeshifter;
                break;
            case CustomRoles.Phantom:
                role = CustomRoles.Phantom;
                break;
        }

        return GetString(Enum.GetName(typeof(CustomRoles), role)).TrimStart('*').ToLower().Trim().Replace(" ", string.Empty).RemoveHtmlTags();
    }
    public static bool GuessCountImp(PlayerControl pc)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        //全体
        var tama = 0;
        if (pc.Is(CustomRoles.Guesser)) tama = Guesser.CanGuessTime.GetInt();
        if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool() && data.GiveGuesser.GetBool())
        {
            if (data.GiveGuesser.GetBool())
                if (data.AddTama.GetBool()) tama += data.CanGuessTime.GetInt();
                else tama = data.CanGuessTime.GetInt();
        }
        if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.AddTama.GetBool() && pc.Is(CustomRoles.Guesser)) tama += LastImpostor.CanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastImpostor) && (!LastImpostor.AddTama.GetBool() || !pc.Is(CustomRoles.Guesser))) tama = LastImpostor.CanGuessTime.GetInt();

        //1会議
        var mtama = 0;
        if (pc.Is(CustomRoles.Guesser)) mtama = Guesser.CanGuessTime.GetInt();
        if (RoleAddAddons.GetRoleAddon(role, out var d, pc) && data.GiveAddons.GetBool() && data.GiveGuesser.GetBool()) if (d.GiveGuesser.GetBool()) mtama = d.OwnCanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool()) mtama = LastImpostor.CanGuessTime.GetInt();

        if (GuesserGuessed[pc.PlayerId] >= tama)
        {
            Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
            return true;
        }
        else
        if (TGuess[pc.PlayerId] >= mtama)
        {
            Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
            return true;
        }
        else return false;
    }
    public static bool GuessCountNeu(PlayerControl pc)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        //全体
        var tama = 0;
        if (pc.Is(CustomRoles.Guesser)) tama = Guesser.CanGuessTime.GetInt();
        if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool())
        {
            if (data.GiveGuesser.GetBool())
            {
                if (data.AddTama.GetBool()) tama += data.CanGuessTime.GetInt();
                else tama = data.CanGuessTime.GetInt();
            }
        }
        if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.AddTama.GetBool() && pc.Is(CustomRoles.Guesser)) tama += LastNeutral.CanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastNeutral) && (!LastNeutral.AddTama.GetBool() || !pc.Is(CustomRoles.Guesser))) tama = LastNeutral.CanGuessTime.GetInt();

        //1会議
        var mtama = 0;
        if (pc.Is(CustomRoles.Guesser)) mtama = Guesser.CanGuessTime.GetInt();
        if (RoleAddAddons.GetRoleAddon(role, out var d, pc) && data.GiveAddons.GetBool()) if (d.GiveGuesser.GetBool()) mtama = d.OwnCanGuessTime.GetInt();
        if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) mtama = LastNeutral.CanGuessTime.GetInt();

        if (GuesserGuessed[pc.PlayerId] >= tama)
        {
            Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
            return true;
        }
        else
        if (TGuess[pc.PlayerId] >= mtama)
        {
            Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
            return true;
        }
        else return false;
    }
    public static bool GuessCountCrewandMad(PlayerControl pc)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        //全体
        var tama = 0;
        if (pc.Is(CustomRoles.Guesser)) tama = Guesser.CanGuessTime.GetInt();
        if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool())
        {
            if (data.GiveGuesser.GetBool())
            {
                if (data.AddTama.GetBool()) tama += data.CanGuessTime.GetInt();
                else tama = data.CanGuessTime.GetInt();
            }
        }
        //1会議
        var mtama = 0;
        if (pc.Is(CustomRoles.Guesser)) mtama = Guesser.CanGuessTime.GetInt();
        if (RoleAddAddons.GetRoleAddon(role, out var d, pc) && d.GiveAddons.GetBool()) if (d.GiveGuesser.GetBool()) mtama = d.OwnCanGuessTime.GetInt();

        if (GuesserGuessed[pc.PlayerId] >= tama)
        {
            Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
            return true;
        }
        else
        if (TGuess[pc.PlayerId] >= mtama)
        {
            Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
            return true;
        }
        else return false;
    }

    public static bool ImpHantei(PlayerControl pc, PlayerControl target)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted())
        {
            var dame = Guesser.ICanGuessTaskDoneSnitch.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) dame = data.ICanGuessTaskDoneSnitch.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool()) dame = LastImpostor.ICanGuessTaskDoneSnitch.GetBool();
            if (!dame)
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, "【=== 緑のあの子は打てないよ！ ===】"));
                return true;
            }
        }
        //仲間を撃ちぬけるか
        if (role.IsImpostor() && target.Is(CustomRoleTypes.Impostor))
        {
            var Nakama = Guesser.ICanGuessNakama.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) Nakama = data.ICanGuessNakama.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool()) Nakama = LastImpostor.ICanGuessNakama.GetBool();
            if (!Nakama) return true;
        }
        //各白を打ち抜けるか
        if (role.IsWhiteCrew())
        {
            var WC = Guesser.ICanWhiteCrew.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) WC = data.ICanWhiteCrew.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool()) WC = LastImpostor.ICanWhiteCrew.GetBool();
            if (!WC) return true;
        }
        //バニラを撃ちぬけるか
        if (role.IsVanilla())
        {
            var va = Guesser.ICanGuessVanilla.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) va = data.ICanGuessVanilla.GetBool();
            if (pc.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool()) va = LastImpostor.ICanGuessVanilla.GetBool();
            if (!va) return true;
        }
        return false;
    }
    public static bool NeuHantei(PlayerControl pc, PlayerControl target)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        //スニッチを撃ちぬけるか
        if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted())
        {
            var dame = Guesser.NCanGuessTaskDoneSnitch.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) dame = data.ICanGuessTaskDoneSnitch.GetBool();
            if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) dame = LastNeutral.ICanGuessTaskDoneSnitch.GetBool();
            if (!dame)
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Neutral")), pc.PlayerId, Utils.ColorString(Palette.DisabledGrey, "【=== スニッチ君は打てないよ！ ===】"));
                return true;
            }
        }
        //各白を打ち抜けるか
        if (role.IsWhiteCrew())
        {
            var WC = Guesser.NCanWhiteCrew.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) WC = data.ICanWhiteCrew.GetBool();
            if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) WC = LastNeutral.ICanWhiteCrew.GetBool();
            if (!WC) return true;
        }
        //バニラを撃ちぬけるか
        if (role.IsVanilla())
        {
            var va = Guesser.NCanGuessVanilla.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) va = data.ICanGuessVanilla.GetBool();
            if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()) va = LastNeutral.ICanGuessVanilla.GetBool();
            if (!va) return true;
        }
        return false;
    }
    public static bool MadHantei(PlayerControl pc, PlayerControl target)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted())
        {
            var dame = Guesser.MCanGuessTaskDoneSnitch.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) dame = data.ICanGuessTaskDoneSnitch.GetBool();
            if (!dame)
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Madmate")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, "【=== 全てわかってる奴は打てないよ ===】"));
                return true;
            }
        }
        //仲間を撃ちぬけるか
        if (role.IsImpostorTeam() && (target.Is(CustomRoleTypes.Impostor) || target.Is(CustomRoleTypes.Madmate)))
        {
            var Nakama = Guesser.MCanGuessNakama.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) Nakama = data.ICanGuessNakama.GetBool();
            if (!Nakama) return true;
        }
        //各白を打ち抜けるか
        if (role.IsWhiteCrew())
        {
            var WC = Guesser.MCanWhiteCrew.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) WC = data.ICanWhiteCrew.GetBool();
            if (!WC) return true;
        }
        //バニラを撃ちぬけるか
        if (role.IsVanilla())
        {
            var va = Guesser.MCanGuessVanilla.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) va = data.ICanGuessVanilla.GetBool();
            if (!va) return true;
        }
        return false;
    }
    public static bool CrewHantei(PlayerControl pc, PlayerControl target)
    {
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        CustomRoles role = RoleNullable.Value;

        //仲間を撃ちぬけるか
        if (role.IsCrewmate() && target.Is(CustomRoleTypes.Crewmate))
        {
            var Nakama = Guesser.CCanGuessNakama.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) Nakama = data.ICanGuessNakama.GetBool();
            if (!Nakama) return true;
        }
        //各白を打ち抜けるか
        if (role.IsWhiteCrew())
        {
            var WC = Guesser.CCanWhiteCrew.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) WC = data.ICanWhiteCrew.GetBool();
            if (!WC) return true;
        }
        //バニラを撃ちぬけるか
        if (role.IsVanilla())
        {
            var va = Guesser.CCanGuessVanilla.GetBool();
            if (RoleAddAddons.GetRoleAddon(role, out var data, pc) && data.GiveAddons.GetBool()) if (data.GiveGuesser.GetBool()) va = data.ICanGuessVanilla.GetBool();
            if (!va) return true;
        }
        return false;
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
}