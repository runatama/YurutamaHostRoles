using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
class GameDataSerializePatch
{
    public static bool Sending;

    public static bool Prefix(NetworkedPlayerInfo __instance, ref bool __result)
    {
        if (AmongUsClient.Instance == null || !GameStates.InGame)
        {
            Sending = false;
            __result = true;
            return true;
        }
        if (MeetingHudPatch.StartPatch.Serialize)
        {
            Sending = false;
            __result = true;
            return true;
        }
        if (Options.CurrentGameMode != CustomGameMode.Standard || !GameStates.IsMeeting || GameStates.Tuihou || AntiBlackout.IsCached)
        {
            Sending = false;
            return true;
        }

        __instance.ClearDirtyBits();
        __result = false;
        return false;
    }
}