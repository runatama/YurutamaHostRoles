using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;
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
            25800,
            SetupOptionItem,
            "Cb",
            "#ff9966",
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.StartFans).FirstOrDefault().MinigamePrefab.OpenSound
        );
    public Comebacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
    }
    private static OptionItem OptionCooldown;
    enum OptionName
    {
        Cooldown
    }
    private static float Cooldown;
    private static Vector2 Tp1 = new(999f, 999f);
    private static Vector2 Tp2 = new(999f, 999f);
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Tp1 == new Vector2(999f, 999f))//Tp1が設定されてないぞ!
        {
            _ = new LateTask(() => Tp1 = (Vector2)Vent.currentVent.transform.position, 0.4f, "Tpset");
            Logger.Info("TP1を設定するよ!", "Comebacker");
            if (Tp2 != new Vector2(999f, 999f))
            {
                _ = new LateTask(() =>
                {
                    Player.RpcSnapToForced(Tp2);
                    Logger.Info("TP2に飛ぶよ!", "Comebacker");
                    Tp2 = new Vector2(999f, 999f);
                }, 0.7f, "TP");
            }
        }
        else
        if (Tp2 == new Vector2(999f, 999f))//Tp2が設定されてないぞ!
        {
            _ = new LateTask(() => Tp2 = (Vector2)Vent.currentVent.transform.position, 0.4f, "Tpset"); ;
            Logger.Info("TP2を設定するよ!", "Comebacker");
            if (Tp1 != new Vector2(999f, 999f))
            {
                _ = new LateTask(() =>
                    {
                        Player.RpcSnapToForced(Tp1);
                        Logger.Info("TP1に飛ぶよ!", "Comebacker");
                        Tp1 = new Vector2(999f, 999f);
                    }, 0.7f, "TP");
            }
        }
        return true;
    }
}