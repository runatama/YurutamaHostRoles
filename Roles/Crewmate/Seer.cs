using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;
public sealed class Seer : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20500,
            SetupOptionItem,
            "se",
            "#61b26c",
            from: From.TheOtherRoles
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanseeComms = OptioACanSeeComms.GetBool();
        DelayMode = OptionDelay.GetBool();
        Maxdelay = OptionMaxdelay.GetFloat();
        MinDelay = OptionMindelay.GetFloat();
    }
    private static bool CanseeComms;
    private static OptionItem OptioACanSeeComms;
    static OptionItem OptionDelay; static bool DelayMode;
    static OptionItem OptionMindelay; static float MinDelay;
    static OptionItem OptionMaxdelay; static float Maxdelay;
    enum OptionName
    {
        CanseeComms,
        BaitChien,
        Baitsaidaichien,
        SeerDelay
    }
    private static void SetupOptionItem()
    {
        OptioACanSeeComms = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanseeComms, true, false);
        OptionDelay = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SeerDelay, false, false);
        OptionMindelay = FloatOptionItem.Create(RoleInfo, 12, OptionName.BaitChien, new(0, 60, 0.5f), 3f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
        OptionMaxdelay = FloatOptionItem.Create(RoleInfo, 13, OptionName.Baitsaidaichien, new(0, 60, 0.5f), 3f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
    }
    public bool? CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        var canseekillflash = !Utils.IsActive(SystemTypes.Comms) || CanseeComms;

        if (DelayMode && canseekillflash)
        {
            var tien = 0f;
            //小数対応
            if (Maxdelay > 0)
            {
                int ti = IRandom.Instance.Next(0, (int)Maxdelay * 10);
                tien = ti * 0.1f;
                Logger.Info($"{Player?.Data?.PlayerName} => {tien}sの追加遅延発生!!", "Seer");
            }
            _ = new LateTask(() =>
            {
                if (GameStates.Meeting || !Player.IsAlive())
                {
                    Logger.Info($"{info?.AppearanceTarget?.Data?.PlayerName ?? "???"}のフラッシュを受け取ろうとしたけどなんかし防いだぜ", "seer");
                    return;
                }
                Player.KillFlash();
            }, tien + MinDelay, "SeerDelayKillFlash", null);
            return null;
        }
        return canseekillflash;
    }
}