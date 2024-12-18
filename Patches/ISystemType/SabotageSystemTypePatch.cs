using HarmonyLib;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
public static class SabotageSystemTypeUpdateSystemPatch
{
    private static readonly LogHandler logger = Logger.Handler(nameof(SabotageSystemType));

    static byte amount;
    public static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        var newReader = MessageReader.Get(msgReader);
        amount = newReader.ReadByte();
        newReader.Recycle();

        var nextSabotage = (SystemTypes)amount;
        logger.Info($"PlayerName: {player.GetNameWithRole().RemoveHtmlTags()}, SabotageType: {nextSabotage}");

        //HASモードではサボタージュ不可
        if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;

        if (SuddenDeathMode.NowSuddenDeathMode) return false;
        if (!ExileControllerWrapUpPatch.AllSpawned && !MeetingStates.FirstMeeting) return false;

        if (!CustomRoleManager.OnSabotage(player, nextSabotage))
        {
            return false;
        }
        var roleClass = player.GetRoleClass();
        if (roleClass is IKiller killer)
        {
            //そもそもサボタージュボタン使用不可ならサボタージュ不可
            if (!killer.CanUseSabotageButton()) return false;
            //その他処理が必要であれば処理
            if (roleClass.OnInvokeSabotage(nextSabotage))
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    Main.SabotageType = (SystemTypes)amount;
                    var sb = Translator.GetString($"sb.{(SystemTypes)amount}");
                    if (!Main.NowSabotage)
                        UtilsGameLog.AddGameLog($"Sabotage", string.Format(Translator.GetString("Log.Sabotage"), Utils.GetPlayerColor(player, true) + $"({UtilsRoleText.GetTrueRoleName(player.PlayerId, false)})", sb));
                    Main.NowSabotage = true;
                    Main.LastSab = player.PlayerId;
                }
            }
            return roleClass.OnInvokeSabotage(nextSabotage);
        }
        else
        {
            return CanSabotage(player);
        }
    }
    private static bool CanSabotage(PlayerControl player)
    {
        //サボタージュ出来ないキラー役職はサボタージュ自体をキャンセル
        if (!player.Is(CustomRoleTypes.Impostor))
        {
            return false;
        }
        if (AmongUsClient.Instance.AmHost)
        {
            if (!Main.NowSabotage)
            {
                Main.SabotageType = (SystemTypes)amount;
                var sb = Translator.GetString($"sb.{(SystemTypes)amount}");

                UtilsGameLog.AddGameLog($"Sabotage", string.Format(Translator.GetString("Log.Sabotage"), Utils.GetPlayerColor(player, true) + $"({UtilsRoleText.GetTrueRoleName(player.PlayerId, false)})", sb));
                Main.NowSabotage = true;
                Main.LastSab = player.PlayerId;
            }
        }
        return true;
    }
    public static void Postfix(SabotageSystemType __instance, bool __runOriginal /* Prefixの結果，本体処理が実行されたかどうか */ )
    {
        if (!__runOriginal || !Options.ModifySabotageCooldown.GetBool() || !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        // サボタージュクールダウンを変更
        __instance.Timer = Options.SabotageCooldown.GetFloat();
        __instance.IsDirty = true;
    }
}

[HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Initialize))]
public static class ElectricTaskInitializePatch
{
    public static void Postfix()
    {
        UtilsOption.MarkEveryoneDirtySettings();
        _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 1f, "1Elec", true);
        _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 2f, "2Elec", true);
        _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 3f, "3Elec", true);
        _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 4f, "4eElec", true);
        _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 5f, "5Elec", true);
        _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 6f, "5Elec", true);
        if (!GameStates.IsMeeting)
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
    }
}
[HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Complete))]
public static class ElectricTaskCompletePatch
{
    public static void Postfix()
    {
        UtilsOption.MarkEveryoneDirtySettings();
        if (!GameStates.IsMeeting)
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
    }
}
