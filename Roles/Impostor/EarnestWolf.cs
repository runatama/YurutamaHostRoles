
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor;
public sealed class EarnestWolf : RoleBase, IImpostor, IUseTheShButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EarnestWolf),
            player => new EarnestWolf(player),
            CustomRoles.EarnestWolf,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            27000,
            SetupOptionItem,
            "EW"
        );
    public EarnestWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCoolDown = OptionKillCoolDown.GetFloat();
        count = 0;
        OverKillMode = false;
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionOverKillCanCount;
    static OptionItem OptionOverKillBairitu;
    static OptionItem OptionNomalKillDistance;
    static OptionItem OptionOverKillDistance;
    float KillCoolDown;
    int count;
    bool OverKillMode;
    public bool CanBeLastImpostor { get; } = false;
    enum OptionName
    {
        EarnestWolfOverKillCount,
        EarnestWolfOverBairitu,
        EarnestWolfNomalKllDistance,
        EarnestWolfOverKillDistance
    }

    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 20f, false).SetValueFormat(OptionFormat.Seconds);
        OptionOverKillCanCount = FloatOptionItem.Create(RoleInfo, 11, OptionName.EarnestWolfOverKillCount, new(0f, 15f, 1f), 2f, false).SetValueFormat(OptionFormat.Times);
        OptionOverKillBairitu = FloatOptionItem.Create(RoleInfo, 12, OptionName.EarnestWolfOverBairitu, new(1f, 10f, 0.1f), 2f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionNomalKillDistance = StringOptionItem.Create(RoleInfo, 13, OptionName.EarnestWolfNomalKllDistance, EnumHelper.GetAllNames<OverrideKilldistance.KillDistance>(), 0, false);
        OptionOverKillDistance = StringOptionItem.Create(RoleInfo, 14, OptionName.EarnestWolfOverKillDistance, EnumHelper.GetAllNames<OverrideKilldistance.KillDistance>(), 0, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.KillDistance = OverKillMode ? OptionOverKillDistance.GetInt() : OptionNomalKillDistance.GetInt();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        //falseだったとしても知らないねっ!
        if (OverKillMode)
        {
            count++;
            KillCoolDown = KillCoolDown * OptionOverKillBairitu.GetFloat();
            info.DoKill = false;

            Player.KillFlash();
            CustomRoleManager.OnCheckMurder(killer, target, killer, target, true, null);
            OverKillMode = false;

            _ = new LateTask(() =>
            {
                Player.SetKillCooldown(delay: true);
                Player.SyncSettings();
            }, 0.2f, "EarnestWolf");
        }
    }
    public override string GetProgressText(bool comms = false)
    {
        var c = OptionOverKillCanCount.GetInt() - count;
        return c <= 0 ? Utils.ColorString(Palette.DisabledGrey, $"{c}") : Utils.ColorString(Palette.ImpostorRed, $"({c})");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen && !isForMeeting) return OverKillMode ? "<color=#ff1919>◎</color>" : "";
        return "";
    }
    public void OnClick()
    {
        if (!Player.IsAlive()) return;
        if (count >= OptionOverKillCanCount.GetFloat()) return;
        OverKillMode = !OverKillMode;
        _ = new LateTask(() =>
        {
            Utils.NotifyRoles(SpecifySeer: Player);
            Player.SyncSettings();
        }, 0.2f, "EarnestWolf OnClick");
    }
    public float CalculateKillCooldown() => KillCoolDown;
}