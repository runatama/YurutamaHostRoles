using System.Collections.Generic;
using System.Linq;

using AmongUs.GameOptions;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Madmate;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;
public sealed class MassMedia : RoleBase, IImpostor, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MassMedia),
            player => new MassMedia(player),
            CustomRoles.MassMedia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            45000,
            SetupOptionItem,
            "MM",
            "#512513",
            true,
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            from: From.None
        );
    public MassMedia(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.OnMurderPlayerOthers.Add(TageKillCh);
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionShikai;
    private static float KillCooldown;
    public byte Target;
    byte Guees;
    bool Makkura;
    bool Winchance;
    bool Win;
    Vector3 TagePo;
    public static HashSet<MassMedia> MassMedias = new();
    enum Option
    {
        MassMediaShikai
    }
    public override void Add()
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        Target = byte.MaxValue;
        Makkura = false;
        TagePo = new Vector3(999f, 999f);
        Winchance = false;
        Win = false;
        Guees = byte.MaxValue;

        MassMedias.Add(this);
    }
    public override void OnDestroy() => MassMedias.Clear();
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionShikai = FloatOptionItem.Create(RoleInfo, 11, Option.MassMediaShikai, new(0f, 0.20f, 0.02f), 0.04f, false)
        .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var tg = Utils.GetPlayerById(Target);
        if (tg == null) return;

        if (!player.IsAlive()) return;

        //範囲
        Vector2 GSpos = player.transform.position;
        var Mieruhani = 7.5f * Main.DefaultCrewmateVision;
        if (player.Is(CustomRoles.Lighting)) Mieruhani = 7.5f * Main.DefaultImpostorVision;

        //position
        float HitoDistance = Vector2.Distance(GSpos, tg.transform.position);
        if (!tg.IsAlive()) HitoDistance = Vector2.Distance(GSpos, TagePo);

        if (HitoDistance <= Mieruhani)//更新があるなら～
        {
            if (!Makkura)
            {
                Makkura = true;
                Player.MarkDirtySettings();
            }
        }
        else
        {
            if (Makkura)
            {
                Makkura = false;
                Player.MarkDirtySettings();
            }
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Is(killer))
        {
            if (Target != byte.MaxValue)
            {
                info.CanKill = false;
                return;
            }
            Target = target.PlayerId;
            info.CanKill = false;
            Main.AllPlayerKillCooldown[killer.PlayerId] = 999;
            killer.SyncSettings();
            killer.RpcProtectedMurderPlayer();
            TargetArrow.Add(Player.PlayerId, target.PlayerId);
        }
    }
    public void TageKillCh(MurderInfo info)//こぉれは特ダネだぁ!!
    {
        var (killer, target) = info.AttemptTuple;

        if (target.PlayerId == Target && Player.Is(CustomRoles.MassMedia))
        {
            GetArrow.Add(Player.PlayerId, target.transform.position);
            TagePo = target.transform.position;
            Guees = killer.PlayerId;
        }
    }
    public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo tg)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Is(repo) && Player.Is(CustomRoles.MassMedia))//自分が通報したならチャンスだよ!!
        {
            if (tg != null)//死体通報なら～
                if (tg.PlayerId == Target)
                {
                    Winchance = true;
                }
        }
        //リセット
        TargetArrow.Remove(Player.PlayerId, Target);
        GetArrow.Remove(Player.PlayerId, TagePo);
        TagePo = new Vector3(999f, 999f);
        Target = byte.MaxValue;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (Target == byte.MaxValue) return "";
        seen ??= seer;

        if (seen == seer && Is(seen))
        {
            if (Utils.GetPlayerById(Target).IsAlive())
                return "<color=#512513>" + TargetArrow.GetArrows(Player, Target) + "</color>";
            else return "<color=#512513>" + GetArrow.GetArrows(Player, TagePo) + "</color>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (Is(voter) && Winchance && Player.Is(CustomRoles.MassMedia))
        {
            if (votedForId == 253)
            {
                Winchance = false;
                return true;
            }
            if (votedForId == Guees)
            {
                //勝利判定
                Win = true;
                MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, Player.PlayerId);
                return true;
            }
            else
            {
                //違うなら消えてもらおうか。
                MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, Player.PlayerId);
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Misfire, Player.PlayerId);
                Utils.GetPlayerById(votedForId).SetRealKiller(Player);
                Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [MassMedia]　" + string.Format(Translator.GetString("MassMedia.log"), Utils.GetPlayerColor(Player));
                return true;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Win)
        {
            foreach (var crew in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsCrewmate()))
            {
                crew.SetRealKiller(Player);
                crew.RpcMurderPlayer(crew);
                var state = PlayerState.GetByPlayerId(crew.PlayerId);
                state.DeathReason = CustomDeathReason.Misfire;
                state.SetDead();
            }
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.MassMedia);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }
        Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
        Makkura = false;
        Player.SyncSettings();
    }
    public bool CanUseKillButton() => true;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        if (Makkura)
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, OptionShikai.GetFloat());
        }
    }
    public float CalculateKillCooldown() => KillCooldown;

    public bool OverrideKillButton(out string text)
    {
        text = "MassMedia_Kill";
        return true;
    }
}