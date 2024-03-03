using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

using static TownOfHost.Translator;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;
public sealed class MeetingSheriff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MeetingSheriff),
            player => new MeetingSheriff(player),
            CustomRoles.MeetingSheriff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20450,
            SetupOptionItem,
            "Ms",
            "#f8cd46"
            );
    public MeetingSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionSheriffShotLimit.GetFloat();
        count = 0;
        mcount = 0;
        cankillMad = OptioncankillMad.GetBool();
        cankillN = OptioncankillN.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
    }

    private static OptionItem OptionSheriffShotLimit;
    private static OptionItem OptioncankillMad;
    private static OptionItem OptioncankillN;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    public float Max;
    float cantaskcount;
    bool cankillMad;
    bool cankillN;
    int count;
    float onemeetingmaximum;
    float mcount;

    enum Option
    {
        SheriffShotLimit,
        cantaskcount,//効果を発揮タスク数
        cankillMad,
        cankillN,
        meetingmc
    }
    private static void SetupOptionItem()
    {
        OptionSheriffShotLimit = FloatOptionItem.Create(RoleInfo, 10, Option.SheriffShotLimit, new(1f, 15f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 11, Option.cantaskcount, new(0, 99, 1), 5, false);
        OptioncankillMad = BooleanOptionItem.Create(RoleInfo, 12, Option.cankillMad, false, true);
        OptioncankillN = BooleanOptionItem.Create(RoleInfo, 13, Option.cankillN, false, true);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 14, Option.meetingmc, new(0f, 99f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Times);
    }

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
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage("正義執行モードになりました！\n\nシェリフの能力を使うプレイヤーに投票→シェリフの能力発動。\n" + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    Sheriff(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    /*次リリースして改良版がだめなら戻そう。

    public void Sheriff(byte votedForId)
    {
        count++;
        var target = Utils.GetPlayerById(votedForId);
        var role = target.GetCustomRole() is CustomRoles.Tairou ? CustomRoles.Crewmate : target.GetCustomRole();
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SendRPC();
        var state = PlayerState.GetByPlayerId(target.PlayerId);

        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}", "MeetingSheriff");

        if (target.Is(CustomRoles.Tairou) && Tairou.DeathReasonTairo)
        {
            var Xplayer = Utils.GetPlayerById(Player.PlayerId);
            Xplayer.RpcExileV2();
            var Xstate = PlayerState.GetByPlayerId(Xplayer.PlayerId);
            Xstate.DeathReason = CustomDeathReason.Revenge1;
            Xstate.SetDead();
            Logger.Info($"{Xplayer.GetNameWithRole()}がシェリフ失敗(大狼)", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerById(Player.PlayerId).name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.KillOverlay.ShowKillAnimation(Player.Data, Player.Data);
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
        }
        else
        if (role.IsImpostor() && !target.Is(CustomRoles.Tairou))
        {
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;//これないと死因その他になる
            state.SetDead();
            Logger.Info($"{target.GetNameWithRole()}がシェリフ成功(IMP)", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerById(votedForId).name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.KillOverlay.ShowKillAnimation(target.Data, target.Data);
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
        }
        else
        if (role.IsMadmate() && cankillMad)
        {
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();
            Logger.Info($"{target.GetNameWithRole()}がシェリフ成功(MAD)", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerById(votedForId).name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.KillOverlay.ShowKillAnimation(target.Data, target.Data);
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
        }
        else
        if (role.IsNeutral() && cankillN)
        {
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();
            Logger.Info($"{target.GetNameWithRole()}がシェリフ成功(Neutral)", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerById(votedForId).name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.KillOverlay.ShowKillAnimation(target.Data, target.Data);
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
        }
        else
        {//どれにも当てはまらない=ミス
            var Xplayer = Utils.GetPlayerById(Player.PlayerId);
            Xplayer.RpcExileV2();
            var Xstate = PlayerState.GetByPlayerId(Xplayer.PlayerId);
            Xstate.DeathReason = CustomDeathReason.Misfire;
            Xstate.SetDead();
            Logger.Info($"{Xplayer.GetNameWithRole()}がシェリフ失敗", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerById(Player.PlayerId).name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.KillOverlay.ShowKillAnimation(Player.Data, Player.Data);
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
        }
    }*/

    public void Sheriff(byte votedForId)
    {
        PlayerState state;
        var target = Utils.GetPlayerById(votedForId);
        var hudManager = DestroyableSingleton<HudManager>.Instance.KillOverlay;
        count++;
        mcount++;//1会議のカウント
        SendRPC();

        if (CanBeKilledBy(target.GetCustomRole()))
        {
            state = PlayerState.GetByPlayerId(target.PlayerId);
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();
            Logger.Info($"{Player.GetNameWithRole()}がシェリフ成功({target.GetNameWithRole()}) 残り{Max - count}", "MeetingSheriff");
            Utils.SendMessage(target.name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            hudManager.ShowKillAnimation(target.Data, target.Data);
            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
            return;
        }
        state = PlayerState.GetByPlayerId(Player.PlayerId);
        Player.RpcExileV2();
        state.DeathReason = target.Is(CustomRoles.Tairou) && Tairou.DeathReasonTairo ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire;
        state.SetDead();
        Logger.Info($"{Player.GetNameWithRole()}がシェリフ失敗({target.GetNameWithRole()}) 残り{Max - count}", "MeetingSheriff");
        Utils.SendMessage(Player.name + GetString("Meetingkill"), title: GetString("MSKillTitle"));
        hudManager.ShowKillAnimation(Player.Data, Player.Data);
        SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
    }
    bool CanBeKilledBy(CustomRoles role)
    {
        return role.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => role is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => cankillMad,
            CustomRoleTypes.Neutral => cankillN,
            _ => false
        };
    }// ↓改良したの作っちゃった☆ 動くかはわかんない byけーわい
    //ｶｲﾘｮｳｼﾃﾓﾗｯﾀﾅﾗﾂｶﾜﾅｲﾜｹｶﾞﾅｲ!!(大老の処理とマッドの処理が出来てたからニュートラルもできるはず!!)
}
//コード多分改良できるけど動いてるからヨシ。(´・ω・｀)