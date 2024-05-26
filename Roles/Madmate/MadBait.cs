using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadBait : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadBait),
            player => new MadBait(player),
            CustomRoles.MadBait,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            10300,
            SetupOptionItem,
            "mb",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadBait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    static OptionItem RandomRepo;
    static OptionItem ImpRepo;
    enum Option
    {
        MBrandomrepo, MBImprepo
    }
    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public static void SetupOptionItem()
    {
        RandomRepo = BooleanOptionItem.Create(RoleInfo, 10, Option.MBrandomrepo, true, false);
        ImpRepo = BooleanOptionItem.Create(RoleInfo, 11, Option.MBImprepo, false, false, RandomRepo);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.MadBait) && !info.IsSuicide && (!killer.GetCustomRole().IsImpostor() || killer.GetCustomRole() == CustomRoles.WolfBoy))
            _ = new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "MadBait Self Report");
        else if (target.Is(CustomRoles.MadBait) && !info.IsSuicide && RandomRepo.GetBool())
        {
            var nise = Main.AllAlivePlayerControls.Where(x => !x.GetCustomRole().IsImpostor() && !x.Is(CustomRoles.WolfBoy)).ToArray();
            if (!ImpRepo.GetBool()) nise = Main.AllAlivePlayerControls.ToArray();
            var rand = IRandom.Instance;
            var P = nise[rand.Next(0, nise.Count())];
            _ = new LateTask(() => P.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
        }
    }
}