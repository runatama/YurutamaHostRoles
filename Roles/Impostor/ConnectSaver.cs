using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Impostor;

//メモ
//タゲ相手の狙われてる設定orタゲ相手が分かる設定追加する
//↑キルク変動つけたから、これでバランス見たい感じはある。

public sealed class ConnectSaver : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ConnectSaver),
            player => new ConnectSaver(player),
            CustomRoles.ConnectSaver,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4500,
            SetupOptionItem,
            "Cs",
            OptionSort: (3, 7)
        );
    public ConnectSaver(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        P1 = byte.MaxValue;
        P2 = byte.MaxValue;
        Max = OptionMaximum.GetFloat();
        cont = 0;
        use = false;
    }
    static OptionItem OptionKillCoolDown;

    static OptionItem OptionTageKillCoolDown;
    static OptionItem OptionMaximum;
    static OptionItem OptionNinzu;
    static OptionItem OptionDeathReason;
    byte P1;
    byte P2;
    static float Max;
    float cont;
    bool use;
    public static readonly CustomDeathReason[] deathReasons =
    {
        CustomDeathReason.Kill,CustomDeathReason.Suicide,CustomDeathReason.Revenge,CustomDeathReason.FollowingSuicide
    };
    enum OptionName
    {
        ConnectSaverPlayerCount, ConnectSaverDeathReason, ConnectSaverTageKillCooldown
    }
    private static void SetupOptionItem()
    {
        var cRolesString = deathReasons.Select(x => x.ToString()).ToArray();

        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false).SetValueFormat(OptionFormat.Seconds);
        OptionTageKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ConnectSaverTageKillCooldown, new(0f, 180f, 0.5f), 40f, false).SetValueFormat(OptionFormat.Seconds);
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.OptionCount, new(1f, 99f, 1f), 1f, false).SetValueFormat(OptionFormat.Times);
        OptionNinzu = IntegerOptionItem.Create(RoleInfo, 12, OptionName.ConnectSaverPlayerCount, new(0, 15, 1), 6, false).SetValueFormat(OptionFormat.Players);
        OptionDeathReason = StringOptionItem.Create(RoleInfo, 13, OptionName.ConnectSaverDeathReason, cRolesString, 3, false);
    }
    public override void Add() => AddSelfVotes(Player);
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(cont);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        cont = reader.ReadInt32();
    }

    public float CalculateKillCooldown() => use ? OptionTageKillCoolDown.GetFloat() : OptionKillCoolDown.GetFloat();
    public override void AfterMeetingTasks()
    {
        if (use) Main.AllPlayerKillCooldown[Player.PlayerId] = OptionTageKillCoolDown.GetFloat();
        else Main.AllPlayerKillCooldown[Player.PlayerId] = OptionKillCoolDown.GetFloat();

        Player.SyncSettings();
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(Max <= cont ? Color.gray : Palette.ImpostorRed, $"({Max - cont})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (PlayerCatch.AllAlivePlayersCount < OptionNinzu.GetInt()) return "";
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && Max > cont)
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Madmate.MadAvenger.Skill) return true;
        if (PlayerCatch.AllAlivePlayersCount < OptionNinzu.GetInt()) return true;
        if (Is(voter) && Max > cont && (P1 == byte.MaxValue || P2 == byte.MaxValue))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.ConnectSaver"), GetString("Vote.ConnectSaver")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                {
                    P1 = byte.MaxValue;
                    P2 = byte.MaxValue;
                    SetMode(Player, false);
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                }
                if (status is VoteStatus.Vote)
                    Tagech();
                return false;
            }
            else
            {
                if (!use && voter.PlayerId != votedForId && Is(voter) && votedForId != SkipId && ((P1 != 255 && P2 == 255) || (P1 == 255 && P2 != 255)))
                {
                    Tagech();
                    return false;
                }
            }
        }
        return true;

        void Tagech()
        {
            if (votedForId == voter.PlayerId) return;

            if (P1 == byte.MaxValue) P1 = votedForId;
            else if (P2 == byte.MaxValue) P2 = votedForId;

            if (P1 == P2) P2 = byte.MaxValue;

            var p1 = PlayerCatch.GetPlayerById(P1);
            var p2 = PlayerCatch.GetPlayerById(P2);

            if (!p1.IsAlive() || p1 == null) P1 = byte.MaxValue;
            if (!p2.IsAlive() || p2 == null) P2 = byte.MaxValue;

            if (P1 != byte.MaxValue || P2 != byte.MaxValue)
            {
                var Nowtargetcount = (P1 != byte.MaxValue && P2 != byte.MaxValue) ? GetString("TowPlayer") : GetString("OnePlayer");
                var lasttext = string.Format(GetString("Skill.Balancer"), Nowtargetcount, UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(votedForId), true));
                Utils.SendMessage(lasttext.ToString(), Player.PlayerId);
            }
            if (P1 != byte.MaxValue && P2 != byte.MaxValue)
            {
                Utils.SendMessage(GetString("Skill.ConnectSaver"), Player.PlayerId);
                cont++;//成功or失敗にかかわらず発動はした記録。
                SendRPC();
                use = true;
            }
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill) return;
        var (killer, target) = info.AttemptTuple;

        if (info.IsFakeSuicide) return;
        if (use && ((target.PlayerId == P1 && P1 != byte.MaxValue) || (target.PlayerId == P2 && P2 != byte.MaxValue)))
        {
            if (Is(killer))
            {
                CheckMurderPatch.TimeSinceLastKill[killer.PlayerId] = 30f;//キル連打とかいう奴を無視する奴
                var targetid = target.PlayerId == P1 ? P2 : P1;
                var connecttarget = PlayerCatch.GetPlayerById(targetid);
                if (CustomRoleManager.OnCheckMurder(killer, connecttarget, connecttarget, connecttarget, true, Killpower: 10))//一応殺した判定は貰うしガードとかいうの知らない。
                {
                    PlayerState.GetByPlayerId(connecttarget.PlayerId).DeathReason = deathReasons[OptionDeathReason.GetValue()];
                    connecttarget.SetRealKiller(killer);
                }
            }
            P1 = byte.MaxValue;
            P2 = byte.MaxValue;
            use = false;
            CheckMurderPatch.TimeSinceLastKill[killer.PlayerId] = 0f;//無視してもキル連打はさせない。
            return;
        }

        if (use && !(target.PlayerId == P1 || target.PlayerId == P2)) info.DoKill = false;//ターゲット生存中は他のキルは不可
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (use && seer == Player)
        {
            if ((seen.PlayerId == P1 && P1 != byte.MaxValue) || (seen.PlayerId == P2 && P2 != byte.MaxValue))
            {
                return Utils.ColorString(Palette.Purple, GetString("CS.Target"));
            }
        }
        return "";
    }
    public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo oniku)
    {
        //会議入ったらリセット
        P1 = byte.MaxValue;
        P2 = byte.MaxValue;
        use = false;
    }
}