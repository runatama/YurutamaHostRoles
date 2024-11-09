using HarmonyLib;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    class ChatBubbleSetNamePatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (GameStates.IsInGame)
            {
                if (!__instance.playerInfo._object) return;
                __instance.NameText.text = __instance.NameText.text.ApplyNameColorData(PlayerControl.LocalPlayer, __instance.playerInfo._object, GameStates.IsMeeting);
            }
            if (__instance.NameText.text.RemoveaAlign() != __instance.NameText.text)
                __instance.SetLeft();
        }
    }
}