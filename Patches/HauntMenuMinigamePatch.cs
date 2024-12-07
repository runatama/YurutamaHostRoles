using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.SetFilterText))]
public static class HauntMenuMinigameSetFilterTextPatch
{
    public static bool Prefix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && (!PlayerControl.LocalPlayer.IsGorstRole() || Options.GRCanSeeOtherRoles.GetBool()) && (Options.GhostCanSeeOtherRoles.GetBool() || !Options.GhostOptions.GetBool()) && !PlayerControl.LocalPlayer.Is(Roles.Core.CustomRoles.AsistingAngel))
        {
            // 役職表示をカスタムロール名で上書き
            __instance.FilterText.text = UtilsRoleText.GetDisplayRoleName(PlayerControl.LocalPlayer, __instance.HauntTarget);
            return false;
        }
        return true;
    }
}
