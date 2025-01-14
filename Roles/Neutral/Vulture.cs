using AmongUs.GameOptions;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Linq;

namespace TownOfHost.Roles.Neutral;
public sealed class Vulture : RoleBase, IKillFlashSeeable, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vulture),
            player => new Vulture(player),
            CustomRoles.Vulture,
            () => OptionCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            36000,
            SetupOptionItem,
            "Vu",
            "#6f4204",
            false,
            from: From.TheOtherRoles
        );
    public Vulture(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        EatCount = 0;
        staticEatedPlayers.Clear();

        OptAddWinEatcount = OptionAddWinEatCount.GetInt();
        OptWinEatcount = OptionWinEatCount.GetInt();
        OptEatShape = OptionEatShape.GetBool();
        OptKillflashTaskcount = OptionKillflashtaskcount.GetInt();
        OptOnikuArrowtaskcount = OptionOnikuArrowtskcount.GetInt();
        OptVentInTime = OptionVentIntime.GetFloat();
        OptVentCooldown = OptionVentCooldown.GetFloat();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);

        MyTaskState.NeedTaskCount = OptKillflashTaskcount < OptOnikuArrowtaskcount ? OptOnikuArrowtaskcount : OptKillflashTaskcount;
    }
    static OptionItem OptionAddWinEatCount; static int OptAddWinEatcount;//追加勝利に必要なつまみ数
    static OptionItem OptionWinEatCount; static int OptWinEatcount; //勝利に必要なつまみ食い数
    static OptionItem OptionEatShape; static bool OptEatShape;//つまみぐいしたらシェイプ出る
    static OptionItem OptionKillflashtaskcount; static int OptKillflashTaskcount;//キルフラ見えるタスク数
    static OptionItem OptionOnikuArrowtskcount; static int OptOnikuArrowtaskcount;//矢印
    static OptionItem OptionCanUseVent;//ベント使えるか
    static OptionItem OptionVentCooldown; static float OptVentCooldown;//ベントクール
    static OptionItem OptionVentIntime; static float OptVentInTime;//ベント最大

    int EatCount;//食べたかず
    Dictionary<byte, Vector2> DiePlayerPos = new();//死体の矢印
    static List<byte> staticEatedPlayers = new();//食べられたおにく
    enum OptionName
    {
        VultrueCanSeeKillFlushTaskCount,
        VultrueCanSeeOnikuArrowTaskCount,
        VultrueWinEatcount,
        VultrueAddWinEatcount,
        VultrueEatShape
    }

    private static void SetupOptionItem()
    {
        OptionAddWinEatCount = FloatOptionItem.Create(RoleInfo, 9, OptionName.VultrueAddWinEatcount, new(0, 14, 1), 2, false, null, null);
        OptionWinEatCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VultrueWinEatcount, new(1, 14, 1), 3, false);
        OptionEatShape = BooleanOptionItem.Create(RoleInfo, 11, OptionName.VultrueEatShape, true, false);
        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.CanVent, true, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 15, StringNames.EngineerCooldown, OptionBaseCoolTime, 15, false, OptionCanUseVent).SetValueFormat(OptionFormat.Seconds);
        OptionVentIntime = FloatOptionItem.Create(RoleInfo, 16, StringNames.EngineerInVentCooldown, OptionBaseCoolTime, 5, false, OptionCanUseVent, true).SetValueFormat(OptionFormat.Seconds);
        OptionKillflashtaskcount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.VultrueCanSeeKillFlushTaskCount, new(0, 255, 1), 3, false);
        OptionOnikuArrowtskcount = IntegerOptionItem.Create(RoleInfo, 13, OptionName.VultrueCanSeeOnikuArrowTaskCount, new(0, 255, 1), 5, false);

        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = OptVentCooldown;
        AURoleOptions.EngineerInVentMaxTime = OptVentInTime;
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
    {
        if (reporter.PlayerId == Player.PlayerId && target != null && !staticEatedPlayers.Contains(target?.PlayerId ?? 250))
        {
            UtilsGameLog.AddGameLog("Vultrue", $"{Utils.GetPlayerColor(Player)}: {Utils.GetPlayerColor(target)}をつまみぐい！");
            Logger.Info($"{EatCount + 1}個目のお食事", "Vulture");
            reason = DontReportreson.Eat;
            EatCount++;
            staticEatedPlayers.Add(target.PlayerId);
            DiePlayerPos.Where(poss => poss.Key == target.PlayerId).Do(poss => GetArrow.Remove(Player.PlayerId, poss.Value));

            //勝利の確認
            if (OptWinEatcount <= EatCount)
            {
                Logger.Info($"ごちそうさまでした！", "Vulture");
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vulture);
                CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            }

            //食べたときにシェイプを
            if (OptEatShape)
            {
                Player.RpcShapeshift(Player, true);
                Player.RpcRejectShapeshift();
            }

            return true;
        }
        if (reporter?.PlayerId != target?.PlayerId && target != null && staticEatedPlayers.Contains(target?.PlayerId ?? 250))
        {
            reason = DontReportreson.Eat;
            Logger.Info($"{target?.PlayerName ?? "???"}は食事済みだからキャンセル", "Vulture");
            return true;
        }

        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        //矢印の削除
        DiePlayerPos.Do(oniku => GetArrow.Remove(Player.PlayerId, oniku.Value));
        //保存データの削除
        DiePlayerPos.Clear();
    }

    public bool? CheckKillFlash(MurderInfo info)
    {
        //矢印の保存。万が一GetTruePositionがずれたことを考えてposで統一。
        var pos = info.AppearanceTarget.GetTruePosition();
        DiePlayerPos.Add(info.AppearanceTarget.PlayerId, pos);
        GetArrow.Add(Player.PlayerId, pos);

        //キルフラッシュが見えるタスク数なら～
        return OptKillflashTaskcount <= MyTaskState.CompletedTasksCount;
    }
    //現在の食事状況を
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $" <color={RoleInfo.RoleColorCode}>({EatCount}/{OptWinEatcount})</color>";
    //死体位置の矢印表示
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //死亡済み or 会議中 or 他人にマークの場合は空
        if (!Player.IsAlive() || isForMeeting || seer.PlayerId != seen.PlayerId) return "";

        //タスク数に達しているか
        if (OptOnikuArrowtaskcount <= MyTaskState.CompletedTasksCount)
        {
            var str = $" <color={RoleInfo.RoleColorCode}>";

            //事前に保存しておいた奴を出す
            foreach (var arrow in DiePlayerPos)
            {
                str += GetArrow.GetArrows(seer, arrow.Value);
            }
            return $"{str}</color>";
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting)
        {
            if (staticEatedPlayers.Contains(seen.PlayerId)) return $"<color={RoleInfo.RoleColorCode}>×</color>";
        }
        return "";
    }
    public override void AfterMeetingTasks() => staticEatedPlayers.Clear();

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (OptAddWinEatcount is 0) return false;
        //君勝ってるやないか
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Vulture && CustomWinnerHolder.WinnerIds.Contains(Player.PlayerId)) return false;

        //生きてて小腹が満たせれば勝
        return OptAddWinEatcount <= EatCount && Player.IsAlive();
    }
}
