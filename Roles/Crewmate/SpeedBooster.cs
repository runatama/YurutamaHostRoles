using System.Linq;
using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class SpeedBooster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpeedBooster),
            player => new SpeedBooster(player),
            CustomRoles.SpeedBooster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11500,
            SetupOptionItem,
            "sb",
            "#00ffff",
            (7, 1),
            from: From.TownOfHost
        );

    public SpeedBooster(PlayerControl player)
    : base(RoleInfo, player)
    {
        UpSpeed = OptionUpSpeed.GetFloat();
        TaskTrigger = OptionTaskTrigger.GetInt();
        TriggerChance = OptionTriggerChance.GetFloat();
        TargetModeInt = OptionTargetMode.GetInt();

        usedBoosts = 0;
        tasksCompletedBySelf = 0;
    }

    // ====== Options ======
    static OptionItem OptionUpSpeed;       // 加速倍率
    static OptionItem OptionTaskTrigger;   // 何タスクごとに1回判定するか
    static OptionItem OptionTriggerChance; // 発動確率(%)
    static OptionItem OptionTargetMode;    // 0=Self, 1=RandomOne, 2=AllPlayers

    enum OptionName
    {
        SpeedBoosterUpSpeed,
        SpeedBoosterTaskTrigger,
        SpeedBoosterTriggerChance,
        SpeedBoosterTargetMode
    }

    // 対象モード
    enum TargetModeOption
    {
        Self = 0,
        RandomOne = 1,
        AllPlayers = 2
    }

    // 設定値キャッシュ
    static float UpSpeed;
    static int TaskTrigger;
    static float TriggerChance;
    static int TargetModeInt; // IntegerOptionItem で選ぶ（0/1/2）

    // ランタイム状態
    int usedBoosts;             // 何回「発動判定（抽選含む）」を消費したか
    int tasksCompletedBySelf;   // この役職プレイヤーが完了したタスク数（OnCompleteTaskでインクリ）

    private static void SetupOptionItem()
    {
        OptionUpSpeed = FloatOptionItem.Create(
            RoleInfo, 10, OptionName.SpeedBoosterUpSpeed,
            new(0.2f, 5.0f, 0.2f), 1.4f, false
        ).SetValueFormat(OptionFormat.Multiplier);

        OptionTaskTrigger = IntegerOptionItem.Create(
            RoleInfo, 11, OptionName.SpeedBoosterTaskTrigger,
            new(1, 99, 1), 3, false
        ).SetValueFormat(OptionFormat.Pieces);

        OptionTriggerChance = FloatOptionItem.Create(
            RoleInfo, 12, OptionName.SpeedBoosterTriggerChance,
            new(0f, 100f, 5f), 100f, false
        ).SetValueFormat(OptionFormat.Percent);

        // 0=自分, 1=ランダム1人, 2=全員（UIでは数字表示だが安全に動く）
        OptionTargetMode = IntegerOptionItem.Create(
            RoleInfo, 13, OptionName.SpeedBoosterTargetMode,
            new(0, 2, 1), 1, false
        );
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (!Player.IsAlive()) return true;

        // 自分のタスク完了カウントを進める（このコールバックは自分のタスク完了時に呼ばれる想定）
        tasksCompletedBySelf++;

        // 次の発動しきい値に達したか？（例：TaskTrigger=3 なら 3,6,9,... のタイミングで1回ずつ試行）
        int neededForNext = TaskTrigger * (usedBoosts + 1);
        if (tasksCompletedBySelf < neededForNext) return true;

        // ここで1回分の「抽選＆発動」を消費する（抽選失敗でも消費）
        usedBoosts++;

        // 発動確率チェック
        var rand = IRandom.Instance;
        // 0〜99 の整数で判定（TriggerChance=100 なら必ず成功）
        if (rand.Next(0, 100) >= TriggerChance)
        {
            Logger.Info($"[SpeedBooster] 抽選失敗（{TriggerChance}%）", "SpeedBooster");
            return true;
        }

        // 対象モード
        var mode = (TargetModeOption)TargetModeInt;

        switch (mode)
        {
            case TargetModeOption.Self:
                ApplyBoost(Player);
                break;

            case TargetModeOption.RandomOne:
                {
                    List<PlayerControl> candidates = new();
                    candidates.AddRange(PlayerCatch.AllAlivePlayerControls.ToArray());
                    if (candidates.Count > 0)
                    {
                        var target = candidates[rand.Next(0, candidates.Count)];
                        ApplyBoost(target);
                    }
                    else
                    {
                        Logger.Warn("[SpeedBooster] ランダム対象が見つからないため発動できませんでした。", "SpeedBooster");
                    }
                    break;
                }

            case TargetModeOption.AllPlayers:
                foreach (var p in PlayerCatch.AllAlivePlayerControls)
                    ApplyBoost(p);
                break;
        }

        return true;
    }

    private void ApplyBoost(PlayerControl target)
    {
        if (target == null) return;

        // ban対策：代入ではなく、既存の速度に倍率を掛ける（元コードと同じ方式）
        Main.AllPlayerSpeed[target.PlayerId] *= UpSpeed;
        target.MarkDirtySettings();

        Logger.Info($"[SpeedBooster] ブースト対象: {target.GetNameWithRole().RemoveHtmlTags()} / x{UpSpeed}", "SpeedBooster");
    }
}
