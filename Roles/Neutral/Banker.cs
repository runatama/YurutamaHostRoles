using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;


namespace TownOfHost.Roles.Neutral;
public sealed class Banker : RoleBase, IKiller, IAdditionalWinner
{
    //Memo
    //バランスがいまいちわからないので要調整
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Banker),
            player => new Banker(player),
            CustomRoles.Banker,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            42000,
            SetUpOptionItem,
            "bu",
            "#489972",
            true,
            introSound: () => GetIntroSound(RoleTypes.Tracker)
        );
    public Banker(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        TaskMode = true;
        Coin = FarstCoin.GetInt();
        Die = false;
    }
    static OptionItem FarstCoin;
    static OptionItem KillCoolDown;
    static OptionItem KillAddCoin;
    static OptionItem TaskAddCoin;
    static OptionItem AddWinCoin;
    static OptionItem ChengeCoin;
    static OptionItem TarnRemoveCoin;
    static OptionItem DieCanWin;
    static OptionItem DieRemoveCoin;
    static OptionItem DieRemoveTarn;
    enum Option
    {
        BankerFarstCoin,
        BankerTaskAddCoin,
        BankerKillAddCoin,
        BankerChengeCoin,
        BankerTranRemoveCoin,
        BankerDieCanWin,
        BankerDieRemoveCoin,
        BankerDieRemoveTarn,
        BankerWincoin,
    }
    bool TaskMode;
    int Coin;
    bool Die;
    static void SetUpOptionItem()
    {
        FarstCoin = IntegerOptionItem.Create(RoleInfo, 9, Option.BankerFarstCoin, new(1, 100, 1), 5, false);
        TaskAddCoin = IntegerOptionItem.Create(RoleInfo, 10, Option.BankerTaskAddCoin, new(1, 100, 1), 5, false);
        KillAddCoin = IntegerOptionItem.Create(RoleInfo, 11, Option.BankerKillAddCoin, new(1, 100, 1), 15, false);
        ChengeCoin = IntegerOptionItem.Create(RoleInfo, 12, Option.BankerChengeCoin, new(0, 100, 1), 5, false);
        TarnRemoveCoin = IntegerOptionItem.Create(RoleInfo, 13, Option.BankerTranRemoveCoin, new(0, 100, 1), 3, false);
        AddWinCoin = IntegerOptionItem.Create(RoleInfo, 14, Option.BankerWincoin, new(1, 100, 1), 60, false);
        DieCanWin = BooleanOptionItem.Create(RoleInfo, 15, Option.BankerDieCanWin, true, false);
        DieRemoveCoin = IntegerOptionItem.Create(RoleInfo, 16, Option.BankerDieRemoveCoin, new(1, 100, 1), 30, false, DieCanWin);
        DieRemoveTarn = IntegerOptionItem.Create(RoleInfo, 17, Option.BankerDieRemoveTarn, new(1, 100, 1), 10, false, DieCanWin);
        KillCoolDown = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 15f, false);
        Options.OverrideTasksData.Create(RoleInfo, 19);
        RoleAddAddons.Create(RoleInfo, 23);
    }
    public bool CanUseSabotageButton() => false;
    public bool CanUseKillButton() => !TaskMode;
    public float CalculateKillCooldown() => KillCoolDown.GetFloat();
    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override bool CanUseAbilityButton() => Player != PlayerControl.LocalPlayer;
    public override bool OnCompleteTask()
    {
        if (!Player.IsAlive()) return true;

        Coin += TaskAddCoin.GetInt();
        return true;
    }
    public override string GetProgressText(bool comms = false)
    => Player.IsAlive() || DieCanWin.GetBool() ? ((TaskMode ? "[Task]" : "[Kill]") + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Banker), $"({Coin})")) : "";
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Player.IsAlive() || DieCanWin.GetBool())
            if (seen && seer)
            {
                if (AddWinCoin.GetInt() <= Coin) return Utils.AdditionalWinnerMark;
            }
        return "";
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0.5f;
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (Is(killer))
        {
            Coin += KillAddCoin.GetInt();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Die)
        {
            if (!player.IsAlive())
            {
                Die = true;
                Coin -= DieRemoveCoin.GetInt();
                _ = new LateTask(() => Utils.NotifyRoles(), Main.LagTime, "Bankerdie");
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;

        if (Player.IsAlive())
            Coin -= TarnRemoveCoin.GetInt();
        else Coin -= DieRemoveTarn.GetInt();
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId, ref bool nouryoku)
    {
        nouryoku = true;
        if (Coin >= ChengeCoin.GetInt())
        {
            if (TaskMode && Utils.IsActive(SystemTypes.Comms)) return false;//Hostはタスクモード(エンジ)での切り替えできるからさせないようにする

            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc == PlayerControl.LocalPlayer)
                        Player.StartCoroutine(Player.CoSetRole(RoleTypes.Engineer, true));
                    else
                        Player.RpcSetRoleDesync(pc == Player && TaskMode ? RoleTypes.Impostor : RoleTypes.Engineer, pc.GetClientId());
                }
                TaskMode = !TaskMode;
            }
            Coin -= ChengeCoin.GetInt();
            _ = new LateTask(() =>
            {
                Player.SetKillCooldown();
                Utils.NotifyRoles();
            }, Main.LagTime, "Bankerchenge");
        }
        return false;
    }
    public override bool CanTask()
    {
        if (!Player.IsAlive()) return false;
        return TaskMode;
    }

    public bool CheckWin(ref CustomRoles winnerRole)
        => AddWinCoin.GetInt() <= Coin && (Player.IsAlive() || DieCanWin.GetBool());
}