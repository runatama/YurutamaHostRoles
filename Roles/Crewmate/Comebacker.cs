using System.Linq;
using System.Collections.Generic;

using AmongUs.GameOptions;
using UnityEngine;
using HarmonyLib;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Comebacker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Comebacker),
            player => new Comebacker(player),
            CustomRoles.Comebacker,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            12200,
            (9, 0),
            SetupOptionItem,
            "cb",
            "#ff9966"//,
                     //introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.StartFans).FirstOrDefault().MinigamePrefab.OpenSound
        );
    public Comebacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
        Tp = new(999f, 999f);
        ComebackPosString = "";
    }
    private static OptionItem OptionCooldown;
    enum OptionName
    {
        Cooldown
    }
    private static float Cooldown;
    private Vector2 Tp;
    private string ComebackPosString;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1.5f;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Tp != new Vector2(999f, 999f))
        {
            var tp = Tp;
            _ = new LateTask(() =>
            {
                Player.RpcSnapToForced(tp + new Vector2(0f, 0.1f));
                Logger.Info("ベントに飛ぶよ!", "Comebacker");
            }, 1f, "TP");
        }
        ShipStatus.Instance.AllVents.DoIf(vent => vent.Id == ventId, vent => Tp = (Vector2)vent.transform.position);
        Logger.Info("ベントを設定するよ!", "Comebacker");

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
            ComebackPosString = now;
        }
        else ComebackPosString = string.Format(GetString($"SantaClausnear"), $"{near}");

        UtilsNotifyRoles.NotifyRoles(Player, OnlyMeName: true);
        return true;
    }
    public override string GetAbilityButtonText() => GetString("CamebackerAbility");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;

        if (isForMeeting || !Player.IsAlive() || ComebackPosString == "") return "";

        if (isForHud) return $"<color={RoleInfo.RoleColorCode}>{string.Format(GetString("ComebackLowerText"), ComebackPosString)}</color>";
        return $"<size=50%><color={RoleInfo.RoleColorCode}{string.Format(GetString("ComebackLowerText"), ComebackPosString)}</color></size>";
    }
}
