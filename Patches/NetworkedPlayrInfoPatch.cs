using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
class GameDataSerializePatch
{
    public static bool Sending;

    public static bool Prefix(NetworkedPlayerInfo __instance, ref bool initialState)
    {
        if (AmongUsClient.Instance == null || !GameStates.InGame)
        {
            Sending = false;
            initialState = true;
            return true;
        }
        if (MeetingHudPatch.StartPatch.Serialize)
        {
            Sending = false;
            initialState = true;
            return true;
        }
        if (Options.CurrentGameMode != CustomGameMode.Standard || !GameStates.IsMeeting || GameStates.Tuihou || AntiBlackout.IsCached)
        {
            Sending = false;
            return true;
        }

        __instance.ClearDirtyBits();
        initialState = false;
        return false;
    }
}