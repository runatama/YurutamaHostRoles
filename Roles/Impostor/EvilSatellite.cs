using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class EvilSatellite : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilSatellite),
            player => new EvilSatellite(player),
            CustomRoles.EvilSatellite,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4400,
            SetupOptionItem,
            "Es"
        );
    public EvilSatellite(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        AllAlivePlayerKeiro.Clear();
        SendPlayerkeiro.Clear();
        AllAlivePlayerLast.Clear();
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            List<SystemTypes> keiro = new();
            if (!AllAlivePlayerKeiro.TryAdd(pc.PlayerId, keiro)) AllAlivePlayerKeiro[pc.PlayerId] = keiro;
        }
        usecount = OptionMax.GetInt();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionRandom;
    static OptionItem OptionMax;
    static Dictionary<byte, List<SystemTypes>> AllAlivePlayerKeiro = new();
    static Dictionary<byte, SystemTypes> AllAlivePlayerLast = new();
    static Dictionary<byte, List<SystemTypes>> SendPlayerkeiro = new();
    static HashSet<EvilSatellite> EvilSatellites = new();
    public override bool CanUseAbilityButton() => GameStates.IsMeeting;
    int usecount;
    public override void Add()
    {
        EvilSatellites.Add(this);
    }
    enum OptionName
    {
        EvilSatelliteRandom
    }
    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        OptionRandom = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EvilSatelliteRandom, true, false);
        OptionMax = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.OptionCount, (1, 99, 1), 5, false);
    }
    public override void AfterMeetingTasks()
    {
        AllAlivePlayerKeiro.Clear();
        SendPlayerkeiro.Clear();
        AllAlivePlayerLast.Clear();
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            List<SystemTypes> keiro = new();
            if (!AllAlivePlayerKeiro.TryAdd(pc.PlayerId, keiro)) AllAlivePlayerKeiro[pc.PlayerId] = keiro;
            if (pc.PlayerId == Player.PlayerId && PlayerControl.LocalPlayer.PlayerId == pc.PlayerId)//導入者
                Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
        }
    }
    public static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (player == null) return;
        if (!player.IsAlive()) return;
        //全員死亡なら終了
        if (PlayerCatch.AllAlivePlayerControls.All(pc => !pc.Is(CustomRoles.EvilSatellite))) return;
        if (!PlayerState.GetByPlayerId(player.PlayerId).HasSpawned) return;
        var nowroom = player.GetPlainShipRoom();
        if (nowroom == null) return;//ぬるぽならｶﾞｯ
        if (AllAlivePlayerLast.TryGetValue(player.PlayerId, out var lastroom))
        {
            //位置が変わってないならreturn
            if (lastroom == nowroom.RoomId) return;
        }
        //最後の追加が被らないように
        if (!AllAlivePlayerLast.TryAdd(player.PlayerId, nowroom.RoomId)) AllAlivePlayerLast[player.PlayerId] = nowroom.RoomId;
        //経路リストに追加
        if (AllAlivePlayerKeiro.TryGetValue(player.PlayerId, out var keiro))
        {
            AllAlivePlayerKeiro[player.PlayerId].Add(nowroom.RoomId);
        }
        else Logger.Error($"{player.name} : AllAlivePlayerKeiroがぬる!", "EvilSatellite");
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public void SendPlayerKeiro(byte playerid)
    {
        if (AllAlivePlayerKeiro.TryGetValue(playerid, out var keirolist))
        {
            //送信済みではない時
            if (!SendPlayerkeiro.TryGetValue(playerid, out var sendlist))
            {
                //設定回数と同じor多いともう使えない
                if (usecount <= 0) return;
                usecount--;
                List<SystemTypes> spk = new(!OptionRandom.GetBool() ? keirolist.ToArray() : keirolist.OrderBy(x => Guid.NewGuid()).ToArray());
                if (!SendPlayerkeiro.TryAdd(playerid, spk)) SendPlayerkeiro[playerid] = spk;
                sendlist = spk;
            }
            var sendtex = "<size=60%><line-height=80%>";
            var c = 0;
            var co = 0;
            foreach (var send in sendlist)
            {
                sendtex += GetString($"{send}") + (sendlist.Count == (co + 1) ? "" : (OptionRandom.GetBool() ? "・" : " → ")) + (c > 3 ? "\n" : "");
                if (c > 3) c = 0;
                c++;
                co++;
            }
            sendtex += string.Format(GetString("EvilSateliteShepeInfo"), Utils.GetPlayerColor(playerid));
            if (OptionRandom.GetBool()) sendtex += GetString("EvilSateliteShepeInfo2");
            sendtex += string.Format(GetString("EvilSateliteShepeInfo3"), usecount);
            Utils.SendMessage(sendtex, Player.PlayerId, string.Format($"<color=#ff1919>{GetString("EvilSateliteShepeInfoTitle")}</color>", Utils.GetPlayerColor(playerid)));
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color=#ff1919>({usecount})</color>";
}