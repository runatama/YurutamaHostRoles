using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        ///<summary>
        ///追放処理を上書きするかどうか
        ///</summary>
        public static bool OverrideExiledPlayer => PlayerCatch.AllPlayerControls.Count() < 4 && !ModClientOnly && (Options.NoGameEnd.GetBool() || GetA()) && (Main.DebugAntiblackout || !DebugModeManager.EnableDebugMode.GetBool()) && !Options.BlackOutwokesitobasu.GetBool();
        public static bool IsCached { get; private set; } = false;
        public static bool IsSet { get; private set; } = false;
        public static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();
        //private static Dictionary<(byte, byte), RoleTypes> RoleTypeCache = new();
        private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

        private static bool GetA()
        {
            foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
                if (info.IsEnable && info.CountType is not CountTypes.Crew and not CountTypes.Impostor)
                    return true;
            return false;
        }

        private static bool ModClientOnly//全員ModClient ↓これじゃダメなの?()
            => PlayerCatch.AllPlayerControls.All(pc => pc.IsModClient());

        public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SetIsDead is called from {callerMethodName}");
            if (IsCached)
            {
                logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。");
                return;
            }
            isDeadCache.Clear();
            var nowcount = PlayerCatch.AllPlayerControls.Count();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                //情報が無い　　　   4人以上正常者がいる場合は役職変えるので回線切断者を生存擬装する必要が多分ない。
                if (info == null || ((info?.Disconnected == true) && 4 <= nowcount)) continue;
                isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
                info.IsDead = false;
                info.Disconnected = false;
            }
            IsCached = true;
            if (doSend) SendGameData();
        }
        public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"RestoreIsDead is called from {callerMethodName}");
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out var val))
                {
                    info.IsDead = val.isDead;
                    info.Disconnected = val.Disconnected;
                }
            }
            isDeadCache.Clear();
            IsCached = false;
            IsSet = false;
            if (doSend) SendGameData();
        }

        public static void SendGameData([CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SendGameData is called from {callerMethodName}");
            foreach (var playerinfo in GameData.Instance.AllPlayers)
            {
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
                // 書き込み {}は読みやすさのためです。
                writer.StartMessage(5); //0x05 GameData
                {
                    writer.Write(AmongUsClient.Instance.GameId);
                    writer.StartMessage(1); //0x01 Data
                    {
                        writer.WritePacked(playerinfo.NetId);
                        playerinfo.Serialize(writer, true);
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();

                AmongUsClient.Instance.SendOrDisconnect(writer);
                writer.Recycle();
            }
        }
        public static void OnDisconnect(NetworkedPlayerInfo player)
        {
            // 実行条件: クライアントがホストである, IsDeadが上書きされている, playerが切断済み
            if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
            isDeadCache[player.PlayerId] = (true, true);
            player.IsDead = player.Disconnected = false;
            SendGameData();
        }
        public static void SetRole(VoteResult? result = null)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                IsSet = true;
                var ImpostorId = PlayerControl.LocalPlayer.PlayerId;
                if (result?.Exiled?.PlayerId == PlayerControl.LocalPlayer.PlayerId || !PlayerControl.LocalPlayer.IsAlive())
                {
                    byte[] DontImpostrTargetIds = [PlayerControl.LocalPlayer.PlayerId, (result?.Exiled?.PlayerId ?? byte.MaxValue)];
                    var impostortarget = PlayerCatch.AllAlivePlayerControls.Where(pc => !DontImpostrTargetIds.Contains(pc.PlayerId)).FirstOrDefault();
                    ImpostorId = impostortarget == null ?
                                PlayerCatch.AllPlayerControls?.FirstOrDefault()?.PlayerId ?? PlayerControl.LocalPlayer.PlayerId : impostortarget.PlayerId;
                }
                foreach (var target in PlayerCatch.AllPlayerControls)
                {
                    if (target == null) continue;
                    foreach (var seer in PlayerCatch.AllPlayerControls)
                    {
                        if (seer.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                        target.RpcSetRoleDesync(target.PlayerId == ImpostorId ? RoleTypes.Impostor : RoleTypes.Crewmate, seer.GetClientId());
                        //Logger.Info($"{seer?.Data?.PlayerName ?? "null"} => {target?.Data?.PlayerName} ({(target.PlayerId == ImpostorId ? "Impostor" : "Crewmate")})", "AntiSetRole");
                    }
                }

            }
        }

        ///<summary>
        ///一時的にIsDeadを本来のものに戻した状態でコードを実行します
        ///<param name="action">実行内容</param>
        ///</summary>
        public static void TempRestore(Action action)
        {
            logger.Info("==Temp Restore==");
            //IsDeadが上書きされた状態でTempRestoreが実行されたかどうか
            bool before_IsCached = IsCached;
            try
            {
                if (before_IsCached) RestoreIsDead(doSend: false);
                action();
            }
            catch (Exception ex)
            {
                logger.Warn("AntiBlackout.TempRestore内で例外が発生しました");
                logger.Exception(ex);
            }
            finally
            {
                if (before_IsCached) SetIsDead(doSend: false);
                logger.Info("==/Temp Restore==");
            }
        }

        [GameModuleInitializer]
        public static void Reset()
        {
            logger.Info("==Reset==");
            if (isDeadCache == null) isDeadCache = new();
            //if (RoleTypeCache == null) RoleTypeCache = new();
            isDeadCache.Clear();
            //RoleTypeCache.Clear();
            IsCached = false;
            IsSet = false;
        }
    }
}
