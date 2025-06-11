using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost.Roles.Neutral;

public sealed class JackalDoll : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackalDoll),
            player => new JackalDoll(player),
            CustomRoles.Jackaldoll,
            () => CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            30600,
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
        Ex = null;
    }
    static OptionItem JackaldieMode;
    static OptionItem RoleChe;
    public static OptionItem sidekick;
    static OptionItem CanVent;
    static OptionItem VentCool;
    static OptionItem VentIntime;
    static NetworkedPlayerInfo Ex;
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
    public static Dictionary<byte, byte> Oyabun = new();
    /// <summary>
    /// key →my
    /// va→role
    /// </summary>
    /// <returns></returns>
    static Dictionary<byte, CustomRoles> role = new();
    public static readonly CustomRoles[] ChangeRoles =
    {
        CustomRoles.Crewmate, CustomRoles.Madmate , CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Monochromer
    };
    public static int GetSideKickCount()
    {
        if ((CustomRoles.Jackal.IsEnable() && Jackal.CanmakeSK.GetBool())
        || (CustomRoles.JackalAlien.IsEnable() && JackalAlien.CanmakeSK.GetBool())
        || (CustomRoles.JackalMafia.IsEnable() && JackalMafia.CanmakeSK.GetBool()))
        {
            // 0%以上なら確認する。
            if (Options.GetRoleChance(CustomRoles.Jackaldoll) > 0)
            {
                return sidekick.GetInt();
            }
            // 0%でサイドキックあり得るなら1は返してあげる。
            return 1;
        }
        else return 0;
    }
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        sidekick = IntegerOptionItem.Create(RoleInfo, 9, Option.SideKickJackaldollMacCount, new(0, 15, 1), 1, false);
        JackaldieMode = StringOptionItem.Create(RoleInfo, 10, Option.JackaldolldieMode, EnumHelper.GetAllNames<diemode>(), 0, false);
        RoleChe = StringOptionItem.Create(RoleInfo, 15, Option.JackaldollRoleChe, cRolesString, 3, false)
        .SetCansee(() => JackaldieMode.GetValue() is 2);
        CanVent = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.CanVent, false, false);
        VentCool = FloatOptionItem.Create(RoleInfo, 17, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 0f, false, CanVent).SetValueFormat(OptionFormat.Seconds);
        VentIntime = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.EngineerInVentCooldown, new(0f, 180f, 0.5f), 0f, false, CanVent, true).SetValueFormat(OptionFormat.Seconds);
        RoleAddAddons.Create(RoleInfo, 20, MadMate: true);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = VentCool.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = VentIntime.GetFloat();
    }
    public static void Sidekick(PlayerControl pc, PlayerControl oyabun)
    {
        if (Oyabun.ContainsKey(pc.PlayerId))
        {
            Oyabun.Remove(pc.PlayerId);
        }

        var state = PlayerState.GetByPlayerId(pc.PlayerId);

        if (oyabun.Is(CustomRoles.Jackal))
        {
            if (Jackal.OptionDoll.GetBool())
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = Jackal.OptionKillCooldown.GetFloat();
                Oyabun.Add(pc.PlayerId, oyabun.PlayerId);
                role.Add(pc.PlayerId, CustomRoles.Jackal);
            }
            if (Jackal.SKcanImp.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    if (state.TargetColorData.ContainsKey(imp.Key)) NameColorManager.Remove(pc.PlayerId, imp.Key);
                    NameColorManager.Add(pc.PlayerId, imp.Key, "ffffff");
                }
            }
            if (Jackal.SKimpwocanimp.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    var iste = PlayerState.GetByPlayerId(imp.Key);
                    if (iste.TargetColorData.ContainsKey(pc.PlayerId)) NameColorManager.Remove(imp.Key, pc.PlayerId);
                    NameColorManager.Add(imp.Key, pc.PlayerId, "ffffff");
                }
            }
        }
        if (oyabun.Is(CustomRoles.JackalMafia))
        {
            if (JackalMafia.OptionDoll.GetBool())
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = JackalMafia.OptionKillCooldown.GetFloat();
                Oyabun.Add(pc.PlayerId, oyabun.PlayerId);
                role.Add(pc.PlayerId, CustomRoles.JackalMafia);
            }
            if (JackalMafia.SKcanImp.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    if (state.TargetColorData.ContainsKey(imp.Key)) NameColorManager.Remove(pc.PlayerId, imp.Key);
                    NameColorManager.Add(pc.PlayerId, imp.Key, "ffffff");
                }
            }
            if (JackalMafia.SKimpwocanimp.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    var iste = PlayerState.GetByPlayerId(imp.Key);
                    if (iste.TargetColorData.ContainsKey(pc.PlayerId)) NameColorManager.Remove(imp.Key, pc.PlayerId);
                    NameColorManager.Add(imp.Key, pc.PlayerId, "ffffff");
                }
            }
        }
        if (oyabun.Is(CustomRoles.JackalAlien))
        {
            if (JackalAlien.OptionDoll.GetBool())
            {
                Main.AllPlayerKillCooldown[pc.PlayerId] = JackalAlien.OptionKillCooldown.GetFloat();
                Oyabun.Add(pc.PlayerId, oyabun.PlayerId);
                role.Add(pc.PlayerId, CustomRoles.JackalAlien);
            }
            if (JackalAlien.SKcanImp.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    if (state.TargetColorData.ContainsKey(imp.Key)) NameColorManager.Remove(pc.PlayerId, imp.Key);
                    NameColorManager.Add(pc.PlayerId, imp.Key, "ffffff");
                }
            }
            if (JackalAlien.SKimpwocanimp.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    var iste = PlayerState.GetByPlayerId(imp.Key);
                    if (iste.TargetColorData.ContainsKey(pc.PlayerId)) NameColorManager.Remove(imp.Key, pc.PlayerId);
                    NameColorManager.Add(imp.Key, pc.PlayerId, "ffffff");
                }
            }
        }

        pc.RpcSetRole(CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, true);

        //サイドキックがガード等発動しないため。
        if (RoleAddAddons.GetRoleAddon(CustomRoles.Jackaldoll, out var d, pc, subrole: [CustomRoles.Guarding, CustomRoles.Speeding]))
        {
            if (d.GiveGuarding.GetBool()) Main.Guard[pc.PlayerId] += d.Guard.GetInt();
            if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[pc.PlayerId] = d.Speed.GetFloat();
        }
        foreach (var pl in PlayerCatch.AllPlayerControls)
        {
            if (pl.Is(CountTypes.Jackal))
            {
                NameColorManager.Add(pl.PlayerId, pc.PlayerId, UtilsRoleText.GetRoleColorCode(CustomRoles.Jackaldoll));
                NameColorManager.Add(pc.PlayerId, pl.PlayerId, UtilsRoleText.GetRoleColorCode(CustomRoles.Jackaldoll));
            }
        }
        //どっちにしろ更新を
        UtilsNotifyRoles.NotifyRoles();
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        Ex = Exiled;
        return false;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (Oyabun.TryGetValue(target.PlayerId, out var oya))
        {//サイドキックされて親分に殺されそうとか言う事になるとキルガード
            if (oya == killer.PlayerId)
            {
                info.CanKill = false;
                killer.RpcProtectedMurderPlayer(target);
                return false;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (Oyabun.ContainsKey(Player.PlayerId)) return;
        var id = Ex?.PlayerId ?? byte.MaxValue;

        if (PlayerCatch.AllAlivePlayerControls.Any(x => (x.Is(CustomRoles.Jackal) || x.Is(CustomRoles.JackalMafia) || x.Is(CustomRoles.JackalAlien) || role.ContainsKey(x.PlayerId)) && x.PlayerId != id)) return;

        foreach (var Jd in PlayerCatch.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackaldoll) && !role.ContainsKey(x.PlayerId)))
        {
            if ((diemode)JackaldieMode.GetValue() == diemode.FollowingSuicide)
            {
                //ガードなどは無視
                PlayerState.GetByPlayerId(Jd.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                Jd.RpcExileV2();
                PlayerState.GetByPlayerId(Jd.PlayerId).SetDead();
            }
            if ((diemode)JackaldieMode.GetValue() == diemode.rolech)
            {
                UtilsGameLog.AddGameLog($"JackalDool", Utils.GetPlayerColor(Jd) + ":  " + string.Format(GetString("Executioner.ch"), Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Jackal), GetString("Jackal")), Translator.GetRoleString($"{ChangeRoles[RoleChe.GetValue()]}").Color(UtilsRoleText.GetRoleColor(ChangeRoles[RoleChe.GetValue()]))));
                if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
                Jd.RpcSetCustomRole(ChangeRoles[RoleChe.GetValue()], log: null);
                UtilsNotifyRoles.NotifyRoles();
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!player.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;

        if (Oyabun.TryGetValue(player.PlayerId, out var oyabunid))
        {
            var oya = PlayerCatch.GetPlayerById(oyabunid);
            var jacrole = CustomRoles.Jackal;
            role.TryGetValue(player.PlayerId, out jacrole);
            if ((!oya.IsAlive() || oya.GetCustomRole() != jacrole) && !shoukaku)
            {
                MyState.SetCountType(CountTypes.Jackal);
                shoukaku = true;
                if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
                player.RpcSetCustomRole(jacrole, true);

                //徒党が存在していて、ジャッカルの徒党がON
                if (PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Faction)) && Faction.OptionRole.TryGetValue(CustomRoles.Jackal, out var option))
                {
                    if (option.GetBool())
                    {
                        player.RpcSetCustomRole(CustomRoles.Faction);
                        Logger.Info($"{player.Data.GetLogPlayerName()} => 昇格徒党", "Faction");
                    }
                }
            }
            shoukaku = false;
        }
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        if (Oyabun.ContainsKey(Player.PlayerId))
        {
            roleText = $"☆" + GetString("Jackaldoll");
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (GameLog) return "";

        if (Oyabun.TryGetValue(Player.PlayerId, out var id))
        {
            return Utils.ColorString(Main.PlayerColors[id], "◎");
        }
        return "";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (seen.Is(CountTypes.Jackal))
            enabled = true;
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (seen.Is(CountTypes.Jackal))
            enabled = true;
    }
}