using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Reloader : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Reloader),
            player => new Reloader(player),
            CustomRoles.Reloader,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            5500,
            SetupOptionItem,
            "rd",
            from: From.RevolutionaryHostRoles
        );
    public Reloader(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
        KillCooldown = OptionKillCooldown.GetFloat();
        RKillCooldown = OptionRKillCooldown.GetFloat();
        Count = OptionCount.GetInt();
    }
    private static OptionItem OptionCooldown;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionRKillCooldown;
    private static OptionItem OptionCount;
    enum OptionName
    {
        ReloaderKillCooldown,
        ReloaderCount
    }
    private static float Cooldown;
    private static float KillCooldown;
    private static float RKillCooldown;
    private static int Count;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionRKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ReloaderKillCooldown, new(0f, 180f, 0.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCount = FloatOptionItem.Create(RoleInfo, 12, OptionName.ReloaderCount, new(1, 15, 1), 2, false);
    }
    public bool UseOneclickButton => true;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = Cooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override bool CanUseAbilityButton() => Count > 0;
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        resetkillcooldown = false;
        fall = false;
        if (Count <= 0) return;

        resetkillcooldown = true;
        Count--;
        Player.SetKillCooldown(RKillCooldown);
        Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
        Player.SyncSettings();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(Count > 0 ? RoleInfo.RoleColor : Palette.DisabledGrey, $"({Count})");

    public override string GetAbilityButtonText()
    {
        return GetString("ReloaderAbilitytext");
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Reloader_Ability";
        return true;
    }
}