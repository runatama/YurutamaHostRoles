using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Hazel;
using InnerNet;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;

using static TownOfHost.Translator;

namespace TownOfHost
{
    static class ExtendedPlayerControl
    {
        /// <summary>
        /// 役職変える奴。
        /// </summary>
        /// <param name="player">対象者</param>
        /// <param name="role">変更する役職</param>
        /// <param name="setRole">基本trueにしよう()</param>
        /// <param name="log">true→役職上書き null→役職変更表示 false→なんもしない</param>
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role, bool setRole = true, bool? log = false)
        {
            if (player.GetCustomRole() == role) return;
            var roleClass = player.GetRoleClass();
            var roleInfo = role.GetRoleInfo();
            if (role < CustomRoles.NotAssigned)
            {
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && GameStates.AfterIntro) Main.showkillbutton = false;

                if (roleClass != null)
                {
                    roleClass.Dispose();
                    CustomRoleManager.CreateInstance(role, player);
                }
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

                if (log == true) UtilsGameLog.LastLogRole[player.PlayerId] = "<b> " + Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString($"{role}")) + "</b>";
                else if (log == null) UtilsGameLog.LastLogRole[player.PlayerId] = $"<size=40%>{UtilsGameLog.LastLogRole[player.PlayerId].RemoveSizeTags()}</size><b>=> " + Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString($"{role}")) + "</b>";

                if (!SuddenDeathMode.NowSuddenDeathMode) NameColorManager.RemoveAll(player.PlayerId);

                //マッドメイトの最初からの内通
                if (role.IsMadmate() && Options.MadCanSeeImpostor.GetBool())
                {
                    if (PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor).Any())
                        foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                        {
                            var iste = PlayerState.GetByPlayerId(imp.Key);
                            if (iste.TargetColorData.ContainsKey(player.PlayerId)) NameColorManager.Remove(player.PlayerId, imp.Key);
                            NameColorManager.Add(player.PlayerId, imp.Key, "ff1919");
                        }
                }
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
            {
                if (role.IsGhostRole()) PlayerState.GetByPlayerId(player.PlayerId).SetGhostRole(role);
                else PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (role < CustomRoles.NotAssigned)
                {
                    if (setRole)
                    {
                        if (AntiBlackout.IsSet)
                        {
                            // 暗転対策が動作している状態なのであれば役職変更したら不味い。
                            new LateTask(() =>
                            {
                                SetRole();
                            }, 10, "ExSetRole");
                        }
                        else
                        {
                            SetRole();
                        }
                    }
                }

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.None, -1);
                writer.Write(player.PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                player.SyncSettings();

                if (GameStates.IsInTask && !GameStates.Meeting && GameStates.AfterIntro && (role.IsGhostRole() || role < CustomRoles.NotAssigned))
                {
                    player.SetKillCooldown(delay: true, kyousei: true, kousin: true);
                    player.RpcResetAbilityCooldown();
                    UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                    (roleClass as IUseTheShButton)?.Shape(player);
                    (roleClass as IUsePhantomButton)?.Init(player);
                    if (Options.Onlyseepet.GetBool()) PlayerCatch.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
                    foreach (var r in CustomRoleManager.AllActiveRoles.Values)
                    {
                        r.Colorchnge();
                    }
                }
            }
            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && GameStates.AfterIntro && role < CustomRoles.NotAssigned)
            {
                CustomButtonHud.BottonHud(true);
                _ = new LateTask(() => Main.showkillbutton = true, 0.02f, "", true);
            }

            void SetRole()
            {
                //会議中なら処理しない
                if (GameStates.Meeting) return;

                if (roleInfo?.IsDesyncImpostor ?? false || SuddenDeathMode.NowSuddenDeathMode)
                {
                    var sender = CustomRpcSender.Create("SetCustomRole", SendOption.Reliable);
                    sender.StartMessage();
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            player.StartCoroutine(player.CoSetRole(RoleTypes.Crewmate, Main.SetRoleOverride));
                            if (player != pc)
                            {
                                sender.AutoStartRpc(pc.NetId, RpcCalls.SetRole, player.GetClientId())
                                .Write((ushort)RoleTypes.Scientist)
                                .Write(true)
                                .EndRpc();
                            }
                        }
                        else
                        {
                            sender.AutoStartRpc(player.NetId, RpcCalls.SetRole, pc.GetClientId())
                            .Write((ushort)(pc.PlayerId == player.PlayerId ? role.GetRoleTypes() : RoleTypes.Crewmate))
                            .Write(true)
                            .EndRpc();
                            if (player != pc)
                            {
                                sender.AutoStartRpc(pc.NetId, RpcCalls.SetRole, player.GetClientId())
                                .Write((ushort)RoleTypes.Scientist)
                                .Write(true)
                                .EndRpc();
                            }
                        }
                    }
                    sender.EndMessage();
                    sender.SendMessage();
                    if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        if ((roleInfo?.IsDesyncImpostor ?? false || SuddenDeathMode.NowSuddenDeathMode) && roleInfo?.BaseRoleType.Invoke() != RoleTypes.Impostor)
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, roleInfo?.BaseRoleType.Invoke() ?? RoleTypes.Crewmate);
                    }
                }
                else
                {
                    player.RpcSetRole(role.GetRoleTypes(), Main.SetRoleOverride);
                }
                if (roleInfo?.IsCantSeeTeammates == true && role.IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode)
                {
                    var clientId = player.GetClientId();
                    foreach (var killer in PlayerCatch.AllPlayerControls)
                    {
                        if (!killer.GetCustomRole().IsImpostor()) continue;
                        //Amnesiac視点インポスターをクルーにする
                        killer.RpcSetRoleDesync(RoleTypes.Scientist, clientId);
                    }
                }
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc == null) continue;
                    if (pc.IsAlive()) continue;

                    pc.RpcExileV2();
                }
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.None, -1);
                writer.Write(PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        public static void RpcExile(this PlayerControl player)
        {
            RPC.ExileAsync(player);
        }
        public static InnerNet.ClientData GetClient(this PlayerControl player)
        {
            if (player.isDummy) return null;
            if (!player) return null;
            var client = AmongUsClient.Instance?.allClients?.ToArray()?.Where(cd => cd?.Character?.PlayerId == player?.PlayerId)?.FirstOrDefault() ?? null;
            return client;
        }
        public static int GetClientId(this PlayerControl player)
        {
            if (player.isDummy) return -1;
            var client = player?.GetClient();
            if (client == null) Logger.Error($"{player?.Data?.PlayerName ?? "null"}のclientがnull", "GetClientId");
            return client == null ? -1 : client.Id;
        }
        public static CustomRoles GetCustomRole(this NetworkedPlayerInfo player)
        {
            return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
        }
        /// <summary>
        /// ※サブロールは取得できません。
        /// </summary>
        public static CustomRoles GetCustomRole(this PlayerControl player)
        {
            if (player == null)
            {
                var caller = new System.Diagnostics.StackFrame(1, false);
                var callerMethod = caller.GetMethod();
                string callerMethodName = callerMethod.Name;
                string callerClassName = callerMethod.DeclaringType.FullName;
                Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
                return CustomRoles.Crewmate;
            }
            var state = PlayerState.GetByPlayerId(player.PlayerId);

            return state?.MainRole ?? CustomRoles.Crewmate;
        }

        public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
        {
            if (player == null)
            {
                Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
                return new() { CustomRoles.NotAssigned };
            }
            else
                return PlayerState.GetByPlayerId(player.PlayerId)?.SubRoles ?? new() { CustomRoles.NotAssigned };
        }
        public static CountTypes GetCountTypes(this PlayerControl player)
        {
            if (player == null)
            {
                var caller = new System.Diagnostics.StackFrame(1, false);
                var callerMethod = caller.GetMethod();
                string callerMethodName = callerMethod.Name;
                string callerClassName = callerMethod.DeclaringType.FullName;
                Logger.Warn(callerClassName + "." + callerMethodName + "がCountTypesを取得しようとしましたが、対象がnullでした。", "GetCountTypes");
                return CountTypes.None;
            }

            return PlayerState.GetByPlayerId(player.PlayerId)?.CountType ?? CountTypes.None;
        }
        public static void RpcSetNameEx(this PlayerControl player, string name)
        {
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            }
            HudManagerPatch.LastSetNameDesyncCount++;

            Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
            player.RpcSetName(name);
        }

        public static bool SetNameCheck(this PlayerControl player, string name, PlayerControl seer = null, bool force = false)
        {
            if (seer == null) seer = player;

            if (Main.LastNotifyNames is null)
                Main.LastNotifyNames = new();

            if (!Main.LastNotifyNames.ContainsKey((player.PlayerId, seer.PlayerId)))
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = "nulldao"; //nullチェック

            if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
            {
                return false;
            }
            {
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
                if (!GameStates.IsLobby) HudManagerPatch.LastSetNameDesyncCount++;
            }

            return true;
        }
        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if (seer == null) seer = player;

            if (!player.SetNameCheck(name, seer, force)) return;

            var clientId = seer.GetClientId();
            if (clientId == -1) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.None, clientId);
            writer.Write(player.Data.NetId);
            writer.Write(name);
            writer.Write(DontShowOnModdedClient);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId, SendOption sendoption = SendOption.Reliable)
        {
            if (clientId == -1)
            {
                Logger.Error($"clientIdが-1!", "RpcSetRoleDesync");
                return;
            }
            //player: ロールの変更対象

            if (player == null) return;

            //Logger.Info($"{player?.Data?.PlayerName ?? "( ᐛ )"} =>  {role}", "RpcSetRoleDesync");

            if (AmongUsClient.Instance.ClientId == clientId)
            {
                player.StartCoroutine(player.CoSetRole(role, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard));
                return;
            }
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, sendoption, clientId);
            writer.Write((ushort)role);
            writer.Write(Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SetKillCooldown(this PlayerControl player, float time = -1f, PlayerControl target = null, bool kyousei = false, bool delay = false, bool kousin = true, bool PB = false)
        {
            if (player == null) return;
            if (!PB)
            {
                (player.GetRoleClass() as IUsePhantomButton)?.Init(player);
            }
            if (target == null) target = player;
            CustomRoles role = player.GetCustomRole();
            if (!player.CanUseKillButton() && !kyousei) return;

            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) CustomButtonHud.BottonHud();

            if (!Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId))
            {
                player.ResetKillCooldown();
            }
            if (time >= 0f)
            {
                Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
            }
            else
            {
                Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
            }
            player.SyncSettings();

            if (!delay)
            {
                player.RpcProtectedMurderPlayer(target);
                if (player != target) player.RpcProtectedMurderPlayer();
                if (kousin)
                    _ = new LateTask(() =>
                        {
                            player.ResetKillCooldown();
                            player.SyncSettings();
                        }, 1f, "", true);
            }
            else
            {
                _ = new LateTask(() =>
                {
                    player.RpcProtectedMurderPlayer(target);
                    if (player != target) player.RpcProtectedMurderPlayer();
                    if (kousin)
                        _ = new LateTask(() =>
                        {
                            player.ResetKillCooldown();
                            player.SyncSettings();
                        }, 1f, "", true);
                }, Main.LagTime, "Setkillcooldown delay", true);
            }
        }
        public static void RpcMeetingKill(this PlayerControl killer, PlayerControl target = null)
        {
            if (!GameStates.InGame) return;
            if (target == null) target = killer;

            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, target.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)MurderResultFlags.Succeeded);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            if (!GameStates.InGame) return;
            if (target == null) target = killer;
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target, MurderResultFlags.Succeeded);
            }
            else
            {
                MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, killer.GetClientId());
                messageWriter.WriteNetObject(target);
                messageWriter.Write((int)MurderResultFlags.Succeeded);
                AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            }
        }
        [Obsolete]
        public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (AmongUsClient.Instance.AmClient)
            {
                killer.ProtectPlayer(target, colorId);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write(colorId);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcResetAbilityCooldownAllPlayer()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            UtilsOption.SyncAllSettings();
            _ = new LateTask(() =>
            {
                var sender = CustomRpcSender.Create("AllplayerresetAbility");
                sender.StartMessage();
                foreach (var target in PlayerCatch.AllPlayerControls)
                {
                    if (PlayerControl.LocalPlayer == target)
                    {
                        //targetがホストだった場合
                        PlayerControl.LocalPlayer?.Data?.Role?.SetCooldown();
                    }
                    else
                    {
                        //targetがホスト以外だった場合
                        sender.AutoStartRpc(target.NetId, RpcCalls.ProtectPlayer, target.GetClientId())
                        .WriteNetObject(target)
                        .Write(0)
                        .EndRpc();
                    }
                }
                sender.EndMessage();
                sender.SendMessage();
                Main.CanUseAbility = true;
            }
            , 0.2f, "AllPlayerResetAbilityCoolDown", null);
        }
        public static void RpcResetAbilityCooldown(this PlayerControl target, bool log = true, bool kousin = false)
        {
            if (target == null || !AmongUsClient.Instance.AmHost)
            {
                return;
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            if (kousin) target.SyncSettings();
            if (log) Logger.Info($"アビリティクールダウンのリセット:{target?.name ?? "ﾇﾙﾎﾟｯ"}({target?.PlayerId ?? 334})", "RpcResetAbilityCooldown");

            if (kousin)
                _ = new LateTask(() =>
                {
                    if (PlayerControl.LocalPlayer == target)
                    {
                        //targetがホストだった場合
                        PlayerControl.LocalPlayer?.Data?.Role?.SetCooldown();
                    }
                    else
                    {
                        //targetがホスト以外だった場合
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
                        writer.WriteNetObject(target);
                        writer.Write(0);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                }, Main.LagTime, "abilityrset", null);
            else
            {
                if (PlayerControl.LocalPlayer == target)
                {
                    //targetがホストだった場合
                    PlayerControl.LocalPlayer?.Data?.Role?.SetCooldown();
                }
                else
                {
                    //targetがホスト以外だった場合
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
                    writer.WriteNetObject(target);
                    writer.Write(0);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            //更新があるなら2f後
            /*
                プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
                ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
                この変更により、役職としての守護天使が無効化されます。
                ホストのクールダウンは直接リセットします。
            */
        }
        public static void RpcSpecificShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (player.PlayerId == 0)
            {
                player.Shapeshift(target, shouldAnimate);
                return;
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Shapeshift, SendOption.None, player.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write(shouldAnimate);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSpecificRejectShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (seer != player)
                {
                    MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.RejectShapeshift, SendOption.None, seer.GetClientId());
                    AmongUsClient.Instance.FinishRpcImmediately(msg);
                }
                else
                {
                    player.RpcSpecificShapeshift(target, shouldAnimate);
                }
            }
        }
        public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
        {
            if (target == null) return;
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.None, target.GetClientId());
            messageWriter.Write((byte)systemType);
            messageWriter.WriteNetObject(target);
            messageWriter.Write((byte)amount);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }

        public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, MessageWriter msgWriter)
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.None, target.GetClientId());
            messageWriter.Write((byte)systemType);
            messageWriter.WriteNetObject(PlayerControl.LocalPlayer);
            messageWriter.Write(msgWriter, false);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void MarkDirtySettings(this PlayerControl player)
        {
            PlayerGameOptionsSender.SetDirty(player.PlayerId);
        }
        public static void SyncSettings(this PlayerControl player)
        {
            PlayerGameOptionsSender.SetDirty(player.PlayerId);
            GameOptionsSender.SendAllGameOptions();
        }
        public static TaskState GetPlayerTaskState(this PlayerControl player)
        {
            return PlayerState.GetByPlayerId(player.PlayerId).GetTaskState();
        }
        public static PlayerState GetPlayerState(this PlayerControl player)
        {
            return PlayerState.GetByPlayerId(player.PlayerId);
        }
        public static string GetSubRoleName(this PlayerControl player)
        {
            var SubRoles = PlayerState.GetByPlayerId(player.PlayerId).SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role == CustomRoles.NotAssigned) continue;
                sb.Append($"{Utils.ColorString(Color.white, " + ")}{UtilsRoleText.GetRoleName(role)}");
            }

            return sb.ToString();
        }
        public static string GetAllRoleName(this PlayerControl player)
        {
            if (!player) return null;
            var text = UtilsRoleText.GetRoleName(player.GetCustomRole());
            text += player.GetSubRoleName();
            return text;
        }
        public static string GetNameWithRole(this PlayerControl player)
        {
            return $"{player?.Data?.PlayerName}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName()})" : "");
        }
        public static string GetRoleColorCode(this PlayerControl player)
        {
            return UtilsRoleText.GetRoleColorCode(player.GetCustomRole());
        }
        public static Color GetRoleColor(this PlayerControl player)
        {
            var roleClass = player.GetRoleClass();
            var role = player.GetCustomRole();
            if (role is CustomRoles.Amnesiac && Amnesiac.iamwolf) return UtilsRoleText.GetRoleColor(CustomRoles.WolfBoy);
            if (roleClass?.Jikaku() is not null and not CustomRoles.NotAssigned)
            {
                return UtilsRoleText.GetRoleColor(roleClass.Jikaku());
            }
            if (player.Is(CustomRoles.Amnesia))
            {
                switch (role.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor: return Palette.ImpostorRed;
                    case CustomRoleTypes.Neutral:
                    case CustomRoleTypes.Madmate: return Palette.DisabledGrey;
                    case CustomRoleTypes.Crewmate: return Palette.CrewmateBlue;
                }
            }
            return UtilsRoleText.GetRoleColor(role);
        }
        public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner || GameStates.IsLobby) return;

            var systemtypes = Utils.GetCriticalSabotageSystemType();
            _ = new LateTask(() =>
            {
                pc.RpcDesyncUpdateSystem(systemtypes, 128);
            }, 0f + delay, "Reactor Desync");

            _ = new LateTask(() =>
            {
                pc.RpcSpecificMurderPlayer();
            }, 0.3f + delay, "Murder To Reset Cam");

            _ = new LateTask(() =>
            {
                pc.RpcDesyncUpdateSystem(systemtypes, 16);
                if (Main.NormalOptions.MapId == 4) //Airship用
                    pc.RpcDesyncUpdateSystem(systemtypes, 17);
            }, 0.4f + delay, "Fix Desync Reactor");
        }
        public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || GameStates.IsLobby) return;
            int clientId = pc.GetClientId();
            // Logger.Info($"{pc}", "ReactorFlash");
            var systemtypes = Utils.GetCriticalSabotageSystemType();
            float FlashDuration = Options.KillFlashDuration.GetFloat();

            Utils.NowKillFlash = true;
            pc.RpcDesyncUpdateSystem(systemtypes, 128);

            _ = new LateTask(() =>
            {
                pc.RpcDesyncUpdateSystem(systemtypes, 16);

                if (Main.NormalOptions.MapId == 4) //Airship用
                    pc.RpcDesyncUpdateSystem(systemtypes, 17);
            }, FlashDuration + delay, "Fix Desync Reactor");
            _ = new LateTask(() => Utils.NowKillFlash = false, (FlashDuration + delay) * 2, "", true);

        }

        public static string GetRealName(this PlayerControl player, bool isMeeting = false)
        {
            return isMeeting ? player?.Data?.PlayerName : player?.name;
        }
        public static bool CanUseKillButton(this PlayerControl pc)
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && !Main.showkillbutton && Main.CustomSprite.Value) return false;
            if (!pc.IsAlive()) return false;
            if (pc?.Data?.Role?.Role == RoleTypes.GuardianAngel) return false;

            if (pc.Is(CustomRoles.Amnesia) && !pc.Is(CustomRoleTypes.Impostor)) return false;

            var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseKillButton();

            if (pc.Is(CustomRoles.SlowStarter) && !pc.Is(CustomRoles.Mafia))
            {
                roleCanUse = SlowStarter.CanUseKill();
            }

            return roleCanUse ?? pc.Is(CustomRoleTypes.Impostor);
        }
        public static bool CanUseImpostorVentButton(this PlayerControl pc)
        {
            if (!pc.IsAlive()) return false;

            if (pc.Is(CustomRoles.Amnesia) && !pc.Is(CustomRoleTypes.Impostor)) return false;

            var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseImpostorVentButton();

            return roleCanUse ?? false;
        }
        public static bool CanUseSabotageButton(this PlayerControl pc)
        {
            if (SuddenDeathMode.NowSuddenDeathMode) return false;
            if (pc.Is(CustomRoles.Amnesia) && !pc.Is(CustomRoleTypes.Impostor)) return false;

            var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseSabotageButton();

            return roleCanUse ?? false;
        }
        public static void ResetKillCooldown(this PlayerControl player)
        {
            if (!Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId)) Main.AllPlayerKillCooldown.Add(player.PlayerId, Options.DefaultKillCooldown);
            Main.AllPlayerKillCooldown[player.PlayerId] = (player.GetRoleClass() as IKiller)?.CalculateKillCooldown() ?? Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
            if (player.Is(CustomRoles.Serial))
                Main.AllPlayerKillCooldown[player.PlayerId] = Serial.KillCooldown.GetFloat();
            if (player.PlayerId == LastImpostor.currentId)
                LastImpostor.SetKillCooldown(player);
            if (player.PlayerId == LastNeutral.currentId)
                LastNeutral.SetKillCooldown(player);

            if (player.Is(CustomRoles.Amnesia) && Amnesia.defaultKillCool.GetBool()) Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown;
        }
        public static bool CanMakeMadmate(this PlayerControl player)
        {
            if (Amnesia.CheckAbilityreturn(player)) return false;
            var role = player.GetCustomRole();

            if (
            Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount ||
            player == null ||
            (player.Data.Role.Role != RoleTypes.Shapeshifter) || role.GetRoleInfo()?.BaseRoleType.Invoke() != RoleTypes.Shapeshifter)
            {
                return false;
            }

            var isSidekickableCustomRole = player.GetRoleClass() is ISidekickable sidekickable && sidekickable.CanMakeSidekick();

            return isSidekickableCustomRole ||
               role.CanMakeMadmate(); // ISideKickable対応前の役職はこちら
        }
        public static void RpcExileV2(this PlayerControl player)
        {
            if (player == null) return;
            player.Exiled();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void MurderPlayer(this PlayerControl killer, PlayerControl target)
        {
            killer.MurderPlayer(target, SucceededFlags);
        }
        public const MurderResultFlags SucceededFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
        public static void RpcMurderPlayer(this PlayerControl killer, PlayerControl target)
        {
            killer.RpcMurderPlayer(target, true);
        }
        public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
        {
            if (target == null) target = killer;
            if (AmongUsClient.Instance.AmClient)
            {
                killer.MurderPlayer(target, SuccessFlags);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)SuccessFlags);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            UtilsNotifyRoles.NotifyRoles();
        }
        public static void RpcProtectedMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            //killerが死んでいる場合は実行しない
            if (!killer.IsAlive()) return;

            if (target == null) target = killer;
            // Host
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
            }
            // Other Clients
            if (killer.PlayerId != 0)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, killer.GetClientId());
                writer.WriteNetObject(target);
                writer.Write((int)MurderResultFlags.FailedProtected);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcProtectedMurderPlayer(this PlayerControl killer, PlayerControl seer, PlayerControl target = null)
        {
            if (seer == null) return;
            //killerが死んでいる場合は実行しない
            if (!killer.IsAlive()) return;

            if (target == null) target = killer;
            // Host
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
            }
            // Other Clients
            if (killer.PlayerId != 0)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, seer.GetClientId());
                writer.WriteNetObject(target);
                writer.Write((int)MurderResultFlags.FailedProtected);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        /*サボタージュ中でも関係なしに会議を起こせるメソッド
        targetがnullの場合はボタンとなる*/
        ///基本<see cref="ReportDeadBodyPatch.DieCheckReport"/>を使用する。
        public static void NoCheckStartMeeting(this PlayerControl reporter, NetworkedPlayerInfo target)
        {
            UtilsNotifyRoles.MeetingMoji = GetString("MI.Kyousei");
            GameStates.Meeting = true;
            GameStates.task = false;
            UtilsNotifyRoles.NotifyMeetingRoles();
            MeetingRoomManager.Instance.AssignSelf(reporter, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
            reporter.RpcStartMeeting(target);
        }
        public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
        ///</summary>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
        ///</summary>
        ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
        {
            var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
            List<PlayerControl> rangePlayers = new();
            player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
            foreach (var pc in rangePlayersIL)
            {
                if (predicate(pc)) rangePlayers.Add(pc);
            }
            return rangePlayers;
        }
        public static PlayerControl killtarget(this PlayerControl pc, bool IsOneclick = false)//SNR参考!(((
        {
            float killdis = GameOptionsData.KillDistances[Mathf.Clamp(GameManager.Instance.LogicOptions.currentGameOptions.GetInt(Int32OptionNames.KillDistance), 0, 2)];

            if (pc.Data.IsDead || pc.inVent) return null;

            var roletype = pc.GetCustomRole().GetCustomRoleTypes();
            Vector2 psi = pc.GetTruePosition();
            var ta = pc;
            foreach (var playerInfo in GameData.Instance.AllPlayers)
            {
                if (playerInfo.Disconnected || playerInfo.PlayerId == pc.PlayerId || playerInfo.IsDead) continue;

                var tage = playerInfo.Object;

                if (tage == null || tage.inVent) continue;
                if (!SuddenDeathMode.NowSuddenDeathTemeMode && IsOneclick && tage.GetCustomRole().GetCustomRoleTypes() == roletype) continue;
                if (SuddenDeathMode.NowSuddenDeathTemeMode)
                {
                    if (SuddenDeathMode.IsOnajiteam(pc.PlayerId, tage.PlayerId)) continue;
                }

                var vector = tage.GetTruePosition() - psi;
                float dis = vector.magnitude;

                if (IsOneclick)//ワンクリ取得のラグが洒落にならん位えぐいから補正
                {
                    dis = Mathf.Clamp(dis - 2f, 0.01f, 99);
                }
                if (dis > killdis || PhysicsHelpers.AnyNonTriggersBetween(psi, vector.normalized, dis, Constants.ShipAndObjectsMask)) continue;
                killdis = dis;
                ta = tage;
            }
            if (ta == pc) return null;
            return ta;
        }
        public static bool IsNeutralKiller(this PlayerControl player)
        {
            if (player.Is(CustomRoles.BakeCat)) return BakeCat.CanKill;

            return
                player.GetCustomRole() is
                CustomRoles.Egoist or
                CustomRoles.Banker or
                CustomRoles.DoppelGanger or
                CustomRoles.Jackal or
                CustomRoles.JackalMafia or
                CustomRoles.JackalAlien or
                CustomRoles.Remotekiller or
                CustomRoles.CountKiller;
        }
        public static bool KnowDeathReason(this PlayerControl seer, PlayerControl seen)
        {
            // targetが生きてたらfalse
            if (seen.IsAlive())
            {
                return false;
            }
            // seerが死亡済で，霊界から死因が見える設定がON
            if (!seer.IsAlive() && (Options.GhostCanSeeDeathReason.GetBool() || !Options.GhostOptions.GetBool()) && !seer.Is(CustomRoles.AsistingAngel) && (!seer.IsGhostRole() || Options.GRCanSeeDeathReason.GetBool()))
            {
                return true;
            }

            var check = false;

            // 役職による仕分け
            if (seer.GetRoleClass() is IDeathReasonSeeable deathReasonSeeable)
            {
                if (Amnesia.CheckAbility(seer))
                {
                    var role = deathReasonSeeable.CheckSeeDeathReason(seen);
                    if (role is null) return false;
                    check |= role is true;
                }
            }

            if (seer.Is(CustomRoles.LastImpostor) && LastImpostor.GiveAutopsy.GetBool()) check |= !Utils.IsActive(SystemTypes.Comms) || LastImpostor.ACanSeeComms.GetBool();
            if (seer.Is(CustomRoles.LastNeutral) && LastNeutral.GiveAutopsy.GetBool()) check |= !Utils.IsActive(SystemTypes.Comms) || LastNeutral.ACanSeeComms.GetBool();

            if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer, subrole: CustomRoles.Autopsy))
                if (data.GiveAutopsy.GetBool()) check |= !Utils.IsActive(SystemTypes.Comms) || data.ACanSeeComms.GetBool();

            // IDeathReasonSeeable未対応役職はこちら
            return check ||
            (seer.Is(CustomRoleTypes.Madmate) && Options.MadmateCanSeeDeathReason.GetBool())
            || (seer.Is(CustomRoles.Autopsy) && (!Utils.IsActive(SystemTypes.Comms) || Autopsy.CanSeeComms.GetBool()));
        }
        public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
        {
            var roleClass = player.GetRoleClass();
            var role = player.GetCustomRole();
            if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
            if (roleClass?.Jikaku() is not null and not CustomRoles.NotAssigned)
            {
                role = roleClass.Jikaku();
            }
            if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                InfoLong = false;

            var text = role.ToString();

            var Prefix = "";
            if (!InfoLong)
                switch (role)
                {
                    case CustomRoles.Mafia:
                        if (roleClass is not Mafia mafia) break;

                        Prefix = mafia.CanUseKillButton() ? "After" : "Before";
                        break;
                    case CustomRoles.MadSnitch:
                    case CustomRoles.MadGuardian:
                        text = CustomRoles.Madmate.ToString();
                        Prefix = player.GetPlayerTaskState().IsTaskFinished ? "" : "Before";
                        break;
                }
            ;

            if (role is CustomRoles.Amnesiac)
            {
                if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                {
                    text = Amnesiac.iamwolf ? CustomRoles.WolfBoy.ToString() : CustomRoles.Sheriff.ToString();
                }
            }

            var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
            if (player.IsGhostRole())
            {
                var state = PlayerState.GetByPlayerId(player.PlayerId);
                if (state != null)
                {
                    return Utils.ColorString(UtilsRoleText.GetRoleColor(state.GhostRole), GetString($"{state.GhostRole}Info"));
                }
            }
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                var r = "<size=60%>" + GetString($"{Prefix}{text}{Info}") + "\n</size>";
                r += "<size=80%>" + GetString("SuddenDeathModeInfo") + "</size>";
                return r;
            }
            return GetString($"{Prefix}{text}{Info}");
        }
        public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
        {
            if (target == null)
            {
                Logger.Info("target=null", "SetRealKiller");
                return;
            }
            var State = PlayerState.GetByPlayerId(target.PlayerId);
            if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
            byte killerId = killer == null ? byte.MaxValue : killer.PlayerId;
            RPC.SetRealKiller(target.PlayerId, killerId);
            if (killer?.PlayerId is 0)
            {
                Main.HostKill.TryAdd(target.PlayerId, State.DeathReason);
            }
            if (!(AntiBlackout.IsCached || GameStates.Meeting || GameStates.Tuihou))
                Twins.TwinsSuicide();
        }
        public static PlayerControl GetRealKiller(this PlayerControl target)
        {
            var killerId = PlayerState.GetByPlayerId(target.PlayerId).GetRealKiller();
            return killerId == byte.MaxValue ? null : PlayerCatch.GetPlayerById(killerId);
        }
        public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc)
        {
            if (!pc.IsAlive()) return null;
            var Rooms = ShipStatus.Instance.AllRooms;
            if (Rooms == null) return null;
            foreach (var room in Rooms)
            {
                if (!room.roomArea) continue;
                if (pc.Collider.IsTouching(room.roomArea))
                    return room;
            }
            return null;
        }
        public static void RpcSnapToForced(this PlayerControl pc, Vector2 position)
        {
            var netTransform = pc.NetTransform;
            if (AmongUsClient.Instance.AmClient)
            {
                netTransform.SnapTo(position, (ushort)(netTransform.lastSequenceId + 128));
            }
            ushort newSid = (ushort)(netTransform.lastSequenceId + 2);
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
            NetHelpers.WriteVector2(position, messageWriter);
            messageWriter.Write(newSid);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSnapToDesync(this PlayerControl pc, PlayerControl target, Vector2 position)
        {
            var net = pc.NetTransform;
            var num = (ushort)(net.lastSequenceId + 2);
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(net.NetId, (byte)RpcCalls.SnapTo, SendOption.None, target.GetClientId());
            NetHelpers.WriteVector2(position, messageWriter);
            messageWriter.Write(num);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        /// <summary>
        /// 自身の色を特定の人視点のみ変更する。<br/>
        /// 前→変更対象者<br/>
        /// target→視認対象者<br/>
        /// </summary>
        /// <param name="target">見せる相手</param>
        /// <param name="Color">色</param>
        public static void RpcChColor(this PlayerControl pc, PlayerControl target, byte Color, bool Nomal = false)
        {
            if (GameStates.IsLobby) return;
            var sender = CustomRpcSender.Create("DChengeColor");
            sender.StartMessage(target.GetClientId());

            sender.StartRpc(pc.NetId, RpcCalls.SetColor)
            .Write(pc.NetId)
            .Write(Color)
            .EndRpc();

            if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                pc.SetColor(Color);
                if (Nomal)
                {
                    pc.SetSkin("", Color);
                    pc.SetHat("", Color);
                    pc.SetVisor("", Color);
                    pc.SetPet("", Color);
                }

                return;
            }

            if (Nomal)
            {
                sender.StartRpc(pc.NetId, RpcCalls.SetHatStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();
                sender.StartRpc(pc.NetId, RpcCalls.SetSkinStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();
                sender.StartRpc(pc.NetId, RpcCalls.SetVisorStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();
                sender.StartRpc(pc.NetId, RpcCalls.SetPetStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();
            }
            sender.EndMessage();
            sender.SendMessage();
        }

        public static void OnlySeeMePet(this PlayerControl pc, string petid)
        {
            //PlayerCatch.AllPlayerControls.Do(p => p.RpcSetPet("")); //ペットを全員視点消して本人視点のみ付けるRPCを送信
            //↑バニラ視点自身の消えるから戻す。
            var sender = CustomRpcSender.Create("OnlySeeMepet");
            sender.StartMessage();
            foreach (var ap in PlayerCatch.AllPlayerControls)
            {
                if (ap.GetClient() == null) continue;
                sender.AutoStartRpc(pc.NetId, RpcCalls.SetPetStr, ap.GetClientId())
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();
            }
            if (!pc.IsAlive() || pc.GetClient() == null)
            {
                sender.EndMessage();
                sender.SendMessage();
                return;
            }
            sender.AutoStartRpc(pc.NetId, RpcCalls.SetPetStr, pc.GetClientId())
            .Write(petid)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();
            sender.EndMessage();
            sender.SendMessage();

            pc.RawSetPet(pc.PlayerId == PlayerControl.LocalPlayer.PlayerId ? petid : "", pc.Data.DefaultOutfit.ColorId);
        }
        public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;

        //汎用
        public static bool Is(this PlayerControl target, CustomRoles role) =>
            role > CustomRoles.NotAssigned ? (role.IsGhostRole() ? PlayerState.GetByPlayerId(target.PlayerId).GhostRole == role : target.GetCustomSubRoles().Contains(role)) : target.GetCustomRole() == role;
        public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
        public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
        public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
        public static bool IsAlive(this PlayerControl target)
        {
            //ロビーなら生きている
            if (GameStates.IsLobby)
            {
                return true;
            }
            //targetがnullならば切断者なので生きていない
            if (target == null)
            {
                return false;
            }
            //targetがnullでなく取得できない場合は登録前なので生きているとする
            if (PlayerState.GetByPlayerId(target.PlayerId) is not PlayerState state)
            {
                return true;
            }
            if (AntiBlackout.IsCached)
            {
                if (AntiBlackout.isDeadCache.TryGetValue(target.PlayerId, out var isDead))
                    return !isDead.Disconnected && !isDead.isDead;
            }
            return !state.IsDead;
        }

        public static PlayerControl GetKillTarget(this PlayerControl player, bool IsOneclick)
        {
            var playerrole = player.GetCustomRole();

            if (IsOneclick && !player.AmOwner) return player.killtarget(true);

            if (player.AmOwner && GameStates.IsInTask && !GameStates.Intro && !(playerrole.IsImpostor() || playerrole is CustomRoles.Egoist) && (playerrole.GetRoleInfo()?.IsDesyncImpostor ?? false) && !player.Data.IsDead)
            {
                return player.killtarget();
            }
            var players = player.GetPlayersInAbilityRangeSorted(false);
            return players.Count <= 0 ? null : players[0];
        }

        //アプデ対応の参考
        //https://github.com/Hyz-sui/TownOfHost-H
        public const MurderResultFlags SuccessFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
    }
}
