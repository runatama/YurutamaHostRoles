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
            11000,
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
    static OptionItem Chien;
    static OptionItem Saiaichien;
    enum Option
    {
        BaitChien, Baitsaidaichien,
        MadBaitRandomReport, MadBaitIgnoreImpostor
    }
    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public static void SetupOptionItem()
    {
        RandomRepo = BooleanOptionItem.Create(RoleInfo, 10, Option.MadBaitRandomReport, true, false);
        ImpRepo = BooleanOptionItem.Create(RoleInfo, 11, Option.MadBaitIgnoreImpostor, false, false, RandomRepo);
        Chien = FloatOptionItem.Create(RoleInfo, 12, Option.BaitChien, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        Saiaichien = FloatOptionItem.Create(RoleInfo, 13, Option.Baitsaidaichien, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);

    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        var tien = 0f;
        if (Saiaichien.GetFloat() != 0)
        {
            int ti = IRandom.Instance.Next(0, (int)Saiaichien.GetFloat() * 10);
            tien = ti * 0.1f;
            Logger.Info($"{tien}sの追加遅延発生!!", "Bait");
        }

        if (target.Is(CustomRoles.MadBait) && !info.IsSuicide && (!killer.GetCustomRole().IsImpostor() || killer.GetCustomRole() == CustomRoles.WolfBoy))
            _ = new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f + Chien.GetFloat() + tien, "MadBait Self Report");
        else if (target.Is(CustomRoles.MadBait) && !info.IsSuicide && RandomRepo.GetBool())
        {
            var nise = PlayerCatch.AllAlivePlayerControls.Where(x => !x.GetCustomRole().IsImpostor() && !x.Is(CustomRoles.WolfBoy)).ToArray();
            if (!ImpRepo.GetBool()) nise = PlayerCatch.AllAlivePlayerControls.ToArray();
            var rand = IRandom.Instance;
            var P = nise[rand.Next(0, nise.Length)];
            _ = new LateTask(() => P.CmdReportDeadBody(target.Data), 0.15f + Chien.GetFloat() + tien, "Bait Self Report");
        }
    }
}