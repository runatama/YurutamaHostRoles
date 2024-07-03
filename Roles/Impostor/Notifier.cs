using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Notifier : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Notifier),
            player => new Notifier(player),
            CustomRoles.Notifier,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4000,
            SetupOptionItems,
            "nt"
        );
    public Notifier(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        NotifierProbability = OptionNotifierProbability.GetInt();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionNotifierProbability;
    enum OptionName
    {
        NotifierProbability,
    }
    private static int NotifierProbability;
    private static float KillCooldown;
    private static void SetupOptionItems()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionNotifierProbability = FloatOptionItem.Create(RoleInfo, 11, OptionName.NotifierProbability, new(0, 100, 5), 50, false)
            .SetValueFormat(OptionFormat.Percent);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            int chance = IRandom.Instance.Next(1, 101);
            if (chance <= NotifierProbability)
            {
                Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知", "Notifier");
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    player.KillFlash();
                }
            }
            else
            {
                Logger.Info($"{killer?.Data?.PlayerName}: フラ通知は無し", "Notifier");
            }
        }
    }
}