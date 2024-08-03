using System;
using System.Globalization;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public static class CredentialsPatch
    {
        public static SpriteRenderer TohkLogo { get; private set; }
        public static TextMeshPro credentialsText;

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        class PingTrackerUpdatePatch
        {
            static StringBuilder sb = new();
            static void Postfix(PingTracker __instance)
            {
                if (!credentialsText)
                {
                    credentialsText = UnityEngine.Object.Instantiate(__instance.text, __instance.transform.parent);
                    credentialsText.name = "credentialsText";
                    credentialsText.transform.parent = __instance.transform.parent;
                    UnityEngine.Object.Destroy(credentialsText.GetComponent<PingTracker>());
                    UnityEngine.Object.Destroy(credentialsText.GetComponent<AspectPosition>());
                }

                credentialsText.alignment = TextAlignmentOptions.TopRight;

                sb.Clear();

                sb.Append("\r\n").Append(Main.credentialsText);

                if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
                if (Options.IsStandardHAS) sb.Append($"\r\n").Append(Utils.ColorString(Color.yellow, GetString("StandardHAS")));
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("HideAndSeek")));
                if (Options.CurrentGameMode == CustomGameMode.TaskBattle) sb.Append($"\r\n").Append(Utils.ColorString(Color.cyan, GetString("TaskBattle")));
                if (Options.EnableGM.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.GM), GetString("GM")));
                if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
                if (DebugModeManager.IsDebugMode)
                {
                    sb.Append("\r\n");
                    sb.Append(DebugModeManager.EnableTOHkDebugMode.GetBool() ? "<color=#0066de>DebugMode</color>" : Utils.ColorString(Color.green, "デバッグモード"));
                }

                var offset_x = 2.5f; //右端からのオフセット
                if (HudManager.InstanceExists && HudManager._instance.Chat.gameObject.active) offset_x += 0.6f; //チャットがある場合の追加オフセット
                credentialsText.transform.localPosition = new Vector3(5.6779f - offset_x, 3.0745f, 0f);

                if (GameStates.IsLobby)
                {
                    if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable())
                        sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin")));
                }

                credentialsText.text = sb.ToString();
#if DEBUG
                if (Main.ViewPingDetails.Value)
                {
                    __instance.text.text += $"({AmongUsClient.Instance.Ping / 1000f}秒/{(GameStates.IsOnlineGame ? (Main.IsCs() ? ServerManager.Instance.CurrentRegion.Name : GetString(ServerManager.Instance.CurrentRegion.TranslateName)) : "ローカル")})";
                    __instance.text.alignment = TextAlignmentOptions.Top;
                }
                else __instance.text.alignment = TextAlignmentOptions.TopLeft;
#endif
            }
        }
        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        class VersionShowerStartPatch
        {
            static TextMeshPro SpecialEventText;
            static void Postfix(VersionShower __instance)
            {
                TMPTemplate.SetBase(__instance.text);
                var Debugver = "";
                if (Main.DebugVersion) Debugver = $"<color={Main.ModColor}>☆Debug☆</color>";
                Main.credentialsText = $"<color={Main.ModColor}>{Main.ModName}</color> v{Main.PluginVersion}" + Debugver;
#if DEBUG
                Main.credentialsText += $"\n<color={Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif
                var credentials = TMPTemplate.Create(
                    "TOHCredentialsText",
                    Main.credentialsText,
                    fontSize: 2f,
                    alignment: TextAlignmentOptions.Right,
                    setActive: true);
                credentials.transform.position = new Vector3(2.3419f, 2.29f, -5f);
#if DEBUG
                credentials.transform.position -= new Vector3(0f, 0.1218f, 0f);
#endif
                if (GameObject.Find("FilterSettings")) credentials.transform.position = new Vector3(3.2438f, -2.8192f, 5f);

                ErrorText.Create(__instance.text);
                if (Main.hasArgumentException && ErrorText.Instance != null)
                {
                    ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
                }

                VersionChecker.Check();
#if DEBUG
                if (OptionItem.IdDuplicated)
                {
                    ErrorText.Instance.AddError(ErrorCode.OptionIDDuplicate);
                }
#endif

                if (SpecialEventText == null && TohkLogo != null)
                {
                    SpecialEventText = TMPTemplate.Create(
                        "SpecialEventText",
                        "",
                        Color.white,
                        alignment: TextAlignmentOptions.Center,
                        parent: TohkLogo.transform);
                    SpecialEventText.name = "SpecialEventText";
                    SpecialEventText.fontSizeMin = 3f;
                    SpecialEventText.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    SpecialEventText.gameObject.SetActive(true);
                }
                SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
                if (Main.IsInitialRelease)
                {
                    SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                    if (ColorUtility.TryParseHtmlString(Main.ModColor, out var col))
                    {
                        SpecialEventText.color = col;
                    }
                }
                if (Main.IsChristmas && CultureInfo.CurrentCulture.Name == "ja-JP")
                {
                    //このソースコ―ドを見た人へ。口外しないでもらえると嬉しいです...
                    //To anyone who has seen this source code. I would appreciate it if you would keep your mouth shut...
                    SpecialEventText.text = $"何とは言いませんが、特別な日ですね。\n<size=15%>\n\n末永く爆発しろ</size>";
                    SpecialEventText.color = Utils.GetRoleColor(CustomRoles.ALovers);
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class TitleLogoPatch
        {
            public static GameObject amongUsLogo;

            [HarmonyPriority(Priority.VeryHigh)]
            static void Postfix(MainMenuManager __instance)
            {
                amongUsLogo = GameObject.Find("LOGO-AU");

                var rightpanel = __instance.gameModeButtons.transform.parent;
                var logoObject = new GameObject("titleLogo_TOHk");
                var logoTransform = logoObject.transform;
                TohkLogo = logoObject.AddComponent<SpriteRenderer>();
                logoTransform.parent = rightpanel;
                logoTransform.localPosition = new(0f, 0.15f, 1f);
                logoTransform.localScale *= 1.0f;
                TohkLogo.sprite = Utils.LoadSprite("TownOfHost.Resources.TownOfHost-K.png", 300f);
            }
        }
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
        class ModManagerLateUpdatePatch
        {
            public static void Prefix(ModManager __instance)
            {
                __instance.ShowModStamp();

                LateTask.Update(Time.deltaTime);
                CheckMurderPatch.Update();
            }
            public static void Postfix(ModManager __instance)
            {
                var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
                __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                    __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                    new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
            }
        }
    }
}
