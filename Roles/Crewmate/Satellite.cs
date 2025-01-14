#if DEBUG //イビルサテライトあるのでデバッグのみで
using UnityEngine;
using AmongUs.GameOptions;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class Satellite : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Satellite),
            player => new Satellite(player),
            CustomRoles.Satellite,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            15100,
            SetupOptionItem,
            "r",
            "#00E1FF",
            introSound: () => DestroyableSingleton<AutoOpenDoor>.Instance.OpenSound
        );
    public Satellite(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        data = new();
        count = (int)OptionMaximum.GetFloat();
        mcount = 0;
        taskCount = OptiontaskCount.GetFloat();
        mMaxCount = Option1MeetingMaximum.GetFloat();
        comms = Optioncomms.GetBool();
        vent = OptionVent.GetBool();
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }
    private static OptionItem OptionMaximum;
    private static OptionItem OptiontaskCount;
    private static OptionItem Option1MeetingMaximum;
    private static OptionItem Optioncomms;
    private static OptionItem OptionVent;

    int count;
    float mcount;

    static float taskCount;
    static float mMaxCount;
    static bool comms;
    static bool vent;

    static Dictionary<byte, LocationData> data;

    enum Option
    {
        cantaskcount,
        meetingmc,
        SatelliteCount,
        SatelliteComms,
        SatelliteVent
    }

    public override void Add()
    {
        AddS(Player);
        foreach (var pc in PlayerCatch.AllPlayerControls) data.TryAdd(pc.PlayerId, new LocationData(pc.PlayerId));
    }

    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 10, Option.SatelliteCount, new(1f, 99f, 1f), 2f, false)
            .SetValueFormat(OptionFormat.Times);
        OptiontaskCount = FloatOptionItem.Create(RoleInfo, 11, Option.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 12, Option.meetingmc, new(0f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        Optioncomms = BooleanOptionItem.Create(RoleInfo, 13, Option.SatelliteComms, false, false);
        OptionVent = BooleanOptionItem.Create(RoleInfo, 14, Option.SatelliteVent, false, false);
    }

    class LocationData
    {
        public byte PlayerId;
        public SystemTypes? LastLocation;
        public HashSet<SystemTypes> visitedLocations;
        public LocationData(byte playerId)
        {
            PlayerId = playerId;
            LastLocation = null;
            visitedLocations = new();
        }
    }

    private bool CanUseAbility => Main.introDestroyed && count > 0 && MyTaskState.HasCompletedEnoughCountOfTasks(taskCount);
    private static bool CheckComms => Utils.IsActive(SystemTypes.Comms) && !comms;

    public static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (CheckComms) return;
        // 検出された当たり判定の格納用に使い回す配列 変換時の負荷を回避するためIl2CppReferenceArrayで扱う
        Il2CppReferenceArray<Collider2D> colliders = new(45);
        // 各部屋の人数カウント処理
        foreach (var room in ShipStatus.Instance.AllRooms)
        {
            var roomId = room.RoomId;
            // 通路か当たり判定がないなら何もしない
            if (room.roomArea == null) continue;

            ContactFilter2D filter = new()
            {
                useLayerMask = true,
                layerMask = Constants.LivingPlayersOnlyMask,
                useTriggers = true,
            };
            // 検出された当たり判定の数 検出された当たり判定はここでcollidersに格納される
            var numColliders = room.roomArea.OverlapCollider(filter, colliders);

            // 検出された各当たり判定への処理
            for (var i = 0; i < numColliders; i++)
            {
                var collider = colliders[i];
                // 生きてる場合
                if (!collider.isTrigger && !collider.CompareTag("DeadBody"))
                {
                    var playerControl = collider.GetComponent<PlayerControl>();
                    if (playerControl.IsAlive())
                    {
                        var locationData = data[playerControl.PlayerId];
                        locationData.LastLocation = roomId;
                        locationData.visitedLocations.Add(roomId);
                    }
                }
            }
        }
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (Is(voter) && CanUseAbility && mcount > 0)
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.CrewSatellite"), GetString("Vote.CrewSatellite")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                {
                    count--;
                    mcount--;
                    StringBuilder sb = new();
                    var visitedLocations = data[votedForId].visitedLocations.OrderBy(x => Guid.NewGuid());
                    foreach (var location in visitedLocations)
                        sb.Append('\n' + DestroyableSingleton<TranslationController>.Instance.GetString(location));
                    Utils.SendMessage(sb.ToString() + '\n', Player.PlayerId, $"<color=yellow><size=2.5>{Main.AllPlayerNames[votedForId]}の情報</color></size>");
                }
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }

    public override void AfterMeetingTasks()
    {
        foreach (var locationData in data.Values)
        {
            locationData.LastLocation = null;
            locationData.visitedLocations.Clear();
        }
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (CanUseAbility && Is(seen) && Is(seer) && !isForMeeting)
            return CheckComms ? "<color=red>通信が妨害されている間の行動は見ることができない</color>" : "";
        return "";
    }

    public override void OnStartMeeting()
    {
        mcount = Option1MeetingMaximum.GetFloat() == 0 ? count : Math.Min(mMaxCount, count);
        var locationData = data[Player.PlayerId];
        var lastLocation = locationData.LastLocation;
        if (lastLocation == SystemTypes.Hallway || lastLocation == null)
        {
            Utils.SendMessage($"あなたの最終位置の人数は不明です！\n自分で確認してるよね☆", Player.PlayerId);
            return;
        }
        if (CheckComms)
        {
            Utils.SendMessage($"通信妨害中のため最終位置の取得に失敗しました。", Player.PlayerId);
            return;
        }
        foreach (var lo in data.Values)
            Logger.Warn($"{lo.LastLocation}", "sate");
        var lastCount = data.Where(x => x.Value.LastLocation == lastLocation && !(vent && PlayerCatch.GetPlayerById(x.Key)?.inVent == true)).Count();
        _ = new LateTask(() => Utils.SendMessage($"あなたの最終位置には{lastCount}人いました\n\nこの会議では能力を{mcount}回使用することが可能", Player.PlayerId), 0.25f);
    }
}
#endif