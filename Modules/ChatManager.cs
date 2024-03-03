using System.Collections.Generic;
using System.Linq;
using Hazel;
using System;
using TownOfHost.Roles.Core;

namespace TownOfHost.Modules.ChatManager
{
    public class ChatManager
    {
        public static bool cancel = false;
        private static List<string> chatHistory = new();
        private const int maxHistorySize = 20;
        public static bool CheckCommond(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            for (int i = 0; i < comList.Count(); i++)
            {
                if (exact)
                {
                    if (msg == "/" + comList[i]) return true;
                }
                else
                {
                    if (msg.StartsWith("/" + comList[i]))
                    {
                        msg = msg.Replace("/" + comList[i], string.Empty);
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool CheckName(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            foreach (var com in comList)
            {
                if (exact)
                {
                    if (msg.Contains(com))
                    {
                        return true;
                    }
                }
                else
                {
                    int index = msg.IndexOf(com);
                    if (index != -1)
                    {
                        msg = msg.Remove(index, com.Length);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void SendMessage(PlayerControl player, string message)
        {
            int operate = 0; // 1:ID 2:猜测
            string msg = message;
            string playername = player.GetNameWithRole();
            message = message.ToLower().TrimStart().TrimEnd();
            if (!player.IsAlive()) return; //|| !AmongUsClient.Instance.AmHost
            if (GameStates.IsInGame) operate = 3;
            if (CheckCommond(ref msg, "id")) operate = 1;
            else if (CheckCommond(ref msg, "bt", false)) operate = 2;
            if (operate == 1)
            {
                Logger.Info($"これは記録しない", "ChatManager");
                message = msg;
                cancel = true;
            }
            else if (operate == 2)
            {
                Logger.Info($"{msg}は記録しない", "ChatManager");
                message = msg;
                cancel = false;
            }
            else if (operate == 4)
            {
                Logger.Info($"{msg}，は記録しない", "ChatManager");
                message = msg;
                SendPreviousMessagesToAll();
            }
            else if (operate == 3)
            {
                message = msg;
                string chatEntry = $"{player.PlayerId}: {message}";
                chatHistory.Add(chatEntry);

                if (chatHistory.Count > maxHistorySize)
                {
                    chatHistory.RemoveAt(0);
                }
                cancel = false;
            }
        }

        public static void SendPreviousMessagesToAll()
        {
            var rd = IRandom.Instance;
            string msg;
            List<CustomRoles> roles = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().ToList();
            string[] specialTexts = new string[] { "bt" };

            for (int i = chatHistory.Count; i < 30; i++)
            {
                msg = "/";
                if (rd.Next(1, 100) < 20)
                {
                    msg += "id";
                }
                else
                {
                    msg += specialTexts[rd.Next(0, specialTexts.Length - 1)];
                    msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                    msg += rd.Next(0, 15).ToString();
                    msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                    CustomRoles role = roles[rd.Next(0, roles.Count)];
                    msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                    msg += Utils.GetRoleName(role);
                }

                var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                writer.StartMessage(-1);
                writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();
            }

            foreach (var entry in chatHistory)
            {
                var entryParts = entry.Split(':');
                var senderId = entryParts[0].Trim();
                var senderMessage = entryParts[1].Trim();

                foreach (var senderPlayer in Main.AllPlayerControls)
                {
                    if (senderPlayer.PlayerId.ToString() == senderId)
                    {
                        if (!senderPlayer.IsAlive())
                        {
                            //var deathReason = (PlayerState.DeathReason)senderPlayer.PlayerId;
                            senderPlayer.Revive();


                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);

                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(-1);
                            writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(senderMessage)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                            senderPlayer.Die(DeathReason.Kill, true);
                            //Main.PlayerStates[senderPlayer.PlayerId].deathReason = deathReason;
                        }
                        else
                        {
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);
                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(-1);
                            writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(senderMessage)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                        }
                    }
                }
            }
        }
    }
}