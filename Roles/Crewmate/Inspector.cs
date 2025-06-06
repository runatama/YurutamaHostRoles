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
            18900,
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
        kakusei = !OptKakusei.GetBool() || Task.GetInt() < 1;
        Max = OptionMaximum.GetInt();
        count = 0;
        TargetPlayerId = byte.MaxValue;
        Votemode = (FortuneTeller.VoteMode)OptionVoteMode.GetValue();
        Isdie = false;
        timer = 0;
    }
    bool kakusei;
    static OptionItem OptionMaximum;
    static OptionItem OptKakusei;
    static OptionItem Task;
    static OptionItem OptionVoteMode;
    public FortuneTeller.VoteMode Votemode;
    int Max;
    int count;
    byte TargetPlayerId;
    float timer;
    bool Isdie;


    enum OptionName
    {
        Votemode
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
    public override void Add() => AddS(Player);
    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.OptionCount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, OptionName.Votemode, EnumHelper.GetAllNames<FortuneTeller.VoteMode>(), 1, false);
        OptKakusei = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.TaskKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.Kakuseitask, new(1f, 99f, 1f), 5f, false, OptKakusei);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (Isdie)
        {
            timer += Time.fixedDeltaTime;
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
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(Task.GetValue()))
        {
            if (kakusei == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            kakusei = true;
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
                        var color = "";
                        if (cos.ColorId is 0 or 1 or 2 or 6 or 8 or 9 or 12 or 15 or 16)
                        {
                            color = Palette.GetColorName((int)ModColors.PlayerColor.Black);
                        }
                        else color = Palette.GetColorName((int)ModColors.PlayerColor.white);

                        sb.AppendFormat(GetString("Inspector.InfoColor"), Utils.GetPlayerColor(TargetPlayerId), color);
                    }
                    break;
                case Infom.DeathReason:
                    sb.AppendFormat(GetString("Inspector.InfoDeathReason"), Utils.GetPlayerColor(TargetPlayerId), GetString($"DeathReason.{targetstate.DeathReason}"));
                    break;
                case Infom.DeathTimer:
                    sb.AppendFormat(GetString("Inspector.InfoTimer"), Utils.GetPlayerColor(TargetPlayerId), (int)timer);
                    break;
                case Infom.KillerRole:
                    sb.AppendFormat(GetString("Inspector.InfoRole"), Utils.GetPlayerColor(TargetPlayerId), UtilsRoleText.GetTrueRoleName(targetstate.RealKiller.killerid, false));
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
                    sb.AppendFormat(GetString("Inspector.InfoTeam"), Utils.GetPlayerColor(TargetPlayerId), str);
                    break;
                case Infom.TargetRoom:
                    if (targetstate.KillRoom is "")
                    {
                        sb.AppendFormat(GetString("Inspector.InfoDeathReason"), Utils.GetPlayerColor(TargetPlayerId), GetString($"DeathReason.{targetstate.DeathReason}"));
                        break;
                    }
                    sb.AppendFormat(GetString("Inspector.InfoRoom"), Utils.GetPlayerColor(TargetPlayerId), targetstate.KillRoom);
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
        timer = 0;
        TargetPlayerId = byte.MaxValue;
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && TargetPlayerId is byte.MaxValue && kakusei)
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
            if (Votemode == FortuneTeller.VoteMode.uvote)
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
        Utils.SendMessage(string.Format(GetString("Skill.Inspector"), Utils.GetPlayerColor(votedForId)), Player.PlayerId);
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}", "Inspector");
    }
}
