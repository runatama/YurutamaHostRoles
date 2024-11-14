using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class AmateurTeller : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AmateurTeller),
            player => new AmateurTeller(player),
            CustomRoles.AmateurTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            18100,
            SetupOptionItem,
            "AT",
            "#6b3ec3"
        );
    public AmateurTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Targets.Clear();
        Divination.Clear();
        count = 0;
        Use = false;
        kakusei = !Kakusei.GetBool();
        UseTarget = byte.MaxValue;
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        CustomRoleManager.MarkOthers.Add(OtherArrow);
    }

    static OptionItem OptionMaximum;
    static OptionItem OptionVoteMode;
    static OptionItem OptionRole;
    static OptionItem OptionCanTaskcount;
    static OptionItem Kakusei;
    static OptionItem TargetCanseeArrow;
    static OptionItem TargetCanseePlayer;
    static OptionItem AbilityUseTarnCanButton;
    public VoteMode Votemode;
    int count;
    bool kakusei;
    bool Use;
    byte UseTarget;
    List<byte> Targets = new();
    Dictionary<byte, CustomRoles> Divination = new();
    static HashSet<AmateurTeller> tellers = new();

    enum Option
    {
        Ucount,
        Votemode,
        tRole,
        AmateurTellerTargetCanseeArrow,
        AmateurTellerCanUseAbilityTarnButton,
        AmateurTellerTargetCanseePlayer
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    public override void Add()
    {
        AddS(Player);
        tellers.Add(this);
    }
    public override void OnDestroy()
    {
        tellers.Clear();
    }
    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 10, Option.Ucount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 1, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 12, Option.tRole, true, false);
        TargetCanseePlayer = BooleanOptionItem.Create(RoleInfo, 13, Option.AmateurTellerTargetCanseePlayer, true, false);
        TargetCanseeArrow = BooleanOptionItem.Create(RoleInfo, 14, Option.AmateurTellerTargetCanseeArrow, true, false, TargetCanseePlayer);
        AbilityUseTarnCanButton = BooleanOptionItem.Create(RoleInfo, 15, Option.AmateurTellerCanUseAbilityTarnButton, true, false);
        OptionCanTaskcount = FloatOptionItem.Create(RoleInfo, 16, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 17, GeneralOption.UKakusei, true, false);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < OptionCanTaskcount.GetInt() && !IsTaskFinished ? Color.gray : OptionMaximum.GetInt() <= count ? Color.gray : Color.cyan, $"({OptionMaximum.GetInt() - count})");
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Use = false;
        TargetArrow.Remove(UseTarget, Player.PlayerId);
        Targets.Add(UseTarget);
        UseTarget = byte.MaxValue;
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reportreson)
    {
        if (UseTarget != byte.MaxValue && reporter.PlayerId == Player.PlayerId && target == null && !AbilityUseTarnCanButton.GetBool())
        {
            reportreson = DontReportreson.CantUseButton;
            return true;
        }
        return false;
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (OptionMaximum.GetInt() > count && Is(voter) && (MyTaskState.CompletedTasksCount >= OptionCanTaskcount.GetInt() || IsTaskFinished) && (!Use))
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
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
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        count++;
        Use = true;
        UseTarget = target.PlayerId;
        TargetArrow.Add(target.PlayerId, Player.PlayerId);
        Utils.SendMessage(Utils.GetPlayerColor(target.PlayerId) + GetString("AmatruertellerTellMeg"), Player.PlayerId);
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptionCanTaskcount.GetInt()))
        {
            if (kakusei == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            kakusei = true;
        }
        return true;
    }
    public static string OtherArrow(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!TargetCanseePlayer.GetBool()) return "";

        foreach (var tell in tellers)
        {
            if (seer.PlayerId == tell.UseTarget && seer == seen)
            {
                var ar = "";
                if (TargetCanseeArrow.GetBool()) ar = $"\n{TargetArrow.GetArrows(seer, tell.Player.PlayerId)}";
                if (seer.GetCustomRole().GetCustomRoleTypes() != CustomRoleTypes.Crewmate)
                    return $"<color=#6b3ec3>★{ar}</color>";
            }
            else
            if (seer.PlayerId == tell.UseTarget && seen == tell.Player)
                return "<color=#6b3ec3>★</color>";
        }
        return "";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (!Player.IsAlive()) return;
        if (UseTarget == seen.PlayerId) return;
        if (Targets.Contains(seen.PlayerId))
        {
            addon = false;
            if (!OptionRole.GetBool())
            {
                enabled = true;
                switch (seen.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                    case CustomRoleTypes.Madmate:
                        roleColor = Palette.CrewmateBlue;
                        roleText = GetString("Crewmate");
                        break;
                    case CustomRoleTypes.Impostor:
                        roleColor = ModColors.ImpostorRed;
                        roleText = GetString("Impostor");
                        break;
                    case CustomRoleTypes.Neutral:
                        roleColor = ModColors.NeutralGray;
                        roleText = GetString("Neutral");
                        break;
                }
            }
            else
            {
                enabled = true;
            }
        }
    }
}