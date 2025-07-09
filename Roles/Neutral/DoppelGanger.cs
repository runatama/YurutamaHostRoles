using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class DoppelGanger : RoleBase, ILNKiller, ISchrodingerCatOwner, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoppelGanger),
            player => new DoppelGanger(player),
            CustomRoles.DoppelGanger,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            13400,
            SetupOptionItem,
            "dg",
            "#47266e",
            (2, 1),
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.DoppelGanger, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            },
            Desc: () => string.Format(GetString("DoppelGangerDesc"), OptionAddWinCount.GetInt(), OptionSoloWinCount.GetInt())
            );
    public DoppelGanger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        Cankill = false;
        Target = byte.MaxValue;
        Afterkill = false;
        SecondsWin = false;
        Seconds = 0;
        Count = 0;
        win = false;
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionShepeCoolDown;
    static OptionItem OptionAddWinCount;
    static OptionItem OptionSoloWinCount;
    static float KillCooldown;
    bool Cankill;
    bool Afterkill;
    bool SecondsWin;
    float Seconds;
    int Count;
    byte Target;
    bool win;
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.DoppelGanger;

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShepeCoolDown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionAddWinCount = FloatOptionItem.Create(RoleInfo, 13, "DoppelGangerWinCount", new(0f, 300f, 1f), 45f, false);
        OptionSoloWinCount = FloatOptionItem.Create(RoleInfo, 14, "DoppelGangerWin", new(0f, 300f, 1f), 70f, false);
        RoleAddAddons.Create(RoleInfo, 15);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public void ApplySchrodingerCatOptions(IGameOptions option)
    {
        option.SetVision(false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShepeCoolDown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 0f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (Is(target))
        {
            animate = false;
            return false;
        }
        Cankill = true;
        Target = target.PlayerId;
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 1f, "DoppelSetNotify", true);
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Player.RpcShapeshift(Player, false);
        Cankill = false;
        Target = byte.MaxValue;
        Afterkill = false;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;

        if (Target == byte.MaxValue || target.PlayerId != Target || !Cankill || Afterkill)
        {
            info.DoKill = false;
            return;
        }
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (Target == byte.MaxValue || target.PlayerId != Target || !Cankill || Afterkill)
            return;

        if (info.CanKill && info.DoKill) Afterkill = true;
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (GameLog)
        {
            return Utils.ColorString(Palette.Purple, $"({Count})");
        }
        return "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen || seen.PlayerId == Target)
        {
            var AddDenominator = OptionAddWinCount.GetFloat();
            var SoloWinDenominator = OptionSoloWinCount.GetFloat();
            if (!Player.IsAlive()) return "";
            if (SecondsWin) return Utils.ColorString(Palette.Purple.ShadeColor(-0.5f), $"({Count}/{SoloWinDenominator}) {Utils.AdditionalWinnerMark}");
            else if (Target != byte.MaxValue)
                return Utils.ColorString(Palette.Purple.ShadeColor(-0.3f), $"({Count}/{AddDenominator})");
            else
                return Utils.ColorString(Palette.Purple.ShadeColor(-0.1f), $"({Count}/{AddDenominator})");
        }
        return "";
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;
        var UseingShape = false;
        if (Afterkill)
        {
            UseingShape = true;
            Seconds += Time.fixedDeltaTime * 0.9f;
        }
        if (Target != byte.MaxValue)
        {
            UseingShape = true;
            Seconds += Time.fixedDeltaTime * 0.1f;
        }

        if (UseingShape is false) return;

        if (OptionAddWinCount.GetFloat() <= Seconds) SecondsWin = true;
        if (OptionSoloWinCount.GetFloat() <= Seconds)
        {
            win = true;
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.DoppelGanger, Player.PlayerId, false))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }
            Cankill = false;
            Target = byte.MaxValue;
            Afterkill = false;
            return;
        }
        if (Count != (int)Seconds)
        {
            Count = (int)Seconds;
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
    }

    public bool CheckWin(ref CustomRoles winnerRole) => Player.IsAlive() && SecondsWin && !win;
}