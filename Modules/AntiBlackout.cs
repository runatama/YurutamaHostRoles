using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        ///<summary>
        ///追放処理を上書きするかどうか
        ///</summary>
        public static bool OverrideExiledPlayer => Main.AllPlayerControls.Count() < 4 && !ModClientOnly && (Options.NoGameEnd.GetBool() || GetA()) && (Main.DebugAntiblackout || !DebugModeManager.EnableDebugMode.GetBool());
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

        private static bool ModClientOnly//全員ModClient
            => Main.AllPlayerControls.Where(pc => pc.IsModClient()).Count() == Main.AllPlayerControls.Count();

        public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SetIsDead is called from {callerMethodName}");
            if (IsCached)
            {
                logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。");
                return;
            }
            isDeadCache.Clear();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
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

        /*public static void SetRole(PlayerControl Exiled, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SetRole is called from {callerMethodName}");
            if (!IsSetRoleRequired(out int Imp, out int Crew, out int aliveImp, out int aliveCrew, Exiled?.GetCustomRole().IsImpostor())) return;

            if (IsSet)
            {
                logger.Info("再度SetRoleを実行する前に、RestoreSetRoleを実行してください。");
                return;
            }

            var sender = CustomRpcSender.Create("[AntiBlackout] SetRole").StartMessage();

            if (Exiled)
            {
                if (aliveImp < 1 || Exiled.GetCustomRole().GetRoleInfo().IsDesyncImpostor)
                {
                    if (Exiled.GetCustomRole().IsImpostor())
                    {
                        var roleSwapped = false;
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            if (pc.GetCustomRole().GetRoleInfo().IsDesyncImpostor) continue;
                            sender.RpcSetRole(Exiled, RoleTypes.Crewmate, pc.GetClientId());
                            if (!roleSwapped && pc != Exiled)
                            { roleSwapped = true; sender.RpcSetRole(pc, RoleTypes.Impostor, Exiled.GetClientId()); }
                        }
                    }
                    else
                    {
                        sender.RpcSetRole(Exiled, RoleTypes.Crewmate, Exiled.GetClientId());
                        sender.RpcSetRole(Main.AllPlayerControls.Where(pc => !Exiled).First(), RoleTypes.Impostor, Exiled.GetClientId());
                    }
                }
            }
            if ((aliveImp >= aliveCrew || Imp >= Crew) && Imp > 1)
            {
                var imp = Main.AllPlayerControls.Where(pc => pc.GetCustomRole().IsImpostor()).Take(Imp - 1);
                foreach (var pc in imp)
                    sender.RpcSetRole(pc, RoleTypes.Crewmate);
            }

            IsSet = true;
            sender.EndMessage();
            sender.SendMessage();
        }

        public static void RestoreSetRole([CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"RestoreSetRole is called from {callerMethodName}");
            if (!IsSet) return;

            var sender = CustomRpcSender.Create("[AntiBlackout] RestoreSetRole").StartMessage();

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.GetCustomRole().GetRoleInfo().IsDesyncImpostor)
                {
                    foreach (var seer in Main.AllAlivePlayerControls)
                        sender.RpcSetRole(pc, seer == PlayerControl.LocalPlayer ? RoleTypes.Crewmate : pc == seer ? pc.GetCustomRole().GetRoleTypes() : RoleTypes.Scientist, seer.GetClientId());
                }
                else if (pc.GetCustomRole().IsImpostor())
                {
                    foreach (var seer in Main.AllAlivePlayerControls)
                        sender.RpcSetRole(pc, seer.GetCustomRole().GetRoleInfo().IsDesyncImpostor ? RoleTypes.Crewmate : pc.GetCustomRole().GetRoleTypes(), seer.GetClientId());
                }
                else sender.RpcSetRole(pc, pc.GetCustomRole().GetRoleTypes());
            }
            sender.EndMessage();
            sender.SendMessage();
        }*/

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
