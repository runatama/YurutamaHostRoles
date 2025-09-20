using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class God : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            17050,
            SetupOptionItem,
            "gd",
            "#ffff00",
            (7, 4),
            true,
            Desc: () => GetString("GodDesc"),
            from: From.TownOfHost_for_E
        );

    public God(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.ForRecompute)
    {
        _revealVotes = OptRevealVotes.GetBool();
        _allowAdditionalWin = OptAllowAdditionalWin.GetBool();
        _requireTaskComplete = OptRequireTaskComplete.GetBool();
    }

    // ===== Options =====
    private static OptionItem OptRevealVotes;
    private static OptionItem OptAllowAdditionalWin;
    private static OptionItem OptRequireTaskComplete;
    private static SoloWinOption GodWinPriority;

    private static bool _revealVotes;
    private static bool _allowAdditionalWin;
    private static bool _requireTaskComplete;

    private static void SetupOptionItem()
    {
        OptRevealVotes = BooleanOptionItem.Create(
            RoleInfo, 10, "GodViewVoteFor", false, false);

        // 追加勝利ON/OFF
        OptAllowAdditionalWin = BooleanOptionItem.Create(
            RoleInfo, 11, "GodAllowAdditionalWin", true, false);

        // タスク必須ON/OFF
        OptRequireTaskComplete = BooleanOptionItem.Create(
            RoleInfo, 12, "GodRequireTaskComplete", false, false);

        // 勝利優先度 (1～50, デフォルト20)
        GodWinPriority = SoloWinOption.Create(RoleInfo, 13, defo: 20);
    }

    // ===== 役職公開 =====
    public override bool NotifyRolesCheckOtherName => true;

    private float _notifyTimer = 0f;
    private bool _lastIsMeeting = false;

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive()) return;

        // 0.5秒ごとに役職更新
        _notifyTimer -= Time.fixedDeltaTime;
        if (_notifyTimer <= 0f)
        {
            _notifyTimer = 0.5f;

            try
            {
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            }
            catch
            {
                UtilsNotifyRoles.NotifyRoles();
            }
        }

        // 会議開始／終了の瞬間に強制更新
        bool isMeeting = MeetingHud.Instance != null;
        if (isMeeting != _lastIsMeeting)
        {
            _lastIsMeeting = isMeeting;
            UtilsNotifyRoles.NotifyRoles();
        }
    }

    // 会議開始時に保険でもう一度更新
    public override void OnStartMeeting()
    {
        UtilsNotifyRoles.NotifyRoles();
    }

    // ===== 匿名投票切替 =====
    public override void ApplyGameOptions(IGameOptions opt)
    {
        try
        {
            opt.SetBool(BoolOptionNames.AnonymousVotes, !_revealVotes);
        }
        catch
        {
            try
            {
                GameOptionsManager.Instance.CurrentGameOptions.SetBool(BoolOptionNames.AnonymousVotes, !_revealVotes);
            }
            catch { }
        }
    }

    // ===== 追加勝利 (IAdditionalWinner) =====
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (!_allowAdditionalWin) return false;
        return CheckGodCondition();
    }

    // ===== 単独勝利 (CheckWinnerオーバーライド) =====
    public override void CheckWinner()
    {
        if (!Player.IsAlive()) return;
        if (_allowAdditionalWin) return; // 追加勝利モードでは無効

        if (CheckGodCondition())
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.God, Player.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }
        }
    }

    // ===== 共通勝利条件 =====
    private bool CheckGodCondition()
    {
        if (!Player.IsAlive()) return false;

        if (!_requireTaskComplete)
            return true;

        var data = Player.Data;
        if (data?.Tasks == null) return false;

        foreach (var t in data.Tasks)
        {
            if (t == null) return false;
            bool complete = false;
            try { complete = t.Complete; } catch { return false; }
            if (!complete) return false;
        }
        return true;
    }

    // ===== タスク進捗 =====
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        if (!_requireTaskComplete) return "";

        var data = Player?.Data;
        if (data?.Tasks == null) return "(0/0)";

        int total = data.Tasks.Count;
        int done = 0;
        for (int i = 0; i < total; i++)
        {
            var t = data.Tasks[i];
            if (t == null) continue;
            try { if (t.Complete) done++; } catch { }
        }
        return Utils.ColorString(RoleInfo.RoleColor, $"({done}/{total})");
    }
}
