using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;
public sealed class JackalDoll : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackalDoll),
            player => new JackalDoll(player),
            CustomRoles.Jackaldoll,
            () => RoleTypes.Crewmate,
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
    }
    static OptionItem JackaldieMode;
    static OptionItem RoleChe;
    public static OptionItem sidekick;
    enum Option
    {
        JackaldieMode, dollRoleChe, dollside
    }
    enum diemode
    {
        Sonomama,
        FollowingSuicide,
        rolech,
    };
    public static int side;
    public static readonly CustomRoles[] ChangeRoles =
    {
        CustomRoles.Crewmate, CustomRoles.Madmate , CustomRoles.Jester, CustomRoles.Opportunist,
    };
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        sidekick = IntegerOptionItem.Create(RoleInfo, 9, Option.dollside, new(0, 2, 1), 1, false);
        JackaldieMode = StringOptionItem.Create(RoleInfo, 10, Option.JackaldieMode, EnumHelper.GetAllNames<diemode>(), 0, false);
        RoleChe = StringOptionItem.Create(RoleInfo, 15, Option.dollRoleChe, cRolesString, 3, false);
        RoleAddAddons.Create(RoleInfo, 20);
    }

    //サイドキック用に
    //全部使えないようにする。
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 0f;
        AURoleOptions.ShapeshifterDuration = 0f;
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
        AURoleOptions.ScientistBatteryCharge = 0f;
        AURoleOptions.ScientistCooldown = 0f;
    }
    public float CalculateKillCooldown() => 0f;
    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override bool CanUseAbilityButton() => false;
    public static void Sidekick(PlayerControl pc)
    {
        //サイドキックがガード等発動しないため。
        if (RoleAddAddons.AllData.TryGetValue(CustomRoles.Jackaldoll, out var d) && d.GiveAddons.GetBool())
        {
            if (d.GiveGuarding.GetBool()) Main.Guard[pc.PlayerId] += d.Guard.GetInt();
            if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[pc.PlayerId] = d.Speed.GetFloat();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackal) || x.Is(CustomRoles.JackalMafia)).Count() != 0) return;

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
    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo t)
    {
        if (Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackal) || x.Is(CustomRoles.JackalMafia)).Count() != 0) return;

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
}