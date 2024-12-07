using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;
public sealed class Snowman : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Snowman),
            player => new Snowman(player),
            CustomRoles.Snowman,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21800,
            SetupOptionItem,
            "snm",
            "#c4d6e3"
        );
    public Snowman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        StartVision = OptStartVision.GetFloat();
        MinVison = OptMinVision.GetFloat();
        isElectricalDontTime = OptIsElectricalDontTime.GetBool();
        Meltvision = OptMeltvision.GetFloat();
        TaskCompleatAddVision = OptTaskCompleatAddVision.GetFloat();
        AllTaskcompMeltvision = OptAllTaskCompMeltvision.GetFloat();
        AllTaskCompMelt = AllTaskcompMeltvision is not 0;

        //最小が最大を超えている！？
        if (MinVison > StartVision)
        {
            Logger.Error("雪だるまちゃんのスタートが最小越えてるよっ", "SnowMan");
            MinVison = 0;
        }
        SyncTimer = 0;
        Vision = StartVision;
    }

    enum OptionName
    {
        SnowmanStartVision,
        SnowmanMinVision,
        SnowmanIsElectricalmode,
        SnowmanMeltvision,
        SnowmanAllTaskCompMeltvision,
        SnowmanTaskCompAddvision,
        SnowmanAllTaskMinVision
    }
    //同期までのタイマー
    float SyncTimer;
    //現在の視界
    float Vision;

    //開始 , 上限の視界
    static OptionItem OptStartVision; static float StartVision;

    //最小の視界
    static OptionItem OptMinVision; static float MinVison;

    // 停電中なら溶けない処理
    static OptionItem OptIsElectricalDontTime; static bool isElectricalDontTime;

    // 1秒間に縮まる視界
    static OptionItem OptMeltvision; static float Meltvision;

    //タスクコンプで視界が減少し辛く　　　　　　 タスクコンプの時の減少量                 タスクコンプしても減少する
    static OptionItem OptAllTaskCompMeltvision; static float AllTaskcompMeltvision; static bool AllTaskCompMelt;

    //タスク完了時に広がる視界
    static OptionItem OptTaskCompleatAddVision; static float TaskCompleatAddVision;


    private static void SetupOptionItem()
    {
        OptStartVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.SnowmanStartVision, new(0.05f, 5, 0.05f), 0.6f, false).SetValueFormat(OptionFormat.Multiplier);
        OptMinVision = FloatOptionItem.Create(RoleInfo, 11, OptionName.SnowmanMinVision, new(0f, 5, 0.05f), 0.15f, false).SetValueFormat(OptionFormat.Multiplier);
        OptMeltvision = FloatOptionItem.Create(RoleInfo, 12, OptionName.SnowmanMeltvision, new(0.0005f, 0.005f, 0.0001f), 0.003f, false).SetValueFormat(OptionFormat.Multiplier);
        OptIsElectricalDontTime = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SnowmanIsElectricalmode, true, false);
        OptTaskCompleatAddVision = FloatOptionItem.Create(RoleInfo, 14, OptionName.SnowmanTaskCompAddvision, new(0, 0.03f, 0.001f), 0.01f, false).SetValueFormat(OptionFormat.Multiplier);
        OptAllTaskCompMeltvision = FloatOptionItem.Create(RoleInfo, 15, OptionName.SnowmanAllTaskCompMeltvision, new(0, 0.001f, 0.0001f), 0f, false).SetValueFormat(OptionFormat.Multiplier);

        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetFloat(FloatOptionNames.CrewLightMod, Vision);
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (!Player.IsAlive()) return true;

        Vision += TaskCompleatAddVision;
        Vision = Mathf.Min(StartVision, Vision);
        Player.MarkDirtySettings();

        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        //ホストじゃない,死んでる,会議中,イントロ終わってない,スポーンしてないならリターン
        if (!AmongUsClient.Instance.AmHost || !player.IsAlive() || GameStates.Meeting || GameStates.Intro || !MyState.HasSpawned) return;

        //夜 (停電中なら進行しない)
        if ((isElectricalDontTime && Utils.IsActive(SystemTypes.Electrical))
        || (!AllTaskCompMelt && MyTaskState.IsTaskFinished)//タスクコンプで減少しない設定かつタスクコンプなら処理しない
        || (Vision == MinVison) //そもそも視界が最小なら減少処理いらん
        ) return;

        //同期までのタイマー
        SyncTimer += Time.fixedDeltaTime;

        if (SyncTimer > 1f)
        {
            //視界を減少させる   減少恩恵なら　こっち　　　　　　　　　　　　　　　　そうじゃないならこっち
            Vision -= MyTaskState.IsTaskFinished ? AllTaskcompMeltvision : Meltvision;
            //最小以下にはしない
            Vision = Mathf.Max(MinVison, Vision);

            player.MarkDirtySettings();
            SyncTimer = 0;
        }
    }
}