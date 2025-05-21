using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(HauntMenuMinigame))]
public static class HauntMenuMinigamePatch
{
    //1回選択しないといけない いつか直す
    [HarmonyPatch(nameof(HauntMenuMinigame.SetHauntTarget)), HarmonyPrefix]
    public static bool SetHauntTargetPrefix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && (!PlayerControl.LocalPlayer.IsGhostRole() || Options.GRCanSeeOtherRoles.GetBool()) && (Options.GhostCanSeeOtherRoles.GetBool() || !Options.GhostOptions.GetBool()) && !PlayerControl.LocalPlayer.Is(Roles.Core.CustomRoles.AsistingAngel))
        {
            // 役職表示をカスタムロール名で上書き
            __instance.FilterText.text = UtilsRoleText.GetDisplayRoleName(PlayerControl.LocalPlayer, __instance.HauntTarget);
            return false;
        }
        return true;
    }
}
