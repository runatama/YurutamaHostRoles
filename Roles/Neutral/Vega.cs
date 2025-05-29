using System;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;
public sealed class Vega : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vega),
            player => new Vega(player),
            CustomRoles.Vega,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            40115,
            SetupOptionItems,
            "vg",
            "#f1d9d9",
            true,
            tab: TabGroup.Combinations,
            countType: CountTypes.MilkyWay,
            assignInfo: new RoleAssignInfo(CustomRoles.Vega, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = [CustomRoles.Vega, CustomRoles.Altair]
            },
            combination: CombinationRoles.VegaandAltair
        );
    public Vega(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        RendezvousCooldown = OptionRendezvousCooldown.GetFloat();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        VegaCanUseVent = OptionVegaCanUseVent.GetBool();
        AltairCanUseVent = OptionAltairCanUseVent.GetBool();
        BuffThreshold = OptionBuffThreshold.GetInt();
        KillCooldownThreshold = OptionKillCooldownThreshold.GetInt();
        KillCooldownAmount = OptionKillCooldownAmount.GetFloat();
        MinimumKillCooldown = OptionMinimumKillCooldown.GetFloat();
        RevealKillableFactions = OptionRevealKillableFactions.GetBool();
        RKFThreshold = OptionRKFThreshold.GetInt();
        FactionBasedStarColor = OptionFactionBasedStarColor.GetBool();

        RendezvousCount = 0;
        Rendezvoused = false;
        CanSeeKiller = false;
        SetAltair();
    }

    public static OptionItem OptionKillCooldown;
    private static OptionItem OptionRendezvousCooldown;
    public static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionVegaCanUseVent;
    private static OptionItem OptionAltairCanUseVent;
    private static OptionItem OptionBuffThreshold;
    private static OptionItem OptionKillCooldownThreshold;
    private static OptionItem OptionKillCooldownAmount;
    private static OptionItem OptionMinimumKillCooldown;
    private static OptionItem OptionRevealKillableFactions;
    private static OptionItem OptionRKFThreshold;
    private static OptionItem OptionFactionBasedStarColor;

    public static float KillCooldown;
    private static float RendezvousCooldown;
    public static bool HasImpostorVision;
    private static bool VegaCanUseVent;
    public static bool AltairCanUseVent;
    private static int BuffThreshold;
    private static int KillCooldownThreshold;
    private static float KillCooldownAmount;
    private static float MinimumKillCooldown;
    private static bool RevealKillableFactions;
    private static int RKFThreshold;
    public static bool FactionBasedStarColor;
    private int RendezvousCount;
    private bool Rendezvoused;
    public bool CanSeeKiller;
    private PlayerControl Altair;

    public static readonly string TeamColor = "#191970";
    public static string TeamText => $"<color={TeamColor}>{GetString(CountTypes.MilkyWay.ToString())}</color>";

    enum OptionName
    {
        VegaRendezvousCooldown,
        VegaCanUseVent,
        VegaAltairCanUseVent,
        VegaBuffThreshold,
        VegaKillCooldownThreshold,
        VegaKillCooldownAmount,
        VegaMinimumKillCooldown,
        VegaRevealKillableFactions,
        VegaRKFThreshold,
        VegaFactionBasedStarColor,
    }

    private static void SetupOptionItems()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 0);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionRendezvousCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.VegaRendezvousCooldown, new(0f, 180f, 0.5f), 15, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.ImpostorVision, true, false);
        OptionVegaCanUseVent = BooleanOptionItem.Create(RoleInfo, 13, OptionName.VegaCanUseVent, true, false);
        OptionAltairCanUseVent = BooleanOptionItem.Create(RoleInfo, 14, OptionName.VegaAltairCanUseVent, true, false);
        OptionBuffThreshold = IntegerOptionItem.Create(RoleInfo, 15, OptionName.VegaBuffThreshold, new(1, 99, 1), 3, false)
                .SetValueFormat(OptionFormat.Times);
        OptionKillCooldownThreshold = IntegerOptionItem.Create(RoleInfo, 16, OptionName.VegaKillCooldownThreshold, new(1, 99, 1), 6, false)
                .SetValueFormat(OptionFormat.Times);
        OptionKillCooldownAmount = FloatOptionItem.Create(RoleInfo, 17, OptionName.VegaKillCooldownAmount, new(1f, 180f, 0.5f), 5f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionMinimumKillCooldown = FloatOptionItem.Create(RoleInfo, 18, OptionName.VegaMinimumKillCooldown, new(0.5f, 180f, 0.5f), 5f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionRevealKillableFactions = BooleanOptionItem.Create(RoleInfo, 19, OptionName.VegaRevealKillableFactions, false, false);
        OptionRKFThreshold = IntegerOptionItem.Create(RoleInfo, 20, OptionName.VegaRKFThreshold, new(1, 99, 1), 8, false, OptionRevealKillableFactions)
                .SetValueFormat(OptionFormat.Times);
        OptionFactionBasedStarColor = BooleanOptionItem.Create(RoleInfo, 21, OptionName.VegaFactionBasedStarColor, true, false, OptionRevealKillableFactions);
    }

    public float CalculateKillCooldown() => RendezvousCooldown;
    public bool CanUseImpostorVentButton() => VegaCanUseVent;
    public bool CanUseSabotageButton() => false;
    public bool CanUseKillButton() => !Rendezvoused;
    public bool CanKill { get; private set; } = false;

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);

    public override void StartGameTasks() => SetAltair();
    public override void AfterMeetingTasks() => CheckAlive();

    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner) => CheckAlive(exiled?.Object);

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;

        //なにしようとしてるの?だめだよ?
        if (info.AttemptTarget != Altair) return;

        //既にそのターンにしているならブロック
        if (Rendezvoused) return;
        RpcRendezvous();
    }

    private void Rendezvous()
    {
        RendezvousCount++;
        Rendezvoused = true;

        if (BuffThreshold <= RendezvousCount)
        {
            //バフを与えましょう
            GiveBuff();
        }
        if (KillCooldownThreshold <= RendezvousCount)
        {
            //キルクールを下げてあげましょう
            KillCooldown -= KillCooldownAmount;
            if (MinimumKillCooldown > KillCooldown)
            {
                KillCooldown = MinimumKillCooldown;
            }
        }
        if (RKFThreshold <= RendezvousCount)
        {
            //見えるようにしてあげましょう
            if (RevealKillableFactions && !CanSeeKiller)
            {
                CanSeeKiller = true;
                UtilsNotifyRoles.NotifyRoles(false, true, OnlyMeName: true, Altair);
            }
        }

        Player.RpcProtectedMurderPlayer();
        Altair.RpcProtectedMurderPlayer();
    }

    private PlayerControl SetAltair()
    {
        foreach (var state in PlayerState.AllPlayerStates)
        {
            if (state.Value.MainRole == CustomRoles.Altair)
            {
                Altair = PlayerCatch.GetPlayerById(state.Key);
                if (Altair.GetRoleClass() is Altair altairClass)
                {
                    altairClass.SetVega(this);
                }
                Logger.Warn($"SET=> {state.Key}", "Vega");
                return Altair;
            }
        }
        Logger.Warn("Altairが見つかりませんでした", "Vega");
        return null;
    }

    private void GiveBuff()
    {
        //ホスト以外は処理しない
        if (!AmongUsClient.Instance.AmHost) return;
        if (Altair == null && SetAltair() == null) return;
        if (!Altair.IsAlive()) return;

        var addons = AddOnsAssignData.AllData.Values
                        .Where(x => x.NeutralMaximum != null && x.Role.IsBuffAddon())
                        .Select(x => x.Role)
                        .ToArray();

        if (addons == null) return;
        var addon = addons.Where(x => !Altair.GetCustomSubRoles().Contains(x) && x is not CustomRoles.Amnesia and not CustomRoles.Amanojaku)
                          .OrderBy(x => Guid.NewGuid())
                          .FirstOrDefault(CustomRoles.NotAssigned);

        if (addon == CustomRoles.NotAssigned) return;
        if (addon == CustomRoles.Guarding)
        {
            Main.Guard[Altair.PlayerId] += Guarding.Guard;
        }
        Altair.RpcSetCustomRole(addon);
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.15f);
    }

    public void RpcRendezvous()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender();
        }
        Rendezvous();
    }

    public void CheckAlive(PlayerControl target = null)
    {
        Rendezvoused = false;

        //既に死亡しているなら処理しない
        if (!Player.IsAlive()) return;

        //ホストのみ処理
        if (AmongUsClient.Instance.AmHost)
        {
            //死亡or切断済みor役職が変化している
            if (Altair == null || target == Altair || !Altair.IsAlive() || Altair.GetCustomRole() != CustomRoles.Altair)
            {
                Player.RpcExileV2();
                var state = PlayerState.GetByPlayerId(Player.PlayerId);
                state.DeathReason = CustomDeathReason.FollowingSuicide;
                state.SetDead();
            }
        }
    }
}