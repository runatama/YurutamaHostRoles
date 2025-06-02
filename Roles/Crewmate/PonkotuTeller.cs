using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using System;
using static TownOfHost.Modules.SelfVoteManager;

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
            18200,
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
        rolename = Optionrolename.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        kakusei = !Kakusei.GetBool() || cantaskcount < 1; ;
        if (!FTOption.GetBool())
        {
            rolename = FortuneTeller.Optionrolename.GetBool();
            srole = FortuneTeller.OptionRole.GetBool();
            cantaskcount = FortuneTeller.OptionCanTaskcount.GetFloat();
            Votemode = (VoteMode)FortuneTeller.OptionVoteMode.GetValue();
            onemeetingmaximum = FortuneTeller.Option1MeetingMaximum.GetFloat();
            kakusei = !FortuneTeller.Kakusei.GetBool();
            Max = FortuneTeller.OptionMaximum.GetFloat();
        }
        GameTell = new();
    }
    static OptionItem FTOption;
    private static OptionItem Optioncollect;
    private static OptionItem OptionMaximum;
    private static OptionItem OptionRole;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    private static OptionItem Optionrolename;
    private static OptionItem OptionDontChengeGame;
    static OptionItem Kakusei;
    static OptionItem MeisFT;
    bool kakusei;
    public float collect;
    public float Max;
    public bool srole;
    public bool rolename;
    public VoteMode Votemode;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float mcount;
    Dictionary<byte, CustomRoles> Divination = new();
    Dictionary<byte, CustomRoles> GameTell = new();

    enum Option
    {
        TellerCollectRect,
        Ucount,
        Votemode,
        rolename,
        tRole,
        PonkotuTellerFTOption,
        PonkotuTellerMyisFT,
        PonkotuDontChengeGame
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    private static void SetupOptionItem()
    {
        Optioncollect = FloatOptionItem.Create(RoleInfo, 10, Option.TellerCollectRect, new(0f, 100f, 2f), 70f, false)
            .SetValueFormat(OptionFormat.Percent);
        FTOption = BooleanOptionItem.Create(RoleInfo, 17, Option.PonkotuTellerFTOption, true, false);
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.Ucount, new(1f, 99f, 1f), 1f, false, FTOption)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 1, false, FTOption);
        Optionrolename = BooleanOptionItem.Create(RoleInfo, 19, Option.rolename, true, false, FTOption);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 13, Option.tRole, true, false, FTOption);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.cantaskcount, new(0, 99, 1), 5, false, FTOption);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 15, GeneralOption.meetingmc, new(0f, 99f, 1f), 0f, false, FTOption, infinity: true)
            .SetValueFormat(OptionFormat.Times);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.UKakusei, true, false, FTOption);
        MeisFT = BooleanOptionItem.Create(RoleInfo, 18, Option.PonkotuTellerMyisFT, false, false);
        OptionDontChengeGame = BooleanOptionItem.Create(RoleInfo, 20, Option.PonkotuDontChengeGame, false, true);
    }
    public override void Add() => AddS(Player);
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override void OnStartMeeting() => mcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && kakusei && seer.PlayerId == seen.PlayerId && Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == VoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {

        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount) && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
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
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Divied"), GetString("Vote.Divied")) + GetString("VoteSkillMode"), Player.PlayerId);
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
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (GameTell.TryGetValue(votedForId, out var telledrole) && OptionDontChengeGame.GetBool())
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(再掲)", "PonkotuTeller");
            var s = GetString("Skill.Tellerfin") + (telledrole.IsCrewmate() ? "!" : "...");
            if (MeisFT.GetBool()) Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{telledrole}").Color(UtilsRoleText.GetRoleColor(telledrole)) + "</b>" : GetString($"{telledrole.GetCustomRoleTypes()}")) + s + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            else Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{telledrole}").Color(UtilsRoleText.GetRoleColor(telledrole)) + "</b>" : GetString($"{telledrole.GetCustomRoleTypes()}")) + $"..?" + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            return;
        }
        count++;
        mcount++;
        if (chance < collect)
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(成功)", "PonkotuTeller");
            var FtR = target.GetRoleClass()?.GetFtResults(Player); //結果を変更するかチェック
            var role = FtR is not CustomRoles.NotAssigned ? FtR.Value : target.GetCustomRole();
            SendRPC();
            GameTell.TryAdd(votedForId, role);
            var s = GetString("Skill.Tellerfin") + (role.IsCrewmate() ? "!" : "...");
            if (MeisFT.GetBool()) Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + s + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            else Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + $"..?" + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        }
        else
        {
            var tage = new List<PlayerControl>(PlayerCatch.AllPlayerControls);
            var rand = IRandom.Instance;
            var P = tage[rand.Next(0, tage.Count)];
            var FtR = target.GetRoleClass()?.GetFtResults(null); //結果を変更するかチェック
            var role = FtR is not CustomRoles.NotAssigned ? FtR.Value : P.GetCustomRole();

            GameTell.TryAdd(votedForId, role);
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(失敗)", "PonkotuTeller");
            var s = GetString("Skill.Tellerfin") + (role.IsCrewmate() ? "!" : "...");
            SendRPC();
            if (MeisFT.GetBool()) Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + s + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            else Utils.SendMessage(string.Format(GetString("Skill.Teller"), Utils.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + $"..?" + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - mcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        }
    }
    public override CustomRoles Jikaku() => kakusei ? (MeisFT.GetBool() ? CustomRoles.FortuneTeller : CustomRoles.NotAssigned) : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount))
        {
            if (kakusei == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            kakusei = true;
        }
        return true;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Divination.ContainsKey(seen.PlayerId) && rolename)
        {
            if (srole)
                return $"<color={UtilsRoleText.GetRoleColorCode(Divination[seen.PlayerId])}>" + GetString(Divination[seen.PlayerId].ToString());
            else return GetString(Divination[seen.PlayerId].GetCustomRoleTypes().ToString());
        }
        return "";
    }
}