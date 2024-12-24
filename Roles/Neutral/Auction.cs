/*
using AmongUs.GameOptions;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost.Roles.Neutral;
public sealed class Auction : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Auction),
            player => new Auction(player),
            CustomRoles.Auction,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            55460,
            null,
            "jsk",
            "#00b4eb",
            assignInfo: new RoleAssignInfo(CustomRoles.Auction, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public Auction(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Target = byte.MaxValue;
        add = byte.MaxValue;
        remove = byte.MaxValue;
        ok = byte.MaxValue;
        IsAuction = false;
        Vote.Clear();
        coin.Clear();
    }
    public byte Target, add, remove, ok;
    public static bool IsAuction;
    public static Dictionary<byte, byte> Vote = new();
    public static Dictionary<byte, byte> coin = new();

    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsAccident || info.IsFakeSuicide || info.IsSuicide) return;

        var target = info.AttemptTarget;

        Target = target.PlayerId;
        Player.RpcProtectedMurderPlayer();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (Target == byte.MaxValue || !Is(seen))
            return "";
        var target = PlayerCatch.GetPlayerById(Target);
        string RealName = (target is IUseTheShButton) ? Main.AllPlayerNames[Target] : target.GetRealName(isForMeeting);
        return Utils.ColorString(Color.red, $"対象: {RealName}");
    }

    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Target == byte.MaxValue) return;

        var target = PlayerCatch.GetPlayerById(Target);

        if (!target.IsAlive()) return;

        if (IsAuction)
        {
            IsAuction = false;

            PlayerControl player = null;

            var maxVote = Vote.Values.Max();
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (Vote[pc.PlayerId] == maxVote)
                {
                    if (player == null)
                        player = pc;
                    else return;
                }
            }

            player.RpcSetCustomRole(target.GetCustomRole(), true);
            target.RpcSetCustomRole(CustomRoles.Crewmate);

            return;
        }

        //オークション会議開始
        IsAuction = true;
        Voteresult += "\n\n<size=3>オークションの始まり！";
        Vote.Clear();
        foreach (var pc in PlayerCatch.AllPlayerControls) Vote[pc.PlayerId] = 0;
        PlayerControl.LocalPlayer.NoCheckStartMeeting(null);
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!IsAuction) return true;

        if (!(votedForId == add || votedForId == remove || votedForId == ok)) return false;

        if (votedForId == add)
        {
            if (coin[voter.PlayerId] <= 0) return false;
            coin[voter.PlayerId]--;
            Vote[voter.PlayerId]++;
            return false;
        }
        if (votedForId == remove)
        {
            if (Vote[voter.PlayerId] <= 0) return false;
            coin[voter.PlayerId]++;
            Vote[voter.PlayerId]--;
            return false;
        }
        if (votedForId == ok)
        {
            if (Vote[voter.PlayerId] <= 0) return false;
            Utils.SendMessage($"???「{Vote[voter.PlayerId]}コイン!」");
            return false;
        }

        return false;

    }
}*/