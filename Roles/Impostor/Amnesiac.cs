using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;
public sealed class Amnesiac : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Amnesiac),
            player => new Amnesiac(player),
            CustomRoles.Amnesiac,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            6600,
            SetupCustomOption,
            "am",
            "#f8cd46",
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            isCantSeeTeammates: true
        );
    public Amnesiac(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CantKillImposter = OptCantKillImposter.GetBool();
        MatchSettingstoSheriff = OptMatchSettingstoSheriff.GetBool();
        omoidaseru = Optomoidaseru.GetBool();
        ImpNeedtoKill = OptImpNeedtoKill.GetBool();
        NeedtoKill = OptNeedtoKill.GetBool();
        KillsRequired = OptKillsRequired.GetInt();
        CanUseVent = OptCanUseVent.GetBool();
        CanUseSabotage = OptCanUseSabotage.GetBool();

        ShShotLimit = OptIamWolfBoy.GetBool() ? Crewmate.WolfBoy.ShotLimitOpt.GetInt() : Crewmate.Sheriff.ShotLimitOpt.GetInt();
        ShKillCooldown = OptIamWolfBoy.GetBool() ? Crewmate.WolfBoy.KillCooldown.GetFloat() : Crewmate.Sheriff.KillCooldown.GetFloat();

        omoidasita = false;
        KillCount = 0;
    }

    public static OptionItem OptCantKillImposter;
    public static OptionItem OptMatchSettingstoSheriff;
    public static OptionItem Optomoidaseru;
    public static OptionItem OptImpNeedtoKill;
    public static OptionItem OptNeedtoKill;
    public static OptionItem OptKillsRequired;
    public static OptionItem OptCanUseVent;
    public static OptionItem OptCanUseSabotage;
    public static OptionItem OptIamWolfBoy;

    public static bool CantKillImposter;
    public static bool MatchSettingstoSheriff;
    public static bool omoidaseru;
    public static bool ImpNeedtoKill;
    public static bool NeedtoKill;
    public static int KillsRequired;
    public static bool CanUseVent;
    public static bool CanUseSabotage;
    public static int ShShotLimit;
    public static float ShKillCooldown;

    public bool omoidasita;
    public int KillCount;

    enum Options
    {
        AmnesiacCantKillImposter,
        AmnesiacMatchSettingstoSheriff,
        Amnesiacomoidaseru,//←がONの状態での追加設定↓
        AmnesiacImpNeedtoKill,
        AmnesiacNeedtoKill,
        AmnesiacKillsRequired,
        AmnesiacCanUseVent,
        AmnesiacCanUseSabotage,
        AmnesiacIamWolfboy
    }

    public static void SetupCustomOption()
    {
        OptCantKillImposter = BooleanOptionItem.Create(RoleInfo, 10, Options.AmnesiacCantKillImposter, true, false);
        OptMatchSettingstoSheriff = BooleanOptionItem.Create(RoleInfo, 11, Options.AmnesiacMatchSettingstoSheriff, true, false);
        Optomoidaseru = BooleanOptionItem.Create(RoleInfo, 12, Options.Amnesiacomoidaseru, false, false);
        OptImpNeedtoKill = BooleanOptionItem.Create(RoleInfo, 13, Options.AmnesiacImpNeedtoKill, false, false, Optomoidaseru);
        OptNeedtoKill = BooleanOptionItem.Create(RoleInfo, 14, Options.AmnesiacNeedtoKill, false, false, Optomoidaseru);
        OptKillsRequired = IntegerOptionItem.Create(RoleInfo, 15, Options.AmnesiacKillsRequired, new(1, 6, 1), 2, false, OptNeedtoKill);
        OptCanUseVent = BooleanOptionItem.Create(RoleInfo, 16, Options.AmnesiacCanUseVent, false, false, Optomoidaseru);
        OptCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 17, Options.AmnesiacCanUseSabotage, false, false, Optomoidaseru);
        OptIamWolfBoy = BooleanOptionItem.Create(RoleInfo, 20, Options.AmnesiacIamWolfboy, false, false);
    }

    public float CalculateKillCooldown() => MatchSettingstoSheriff && !omoidasita ? ShKillCooldown : TownOfHost.Options.DefaultKillCooldown;
    public bool CanUseImpostorVentButton() => omoidasita && CanUseVent;
    public bool CanUseSabotageButton() => omoidasita && CanUseSabotage;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(omoidasita || (MatchSettingstoSheriff && OptIamWolfBoy.GetBool() && Crewmate.WolfBoy.ImpostorVision.GetBool()));
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!Is(killer)) return;

        if (CantKillImposter && target.GetCustomRole().IsImpostor())
            info.DoKill = false;
        if (omoidaseru && ImpNeedtoKill && !omoidasita)
            Omoidasu();
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!Is(killer)) return;

        KillCount++;
        if (omoidaseru && NeedtoKill && !omoidasita && KillCount >= KillsRequired)
            Omoidasu();
    }

    private void Omoidasu()
    {
        omoidasita = true;

        var clientId = Player.GetClientId();
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            var role = pc.GetCustomRole();
            if (!role.IsImpostor()) continue;
            pc.RpcSetRoleDesync(role.GetRoleTypes(), clientId);
        }

        if (!Utils.RoleSendList.Contains(Player.PlayerId))
            Utils.RoleSendList.Add(Player.PlayerId);
        Player.RpcProtectedMurderPlayer();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (seer.GetCustomRole().IsImpostor())
        {
            roleColor = Vanilla.Impostor.RoleInfo.RoleColor;
            roleText = omoidasita ? roleText : GetString("Amnesiac");
        }
        //本人にはシェリフ、インポスターにはロールカラーを赤に
        if (Is(seer))
        {
            roleColor = OptIamWolfBoy.GetBool() ? Crewmate.WolfBoy.RoleInfo.RoleColor : Crewmate.Sheriff.RoleInfo.RoleColor;
            roleText = omoidasita ? roleText : (OptIamWolfBoy.GetBool() ? GetString(CustomRoles.WolfBoy.ToString()) : GetString(CustomRoles.Sheriff.ToString()));
        }
    }
    public bool OverrideKillButton(out string text)
    {
        text = OptIamWolfBoy.GetBool() ? "WolfBoy_Kill" : "Sheriff_Kill";
        return true;
    }

    public override string GetProgressText(bool comms = false, bool gamelog = false) => MatchSettingstoSheriff && !omoidasita ? Utils.ColorString(ShShotLimit - KillCount > 0 ? Color.yellow : Color.gray, $"({ShShotLimit - KillCount})") : "";
}

