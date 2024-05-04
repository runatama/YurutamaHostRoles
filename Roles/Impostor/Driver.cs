using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Madmate;

namespace TownOfHost.Roles.Impostor;
public sealed class Driver : RoleBase, IImpostor, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Driver),
            player => new Driver(player),
            CustomRoles.Driver,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            15000,
            SetupOptionItems,
            "dr",
            tab: TabGroup.Combinations,
            assignInfo: new RoleAssignInfo(CustomRoles.Driver, CustomRoleTypes.Impostor)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Driver, CustomRoles.Braid }
            },
            combination: CombinationRoles.DriverandBraid
        );
    public Driver(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
    }
    public static OptionItem OptionBraidKillCooldown;
    public static OptionItem OptionKillCooldown;
    public static OptionItem OptionDriverseeKillFlash;
    public static OptionItem OptionKtaskTrigger;
    public static OptionItem OptionDriverseedeathreason;
    public static OptionItem OptionDtaskTrigger;
    public static OptionItem OptionGado;
    public static OptionItem OptionGtaskTrigger;
    public static OptionItem OptionVote;
    public static OptionItem OptionVtaskTrigger;
    public static OptionItem OptionBseeing;
    public static OptionItem OptionDseeing;
    public static OptionItem OptionCanVent;
    enum OptionName
    {
        KillCooldown,
        BraidKillCooldown,
        DriverseeKillFlash,
        Driverseedeathreason,
        Gado,
        Vote, TaskTrigger,
        seen, Dseeing,
        BCanVent
    }
    public static float BraidKillCooldown;
    public static float KillCooldown;
    public static bool Guard;
    public static void SetupOptionItems()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, OptionName.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionBraidKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.BraidKillCooldown, new(0f, 180f, 2.5f), 15f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionDriverseeKillFlash = BooleanOptionItem.Create(RoleInfo, 11, OptionName.DriverseeKillFlash, false, false);
        OptionKtaskTrigger = FloatOptionItem.Create(RoleInfo, 12, OptionName.TaskTrigger, new(1, 297, 1), 5, false, OptionDriverseeKillFlash);
        OptionDriverseedeathreason = BooleanOptionItem.Create(RoleInfo, 13, OptionName.Driverseedeathreason, false, false);
        OptionDtaskTrigger = FloatOptionItem.Create(RoleInfo, 14, OptionName.TaskTrigger, new(1, 297, 1), 5, false, OptionDriverseedeathreason);
        OptionVote = BooleanOptionItem.Create(RoleInfo, 15, OptionName.Vote, false, false);
        OptionVtaskTrigger = FloatOptionItem.Create(RoleInfo, 16, OptionName.TaskTrigger, new(1, 297, 1), 5, false, OptionVote);
        OptionGado = BooleanOptionItem.Create(RoleInfo, 17, OptionName.Gado, false, false);
        OptionGtaskTrigger = FloatOptionItem.Create(RoleInfo, 18, OptionName.TaskTrigger, new(1, 297, 1), 5, false, OptionGado);
        OptionBseeing = BooleanOptionItem.Create(RoleInfo, 19, OptionName.seen, false, false);
        OptionDseeing = BooleanOptionItem.Create(RoleInfo, 21, OptionName.Dseeing, false, false);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 22, OptionName.BCanVent, true, false);
        Braid.Tasks = Options.OverrideTasksData.Create(RoleInfo, 50, CustomRoles.Braid);
    }

    public bool CheckKillFlash(MurderInfo info) => Braid.DriverseeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => Braid.Driverseedeathreason;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        //匿名投票
        opt.SetBool(BoolOptionNames.AnonymousVotes, !Braid.DriverseeVote);
    }
    public float CalculateKillCooldown() => Braid.TaskFin ? BraidKillCooldown : KillCooldown;
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (Braid.Gado && Guard)
        {
            Guard = false;
            (var killer, var target) = info.AttemptTuple;
            // 直接キル出来る役職チェック

            if (Player.IsAlive())

                if (!NameColorManager.TryGetData(killer, target, out var value) || value != RoleInfo.RoleColorCode)
                {
                    NameColorManager.Add(killer.PlayerId, target.PlayerId);
                    Utils.NotifyRoles();
                }
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(target);
            info.CanKill = false;
            Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Driver]　" + Utils.GetPlayerColor(Player) + ":  " + string.Format(Translator.GetString("GuardMaster.Guard"), Utils.GetPlayerColor(killer, true) + $"(<b>{Utils.GetTrueRoleName(killer.PlayerId, false)}</b>)");
            Logger.Info($"{target.GetNameWithRole()} : ガード残り{Guard}回", "GuardMaster");
            Utils.NotifyRoles();
        }
        return true;
    }
    public override void OnStartMeeting()
    {
        if (Player.IsAlive())
        {
            if (Braid.DriverseeKillFlash)
            {
                Utils.SendMessage("ブレイドからキルフラの能力を習得しました。", Player.PlayerId);
            }
            if (Braid.Driverseedeathreason)
            {
                Utils.SendMessage("ブレイドから死因の能力を習得しました。", Player.PlayerId);
            }
            if (Braid.Gado)
            {
                Utils.SendMessage("ブレイドからガードの能力を習得しました。", Player.PlayerId);
            }
            if (Braid.DriverseeVote)
            {
                Utils.SendMessage("ブレイドから匿名投票解除しました。", Player.PlayerId);
            }
            if (Braid.TaskFin)
            {
                Utils.SendMessage("ブレイドからキルクール減少能力を習得しました。", Player.PlayerId);
            }
            //チャットに装飾は夜藍に任せますwww
            //文章はりぃりぃに任せます
        }
    }
}