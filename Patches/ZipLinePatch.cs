using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
    class PlayerControlRpcUseZiplinePatch
    {
        static List<byte> ZipdiePlayers = new();
        static List<byte> ZiplineCools = new();
        public static void reset()
        {
            ZiplineCools.Clear();
            ZipdiePlayers.Clear();
        }
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(2)] bool fromTop)
        {
            if (!fromTop && Options.CantUseZipLineTotop.GetBool()) return false;
            if (fromTop && Options.CantUseZipLineTodown.GetBool()) return false;

            if (ZiplineCools.Contains(__instance.PlayerId)) return true;

            ZiplineCools.Add(__instance.PlayerId);
            _ = new LateTask(() =>
            {
                if (ZiplineCools.Contains(__instance.PlayerId))
                    ZiplineCools.Remove(__instance.PlayerId);
            }, 3f, "ZiplineCoolsremove", true);

            if (!GameStates.Meeting && !__instance.Data.IsDead && Options.LadderDeathZipline.GetBool())
            {
                int chance = IRandom.Instance.Next(1, 101);
                if (chance <= FallFromLadder.Chance)
                {
                    if (__instance.Data.IsDead) return true;
                    var speed = Main.AllPlayerSpeed[__instance.PlayerId];

                    _ = new LateTask(() =>
                    {
                        if (!GameStates.Meeting && !__instance.Data.IsDead && Options.LadderDeathZipline.GetBool())
                        {
                            Main.AllPlayerSpeed[__instance.PlayerId] = Main.MinSpeed;
                            __instance.SyncSettings();
                            ZipdiePlayers.Add(__instance.PlayerId);
                        }
                    }, 3f, "ZipLineDeath", true);
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[__instance.PlayerId] = speed;
                        __instance.SyncSettings();
                        if (!GameStates.Meeting && !__instance.Data.IsDead && Options.LadderDeathZipline.GetBool())
                        {
                            __instance.Data.IsDead = true;
                            __instance.RpcMurderPlayer(__instance);
                            var state = PlayerState.GetByPlayerId(__instance.PlayerId);
                            state.DeathReason = CustomDeathReason.Fall;
                            state.SetDead();
                        }
                    }, fromTop ? 5f : 8f, "ZipLineFall", null);
                }
            }
            return true;
        }
        public static void OnMeeting(PlayerControl reporter, NetworkedPlayerInfo oniku)
        {
            //速度は処理止まらんぜ
            foreach (var playerId in ZipdiePlayers)
            {
                var player = PlayerCatch.GetPlayerById(playerId);
                if (player == null) continue;
                if (player.Data.IsDead) continue;
                player.Data.IsDead = true;
                player.RpcMurderPlayer(player);
                var state = PlayerState.GetByPlayerId(player.PlayerId);
                state.DeathReason = CustomDeathReason.Fall;
                state.SetDead();
            }
            if (ZipdiePlayers.Contains(reporter.PlayerId))
            {
                ZipdiePlayers.Clear();
                ReportDeadBodyPatch.DieCheckReport(reporter, oniku);
            }
            ZiplineCools.Clear();
            ZipdiePlayers.Clear();
        }
    }
}