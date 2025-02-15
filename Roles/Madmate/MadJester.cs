using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadJester : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadJester),
            player => new MadJester(player),
            CustomRoles.MadJester,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            13100,
            SetupOptionItem,
            "mje",
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.au_libhalt_net
        );
    public MadJester(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }
    private static OptionItem OptionCanVent;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;

    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles GetFtResults(PlayerControl player) => Options.MadTellOpt();

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        Options.OverrideTasksData.Create(RoleInfo, 11);
    }

    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;
        if (!IsTaskFinished) return;
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
        DecidedWinner = true;

    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished)
        {
            Player.MarkDirtySettings();
        }

        return true;
    }
}