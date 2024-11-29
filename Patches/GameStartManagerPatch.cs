using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using static TownOfHost.Translator;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class GameStartManagerPatch
    {
        public static float GetTimer() => timer;
        public static float SetTimer(float time) => timer = time;
        public static float Timer2 = 0; //毎秒タイマー送るのはあれだから
        private static float timer = 600f;
        private static TextMeshPro warningText;
        public static TextMeshPro HideName;
        private static TextMeshPro GameMaster;

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public class GameStartManagerStartPatch
        {
            public static void Postfix(GameStartManager __instance)
            {
                __instance.MinPlayers = 1;

                __instance.GameRoomNameCode.text = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                // Reset lobby countdown timer
                timer = 600f;
                Timer2 = 0f;
                //ゲームマスターONのテキスト HideNameの後に作るとおかしくなるので先にInstantiateしておく
                GameMaster = Object.Instantiate(__instance.GameRoomNameCode, __instance.StartButton.transform.parent);
                GameMaster.gameObject.SetActive(false);
                GameMaster.name = "GMText";
                GameMaster.text = Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.GM), GetString("GameMasterON"));
                GameMaster.SetOutlineColor(Color.black);
                GameMaster.SetOutlineThickness(0.2f);

                HideName = Object.Instantiate(__instance.GameRoomNameCode, __instance.GameRoomNameCode.transform);
                HideName.gameObject.SetActive(true);
                HideName.name = "HideName";
                HideName.color =
                    ColorUtility.TryParseHtmlString(Main.HideColor.Value, out var color) ? color :
                    ColorUtility.TryParseHtmlString(Main.ModColor, out var modColor) ? modColor : HideName.color;
                HideName.text = Main.HideName.Value;

                warningText = Object.Instantiate(__instance.GameStartText, __instance.transform);
                warningText.name = "WarningText";
                warningText.transform.localPosition = new(0f, 0f - __instance.transform.localPosition.y, -1f);
                warningText.gameObject.SetActive(false);

                var cancelButton = __instance.GameStartText.transform.parent.gameObject.AddComponent<PassiveButton>();
                cancelButton.gameObject.AddComponent<BoxCollider2D>().autoTiling = true;
                cancelButton.OnMouseOut = new();
                cancelButton.OnMouseOver = new();
                cancelButton.OnClick = new();
                cancelButton.OnClick.AddListener((Action)(() =>
                {
                    if (AmongUsClient.Instance.AmHost)
                        GameStartManager.Instance.ResetStartState();
                }));

                if (GameStates.IsOnlineGame)
                {
                    __instance.GameRoomNameCode.gameObject.AddComponent<BoxCollider2D>().size = new(2, 1);
                    var codePassive = __instance.GameRoomNameCode.gameObject.AddComponent<PassiveButton>();
                    codePassive.OnClick = new();
                    codePassive.OnMouseOut = new();
                    codePassive.OnMouseOver = new();
                    codePassive.OnClick.AddListener((Action)(() => LobbyInfoPane.Instance.CopyGameCode()));
                }

                LobbyInfoPanePatch.Postfix();

                if (!AmongUsClient.Instance.AmHost) return;

                // Make Public Button
                /*if (!Main.IsPublicRoomAllowed())
                {
                    __instance.HostPrivateButton.activeTextColor = Palette.DisabledGrey;
                    __instance.HostPrivateButton.selectedTextColor = Palette.DisabledGrey;
                    __instance.HostPrivateButton.ClickSound = null;
                }*/

                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public class GameStartManagerUpdatePatch
        {
            private static float exitTimer = 0f;
            //private static float ext = 0f;
            public static void Prefix(GameStartManager __instance)
            {
                // Lobby code
                if (DataManager.Settings.Gameplay.StreamerMode
                && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
                {
                    var co = StringHelper.CodeColor($"{Main.ModColor}");
                    __instance.GameRoomNameCode.color = new(co.r, co.g, co.b, 0);
                    HideName.enabled = true;
                }
                else
                {
                    var co = StringHelper.CodeColor($"{Main.ModColor}");
                    __instance.GameRoomNameCode.color = new(co.r, co.g, co.b, 255);
                    HideName.enabled = false;
                }

                // GameMaster Text
                GameMaster.gameObject.SetActive(Options.EnableGM.GetBool());
                GameMaster.transform.localPosition = new Vector3(0f, -0.25f);
            }
            public static void Postfix(GameStartManager __instance)
            {
                if (!AmongUsClient.Instance) return;
                if (__instance == null) return;
                string warningMessage = "";
                if (AmongUsClient.Instance.AmHost)
                {
                    bool canStartGame = true;
                    List<string> mismatchedPlayerNameList = new();
                    foreach (var client in AmongUsClient.Instance.allClients.ToArray())
                    {
                        if (client.Character == null || client == null) continue;
                        var dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                        if (dummyComponent != null && dummyComponent.enabled)
                            continue;

                        /* Debugversion
                        if (!Main.NotKigenDebug && Main.DebugVersion)
                        {
                            var now = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
                            int Re = Main.ReleaseYear * 10000 + Main.ReleaseMonth * 100 + Main.ReleaseDay;
                            int Rem = Main.DebugvalidityYear * 10000 + Main.DebugvalidityMonth * 100 + Main.DebugvalidityDay;

                            if (!(Re <= now && now <= Rem))
                            {
                                __instance.StartButton.gameObject.SetActive(false);
                                warningMessage = " <color=red>このデバッグ版は期限切れのため利用できません...(´・ω・｀)</color>";
                            }
                        }*/

                        /*if (Options.KickModClient.GetBool() && Client(client.Character.PlayerId) && client.Character.PlayerId != 0)
                        {
                            canStartGame = false;
                            mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                        }*/
                        if (!MatchVersions(client.Character.PlayerId, true))
                        {
                            canStartGame = false;
                            mismatchedPlayerNameList.Add(Utils.ColorString(Palette.PlayerColors[client.ColorId], client.Character.Data.PlayerName));
                        }
                    }
                    if (!canStartGame)
                    {
                        __instance.StartButton.gameObject.SetActive(false);
                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.MismatchedVersion"), String.Join(" ", mismatchedPlayerNameList), $"<color={Main.ModColor}>{Main.ModName}</color>"));
                    }

                    __instance.GameStartText.text += "\n<size=2.5><color=red>(クリックしてキャンセル)</size>";
                }
                else
                {
                    /*if (Options.KickModClient.GetBool())
                    {
                        if (GameStates.IsModHost)
                        {
                            ext += Time.deltaTime;
                            if (ext > 10)
                            {
                                ext = 0;
                                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                                SceneChanger.ChangeScene("MainMenu");
                            }
                            warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.CantModClient"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(10 - ext).ToString()));
                        }
                    }*/
                    if (MatchVersions(0))
                        exitTimer = 0;
                    else
                    {
                        exitTimer += Time.deltaTime;
                        if (exitTimer > 10)
                        {
                            exitTimer = 0;
                            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                            SceneChanger.ChangeScene("MainMenu");
                        }

                        warningMessage = Utils.ColorString(Color.red, string.Format(GetString("Warning.AutoExitAtMismatchedVersion"), $"<color={Main.ModColor}>{Main.ModName}</color>", Math.Round(10 - exitTimer).ToString()));
                    }
                }
                if (warningMessage == "")
                {
                    warningText.gameObject.SetActive(false);
                }
                else
                {
                    if (warningText != null)
                    {
                        warningText.text = warningMessage;
                        warningText.gameObject.SetActive(true);
                    }
                }

                // Lobby timer
                if (!GameData.Instance || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                    return;

                timer = Mathf.Max(0f, timer -= Time.deltaTime);
                int minutes = (int)timer / 60;
                int seconds = (int)timer % 60;
                string countDown = $"{minutes:00}:{seconds:00}";

                var timerv = GameObject.Find("ModeLabel")?.transform?.FindChild("Text_TMP")?.GetComponent<TextMeshPro>();

                if (__instance.RulesPresetText != null && timerv != null)
                {
                    if (timer <= 60) countDown = Utils.ColorString(Color.red, countDown);
                    timerv.DestroyTranslator();
                    __instance.RulesPresetText.DestroyTranslator();
                    __instance.RulesPresetText.text = countDown;
                    timerv.text = "タイマー";
                }
            }
            private static bool MatchVersions(byte playerId, bool acceptVanilla = false)
            {
                if (!Main.playerVersion.TryGetValue(playerId, out var version)) return acceptVanilla;
                return Main.ForkId == version.forkId
                    && Main.version.CompareTo(version.version) == 0
                    && version.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})";
            }
            private static bool Client(byte playerId)
                => Main.playerVersion.TryGetValue(playerId, out var version);
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
        public static class GameStartManagerBeginGamePatch
        {
            public static bool Prefix(GameStartManager __instance)
            {
                SelectRandomMap();

                var invalidColor = PlayerCatch.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
                if (invalidColor.Any())
                {
                    var msg = GetString("Error.InvalidColor");
                    Logger.seeingame(msg);
                    msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                    Utils.SendMessage(msg);
                    return false;
                }

                if (Options.CurrentGameMode == CustomGameMode.TaskBattle && Options.TaskBattleTeamMode.GetBool())
                {
                    //チェック
                    var teamc = Math.Min(Options.TaskBattleTeamC.GetFloat(), PlayerCatch.AllPlayerControls.Count());
                    var playerc = PlayerCatch.AllPlayerControls.Count() / teamc;

                    //チーム数でプレイヤーが足りない場合
                    if (Options.TaskBattleTeamC.GetFloat() > PlayerCatch.AllPlayerControls.Count())
                    {
                        var msg = GetString("Warning.MoreTeamsThanPlayers");
                        Logger.seeingame(msg);
                        Logger.Warn(msg, "BeginGame");
                    }
                    //合計タスク数が足りない場合
                    if (Options.TaskBattleTeamWinType.GetBool() && Main.NormalOptions.TotalTaskCount * playerc < Options.TaskBattleTeamWinTaskc.GetFloat())
                    {
                        var msg = GetString("Warning.TBTask");
                        Logger.seeingame(msg);
                        Logger.Warn(msg, "BeginGame");
                    }
                }

                RoleAssignManager.CheckRoleCount();

                Options.DefaultKillCooldown = Main.NormalOptions.KillCooldown;
                Main.LastKillCooldown.Value = Main.NormalOptions.KillCooldown;
                Main.NormalOptions.KillCooldown = 0f;

                var opt = Main.NormalOptions.Cast<IGameOptions>();
                AURoleOptions.SetOpt(opt);
                Main.LastShapeshifterCooldown.Value = AURoleOptions.ShapeshifterCooldown;
                AURoleOptions.ShapeshifterCooldown = 0f;

                PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(opt, AprilFoolsMode.IsAprilFoolsModeToggledOn));

                __instance.ReallyBegin(false);
                TemplateManager.SendTemplate("Start", noErr: true);
                return false;
            }
            private static void SelectRandomMap()
            {
                if (Options.RandomMapsMode.GetBool())
                {
                    var rand = IRandom.Instance;
                    List<byte> randomMaps = new();
                    /*TheSkeld   = 0
                    MIRAHQ     = 1
                    Polus      = 2
                    Dleks      = 3
                    TheAirShip = 4
                    TheFungle  = 5*/
                    if (Options.AddedTheSkeld.GetBool()) randomMaps.Add(0);
                    if (Options.AddedMiraHQ.GetBool()) randomMaps.Add(1);
                    if (Options.AddedPolus.GetBool()) randomMaps.Add(2);
                    // if (Options.AddedDleks.GetBool()) RandomMaps.Add(3);
                    if (Options.AddedTheAirShip.GetBool()) randomMaps.Add(4);
                    if (Options.AddedTheFungle.GetBool()) randomMaps.Add(5);

                    if (randomMaps.Count <= 0) return;
                    var mapsId = randomMaps[rand.Next(randomMaps.Count)];
                    Main.NormalOptions.MapId = mapsId;
                }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
        class ResetStartStatePatch
        {
            public static void Prefix()
            {
                if (GameStates.IsCountDown)
                {
                    Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
                    PlayerControl.LocalPlayer.RpcSyncSettings(GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions, AprilFoolsMode.IsAprilFoolsModeToggledOn));
                }
            }
        }
        [HarmonyPatch(typeof(HostInfoPanel), nameof(HostInfoPanel.Update))]
        class HostInfoPanelUpdatePatch
        {
            public static void Postfix(HostInfoPanel __instance)
            {
                if (!__instance) return;
                if (!AmongUsClient.Instance || (AmongUsClient.Instance?.GetHost()?.PlayerName == null)) return;
                __instance.playerName.text = $"<b>{AmongUsClient.Instance.GetHost().PlayerName.Color(ModColors.GetPlayerColor32((ModColors.PlayerColor)AmongUsClient.Instance.GetHost().ColorId))}</b>";
            }
        }
    }

    [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
    public static class HiddenTextPatch
    {
        private static void Postfix(TextBoxTMP __instance)
        {
            if (__instance.name == "GameIdText") __instance.outputText.text = new string('*', __instance.text.Length);
        }
    }

    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
    class UnrestrictedNumImpostorsPatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = Main.NormalOptions.NumImpostors;
            return false;
        }
    }

    [HarmonyPatch(typeof(LobbyTimerExtensionUI), nameof(LobbyTimerExtensionUI.ShowLobbyTimer))]
    class ShowLobbyTimerPatch
    {
        public static void Postfix(LobbyTimerExtensionUI __instance, [HarmonyArgument(0)] int timeRemainingSeconds)
        {
            //タイマー関連だからここに置かせて！
            GameStartManagerPatch.SetTimer(timeRemainingSeconds + 1);
        }
    }
}
