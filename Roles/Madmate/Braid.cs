using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Roles.Impostor.Driver;

namespace TownOfHost.Roles.Madmate;
public sealed class Braid : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Braid),
            player => new Braid(player),
            CustomRoles.Braid,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            40100,
            null,
            "br",
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            combination: CombinationRoles.DriverandBraid
        );
    public Braid(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        Bseeing = OptionBseeing.GetBool();
        Dseeing = OptionDseeing.GetBool();
        canVent = OptionCanVent.GetBool();
        TaskFin = false;
        DriverseeKillFlash = false;
        Driverseedeathreason = false;
        DriverseeVote = false;
        Gado = false;
        Guard = true;
        KtaskTrigger = OptionKtaskTrigger.GetInt();
        DtaskTrigger = OptionDtaskTrigger.GetInt();
        GtaskTrigger = OptionGtaskTrigger.GetInt();
        VtaskTrigger = OptionVtaskTrigger.GetInt();
        BraidKillCooldown = OptionBraidKillCooldown.GetFloat();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    public static Options.OverrideTasksData Tasks;
    public static bool TaskFin;
    public static bool DriverseeKillFlash;
    public static bool Driverseedeathreason;
    public static bool DriverseeVote;
    public static bool Gado;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    public static bool Bseeing;
    public static bool Dseeing;
    public static int KtaskTrigger;
    public static int DtaskTrigger;
    public static int GtaskTrigger;
    public static int VtaskTrigger;
    public static bool canVent;
    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.CompletedTasksCount >= KtaskTrigger && OptionDriverseeKillFlash.GetBool())
        {
            DriverseeKillFlash = true;
            Logger.Info("キルフラの能力を付与。", "Braid");
        }
        if (MyTaskState.CompletedTasksCount >= DtaskTrigger && OptionDriverseedeathreason.GetBool())
        {
            Driverseedeathreason = true;
            Logger.Info("死因の能力を付与。", "Braid");
        }
        if (MyTaskState.CompletedTasksCount >= GtaskTrigger && OptionGado.GetBool())
        {
            Gado = true;
            Logger.Info("ガードを付与。", "Braid");
        }
        if (MyTaskState.CompletedTasksCount >= VtaskTrigger && OptionVote.GetBool())
        {
            DriverseeVote = true;
            Logger.Info("匿名投票を解除。", "Braid");
        }
        if (IsTaskFinished)
        {
            TaskFin = true;
            Logger.Info("キルクール軽減。", "Braid");
        }
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seer.Is(CustomRoles.Braid) && seen.Is(CustomRoles.Driver) && Dseeing) return Utils.ColorString(RoleInfo.RoleColor, "☆");
        else return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer.Is(CustomRoles.Driver) && seen.Is(CustomRoles.Braid) && Bseeing) return Utils.ColorString(RoleInfo.RoleColor, "☆");
        else return "";
    }
}