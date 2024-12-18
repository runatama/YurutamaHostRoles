using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Crewmate;
public sealed class King : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(King),
            player => new King(player),
            CustomRoles.King,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21400,
            SetupOptionItem,
            "k",
            "#FFD700"
        );
    public King(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Die = false;
        exdie = false;
    }
    static OptionItem ExileVoteCount;
    static OptionItem ExiledCrewDie;
    static OptionItem OpDeathReason;
    static OptionItem ExiledAddonBaaaaay;
    static OptionItem ExiledRoleAboooon;
    public static OptionItem CanGuesser;
    public static readonly CustomDeathReason[] deathReasons =
    {
        CustomDeathReason.Kill,CustomDeathReason.Suicide,CustomDeathReason.Revenge,CustomDeathReason.FollowingSuicide
    };
    bool Die;
    bool exdie;
    enum Op
    {
        KingExileVoteCount,
        KingExileCrewDies,
        KingDeathReason,
        KingAddon,
        KingRole,
        KingCanGuesser
    }
    static void SetupOptionItem()
    {
        var cRolesString = deathReasons.Select(x => x.ToString()).ToArray();
        ExileVoteCount = FloatOptionItem.Create(RoleInfo, 10, Op.KingExileVoteCount, new(1, 15, 1), 3, false).SetValueFormat(OptionFormat.Votes);
        ExiledCrewDie = FloatOptionItem.Create(RoleInfo, 11, Op.KingExileCrewDies, new(0, 15, 1), 5, false).SetValueFormat(OptionFormat.Players);
        OpDeathReason = StringOptionItem.Create(RoleInfo, 12, Op.KingDeathReason, cRolesString, 3, false);
        ExiledAddonBaaaaay = FloatOptionItem.Create(RoleInfo, 13, Op.KingAddon, new(0, 15, 1), 5, false).SetValueFormat(OptionFormat.Players);
        ExiledRoleAboooon = FloatOptionItem.Create(RoleInfo, 14, Op.KingRole, new(0, 15, 1), 5, false).SetValueFormat(OptionFormat.Players);
        CanGuesser = BooleanOptionItem.Create(RoleInfo, 15, Op.KingCanGuesser, true, false);
    }
    public override bool? CheckGuess(PlayerControl killer)
    {
        return CanGuesser.GetBool();
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (vote.TryGetValue(Player.PlayerId, out var count))
        {
            if (count >= ExileVoteCount.GetInt())
            {
                IsTie = false;
                Exiled = Player.Data;
                exdie = true;
                return true;
            }
        }
        return false;
    }
    public override void OnLeftPlayer(PlayerControl player)
    {
        if (player == Player)
            if (exdie && !Die)
            {
                _ = new LateTask(() => CrewMateAbooooon(), 20f, "KingExdie");
            }
        if (player == Player) Die = true;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        info.CanKill = false;
        var (killer, target) = info.AppearanceTuple;
        if (killer.GetRoleClass() is BountyHunter bountyHunter)
        {
            bountyHunter.OnKingKill(this);
        }
        killer.SetKillCooldown(target: target);
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.Tuihou) return;
        if (!exdie)
        {
            if (Die) return;

            if (player.Data.Disconnected && MyState.DeathReason is CustomDeathReason.Disconnected) return;
        }
        if (!player.IsAlive())
        {
            CrewMateAbooooon();
            exdie = false;
            Die = true;
        }
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref UnityEngine.Color roleColor, ref string roleText, ref bool addon)
    {
        seer ??= Player;
        if (seer == Player) return;
        if (seer.Is(CustomRoleTypes.Crewmate) || seer.Is(CustomRoles.BakeCat))
        {
            enabled = true;
            roleColor = StringHelper.CodeColor("#FFD700");
            roleText = GetString("King");
            addon = false;
        }
    }
    void CrewMateAbooooon()
    {
        if (Die && !exdie) return;
        var rand = IRandom.Instance;
        int Count = ExiledCrewDie.GetInt();

        List<PlayerControl> crews = new();

        //対象者
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            if (!pc) continue;
            if (pc == Player) continue;
            if (!pc.IsAlive() || !pc.Is(CustomRoleTypes.Crewmate)) continue;
            if (!crews.Contains(pc)) crews.Add(pc);
        }

        if (!GameStates.Meeting)
        {
            for (var i = 0; i < Count; i++)
            {
                if (crews.Count == 0) break;
                var pc = crews[rand.Next(0, crews.Count)];

                if (pc == null)
                {
                    i--;
                    continue;
                }
                if (!pc.IsAlive())
                {
                    i--;
                    continue;
                }

                PlayerState state = PlayerState.GetByPlayerId(pc.PlayerId);
                state.DeathReason = deathReasons[OpDeathReason.GetValue()];
                CustomRoleManager.OnCheckMurder(Player, pc, pc, pc, true);
                Logger.Info($"{pc.name}が巻き込まれちゃった！", "Kingaboooooon");
                crews.Remove(pc);
            }
        }
        else
        {
            for (var i = 0; i < Count; i++)
            {
                if (crews.Count == 0) break;
                var pc = crews[rand.Next(0, crews.Count)];

                if (pc == null)
                {
                    i--;
                    continue;
                }
                if (!pc.IsAlive())
                {
                    i--;
                    continue;
                }

                PlayerState state = PlayerState.GetByPlayerId(pc.PlayerId);
                state.DeathReason = deathReasons[OpDeathReason.GetValue()];
                Player.RpcExileV2();
                state.SetDead();
                ReportDeadBodyPatch.Musisuruoniku[Player.PlayerId] = false;

                Logger.Info($"{pc.name}が後追いしちゃった！", "KingEx");
                crews.Remove(pc);
            }
        }

        //役職 & 属性ぼっしゅー

        var addoncount = ExiledAddonBaaaaay.GetInt();
        if (addoncount != 0)
        {
            for (var i = 0; i < addoncount; i++)
            {
                if (crews.Count == 0) break;
                var pc = crews[rand.Next(0, crews.Count)];

                if (pc == null)
                {
                    i--;
                    continue;
                }
                if (!pc.IsAlive())
                {
                    i--;
                    continue;
                }

                var ps = PlayerState.GetByPlayerId(pc.PlayerId);
                List<CustomRoles> remove = new();
                if (pc.GetCustomSubRoles() != null)
                    foreach (var addon in pc.GetCustomSubRoles())
                        if (addon.IsBuffAddon())
                        {
                            if (!remove.Contains(addon)) remove.Add(addon);
                            Logger.Info($"{pc.name}の{addon}ぼっしゅー", "KingAddon");
                        }

                if (remove == null && remove?.Count != 0)
                {
                    foreach (var addon in remove)
                        ps.RemoveSubRole(addon);
                }
            }
        }

        var rolecount = ExiledRoleAboooon.GetInt();
        if (rolecount != 0)
        {
            for (var i = 0; i < rolecount; i++)
            {
                if (crews.Count == 0) break;
                var pc = crews[rand.Next(0, crews.Count)];

                if (pc == null)
                {
                    i--;
                    continue;
                }
                if (!pc.IsAlive())
                {
                    i--;
                    continue;
                }

                pc.RpcSetCustomRole(CustomRoles.Crewmate, true, null);
                Logger.Info($"{pc.name}の役職クルーな！！ハハハ!!", "KingRoles");
            }
        }
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.4f, "KingResetNotify");
        Die = true;
        exdie = false;
    }
}