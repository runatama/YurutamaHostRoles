using AmongUs.GameOptions;
using HarmonyLib;

namespace TownOfHost
{
    /* つかわないから消す
    [HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
    class ChanceChangePatch
    {
        public static void Postfix(RoleOptionSetting __instance)
        {
            string DisableText = $" ({GetString("Disabled")})";
            if (__instance.Role.Role == RoleTypes.Phantom)
            {
                __instance.titleText.text = GetString("Phantom");
            }
        }
    }*/

    [HarmonyPatch(typeof(GameOptionsManager), nameof(GameOptionsManager.SwitchGameMode))]
    class SwitchGameModePatch
    {
        public static void Postfix(GameModes gameMode)
        {
            Main.HnSFlag = false;
            if (gameMode == GameModes.HideNSeek)
            {
                Main.HnSFlag = true;
                if (!DebugModeManager.AmDebugger)
                {
                    ErrorText.Instance.HnSFlag = true;
                    ErrorText.Instance.AddError(ErrorCode.HnsUnload);
                    Harmony.UnpatchAll();
                    Main.Instance.Unload();
                }
            }
        }
    }
}