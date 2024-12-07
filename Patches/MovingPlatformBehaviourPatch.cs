using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(MovingPlatformBehaviour))]
public static class MovingPlatformBehaviourPatch
{
    private static bool isDisabled = false;

    [HarmonyPatch(nameof(MovingPlatformBehaviour.Start)), HarmonyPrefix]
    public static void StartPrefix(MovingPlatformBehaviour __instance)
    {
        isDisabled = Options.DisableAirshipMovingPlatform.GetBool();

        if (isDisabled)
        {
            __instance.transform.localPosition = __instance.DisabledPosition;
            ShipStatus.Instance.Cast<AirshipStatus>().outOfOrderPlat.SetActive(true);
        }
    }
    [HarmonyPatch(nameof(MovingPlatformBehaviour.IsDirty), MethodType.Getter), HarmonyPrefix]
    public static bool GetIsDirtyPrefix(ref bool __result)
    {
        if (isDisabled)
        {
            __result = false;
            return false;
        }
        return true;
    }
    public static byte MovingPlatformPlayerId = 200;
    [HarmonyPatch(nameof(MovingPlatformBehaviour.Use), typeof(PlayerControl)), HarmonyPrefix]
    public static bool UsePrefix([HarmonyArgument(0)] PlayerControl player)
    {
        // プレイヤーがぬーん使用不可状態のときに使用をブロック
        if (!PlayerState.GetByPlayerId(player.PlayerId).CanUseMovingPlatform)
        {
            return false;
        }
        if (!isDisabled)
        {
            if (!GameStates.Meeting && !player.Data.IsDead && Options.LadderDeathNuuun.GetBool())
            {
                int chance = IRandom.Instance.Next(1, 101);
                if (chance <= FallFromLadder.Chance)
                {
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Fall;
                    state.SetDead();
                    player.RpcMurderPlayerV2(player);
                    return false;
                }
            }
            MovingPlatformPlayerId = player.PlayerId;
            _ = new LateTask(() => MovingPlatformPlayerId = 0, 5);
        }
        return !isDisabled;
    }
    public static bool UseMovingPlatform(this PlayerControl player)
    {
        if (player.PlayerId == MovingPlatformPlayerId) return true;
        return false;
    }
    [HarmonyPatch(nameof(MovingPlatformBehaviour.SetSide)), HarmonyPrefix]
    public static bool SetSidePrefix() => !isDisabled;
}
