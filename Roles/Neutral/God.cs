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
            () => RoleTypes.Crewmate,          // 表示カテゴリ（安全側）
            CustomRoleTypes.Neutral,           // 実カテゴリ
            17050,                             // ★一意のID（衝突したらさらに離す）
            SetupOptionItem,
            "gd",
            "#ffff00",
            (7, 4),
            true,
            Desc: () => GetString("GodDesc")
        );

    public God(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.ForRecompute)
    {
        _taskCompleteToWin = OptTaskCompleteToWin.GetBool();
        _revealVotes = OptRevealVotes.GetBool();

        // 初期化直後に「自分視点だけ」名札（名前上の役職）を更新
        NotifyNamesOnce();
    }

    // ===== Options =====
    private static OptionItem OptTaskCompleteToWin;
    private static OptionItem OptRevealVotes;

    private static bool _taskCompleteToWin;
    private static bool _revealVotes;

    private enum OptionName
    {
        GodTaskCompleteToWin,   // 「タスクを全て終えたら勝利」
        GodViewVoteFor,         // 「投票先を公開（匿名投票OFF）」
    }

    private static void SetupOptionItem()
    {
        // 既存ロールの並びに合わせる
        SoloWinOption.Create(RoleInfo, 9, defo: 1);

        OptTaskCompleteToWin = BooleanOptionItem.Create(
            RoleInfo, 10, OptionName.GodTaskCompleteToWin, true, false);

        // 既定は匿名投票（＝公開しない）
        OptRevealVotes = BooleanOptionItem.Create(
            RoleInfo, 11, OptionName.GodViewVoteFor, false, false);
    }

    // ===== 他者の役職名を“名前の上”に表示（自分視点許可） =====
    public override bool NotifyRolesCheckOtherName => true;

    // 会議切替で1回だけ名札更新するための簡易トグル
    private static bool _lastIsMeeting = false;
    private static bool _notifiedThisPhase = false;

    // Kの多くの派生は publicメソッド名で定期呼び出しする仕組みがある（override不要）
    public void OnFixedUpdate()
    {
        bool isMeeting = MeetingHud.Instance != null;

        // フェーズが変わったら通知リセット
        if (isMeeting != _lastIsMeeting)
        {
            _lastIsMeeting = isMeeting;
            _notifiedThisPhase = false;
        }

        // このフェーズで未通知なら1回だけ更新
        if (!_notifiedThisPhase && Player != null)
        {
            NotifyNamesOnce();
            _notifiedThisPhase = true;
        }
    }

    // 名札更新（存在しない環境でも落ちないように多段フォールバック）
    private void NotifyNamesOnce()
    {
        // 1) UtilsNotifyRoles がある版（Chef/Bankerと同系） → これが最優先で最も自然
        try
        {
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            return;
        }
        catch { /* 次の経路へ */ }

        // 2) 旧来の“seer想定フック”がある版（存在すれば）
        try
        {
            // 一部版では Seer通知で名札更新が走る実装がある
            // メソッド名や場所が違っても例外を飲み、次の経路へ進む
            var m = typeof(UtilsNotifyRoles).GetMethod("NotifyRoles", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m != null)
            {
                m.Invoke(null, new object[] { true, Player });
                return;
            }
        }
        catch { /* 次の経路へ */ }

        // 3) 何も無い派生でも“最低限”は動くように、会議画面で再描画を誘発
        // （MeetingHudの再生成やSelf更新は実装差が大きいので、ノーオペで安全終了）
    }

    // ===== 匿名投票の切り替え（投票先の公開/非公開） =====
    public override void ApplyGameOptions(IGameOptions opt)
    {
        // _revealVotes = true → AnonymousVotes を false（＝投票先が見える）
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
            catch
            {
                // どうしても該当APIが無い場合は、その版にある「匿名投票オプション」を探して呼ぶ必要あり
            }
        }
    }

    // ===== 追加勝利（IAdditionalWinner 合流） =====
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        // 生存必須
        if (!Player.IsAlive()) return false;

        // タスク完了不要なら、生存だけでOK
        if (!_taskCompleteToWin) return true;

        // 自タスク完了チェック（LINQ不使用）
        var data = Player.Data;
        if (data?.Tasks == null) return false;

        for (int i = 0; i < data.Tasks.Count; i++)
        {
            var t = data.Tasks[i];
            if (t == null) return false;
            bool complete = false;
            try { complete = t.Complete; } catch { return false; }
            if (!complete) return false;
        }
        return true;
    }

    // ===== 進捗表示（タスク必須のときだけ） =====
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        if (!_taskCompleteToWin) return "";

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

    // =====（任意）Yのサボ抑止：K側に同名フックがある場合だけ有効化 =====
    // 署名不一致でロード落ちしやすいので“既定は無効”。K側に完全一致の ISystemTypeUpdateHook があると分かったら
    // プロジェクトの定義シンボルに GOD_K_HAS_SYSTEM_HOOKS を追加して使ってください。
#if GOD_K_HAS_SYSTEM_HOOKS
    bool ISystemTypeUpdateHook.UpdateHudOverrideSystem(HudOverrideSystemType sys, byte amount)
    {
        if ((amount & HudOverrideSystemType.DamageBit) <= 0) return false;
        return true; // 破壊通知は吸収（コミュ等の抑止）
    }
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType sys, byte amount)
    {
        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        if (tags == HqHudSystemType.Tags.FixBit) return false; // 修理は素通し
        return true; // 破壊系は吸収
    }
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem sys, byte amount) => false;        // 停電は素通し
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType sys, byte amount) => false;// O2 素通し
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType sys, byte amount) => false;  // リアクター素通し
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem sys, byte amount) => false;
#endif
}
