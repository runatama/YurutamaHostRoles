using System.Collections.Generic;
using System.Linq;

using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class MassMedia : RoleBase, IKiller, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MassMedia),
            player => new MassMedia(player),
            CustomRoles.MassMedia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            14500,
            (5, 1),
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
    public bool CanKill { get; private set; } = false;
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionShikai;
    static OptionItem OptionMeetingReset;
    static OptionItem OptionCanSeeKillflash;
    static OptionItem OptionCriminalprofile;
    List<byte> SesshokuPlayer;
    bool Search;
    static bool MeetingReset;
    static float KillCooldown;
    static bool Canseekillflash;
    public byte Target;
    byte Guees;
    bool Makkura;
    bool Winchance;
    bool Win;
    Vector3 TagePo;
    public static HashSet<MassMedia> MassMedias = new();
    enum Option
    {
        MassMediaShikai,
        MassMediaMeetingReset,
        MassMediaCanSeeKillflash,
        MassMediaCriminalprofile
    }
    public override void Add()
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        MeetingReset = OptionMeetingReset.GetBool();
        Canseekillflash = OptionCanSeeKillflash.GetBool();
        Target = byte.MaxValue;
        Makkura = false;
        TagePo = new Vector3(999f, 999f);
        Winchance = false;
        Win = false;
        Guees = byte.MaxValue;
        SesshokuPlayer = new();
        Search = false;

        MassMedias.Add(this);
    }
    public override void OnDestroy() => MassMedias.Clear();
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionShikai = FloatOptionItem.Create(RoleInfo, 11, Option.MassMediaShikai, new(0f, 0.20f, 0.02f), 0.04f, false)
                .SetValueFormat(OptionFormat.Multiplier);
        OptionMeetingReset = BooleanOptionItem.Create(RoleInfo, 12, Option.MassMediaMeetingReset, false, false);
        OptionCanSeeKillflash = BooleanOptionItem.Create(RoleInfo, 13, Option.MassMediaCanSeeKillflash, false, false);
        OptionCriminalprofile = BooleanOptionItem.Create(RoleInfo, 14, Option.MassMediaCriminalprofile, false, false);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;

        var tg = PlayerCatch.GetPlayerById(Target);
        if (tg == null) return;

        //範囲
        Vector2 GSpos = player.transform.position;
        var Mieruhani = 6.5f * Main.DefaultCrewmateVision;
        if (player.Is(CustomRoles.Lighting)) Mieruhani = 6.5f * Main.DefaultImpostorVision;

        //position
        float HitoDistance = Vector2.Distance(GSpos, tg.transform.position);
        if (!tg.IsAlive())
        {
            HitoDistance = Vector2.Distance(GSpos, TagePo);
            Mieruhani *= 0.4f;
        }

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

        if (OptionCriminalprofile.GetBool() && tg.IsAlive())
        {
            foreach (var otherpc in PlayerCatch.AllAlivePlayerControls.Where(pc => pc.PlayerId != Target && pc.PlayerId != Player.PlayerId))
            {
                if (SesshokuPlayer.Contains(otherpc.PlayerId)) continue;
                float distance = Vector2.Distance(tg.transform.position, otherpc.GetTruePosition());
                if (distance <= 4.5f && Search)
                {
                    SesshokuPlayer.Add(otherpc.PlayerId);
                }
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
                info.DoKill = false;
                return;
            }
            Target = target.PlayerId;
            info.DoKill = false;
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
        if (MeetingReset || !PlayerCatch.GetPlayerById(Target).IsAlive())
        {
            TargetArrow.Remove(Player.PlayerId, Target);
            GetArrow.Remove(Player.PlayerId, TagePo);
            TagePo = new Vector3(999f, 999f);
            Target = byte.MaxValue;
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting && SesshokuPlayer.Contains(seen.PlayerId))
        {
            return "<#512513>〇</color>";
        }

        if (Target == byte.MaxValue) return "";

        if (seen == seer && Is(seen))
        {
            if (PlayerCatch.GetPlayerById(Target).IsAlive())
                return "<color=#512513>" + TargetArrow.GetArrows(Player, Target) + "</color>";
            else return "<color=#512513>" + GetArrow.GetArrows(Player, TagePo) + "</color>";
        }
        if (seen.PlayerId == Target) return "<color=#512513>★</color>";

        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!seer.IsAlive() ||
            !Winchance ||
            !isForMeeting ||
            Target == byte.MaxValue
        ) return "";

        return GetString("MassMediaChance");
    }
    public bool? CheckKillFlash(MurderInfo info) => info.AppearanceTarget.PlayerId == Target && Canseekillflash;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!SelfVoteManager.Canuseability()) return true;
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
                PlayerCatch.GetPlayerById(votedForId).SetRealKiller(Player);

                UtilsGameLog.AddGameLog($"MassMedia", string.Format(GetString("MassMedia.log"), Utils.GetPlayerColor(Player)));
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
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MassMedia, Player.PlayerId, true))
            {
                foreach (var crew in PlayerCatch.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsCrewmate()))
                {
                    crew.SetRealKiller(Player);
                    crew.RpcMurderPlayer(crew);
                    var state = PlayerState.GetByPlayerId(crew.PlayerId);
                    state.DeathReason = CustomDeathReason.Misfire;
                    state.SetDead();
                }
            }
        }
        Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
        Makkura = false;
        SesshokuPlayer = new();
        Player.SyncSettings();
        _ = new LateTask(() => Search = true, 10, "MassMediaSearch");
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