using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Ghost;
using static TownOfHost.Translator;
using HarmonyLib;
using static TownOfHost.UtilsRoleText;
using System.Collections.Generic;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            if (GameStates.IsFreePlay && Main.EditMode) return false;
            // ないものはfalse
            if (!ShipStatus.Instance.Systems.ContainsKey(type))
            {
                return false;
            }
            int mapId = Main.NormalOptions.MapId;
            switch (type)
            {
                case SystemTypes.Electrical:
                    {
                        var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                        return SwitchSystem != null && SwitchSystem.IsActive;
                    }
                case SystemTypes.Reactor:
                    {
                        if (mapId == 2) return false;
                        else
                        {
                            var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                            return ReactorSystemType != null && ReactorSystemType.IsActive;
                        }
                    }
                case SystemTypes.Laboratory:
                    {
                        if (mapId != 2) return false;
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                case SystemTypes.LifeSupp:
                    {
                        if (mapId is 2 or 4) return false;
                        var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                        return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                    }
                case SystemTypes.Comms:
                    {
                        if (mapId is 1 or 5)
                        {
                            var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                            return HqHudSystemType != null && HqHudSystemType.IsActive;
                        }
                        else
                        {
                            var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                            return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                        }
                    }
                case SystemTypes.HeliSabotage:
                    {
                        var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                        return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                    }
                case SystemTypes.MushroomMixupSabotage:
                    {
                        var mushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                        return mushroomMixupSabotageSystem != null && mushroomMixupSabotageSystem.IsActive;
                    }
                default:
                    return false;
            }
        }
        /// <summary></summary>
        /// <param name="player">色表示にするプレイヤー</param>
        /// <param name="borudo">trueの場合ボールドで返します。</param>
        public static string GetPlayerColor(PlayerControl player, bool borudo = false)
        {
            if (player == null) return "";
            var name = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var N) ? N : player.Data.PlayerName;
            if (borudo) return "<b>" + ColorString(Main.PlayerColors[player.PlayerId], $"{name}</b>");
            else return ColorString(Main.PlayerColors[player.PlayerId], $"{name}");
        }
        /// <summary></summary>
        /// <param name="player">色表示にするプレイヤー</param>
        /// <param name="borudo">trueの場合ボールドで返します。</param>
        public static string GetPlayerColor(byte player, bool borudo = false)
        {
            var pc = PlayerCatch.GetPlayerById(player);
            if (pc == null) return "";
            var name = Main.AllPlayerNames.TryGetValue(player, out var N) ? N : pc.Data.PlayerName;
            if (borudo) return "<b>" + ColorString(Main.PlayerColors[player], $"{name}</b>");
            else return ColorString(Main.PlayerColors[player], $"{name}");
        }
        /// <summary></summary>
        /// <param name="player">色表示にするプレイヤー</param>
        /// <param name="borudo">trueの場合ボールドで返します。</param>
        public static string GetPlayerColor(NetworkedPlayerInfo player, bool borudo = false)
        {
            if (player == null) return "";
            var name = player.PlayerName;
            if (borudo) return "<b>" + ColorString(Main.PlayerColors[player.PlayerId], $"{name}</b>");
            else return ColorString(Main.PlayerColors[player.PlayerId], $"{name}");
        }
        public static SystemTypes GetCriticalSabotageSystemType() => (MapNames)Main.NormalOptions.MapId switch
        {
            MapNames.Polus => SystemTypes.Laboratory,
            MapNames.Airship => SystemTypes.HeliSabotage,
            _ => SystemTypes.Reactor,
        };
        //誰かが死亡したときのメソッド
        public static void TargetDies(MurderInfo info)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (!target.Data.IsDead || GameStates.IsMeeting) return;
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (KillFlashCheck(info, seer))
                {
                    seer.KillFlash();
                }
            }
        }
        public static bool KillFlashCheck(MurderInfo info, PlayerControl seer)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (seer.Is(CustomRoles.GM) ||
                Options.SuddenCanSeeKillflash.GetBool()) return true;

            if (seer.Data.IsDead && (Options.GhostCanSeeKillflash.GetBool() || !Options.GhostOptions.GetBool()) && !seer.Is(CustomRoles.AsistingAngel) && (!seer.IsGorstRole() || Options.GRCanSeeKillflash.GetBool()) && target != seer) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;

            //ラスポスで付いてるのに！とかが一応ありえる。
            var check = false;

            if (seer.GetRoleClass() is IKillFlashSeeable killFlashSeeable)
            {
                if (Amnesia.CheckAbility(seer))
                {
                    var role = killFlashSeeable.CheckKillFlash(info);
                    if (role is null) return false;
                    check |= role is true;
                }
            }


            if (seer.Is(CustomRoles.LastImpostor) && LastImpostor.Giveseeing.GetBool()) check |= !IsActive(SystemTypes.Comms) || LastImpostor.SCanSeeComms.GetBool();
            if (seer.Is(CustomRoles.LastNeutral) && LastNeutral.Giveseeing.GetBool()) check |= !IsActive(SystemTypes.Comms) || LastNeutral.SCanSeeComms.GetBool();

            if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer, subrole: CustomRoles.seeing))
                if (data.Giveseeing.GetBool()) check |= !IsActive(SystemTypes.Comms) || data.SCanSeeComms.GetBool();

            return check || seer.GetCustomRole() switch
            {
                // IKillFlashSeeable未適用役職はここに書く
                _ => (seer.Is(CustomRoleTypes.Madmate) && Options.MadmateCanSeeKillFlash.GetBool())
                || (seer.Is(CustomRoles.seeing) && (!IsActive(SystemTypes.Comms) || seeing.CanSeeComms.GetBool()))
            };
        }
        public static bool NowKillFlash = false;
        public static void KillFlash(this PlayerControl player, bool sound = true, bool kiai = false)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = IsActive(GetCriticalSabotageSystemType());

            var Duration = Options.KillFlashDuration.GetFloat();
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            if (!kiai) state.IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0 && !kiai)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx() && sound) RPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            player.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                state.IsBlackOut = false; //ブラックアウト解除
                player.MarkDirtySettings();
            }, Options.KillFlashDuration.GetFloat(), "RemoveKillFlash");
        }
        public static void AllPlayerKillFlash()
        {
            if (SuddenDeathMode.NowSuddenDeathMode) return;

            var systemtypes = GetCriticalSabotageSystemType();
            ShipStatus.Instance.RpcUpdateSystem(systemtypes, 128);

            NowKillFlash = true;
            _ = new LateTask(() =>
            {
                ShipStatus.Instance.RpcUpdateSystem(systemtypes, 16);

                if (Main.NormalOptions.MapId == 4) //Airship用            
                    ShipStatus.Instance.RpcUpdateSystem(systemtypes, 17);
            }, Options.KillFlashDuration.GetFloat(), "Fix Reactor");
            _ = new LateTask(() => NowKillFlash = false, Options.KillFlashDuration.GetFloat() * 2, "", true);
        }
        public static void BlackOut(this IGameOptions opt, bool IsBlackOut)
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            if (IsBlackOut)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
                opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
            }
            return;
        }
        public static bool CanDeathReasonKillerColor(this byte playerId)
        {
            var pc = PlayerCatch.GetPlayerById(playerId);
            if (pc == null) return false;
            var isAlive = pc.IsAlive();
            var IsGorstRole = pc.IsGorstRole();
            if (!isAlive && IsGorstRole) return Options.GRCanSeeKillerColor.GetBool();
            if (!isAlive && !IsGorstRole) return Options.GhostCanSeeKillerColor.GetBool() || !Options.GhostOptions.GetBool();
            return false;
        }
        public static string GetVitalText(byte playerId, bool? RealKillerColor = false)
        {
            var state = PlayerState.GetByPlayerId(playerId);

            if (state == null) return GetString("DeathReason.Disconnected");

            string deathReason = state.IsDead ? GetString("DeathReason." + state.DeathReason) : GetString("Alive");
            switch (RealKillerColor)
            {
                case true:
                    {
                        var KillerId = state.GetRealKiller();
                        Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : new Color32(162, 194, 230, 255);
                        deathReason = ColorString(color, deathReason);
                    }
                    break;
                case null:
                    deathReason = $"<color=#80ffdd>{deathReason}</color>";
                    break;
            }
            return deathReason;
        }
        public static string GetDeathReason(CustomDeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(CustomDeathReason), status));
        }
        public static void ShowTimer(byte PlayerId = byte.MaxValue) => SendMessage(GetTimer(), PlayerId);
        public static string GetTimer()
        {
            var sb = new StringBuilder();
            float timerValue = GameStartManagerPatch.GetTimer();
            int minutes = (int)timerValue / 60;
            int seconds = (int)timerValue % 60;
            return $"{minutes:00}:{seconds:00}";
        }
        public static void ShowHelp(byte to = 255)
        {
            var a = "";
            var send = "";
            if (Options.sotodererukomando.GetBool() && GameStates.IsLobby)
            {
                a += $"\n/tp o - {GetString("Command.tpo")}";
                a += $"\n/tp i - {GetString("Command.tpi")}";
            }
            send = GetString("CommandList")
            + "<size=60%><line-height=1.3pic>";
            if (to == 0)
            {
                //ホスト限定
                send += $"<size=80%></line-height>\n【~~~~~~~{GetString("OnlyHost")}~~~~~~~】</size><line-height=1.3pic>"
                + $"\n/rename(/r) - {GetString("Command.rename")}"
                + $"\n/dis - {GetString("Command.dis")}"
                + $"\n/sw - {GetString("Command.sw")}"
                + $"\n/forceend(/fe) - {GetString("Command.forceend")}"
                + $"\n/mw - {GetString("Command.mw")}"
                + $"\n/kf - {GetString("Command.kf")}"
                + $"\n/allplayertp(/apt) - {GetString("Command.apt")}";
            }
            //導入者
            send += $"<size=80%></line-height>\n【~~~~~~~{GetString("OnlyClient")}~~~~~~~】</size><line-height=1.3pic>"
            + $"\n/dump - {GetString("Command.dump")}"
            //全員
            + $"<size=80%></line-height>\n【~~~~~~~{GetString("Allplayer")}~~~~~~~】</size><line-height=1.3pic>"
            + $"\n/now(/n) - {GetString("Command.now")}"
            + $"\n/now role(/n r) - {GetString("Command.nowrole")}"
            + $"\n/now set(/n s) - {GetString("Command.nowset")}"
            + $"\n/h now(/h n) - {GetString("Command.h_now")}"
            + $"\n/h roles (/h r ) {GetString("Command.h_roles")}"
            + $"\n/myrole - {GetString("Command.m")}"
            + $"\n/meetinginfo - {GetString("Command.mi")}";
            if (Options.ImpostorHideChat.GetBool()) send += $"\n/ic - {GetString("Command.impchat")}";
            if (Options.JackalHideChat.GetBool()) send += $"\n/jc - {GetString("Command.jacchat")}";
            if (Options.LoversHideChat.GetBool()) send += $"\n/jc - {GetString("Command.LoverChat")}";
            if (GameStates.IsLobby)
            {
                send += $"\n/lastresult(/l) - {GetString("Command.lastresult")}"
                    + $"\n/killlog(/kl) - {GetString("Command.killlog")}"
                    + $"\n/timer - {GetString("Command.timer")}";
            }
            if (Main.UseYomiage.Value) send += $"\n/voice - {GetString("Command.voice")}";

            SendMessage(send + a, to);
        }
        public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "", bool rob = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (text.RemoveHtmlTags() == "") return;
            if (title == "") title = $"<color={Main.ModColor}>" + GetString($"DefaultSystemMessageTitle") + "</color>";
            //すぐ</align>すると最終行もあれなので。
            var fir = rob ? "" : "<align=\"left\">";
            Main.MessagesToSend.Add(($"{fir}{text}", sendTo, $"{fir}{title}"));
        }
        public static bool aftersystemmeg;
        public static void ApplySuffix(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            string n = name;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (!Camouflage.PlayerSkins.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var color)) return;

                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(color.ColorId);
            }
            else if (GameStates.IsLobby)
            {
                if (!GameStates.IsCountDown)
                {
                    switch (Options.GetSuffixMode())
                    {
                        case SuffixModes.None:
                            break;
                        case SuffixModes.TOH:
                            name += $"</size>\r\n<color={Main.ModColor}>TOH-K v{Main.PluginShowVersion}</color><size=150%>";
                            break;
                        case SuffixModes.Streaming:
                            name += $"</size>\r\n<color={Main.ModColor}>{GetString("SuffixMode.Streaming")}</color><size=150%>";
                            break;
                        case SuffixModes.Recording:
                            name += $"</size>\r\n<color={Main.ModColor}>{GetString("SuffixMode.Recording")}</color><size=150%>";
                            break;
                        case SuffixModes.RoomHost:
                            name += $"</size>\r\n<color={Main.ModColor}>{GetString("SuffixMode.RoomHost")}</color><size=150%>";
                            break;
                        case SuffixModes.OriginalName:
                            name += $"</size>\r\n<color={Main.ModColor}>{DataManager.player.Customization.Name}</color><size=150%>";
                            break;
                        case SuffixModes.Timer:
                            if (GameStates.IsLocalGame
                            || GameStates.IsCountDown) break;
                            float timerValue = GameStartManagerPatch.GetTimer();
                            if (timerValue < GameStartManagerPatch.Timer2 - 2 || GameStartManagerPatch.Timer2 < 25)
                                GameStartManagerPatch.Timer2 = timerValue;
                            timerValue = GameStartManagerPatch.Timer2;
                            int minutes = (int)timerValue / 60;
                            int seconds = (int)timerValue % 60;
                            string Color = "<color=#00ffff>";
                            if (minutes <= 4) Color = "<color=#9acd32>";//5分切ったら
                            if (minutes <= 2) Color = "<color=#ffa500>";//3分切ったら。
                            if (minutes <= 0) Color = "<color=red>";//1分切ったら。
                            name += $"</size><size=75%>({Color}{minutes:00}:{seconds:00}</color>)</size><size=150%>";
                            break;
                    }
                }
            }
            //Dataのほう変えるのはなぁっておもいました。うん。
            if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default)
                PlayerControl.LocalPlayer.RpcSetName(name);

            if (GameStates.IsLobby && !GameStates.IsCountDown && pc.name != "Player(Clone)" && pc.PlayerId != PlayerControl.LocalPlayer.PlayerId && !pc.IsModClient())
            {
                name = $"<b>{name}</b>";

                var info = "<size=120%>";
                var at = "";
                if (Options.NoGameEnd.OptionMeGetBool()) info += $"\r\n" + ColorString(Color.red, GetString("NoGameEnd")); else at += "\r\n";
                if (Options.IsStandardHAS) info += $"\r\n" + ColorString(Color.yellow, GetString("StandardHAS")); else at += "\r\n";
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) info += $"\r\n" + ColorString(Color.red, GetString("HideAndSeek")); else at += "\r\n";
                if (Options.CurrentGameMode == CustomGameMode.TaskBattle) info += $"\r\n" + ColorString(Color.cyan, GetString("TaskBattle")); else at += "\r\n";
                if (Options.SuddenDeathMode.OptionMeGetBool()) info += "\r\n" + ColorString(GetRoleColor(CustomRoles.Comebacker), GetString("SuddenDeathMode")); else at += "\r\n";
                if (Options.EnableGM.OptionMeGetBool()) info += $"\r\n" + ColorString(GetRoleColor(CustomRoles.GM), GetString("GM")); else at += "\r\n";
                if (DebugModeManager.IsDebugMode)
                    info += "\r\n" + (DebugModeManager.EnableTOHkDebugMode.OptionMeGetBool() ? "<color=#0066de>DebugMode</color>" : ColorString(Color.green, "デバッグモード"));
                else at += "\r\n";

                info += "</size>";
                n = "<size=150%><line-height=-1400%>\n\r<b></line-height>" + name + "\n<line-height=-100%>" + info.RemoveText() + at + $"</line-height><line-height=-1300%>\r\n<size=145%><color={Main.ModColor}>TownOfHost-K <color=#ffffff>v{Main.PluginShowVersion}</size></size></line-height>{info}{at}</b><size=0>　　　　　　　　　　　　　　　　　　　　";
                PlayerControl.LocalPlayer.RpcSetNamePrivate(n, true, pc, aftersystemmeg);
            }
        }
        #region AfterMeetingTasks
        public static bool CanVent;
        public static List<byte> RoleSendList = new();
        public static void AfterMeetingTasks()
        {
            GameStates.Meeting = false;
            //天秤会議だと送らない
            if (Balancer.Id == 255 && Balancer.target1 != 255 && Balancer.target2 != 255 && (!Options.firstturnmeeting || !MeetingStates.First))
            {
                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                    roleClass.BalancerAfterMeetingTasks();
            }
            else
            {
                if (!Options.firstturnmeeting || !MeetingStates.First)
                {
                    if (Amanojaku.Amaday.GetFloat() == UtilsGameLog.day) AmanojakuAssing.AssignAddOnsFromList();
                    if (Amnesia.Modoru.GetFloat() <= UtilsGameLog.day && Amnesia.TriggerDay.GetBool())
                    {
                        foreach (var p in PlayerCatch.AllPlayerControls)
                        {
                            if (p.Is(CustomRoles.Amnesia))
                            {
                                if (!RoleSendList.Contains(p.PlayerId)) RoleSendList.Add(p.PlayerId);
                                Amnesia.Kesu(p.PlayerId);
                            }
                        }
                    }
                }
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var roleClass = pc.GetRoleClass();
                    if (!Options.firstturnmeeting || !MeetingStates.First) roleClass?.AfterMeetingTasks();
                    roleClass?.Colorchnge();
                }
                if (!Options.firstturnmeeting || !MeetingStates.First)
                {
                    if (AsistingAngel.ch())
                        AsistingAngel.Limit++;
                    UtilsTask.DelTask();
                    UtilsGameLog.day++;
                    UtilsGameLog.gamelog += "\n<size=80%>" + string.Format(GetString("Message.Day"), UtilsGameLog.day).Color(Palette.Orange) + "</size><size=60%>";
                }
            }
            if (Options.AirShipVariableElectrical.GetBool()) AirShipElectricalDoors.Initialize();
            DoorsReset.ResetDoors();
            // 空デデンバグ対応 会議後にベントを空にする
            var ventilationSystem = ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) ? systemType.TryCast<VentilationSystem>() : null;
            if (ventilationSystem != null)
            {
                ventilationSystem.PlayersInsideVents.Clear();
                ventilationSystem.IsDirty = true;
            }
            GuessManager.Reset();//会議後にリセット入れる
            if (Options.Onlyseepet.GetBool()) PlayerCatch.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
            GameStates.Tuihou = false;
        }
        #endregion
        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        #region Remove
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static string RemoveColorTags(this string str) => Regex.Replace(str, "</?color(=#[0-9a-fA-F]*)?>", "");
        public static string RemoveSizeTags(this string str) => Regex.Replace(str, "</?size[^>]*?>", "");
        public static string RemoveGiveAddon(this string str) => Regex.Replace(str, "を付与する", "");
        public static string RemoveSN(this string str) => Regex.Replace(str, "\n", "");
        public static string RemoveaAlign(this string str) => Regex.Replace(str, "align", "");
        public static string RemoveText(this string str, bool Update = false)
        {
            bool musi = false;
            string returns = "";
            if (Update)
            {
                for (var i = 0; i < str.Length; i++)
                {
                    string s = "";
                    s = str.Substring(i, 1);
                    if (s == Regex.Replace(s, "[0-9]", "")) continue;
                    returns += s;
                }
                return returns;
            }
            for (var i = 0; i < str.Length; i++)
            {
                string s = "";
                s = str.Substring(i, 1);

                {
                    if (s == "<")
                        musi = true;
                    if (s == ">")
                    {
                        returns += ">";
                        musi = false;
                    }

                    if (musi)
                        returns += s;
                    else
                        if (s != ">")
                    {
                        if (s == "\n")
                            returns += "\n ";
                        else if (s == "\r")
                            returns += "\r";
                        else
                            returns += " ";
                    }
                }
            }
            return returns;
        }
        #endregion
        public static void FlashColor(Color color, float duration = 1f)
        {
            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud.FullScreen == null) return;
            var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
            if (obj == null)
            {
                obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
                obj.name = "FlashColor_FullScreen";
            }
            hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
            {
                obj.SetActive(t != 1f);
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }

        public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
        /// <summary>
        /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
        /// </summary>
        public static Color ShadeColor(this Color color, float Darkness = 0)
        {
            bool IsDarker = Darkness >= 0; //黒と混ぜる
            if (!IsDarker) Darkness = -Darkness;
            float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
            float R = (color.r + Weight) / (Darkness + 1);
            float G = (color.g + Weight) / (Darkness + 1);
            float B = (color.b + Weight) / (Darkness + 1);
            return new Color(R, G, B, color.a);
        }

        /// <summary>
        /// 乱数の簡易的なヒストグラムを取得する関数
        /// <params name="nums">生成した乱数を格納したint配列</params>
        /// <params name="scale">ヒストグラムの倍率 大量の乱数を扱う場合、この値を下げることをお勧めします。</params>
        /// </summary>
        public static string WriteRandomHistgram(int[] nums, float scale = 1.0f)
        {
            int[] countData = new int[nums.Max() + 1];
            foreach (var num in nums)
            {
                if (0 <= num) countData[num]++;
            }
            StringBuilder sb = new();
            for (int i = 0; i < countData.Length; i++)
            {
                // 倍率適用
                countData[i] = (int)(countData[i] * scale);

                // 行タイトル
                sb.AppendFormat("{0:D2}", i).Append(" : ");

                // ヒストグラム部分
                for (int j = 0; j < countData[i]; j++)
                    sb.Append('|');

                // 改行
                sb.Append('\n');
            }

            // その他の情報
            sb.Append("最大数 - 最小数: ").Append(countData.Max() - countData.Min());

            return sb.ToString();
        }

        public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
        where T : Il2CppObjectBase
        {
            casted = obj.TryCast<T>();
            return casted != null;
        }
        public const string AdditionalWinnerMark = "<color=#dddd00>★</color>";
    }
}