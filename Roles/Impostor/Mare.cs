using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Mare : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mare),
            player => new Mare(player),
            CustomRoles.Mare,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            5200,
            SetupCustomOption,
            "ma",
            OptionSort: (4, 5),
            assignInfo: new(CustomRoles.Mare, CustomRoleTypes.Impostor)
            {
                IsInitiallyAssignableCallBack = () => ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Electrical, out var systemType) && systemType.TryCast<SwitchSystem>(out _),  // 停電が存在する
            },
            from: From.TownOfHost,
            Desc: () => string.Format(GetString("MareDesc"), OptionKillCooldownInLightsOut.GetFloat(), OptionSpeedInLightsOut.GetFloat(), OptionCanSeeNameColor.GetBool() ? GetString("MareDescNameColor") : ""
            , OptionAllCanKill.GetBool() ? GetString("MareDescCanKill") : GetString("MareDescNonKill"))
        );
    public Mare(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldownInLightsOut = OptionKillCooldownInLightsOut.GetFloat();
        SpeedInLightsOut = OptionSpeedInLightsOut.GetFloat();

        IsActivateKill = false;
        IsAccelerated = false;
    }

    private static OptionItem OptionKillCooldownInLightsOut;
    private static OptionItem OptionSpeedInLightsOut;
    private static OptionItem OptionCanSeeNameColor;
    private static OptionItem OptionAllCanKill;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDarkKilldis;
    enum OptionName
    {
        MareAddSpeedInLightsOut,
        MareKillCooldownInLightsOut,
        MareCanSeeNameColor,
        MareAllCanKill,
        MareDarkKilldistance
    }
    private float KillCooldownInLightsOut;
    private float SpeedInLightsOut;
    private static bool IsActivateKill;
    private bool IsAccelerated;  //加速済みかフラグ

    public static void SetupCustomOption()
    {
        OptionSpeedInLightsOut = FloatOptionItem.Create(RoleInfo, 10, OptionName.MareAddSpeedInLightsOut, new(0.0f, 5.0f, 0.2f), 0.0f, false);
        OptionKillCooldownInLightsOut = FloatOptionItem.Create(RoleInfo, 11, OptionName.MareKillCooldownInLightsOut, new(0f, 180f, 0.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanSeeNameColor = BooleanOptionItem.Create(RoleInfo, 12, OptionName.MareCanSeeNameColor, false, false);
        OptionAllCanKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.MareAllCanKill, false, false);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 40f, false, OptionAllCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDarkKilldis = StringOptionItem.Create(RoleInfo, 15, OptionName.MareDarkKilldistance, EnumHelper.GetAllNames<OverrideKilldistance.KillDistance>(), 0, false);
    }
    public bool CanUseKillButton() => IsActivateKill || OptionAllCanKill.GetBool();
    public float CalculateKillCooldown() => IsActivateKill ? KillCooldownInLightsOut : OptionKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (IsActivateKill && !IsAccelerated)
        { //停電中で加速済みでない場合
            IsAccelerated = true;
            Main.AllPlayerSpeed[Player.PlayerId] += SpeedInLightsOut;//Mareの速度を加算
        }
        else if (!IsActivateKill && IsAccelerated)
        { //停電中ではなく加速済みになっている場合
            IsAccelerated = false;
            Main.AllPlayerSpeed[Player.PlayerId] -= SpeedInLightsOut;//Mareの速度を減算
        }
        if (IsActivateKill)
        {
            AURoleOptions.KillDistance = OptionDarkKilldis.GetInt();
        }
        else if (!IsActivateKill)
        {
            AURoleOptions.KillDistance = Main.NormalOptions.KillDistance;
        }
    }
    private void ActivateKill(bool activate)
    {
        IsActivateKill = activate;
        if (AmongUsClient.Instance.AmHost)
        {
            SendRPC();
            Player.SyncSettings();
            _ = new LateTask(() => Player.SetKillCooldown(delay: true), Main.LagTime, "MareKillCool");
            UtilsNotifyRoles.NotifyRoles();
        }
    }
    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsActivateKill);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        IsActivateKill = reader.ReadBoolean();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask && IsActivateKill)
        {
            if (!Utils.IsActive(SystemTypes.Electrical))
            {
                //停電解除されたらキルモード解除
                ActivateKill(false);
            }
        }
    }
    public override bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return true;

        if (systemType == SystemTypes.Electrical)
        {
            _ = new LateTask(() =>
            {
                //まだ停電が直っていなければキル可能モードに
                if (Utils.IsActive(SystemTypes.Electrical))
                {
                    ActivateKill(true);
                }
            }, OptionCanSeeNameColor.GetBool() ? 0.5f : 4.0f, "Mare Activate Kill");
        }
        return true;
    }
    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
        => OptionCanSeeNameColor.GetBool() && !isMeeting && IsActivateKill && target.Is(CustomRoles.Mare);
}