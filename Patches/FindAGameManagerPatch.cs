using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(FindAGameManager))]
    class FindAGameManagerPatch
    {
        [HarmonyPatch(nameof(FindAGameManager.CoShow)), HarmonyPostfix]
        public static void CoShowPostfix(FindAGameManager __instance)
        {
            var text = CredentialsPatch.CreateText();
            if (__instance == null || text == null) return;
            text.transform.position += __instance.container.position;
            text.transform.parent = __instance.container;
        }
    }
}
