using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Modules;

namespace TownOfHost.Roles.Crewmate;

public sealed class WhiteHacker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(WhiteHacker),
            player => new WhiteHacker(player),
            CustomRoles.WhiteHacker,
            () => CanUseTrackAbility.GetBool() && !OptAwakening.GetBool() ? RoleTypes.Tracker : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10100,
            (3, 7),
            SetupOptionItem,
            "WH",
            "#efefef"
        );

    public WhiteHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        cantaskcount = Optioncantaskcount.GetFloat();
        targetId = byte.MaxValue;
        Maximum = OptionMaximum.GetFloat();
        cont = 0;
        Useing = false;
        NowTracker = false;
        Awakened = !OptAwakening.GetBool() || cantaskcount < 1; ;
    }

    private static OptionItem Optioncantaskcount;
    private static OptionItem OptionMaximum;
    static OptionItem CanUseTrackAbility;
    static OptionItem TrackerCooldown;
    static OptionItem TrackerDelay;
    static OptionItem TrackerDuration;
    static OptionItem OptAwakening;
    bool Awakened;
    private static float cantaskcount;
    private int targetId;
    static float Maximum;
    float cont;
    bool Useing;
    bool NowTracker;
    enum Option
    {
        WhiteHackerTrackTimes,
        WhiteHackerCanUseTrackAbility
    }

    private static void SetupOptionItem()
    {
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        CanUseTrackAbility = BooleanOptionItem.Create(RoleInfo, 13, Option.WhiteHackerCanUseTrackAbility, false, false);
        TrackerCooldown = FloatOptionItem.Create(RoleInfo, 14, "TrackerCooldown", new(0f, 180f, 0.5f), 15f, false, CanUseTrackAbility)
        .SetValueFormat(OptionFormat.Seconds);
        TrackerDelay = FloatOptionItem.Create(RoleInfo, 15, "TrackerDelay", new(0f, 180f, 0.5f), 5f, false, CanUseTrackAbility)
                .SetValueFormat(OptionFormat.Seconds);
        TrackerDuration = FloatOptionItem.Create(RoleInfo, 16, "TrackerDuration", new(0f, 180f, 1f), 5f, false, CanUseTrackAbility, true)
                .SetValueFormat(OptionFormat.Seconds);
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.WhiteHackerTrackTimes, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.AbilityAwakening, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.TrackerCooldown = TrackerCooldown.GetFloat();
        AURoleOptions.TrackerDelay = TrackerDelay.GetFloat();
        AURoleOptions.TrackerDuration = TrackerDuration.GetFloat();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(cont);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        cont = reader.ReadInt32();
    }
    private bool IsTrackTarget(PlayerControl target)
    => (Player.IsAlive() && target.IsAlive() && !Is(target)) || targetId == target.PlayerId;
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && Awakened && seer.PlayerId == seen.PlayerId && SelfVoteManager.Canuseability() && Maximum > cont && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("NomalVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!SelfVoteManager.Canuseability()) return true;
        if (Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && Maximum > cont)
        {
            if (Player.PlayerId == votedForId || votedForId == 253)
            {
                targetId = byte.MaxValue;
                return true;
            }
            else
            {
                cont++;
                targetId = votedForId;
                Useing = true;
                Utils.SendMessage(string.Format(GetString("Skill.WhiteHacker"), UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(votedForId), true), Maximum - cont), Player.PlayerId);
                SendRPC();
                return true;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (!Useing)
            targetId = 255;
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __) => Useing = false;
    // 表示系の関数群
    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (targetId == byte.MaxValue) return "";
        seen ??= seer;
        if (GameStates.CalledMeeting && targetId == seen.PlayerId && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var roomName = GetLastRoom(seen);
            // 空のときにタグを付けると，suffixが空ではない判定となりなにもない3行目が表示される
            return roomName.Length == 0 ? "" : $"<size=1.5>{roomName}</size>";
        }
        return "";
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : Maximum <= cont ? Color.gray : Color.cyan, $"({Maximum - cont})");

    public string GetLastRoom(PlayerControl seen)
    {
        if (targetId == byte.MaxValue || !Player.IsAlive()) return "";
        if (!IsTrackTarget(seen) && targetId == seen.PlayerId) return "";
        var text = "";
        var room = PlayerState.GetByPlayerId(seen.PlayerId).LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else
        {
            text += Utils.ColorString(Palette.LightBlue, "@" + GetString(room.RoomId.ToString()));
        }

        return text;
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        if (!NowTracker && Awakened && CanUseTrackAbility.GetBool())
        {
            Player.RpcSetRole(RoleTypes.Tracker, true);
            NowTracker = true;
        }
        return true;
    }
    public override RoleTypes? AfterMeetingRole => NowTracker && Awakened && CanUseTrackAbility.GetBool() ? RoleTypes.Tracker : RoleTypes.Crewmate;
}