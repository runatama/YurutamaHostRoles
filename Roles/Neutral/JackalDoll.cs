using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;
public sealed class JackalDoll : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackalDoll),
            player => new JackalDoll(player),
            CustomRoles.Jackaldoll,
            () => CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            51200,
            SetupOptionItem,
            "jacd",
            "#00b4eb",
                assignInfo: new RoleAssignInfo(CustomRoles.Jackaldoll, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(0, 15, 1)
                }
        );
    public JackalDoll(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Oyabun.Clear();
        shoukaku = false;
        role.Clear();
    }
    static OptionItem JackaldieMode;
    static OptionItem RoleChe;
    public static OptionItem sidekick;
    static OptionItem CanVent;
    static OptionItem VentCool;
    static OptionItem VentIntime;
    enum Option
    {
        JackaldolldieMode, JackaldollRoleChe, SideKickJackaldollMacCount
    }
    enum diemode
    {
        Sonomama,
        FollowingSuicide,
        rolech,
    };
    public static int side;
    bool shoukaku;
    /// <summary>
    /// key→my
    /// Va→oyabun
    /// </summary>
    /// <returns></returns>
    public static Dictionary<byte, PlayerControl> Oyabun = new();
    /// <summary>
    /// key →my
    /// va→role
    /// </summary>
    /// <returns></returns>
    static Dictionary<PlayerControl, CustomRoles> role = new();
    public static readonly CustomRoles[] ChangeRoles =
    {
        CustomRoles.Crewmate, CustomRoles.Madmate , CustomRoles.Jester, CustomRoles.Opportunist,
    };
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        sidekick = IntegerOptionItem.Create(RoleInfo, 9, Option.SideKickJackaldollMacCount, new(0, 15, 1), 1, false);
        JackaldieMode = StringOptionItem.Create(RoleInfo, 10, Option.JackaldolldieMode, EnumHelper.GetAllNames<diemode>(), 0, false);
        RoleChe = StringOptionItem.Create(RoleInfo, 15, Option.JackaldollRoleChe, cRolesString, 3, false);
        CanVent = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.CanVent, false, false);
        VentCool = FloatOptionItem.Create(RoleInfo, 17, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 0f, false, CanVent).SetValueFormat(OptionFormat.Seconds);
        VentIntime = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.EngineerInVentMaxTime, new(0f, 180f, 2.5f), 0f, false, CanVent, true).SetValueFormat(OptionFormat.Seconds);
        RoleAddAddons.Create(RoleInfo, 20);
    }

    //サイドキック用に
    //全部使えないようにする。
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = VentCool.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = VentIntime.GetFloat();
    }
    public float CalculateKillCooldown() => 0f;
    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public static void Sidekick(PlayerControl pc, PlayerControl oyabun)
    {
        if (Oyabun.ContainsKey(pc.PlayerId))
        {
            Oyabun.Remove(pc.PlayerId);
        }

        if (oyabun.Is(CustomRoles.Jackal))
        {
            if (Jackal.OptionDoll.GetBool())
            {
                Oyabun.Add(pc.PlayerId, oyabun);
                role.Add(pc, CustomRoles.Jackal);
            }
        }
        if (oyabun.Is(CustomRoles.JackalMafia))
        {
            if (JackalMafia.OptionDoll.GetBool())
            {
                Oyabun.Add(pc.PlayerId, oyabun);
                role.Add(pc, CustomRoles.JackalMafia);
            }
        }

        foreach (var pl in Main.AllPlayerControls)
        {
            if (pl == PlayerControl.LocalPlayer)
                pc.StartCoroutine(pc.CoSetRole(CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, Main.SetRoleOverride));
            else
                pc.RpcSetRoleDesync(CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, pl.GetClientId());
        }

        //サイドキックがガード等発動しないため。
        if (RoleAddAddons.AllData.TryGetValue(CustomRoles.Jackaldoll, out var d) && d.GiveAddons.GetBool())
        {
            if (d.GiveGuarding.GetBool()) Main.Guard[pc.PlayerId] += d.Guard.GetInt();
            if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[pc.PlayerId] = d.Speed.GetFloat();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (Oyabun.ContainsKey(Player.PlayerId)) return;

        if (Main.AllAlivePlayerControls.Any(x => x.Is(CustomRoles.Jackal) || x.Is(CustomRoles.JackalMafia))) return;

        foreach (var Jd in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackaldoll)))
        {
            if ((diemode)JackaldieMode.GetValue() == diemode.FollowingSuicide)
            {
                //ガードなどは無視
                PlayerState.GetByPlayerId(Jd.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                PlayerState state = PlayerState.GetByPlayerId(Player.PlayerId);
                PlayerState.GetByPlayerId(Player.PlayerId);
                Player.RpcExileV2();
                state.SetDead();
            }
            if ((diemode)JackaldieMode.GetValue() == diemode.rolech)
            {
                Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Jackaldool]　" + Utils.GetPlayerColor(Jd) + ":  " + string.Format(Translator.GetString("Executioner.ch"), Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), Translator.GetString("Jackal")), Translator.GetRoleString($"{ChangeRoles[RoleChe.GetValue()]}").Color(Utils.GetRoleColor(ChangeRoles[RoleChe.GetValue()])));
                Jd.RpcSetCustomRole(ChangeRoles[RoleChe.GetValue()]);
                Utils.NotifyRoles();
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo t)
    {
        if (Oyabun.ContainsKey(Player.PlayerId)) return;
        if (Main.AllAlivePlayerControls.Any(x => x.Is(CustomRoles.Jackal) || x.Is(CustomRoles.JackalMafia))) return;

        foreach (var Jd in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackaldoll)))
        {
            if ((diemode)JackaldieMode.GetValue() == diemode.FollowingSuicide)
            {
                //ガードなどは無視
                PlayerState.GetByPlayerId(Jd.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                Jd.RpcMurderPlayer(Jd, true);
                if (_ == Jd) ReportDeadBodyPatch.DieCheckReport(_, t);
            }
            if ((diemode)JackaldieMode.GetValue() == diemode.rolech)
            {
                Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Jackaldool]　" + Utils.GetPlayerColor(Jd) + ":  " + string.Format(Translator.GetString("Executioner.ch"), Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), Translator.GetString("Jackal")), Translator.GetRoleString($"{ChangeRoles[RoleChe.GetValue()]}").Color(Utils.GetRoleColor(ChangeRoles[RoleChe.GetValue()])));
                Jd.RpcSetCustomRole(ChangeRoles[RoleChe.GetValue()]);
                Utils.NotifyRoles();
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!player.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;

        if (Oyabun.ContainsKey(player.PlayerId))
        {
            if (!Oyabun[player.PlayerId].IsAlive() && !shoukaku)
            {
                if (!role.ContainsKey(player)) role.Add(player, CustomRoles.Jackal);

                player.RpcSetCustomRole(role[player], true);
                PlayerState.GetByPlayerId(player.PlayerId).SetCountType(CountTypes.Jackal);
                shoukaku = true;
            }
            shoukaku = false;
        }
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        if (Oyabun.ContainsKey(Player.PlayerId))
        {
            roleText = Translator.GetString("Sidekick") + Translator.GetString("Jackaldoll");
        }
    }
}