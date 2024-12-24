using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;
public sealed class SantaClaus : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SantaClaus),
            player => new SantaClaus(player),
            CustomRoles.SantaClaus,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            35900,
            SetupOptionItem,
            "Sac",
            "#e05050"
        );
    public SantaClaus(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        WinGivePresentCount = OptWinGivePresentCount.GetInt();
        AddWin = OptAddWin.GetBool();

        IWinflag = false;
        MeetingNotify = false;
        MeetingNotifyRoom = "";
        havepresent = 0;
        giftpresent = 0;
        EntotuVentId = null;
        EntotuVentPos = null;
    }
    static OptionItem OptWinGivePresentCount; static int WinGivePresentCount;
    static OptionItem OptAddWin; static bool AddWin;
    enum OptionName
    {
        SantaClausWinGivePresentCount,
        CountKillerAddWin//追加勝利
    }
    bool IWinflag;
    bool MeetingNotify;
    string MeetingNotifyRoom;
    int havepresent;
    int giftpresent;
    int? EntotuVentId;
    Vector3? EntotuVentPos;
    private static void SetupOptionItem()
    {
        OptWinGivePresentCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SantaClausWinGivePresentCount, new(1, 30, 1), 4, false);
        OptAddWin = BooleanOptionItem.Create(RoleInfo, 15, OptionName.CountKillerAddWin, false, false);
        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 3f;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (AmongUsClient.Instance.AmHost && MyTaskState.IsTaskFinished && Player.IsAlive())
        {
            havepresent++;
            UtilsNotifyRoles.NotifyRoles();

            // プレゼントの配達先リスト
            List<Vent> AllVents = new(ShipStatus.Instance.AllVents);

            var ev = AllVents[IRandom.Instance.Next(AllVents.Count)];

            EntotuVentId = ev.Id;
            EntotuVentPos = new Vector3(ev.transform.position.x, ev.transform.position.y);
            GetArrow.Add(Player.PlayerId, (Vector3)EntotuVentPos);
        }
        return true;
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        var wariai = 1 - (giftpresent / WinGivePresentCount);
        var win = $"{giftpresent}/{WinGivePresentCount}";

        return $" <color=#e05050>({win})</color>";
    }
    public override string MeetingMeg()
    {
        if (!MeetingNotify || !Player.IsAlive() || MeetingNotifyRoom == "") return "";

        var chance = IRandom.Instance.Next(0, 20);
        var mesnumber = 0;

        if (chance > 18) mesnumber = 2;
        if (chance > 15) mesnumber = 1;

        var msg = string.Format(GetString($"SantaClausMeetingMeg{mesnumber}"), MeetingNotifyRoom);

        MeetingNotifyRoom = "";
        MeetingNotify = false;
        return $"<size=60%><color=#e05050>{msg}</color></size>";
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!Player.IsAlive() || ventId != EntotuVentId || havepresent <= 0 || EntotuVentPos == null) return false;

        havepresent--;
        //プレゼントを渡せたって言う処理
        Player.RpcProtectedMurderPlayer();

        Player.Data.RpcSetTasks(Array.Empty<byte>());
        MyTaskState.CompletedTasksCount = 0;
        giftpresent++;
        Player.SyncSettings();
        EntotuVentId = null;
        MeetingNotify = true;

        // 通知の奴
        var NowRoom = Player.GetPlainShipRoom();

        var Rooms = ShipStatus.Instance.AllRooms;
        Dictionary<PlainShipRoom, float> Distance = new();

        if (Rooms != null)
            foreach (var r in Rooms)
            {
                if (r.RoomId == SystemTypes.Hallway) continue;
                Distance.Add(r, Vector2.Distance(Player.GetTruePosition(), r.transform.position));
            }

        var near = GetString($"{Distance.OrderByDescending(x => x.Value).Last().Key.RoomId}");

        if (NowRoom != null)
        {
            var now = GetString($"{NowRoom.RoomId}");

            if (NowRoom.RoomId == SystemTypes.Hallway)
            {
                now = near + now;
            }
            MeetingNotifyRoom = now;
        }
        else MeetingNotifyRoom = string.Format(GetString($"SantaClausnear"), $"{near}");

        GetArrow.Remove(Player.PlayerId, (Vector3)EntotuVentPos);
        if (WinGivePresentCount <= giftpresent)
        {
            Logger.Info($"{Player?.Data?.PlayerName ?? "null"}が勝利条件達成！", "SantaClaus");

            if (!AddWin)//単独勝利設定なら即勝利で処理終わり
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SantaClaus);
                CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
                return false;
            }
            else
            {
                IWinflag = true;
            }
        }
        UtilsNotifyRoles.NotifyRoles();

        return false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        //自分だけで完結しないならお帰り！
        if (seen.PlayerId != seer.PlayerId) return "";
        //会議、死亡するとおしまい
        if (isForMeeting || !Player.IsAlive()) return "";

        //配達先が決まっている時
        if (EntotuVentPos != null && EntotuVentId != null)
            return $"<color=#e05050>{GetString("SantaClausLower1") + GetArrow.GetArrows(seer, (Vector3)EntotuVentPos)}</color>";

        // プレゼントの用意をするんだぜ
        return $"<color=#e05050>{GetString("SantaClausLower2")}</color>";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (IWinflag && seen == seer) return Utils.AdditionalWinnerMark;
        return "";
    }
    public bool CheckWin(ref CustomRoles winnerRole) => IWinflag;
    public override string GetAbilityButtonText() => GetString("ChefButtonText");
}