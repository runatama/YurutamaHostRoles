using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using System;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class ShrineMaiden : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShrineMaiden),
            player => new ShrineMaiden(player),
            CustomRoles.ShrineMaiden,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9600,
            (3, 4),
            SetupOptionItem,
            "SM",
            "#b7282e",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public ShrineMaiden(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionMaximum.GetFloat();
        Repo = false;
        count = 0;
        mcount = 0;
        cantaskcount = Optioncantaskcount.GetFloat();
        kakusei = !Kakusei.GetBool() || cantaskcount < 1;
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        Oniku = 111;
    }

    private static OptionItem OptionMaximum;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    static OptionItem Kakusei;
    bool kakusei;
    public float Max;
    public VoteMode Votemode;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float mcount;
    static bool Repo;
    static byte Oniku;

    enum Option
    {
        Ucount,
        Votemode,
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.Ucount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 1, false);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 15, GeneralOption.meetingmc, new(0f, 99f, 1f), 0f, false, infinity: true)
            .SetValueFormat(OptionFormat.Times);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.UKakusei, true, false);
    }

    public override void Add()
        => AddS(Player);

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo O)
    {
        if (O != null)
        {
            Repo = true;
            Oniku = O.PlayerId;
        }
        else
        {
            Repo = false;
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        Repo = false;//いらない気がするけど一応保険
        Oniku = 111;
    }
    public override void OnStartMeeting() => mcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (Repo && Player.IsAlive() && isForMeeting && kakusei && seer.PlayerId == seen.PlayerId && Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == VoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Repo && Max > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            if (Votemode == VoteMode.uvote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Miko(votedForId);
                return false;
            }
            else
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Divied"), GetString("Vote.Divied")) + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        Miko(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void Miko(byte votedForId)
    {
        var target1 = PlayerCatch.GetPlayerById(Oniku);
        var target2 = PlayerCatch.GetPlayerById(votedForId);
        if (!target2.IsAlive()) return;
        count++;
        mcount++;

        Logger.Info($"Player: {Player.name},Target1: {target1.name}Target2: {target2.name}", "ShrineMaiden");
        var FtR1 = target1.GetRoleClass()?.GetFtResults(Player);
        var role1 = FtR1 is not CustomRoles.NotAssigned ? FtR1.Value : target1.GetCustomRole();
        SendRPC();
        var ta1 = target1.GetCustomRole();
        var ta2 = target2.GetCustomRole();
        var t1 = ta1.GetCustomRoleTypes();
        var t2 = ta2.GetCustomRoleTypes();
        var madmate = Options.MadTellOpt().GetCustomRoleTypes();
        //マッドならimpにする
        if (t1 == CustomRoleTypes.Madmate) t1 = madmate is CustomRoleTypes.Madmate ? madmate : CustomRoleTypes.Impostor;
        if (t2 == CustomRoleTypes.Madmate) t2 = madmate is CustomRoleTypes.Madmate ? madmate : CustomRoleTypes.Impostor;

        if (t1 == t2)
        {
            Utils.SendMessage(string.Format(GetString("ShrineMaidencollect"), Utils.GetPlayerColor(target1, true), Utils.GetPlayerColor(target2, true)) + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count)) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
        else
        {
            Utils.SendMessage(string.Format(GetString("ShrineMaidennotcollect"), Utils.GetPlayerColor(target1, true), Utils.GetPlayerColor(target2, true)) + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count)) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount)) kakusei = true;
        return true;
    }
}