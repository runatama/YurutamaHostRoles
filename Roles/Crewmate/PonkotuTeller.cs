using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using System;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class PonkotuTeller : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(PonkotuTeller),
            player => new PonkotuTeller(player),
            CustomRoles.PonkotuTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            28310,
            SetupOptionItem,
            "po",
            "#6b3ec3",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public PonkotuTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        collect = Optioncollect.GetInt();
        Max = OptionMaximum.GetFloat();
        Divination.Clear();
        count = 0;
        mcount = 0;
        srole = OptionRole.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
    }

    private static OptionItem Optioncollect;
    private static OptionItem OptionMaximum;
    private static OptionItem OptionRole;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    public float collect;
    public float Max;
    public bool srole;
    public VoteMode Votemode;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float mcount;
    Dictionary<byte, CustomRoles> Divination = new();

    enum Option
    {
        collectrect,
        Ucount,
        Votemode,
        tRole,
        cantaskcount,
        meetingmc
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    private static void SetupOptionItem()
    {
        Optioncollect = FloatOptionItem.Create(RoleInfo, 10, Option.collectrect, new(0f, 100f, 2f), 70f, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.Ucount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 0, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 13, Option.tRole, true, false);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 14, Option.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 15, Option.meetingmc, new(0f, 99f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Add()
        => AddS(Player);

    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetCount);
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetCount)
        {
            count = reader.ReadInt32();
        }
    }
    public override void OnStartMeeting() => mcount = 0;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (Max > count && Is(voter) && MyTaskState.CompletedTasksCount >= cantaskcount && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            if (Votemode == VoteMode.uvote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Uranai(votedForId);
                return false;
            }
            else
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage("占いモードになりました！\n\n占いたいプレイヤーに投票→占い能力発動\n" + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        Uranai(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void Uranai(byte votedForId)
    {
        int chance = IRandom.Instance.Next(1, 101);
        var target = Utils.GetPlayerById(votedForId);
        count++;
        mcount++;
        if (chance < collect)
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(成功)", "PonkotuTeller");
            var FtR = target.GetRoleClass()?.GetFtResults(Player); //結果を変更するかチェック
            var role = FtR is not CustomRoles.NotAssigned ? FtR.Value : target.GetCustomRole();
            SendRPC();
            Utils.SendMessage(target.name + "さんを占いました。\n結果は.." + (srole ? GetString($"{role}") : GetString($"{role.GetCustomRoleTypes()}")) + $"..?" + $"\n\n{(onemeetingmaximum != 0 ? $"この会議では残り{Math.Min(onemeetingmaximum - mcount, Max - count)}" : $"残り{Max - count}")}回占うことができます" + (Votemode == VoteMode.SelfVote ? GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
        else
        {
            var tage = new List<PlayerControl>(Main.AllPlayerControls);
            var rand = IRandom.Instance;
            var P = tage[rand.Next(0, tage.Count)];
            var role = P.GetCustomRole();
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(失敗)", "PonkotuTeller");
            SendRPC();
            Utils.SendMessage(target.name + "さんを占いました。\n結果は.." + (srole ? GetString($"{role}") : GetString($"{role.GetCustomRoleTypes()}")) + $"..?" + $"\n\n{(onemeetingmaximum != 0 ? $"この会議では残り{Math.Min(onemeetingmaximum - mcount, Max - count)}" : $"残り{Max - count}")}回占うことができます" + (Votemode == VoteMode.SelfVote ? GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
    }
}