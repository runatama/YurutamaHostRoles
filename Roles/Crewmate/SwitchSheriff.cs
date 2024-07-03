using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Crewmate;
//Memo
//ちゃんと動くかの挙動を確認。
//通信中どうするか
public sealed class SwitchSheriff : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SwitchSheriff),
            player => new SwitchSheriff(player),
            CustomRoles.SwitchSheriff,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            20500,
            SetupOptionItem,
            "swsh",
            "#f8cd46",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );

    public SwitchSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.True
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        Taskmode = true;
        nowcool = CurrentKillCooldown;
        last = 0;
    }

    public static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    public static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public bool Taskmode;
    float nowcool;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKill,
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<SchrodingerCat.TeamType, OptionItem> SchrodingerCatKillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    public static readonly string[] KillOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately"
    };

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Crew;

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 990f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        Options.OverrideKilldistance.Create(RoleInfo, 8);
        Options.OverrideTasksData.Create(RoleInfo, 16);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        SetUpKillTargetOption(CustomRoles.Madmate, 13);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
    }
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllStandardRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (neutral is CustomRoles.SchrodingerCat) continue;
            SetUpKillTargetOption(neutral, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
        foreach (var catType in EnumHelper.GetAllValues<SchrodingerCat.TeamType>())
        {
            if ((byte)catType < 50)
            {
                continue;
            }
            SetUpSchrodingerCatKillTargetOption(catType, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
    }
    public static void SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        if (parent == null) parent = RoleInfo.RoleOption;
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public static void SetUpSchrodingerCatKillTargetOption(SchrodingerCat.TeamType catType, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;
        // (%team%陣営)
        var inTeam = GetString("In%team%", new Dictionary<string, string>() { ["%team%"] = GetRoleString(catType.ToString()) });
        // シュレディンガーの猫(%team%陣営)
        var catInTeam = Utils.ColorString(SchrodingerCat.GetCatColor(catType), Utils.GetRoleName(CustomRoles.SchrodingerCat) + inTeam);
        Dictionary<string, string> replacementDic = new() { ["%role%"] = catInTeam };
        SchrodingerCatKillTargetOptions[catType] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        SchrodingerCatKillTargetOptions[catType].ReplacementDictionary = replacementDic;
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "SwitchSheriff");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ShotLimit = reader.ReadInt32();
    }
    public bool CanUseKillButton()
        => Player.IsAlive()
        && !Taskmode
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;

    bool Ch()
    => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseSabotageButton() => false;

    public override bool CanUseAbilityButton() => Player != PlayerControl.LocalPlayer;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0.5f;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            if (last != 0)
            {
                info.DoKill = false;
                return;
            }
            if (AmongUsClient.Instance.AmHost)
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc == PlayerControl.LocalPlayer)
                        Player.StartCoroutine(Player.CoSetRole(RoleTypes.Engineer, true));
                    else
                        Player.RpcSetRoleDesync(RoleTypes.Engineer, pc.GetClientId());
                }
            Taskmode = true;

            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit}発", "SwitchSheriff");
            if (ShotLimit <= 0)
            {
                info.DoKill = false;
                return;
            }
            ShotLimit--;
            SendRPC();
            if (!CanBeKilledBy(target) || (target.Is(CustomRoles.Alien) && Alien.modeTairo))
            {
                //ターゲットが大狼かつ死因を変える設定なら死因を変える、それ以外はMisfire
                PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Revenge1 : target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire;

                _ = new LateTask(() => killer.RpcMurderPlayer(killer), 0.2f, "SwSheMiss");
                if (!MisfireKillsTarget.GetBool())
                {
                    info.DoKill = false;
                    return;
                }
            }
            killer.RpcResetAbilityCooldown(kousin: true);
            nowcool = KillCooldown.GetFloat();
            Main.AllPlayerKillCooldown[killer.PlayerId] = nowcool;
            Player.SyncSettings();
            Player.SetKillCooldown();
        }
        return;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (Player.IsAlive())
        {
            if (AmongUsClient.Instance.AmHost)
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc == PlayerControl.LocalPlayer)
                        Player.StartCoroutine(Player.CoSetRole(RoleTypes.Engineer, true));
                    else if (pc == Player)
                        Player.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId());
                    else Player.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                }
            Taskmode = false;
        }
        else
        {
            if (AmongUsClient.Instance.AmHost)
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc != PlayerControl.LocalPlayer && pc.GetCustomRole().IsImpostor())
                        pc.RpcSetRoleDesync(pc.IsAlive() ? pc.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() ?? (RoleTypes)pc.GetCustomRole() : RoleTypes.ImpostorGhost, Player.GetClientId());
                }
        }
        Player.RpcResetAbilityCooldown(kousin: true);
    }
    public override void AfterMeetingTasks()
    {
        _ = new LateTask(() =>
        {
            if (AmongUsClient.Instance.AmHost)
                if (Player.IsAlive())
                {
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == PlayerControl.LocalPlayer)
                            Player.StartCoroutine(Player.CoSetRole(RoleTypes.Engineer, true));
                        else
                            Player.RpcSetRoleDesync(RoleTypes.Engineer, pc.GetClientId());
                    }
                    Taskmode = true;
                }
            nowcool = KillCooldown.GetFloat();
        }, 0.2f, "");
    }
    public override string GetProgressText(bool comms = false)
    {
        var r = Utils.ColorString(Ch() ? Color.yellow : Color.gray, $"({ShotLimit})");
        if (!GameStates.Meeting && Ch()) r += Utils.ColorString(Color.yellow, Taskmode ? $" [Task]<color=#ffffff>({last})</color>" : $"  [Sheriff]<color=#ffffff>({last})</color>");
        return r;
    }
    public static bool CanBeKilledBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();

        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
            {
                Logger.Warn($"シェリフ({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", nameof(Sheriff));
                return false;
            }
            return schrodingerCat.Team switch
            {
                SchrodingerCat.TeamType.Mad => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
                SchrodingerCat.TeamType.Crew => false,
                _ => CanKillNeutrals.GetValue() == 0 || (SchrodingerCatKillTargetOptions.TryGetValue(schrodingerCat.Team, out var option) && option.GetBool()),
            };
        }
        if (cRole == CustomRoles.Jackaldoll) return CanKillNeutrals.GetValue() == 0 || (!KillTargetOptions.TryGetValue(CustomRoles.Jackal, out var option) && option.GetBool()) || (!KillTargetOptions.TryGetValue(CustomRoles.JackalMafia, out var op) && op.GetBool());
        if (cRole == CustomRoles.SKMadmate) return KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool();

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => cRole is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
            CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0 || (!KillTargetOptions.TryGetValue(cRole, out var option) && option.GetBool()),
            CustomRoleTypes.Crewmate => cRole is CustomRoles.WolfBoy,
            _ => false,
        };
    }
    public bool OverrideKillButton(out string text)
    {
        text = "Sheriff_Kill";
        return true;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId, ref bool nouryoku)
    {
        if (!Ch()) return false;
        if (Taskmode && Utils.IsActive(SystemTypes.Comms)) return false;//Hostはタスクモード(エンジ)での切り替えできるからさせないようにする

        if (AmongUsClient.Instance.AmHost)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc == PlayerControl.LocalPlayer)
                    Player.StartCoroutine(Player.CoSetRole(RoleTypes.Engineer, true));
                else
                    Player.RpcSetRoleDesync(pc == Player && Taskmode ? RoleTypes.Impostor : RoleTypes.Engineer, pc.GetClientId());
            }
            Taskmode = !Taskmode;
        }

        float kill = last;
        if ((kill - 2) <= 0.5f) kill = 0.5f;
        else kill = last - 2;

        Main.AllPlayerKillCooldown[Player.PlayerId] = kill;
        Player.SyncSettings();
        _ = new LateTask(() => Player.SetKillCooldown(), 0.2f, "");
        nouryoku = true;
        return false;
    }
    public override bool CanTask()
    {
        if (!Player.IsAlive()) return true;
        return Taskmode;
    }
    int last;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.Meeting || GameStates.Intro) return;

        if (!player.IsAlive()) return;
        if (nowcool > 1)
            nowcool -= Time.fixedDeltaTime;
        else if (nowcool != 0.5f) nowcool = 0.5f;
        var now = (int)nowcool;
        if (now != last)
        {
            if (now == 1f) player.SetKillCooldown();
            last = now;
            Utils.NotifyRoles();
        }
    }
}
