using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Madmate;
public sealed class MadGuardian : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadGuardian),
            player => new MadGuardian(player),
            CustomRoles.MadGuardian,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            10200,
            SetupOptionItem,
            "mg",
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.TownOfHost
        );
    public MadGuardian(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        FieldCanSeeKillFlash = MadmateCanSeeKillFlash.GetBool();
        CanSeeWhoTriedToKill = OptionCanSeeWhoTriedToKill.GetBool();
        MyTaskState.NeedTaskCount = OptionTaskTrigger.GetInt();
    }

    private static OptionItem OptionTaskTrigger;
    private static OptionItem OptionCanSeeWhoTriedToKill;
    public static OverrideTasksData Tasks;
    enum OptionName
    {
        MadGuardianCanSeeWhoTriedToKill
    }
    private static bool FieldCanSeeKillFlash;
    private static bool CanSeeWhoTriedToKill;

    private static void SetupOptionItem()
    {
        OptionCanSeeWhoTriedToKill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.MadGuardianCanSeeWhoTriedToKill, false, false);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.TaskTrigger, new(0, 99, 1), 1, false).SetValueFormat(OptionFormat.Pieces);
        //ID10120~10123を使用
        Tasks = OverrideTasksData.Create(RoleInfo, 20);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        //MadGuardianを切れるかの判定処理
        if (!MyTaskState.HasCompletedEnoughCountOfTasks(OptionTaskTrigger.GetInt())) return true;

        UtilsGameLog.AddGameLog($"MadGuardian", Utils.GetPlayerColor(Player) + ":  " + string.Format(GetString("GuardMaster.Guard"), Utils.GetPlayerColor(killer, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(killer.PlayerId, false)}</b>)"));
        info.CanKill = false;

        killer.SetKillCooldown();

        if (!NameColorManager.TryGetData(killer, target, out var value) || value != RoleInfo.RoleColorCode)
        {
            if (killer.Is(CustomRoles.WolfBoy))
                NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ff1919");
            else
                NameColorManager.Add(killer.PlayerId, target.PlayerId);

            if (CanSeeWhoTriedToKill)
                NameColorManager.Add(target.PlayerId, killer.PlayerId, RoleInfo.RoleColorCode);
            UtilsNotifyRoles.NotifyRoles();
        }

        return false;
    }
    public bool? CheckKillFlash(MurderInfo info) => FieldCanSeeKillFlash;
}