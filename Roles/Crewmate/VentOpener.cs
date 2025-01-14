using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class VentOpener : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentOpener),
            player => new VentOpener(player),
            CustomRoles.VentOpener,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            23600,
            SetupOptionItem,
            "vo",
            "#fbe000",
            introSound: () => GetIntroSound(RoleTypes.Engineer)
        );
    public VentOpener(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
        currentVent = new();
        count = OptionCount.GetFloat();
        cooldown = OptionCooldown.GetInt();
        Defo = count is 0;
        fuhatu = OptinoFuhatu.GetBool();
        Imp = OptionImp.GetBool();
        Mad = OptionMad.GetBool();
        Crew = OptionCrew.GetBool();
        Neutral = OptionNeutral.GetBool();
        taskc = OptionCanTaskcount.GetFloat();
    }

    private static OptionItem OptionCount;
    private static OptionItem OptionCooldown;
    private static OptionItem OptinoFuhatu;
    private static OptionItem OptionImp;
    private static OptionItem OptionCrew;
    private static OptionItem OptionMad;
    private static OptionItem OptionNeutral;
    private static OptionItem OptionCanTaskcount;
    static int cooldown;
    static bool fuhatu;
    static bool Imp;
    static bool Crew;
    static bool Mad;
    static bool Neutral;
    static bool Defo;
    static float taskc;
    float count;

    static Dictionary<byte, int> currentVent;

    enum OptionName
    {
        Cooldown,
        cantaskcount,
        VentOpenerFuhatu,
        VentOpenerImp,
        VentOpenerMad,
        VentOpenerCrew,
        VentOpenerNeutral,
        VentOpenerCount
    }

    private static void SetupOptionItem()
    {
        OptionCount = FloatOptionItem.Create(RoleInfo, 10, OptionName.VentOpenerCount, new(0, 30, 1), 3, false);
        OptionCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(0, 999, 1), 30, false);
        OptinoFuhatu = BooleanOptionItem.Create(RoleInfo, 12, OptionName.VentOpenerFuhatu, false, false);
        OptionImp = BooleanOptionItem.Create(RoleInfo, 13, OptionName.VentOpenerImp, true, false);
        OptionMad = BooleanOptionItem.Create(RoleInfo, 14, OptionName.VentOpenerMad, true, false);
        OptionCrew = BooleanOptionItem.Create(RoleInfo, 15, OptionName.VentOpenerCrew, true, false);
        OptionNeutral = BooleanOptionItem.Create(RoleInfo, 16, OptionName.VentOpenerNeutral, true, false);
        OptionCanTaskcount = FloatOptionItem.Create(RoleInfo, 17, OptionName.cantaskcount, new(0, 99, 1), 5, false);
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseAbility) return false;
        bool check = false;
        foreach (var (playerid, id) in currentVent)
        {
            var pc = PlayerCatch.GetPlayerById(playerid);
            var role = pc.GetCustomRole();
            if (pc.inVent //ベントに入ってるか ↓設定とかのチェック
                 && ((role.IsImpostor() && Imp)
                 || (role.IsMadmate() && Mad)
                 || (role.IsCrewmate() && Crew)
                 || (role.IsNeutral() && Neutral)))
            {
                pc.MyPhysics?.RpcBootFromVent(id);
                check = true;
            }
        }
        if (check)
        {
            Player.KillFlash(false);
        }
        if ((check || fuhatu) && count > 0)
        {
            count--;
        }
        if (!CanUseAbility)
        {
            Player.MarkDirtySettings();
        }

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        return false;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskCompleted)
            Player.RpcProtectedMurderPlayer();
        return true;
    }

    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Defo ? "" : Utils.ColorString(CanUseAbility ? RoleInfo.RoleColor : IsTaskCompleted ? Color.red : Color.gray, $"({count})");
    public bool CanUseAbility => (Defo || count > 0) && IsTaskCompleted;
    public bool IsTaskCompleted => MyTaskState.HasCompletedEnoughCountOfTasks(taskc);
    public override bool CanClickUseVentButton => CanUseAbility;

    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        currentVent[physics.myPlayer.PlayerId] = ventId;
        return true;
    }
    public override void OnVentilationSystemUpdate(PlayerControl user, VentilationSystem.Operation Operation, int ventId)
    {
        if (Operation == VentilationSystem.Operation.Exit)
            currentVent.Remove(user.PlayerId);
        if (Operation == VentilationSystem.Operation.Move)
            currentVent[user.PlayerId] = ventId;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override string GetAbilityButtonText() => GetString("VentOpenerAbility");
}