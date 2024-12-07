using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Stolener : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Stolener),
            player => new Stolener(player),
            CustomRoles.Stolener,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21700,
            SetupOptionItem,
            "slt",
            "#605eb7",
            false
        );
    public Stolener(PlayerControl player)
    : base(
        RoleInfo,
        player)
    {
        Killer = byte.MaxValue;
        CanUseaddon = OptionCanUseaddon.GetBool();
        CanUseAddonfinish = OptionCanUseaddonOnfinish.GetBool();
    }
    enum OptionName
    {
        StolenerCanuseaddon,
        StolenerCanUseaddonOnfinish,
    }
    static OptionItem OptionCanUseaddon; static bool CanUseaddon;
    static OptionItem OptionCanUseaddonOnfinish; static bool CanUseAddonfinish;
    //自身がアドオンを使えるか
    public bool ICanUseaddon => CanUseaddon && (!CanUseAddonfinish || MyTaskState.IsTaskFinished);
    byte Killer;//処理キャンセル用のキラー関数
    public static List<byte> Killers = new();//付与する奴
    static void SetupOptionItem()
    {
        OptionCanUseaddon = BooleanOptionItem.Create(RoleInfo, 10, OptionName.StolenerCanuseaddon, true, false);
        OptionCanUseaddonOnfinish = BooleanOptionItem.Create(RoleInfo, 11, OptionName.StolenerCanUseaddonOnfinish, true, false, OptionCanUseaddon);
        RoleAddAddons.Create(RoleInfo, 20, DefaaultOn: true);
    }
    ///マジシャンとかのキルでも受け渡したい
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || player.IsAlive() || Killer != byte.MaxValue) return;
        var kier = player?.GetRealKiller();

        Killer = kier?.PlayerId ?? (byte.MaxValue - 1);
        Killers.Add(Killer);
        Logger.Info($"キラー設定：{kier?.Data?.name ?? "無し"}", "Stolener");
        kier?.SetKillCooldown(kyousei: true);
        UtilsNotifyRoles.NotifyRoles();

        if (kier is not null)
            if (RoleAddAddons.GetRoleAddon(CustomRoles.Stolener, out var d, null, subrole: [CustomRoles.Guarding, CustomRoles.Speeding]) && d.GiveAddons.GetBool())
            {
                if (d.GiveGuarding.GetBool()) Main.Guard[kier.PlayerId] += d.Guard.GetInt();
                if (d.GiveSpeeding.GetBool() && !kier.Is(CustomRoles.UltraStar)) Main.AllPlayerSpeed[kier.PlayerId] = d.Speed.GetFloat();
            }
        UtilsOption.SyncAllSettings();
    }
}