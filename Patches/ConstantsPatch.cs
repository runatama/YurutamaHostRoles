using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
public static class ConstantsGetBroadcastVersionPatch
{
    public static void Postfix(ref int __result)
    {
        if (GameStates.IsLocalGame || GameStates.IsFreePlay || CustomServerHelper.IsCs())
        {
            return;
        }
        __result += 25;
    }
}

// AU side bug?
[HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
public static class ConstantsIsVersionModdedPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
public static class CustomServerHelper //名前適当だから気にしないでね
{
    public static bool IsCs()
    {
        if (ServerManager.Instance == null) return false;
        var sn = ServerManager.Instance.CurrentRegion.TranslateName;
        if (sn is StringNames.ServerNA or StringNames.ServerEU or StringNames.ServerAS or StringNames.ServerSA)
            return false;
        else return true;
    }
}

