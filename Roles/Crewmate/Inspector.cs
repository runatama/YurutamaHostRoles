using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class Inspector : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Inspector),
            player => new Inspector(player),
            CustomRoles.Inspector,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9700,
            (3, 5),
            SetupOptionItem,
            "Is",
            "#977b48"
        );
    public Inspector(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Awakened = !OptAwakening.GetBool() || OptAwakeningTaskcount.GetInt() < 1;
        Max = OptionMaximum.GetInt();
        count = 0;
        TargetPlayerId = byte.MaxValue;
        Votemode = (AbilityVoteMode)OptionVoteMode.GetValue();
        Isdie = false;
        deadtimer = 0;
    }
    bool Awakened;
    static OptionItem OptionMaximum;
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTaskcount;
    static OptionItem OptionVoteMode;
    public AbilityVoteMode Votemode;
    static int Max;
    int count;
    byte TargetPlayerId;
    float deadtimer;
    bool Isdie;

    enum OptionName
    {
        InspectVoteMode
    }

    enum Infom
    {
        DeathReason,
        Color,
        TargetTeam,
        KillerRole,
        DeathTimer,
        TargetRoom,
    }
    public override void Add() => AddSelfVotes(Player);
    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.OptionCount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, OptionName.InspectVoteMode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.TaskAwakening, false, false);
        OptAwakeningTaskcount = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.AwakeningTaskcount, new(1f, 99f, 1f), 5f, false, OptAwakening);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (Isdie)
        {
            deadtimer += Time.fixedDeltaTime;
            return;
        }
        if (!player.IsAlive() || !AmongUsClient.Instance.AmHost || TargetPlayerId is byte.MaxValue) return;

        var target = PlayerCatch.GetPlayerById(TargetPlayerId);
        if (target.IsAlive()) return;

        if (PlayerState.GetByPlayerId(TargetPlayerId).DeathReason is CustomDeathReason.Disconnected)
        {
            TargetPlayerId = byte.MaxValue;
            return;
        }

        Isdie = true;
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptAwakeningTaskcount.GetValue()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override void OnStartMeeting()
    {
        if (Isdie)
        {
            PlayerState targetstate = TargetPlayerId.GetPlayerState();
            StringBuilder sb = new();
            var chance = (Infom)IRandom.Instance.Next(EnumHelper.GetAllValues<Infom>().Count());
            switch (chance)
            {
                case Infom.Color:
                    if (Camouflage.PlayerSkins.TryGetValue(targetstate.RealKiller.killerid, out var cos))
                    {
                        var lightName = "";
                        if (cos.ColorId is 0 or 1 or 2 or 6 or 8 or 9 or 12 or 15 or 16)
                        {
                            lightName = Palette.GetColorName((int)ModColors.PlayerColor.Black);
                        }
                        else lightName = Palette.GetColorName((int)ModColors.PlayerColor.white);

                        sb.AppendFormat(GetString("Inspector.InfoColor"), UtilsName.GetPlayerColor(TargetPlayerId), lightName);
                    }
                    break;
                case Infom.DeathReason:
                    sb.AppendFormat(GetString("Inspector.InfoDeathReason"), UtilsName.GetPlayerColor(TargetPlayerId), GetString($"DeathReason.{targetstate.DeathReason}"));
                    break;
                case Infom.DeathTimer:
                    sb.AppendFormat(GetString("Inspector.InfoTimer"), UtilsName.GetPlayerColor(TargetPlayerId), (int)deadtimer);
                    break;
                case Infom.KillerRole:
                    sb.AppendFormat(GetString("Inspector.InfoRole"), UtilsName.GetPlayerColor(TargetPlayerId), UtilsRoleText.GetTrueRoleName(targetstate.RealKiller.killerid, false));
                    break;
                case Infom.TargetTeam:
                    string str = "";
                    switch (targetstate.MainRole.GetCustomRoleTypes())
                    {
                        case CustomRoleTypes.Crewmate:
                            str = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Crewmate);
                            break;
                        case CustomRoleTypes.Impostor:
                            str = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Impostor);
                            break;
                        case CustomRoleTypes.Madmate:
                            str = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Madmate);
                            break;
                        case CustomRoleTypes.Neutral:
                            str = Utils.ColorString(ModColors.Gray, GetString("Neutral"));
                            break;
                    }
                    sb.AppendFormat(GetString("Inspector.InfoTeam"), UtilsName.GetPlayerColor(TargetPlayerId), str);
                    break;
                case Infom.TargetRoom:
                    if (targetstate.KillRoom is "")
                    {
                        sb.AppendFormat(GetString("Inspector.InfoDeathReason"), UtilsName.GetPlayerColor(TargetPlayerId), GetString($"DeathReason.{targetstate.DeathReason}"));
                        break;
                    }
                    sb.AppendFormat(GetString("Inspector.InfoRoom"), UtilsName.GetPlayerColor(TargetPlayerId), targetstate.KillRoom);
                    break;
                default:
                    sb.Append("???");
                    break;
            }
            Logger.Info($"{Player.Data.GetLogPlayerName()} => {chance}", "Inspector");
            _ = new LateTask(() =>
                Utils.SendMessage(sb.ToString(), Player.PlayerId, $"<{RoleInfo.RoleColorCode}>{GetString("Inspector.Title")}</color>")
                , 3, "InspectorSend", true);
        }
        deadtimer = 0;
        Isdie = false;
        TargetPlayerId = byte.MaxValue;
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && TargetPlayerId is byte.MaxValue && Awakened)
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
            if (Votemode == AbilityVoteMode.NomalVote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Inspect(votedForId);
                return false;
            }
            else
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Inspect"), GetString("Vote.Inspect")) + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        Inspect(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }

    public void Inspect(byte votedForId)
    {
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;//死んでるならここで処理を止める。
        count++;//全体のカウント
        TargetPlayerId = votedForId;
        Utils.SendMessage(string.Format(GetString("Skill.Inspector"), UtilsName.GetPlayerColor(votedForId)), Player.PlayerId);
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}", "Inspector");
    }
}
