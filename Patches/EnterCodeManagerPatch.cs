using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Templates;
using System.Linq;
using TownOfHost.Modules.ClientOptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(EnterCodeManager))]
    class EnterCodeManagerPatch
    {
        private static GameObject ModScreen;
        private static PassiveButton RestoreButton;
        private static PassiveButton DownloadButton;
        private static PassiveButton UnloadAndJoinButton;
        private static TextMeshPro VersionText;
        public static string GameId = "";

        [HarmonyPatch(nameof(EnterCodeManager.ClickJoin)), HarmonyPrefix]
        public static void ClickJoinPrefix(EnterCodeManager __instance)
        {
            if (__instance.enterCodeField == null) return;
            SetGameId(__instance.enterCodeField.text);
        }

        [HarmonyPatch(nameof(EnterCodeManager.OnEnable)), HarmonyPrefix]
        public static void OnEnablePrefix(EnterCodeManager __instance)
        {
            if (__instance == null || __instance.joinGamePassiveButton == null) return;

            if (ModScreen.IsDestroyedOrNull())
            {
                var joinGameButton = __instance.joinGamePassiveButton;
                ModScreen = new GameObject("Mod_Screen");
                ModScreen.transform.SetParent(joinGameButton.transform.parent);
                ModScreen.transform.localPosition = Vector3.zero;
            }

            if (RestoreButton.IsDestroyedOrNull())
            {
                var joinGameButton = __instance.joinGamePassiveButton;
                var button = UnityEngine.Object.Instantiate(joinGameButton, ModScreen.transform);
                var text = button.buttonText;

                button.name = "Restore";
                button.transform.localPosition += new Vector3(-2.2f, 0f);
                button.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

                text.DestroyTranslator();
                text.text = "前回のコードを復元";

                button.OnClick = new();
                button.OnClick.AddListener((Action)(() =>
                {
                    if (GameId != "")
                    {
                        __instance.enterCodeField.SetText(GameId);
                        __instance.enterCodeField.placeholderText.gameObject.SetActive(false);
                    }
                }));
                RestoreButton = button;
            }

            if (DownloadButton.IsDestroyedOrNull())
            {
                var joinGameButton = __instance.joinGamePassiveButton;
                var button = UnityEngine.Object.Instantiate(joinGameButton, ModScreen.transform);
                var text = button.buttonText;

                button.name = "Download";
                button.transform.localPosition += new Vector3(2.5f, 0.25f);
                button.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

                text.DestroyTranslator();
                text.text = "ホストのバージョンをダウンロード";

                button.OnClick = new();

                DownloadButton = button;
                ModUpdater.CheckRelease(all: true, snap: true).GetAwaiter().GetResult();
            }

            if (UnloadAndJoinButton.IsDestroyedOrNull())
            {
                var joinGameButton = __instance.joinGamePassiveButton;
                var button = UnityEngine.Object.Instantiate(joinGameButton, ModScreen.transform);
                var text = button.buttonText;

                button.name = "UnloadAndJoin";
                button.transform.localPosition += new Vector3(2.5f, 0f);
                button.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

                text.DestroyTranslator();
                text.text = "MODをアンロードして参加";

                button.OnClick = new();
                button.OnClick.AddListener((Action)(() =>
                {
                    ModUnloaderScreen.Unload();
                    ModScreen.SetActive(false);
                    __instance.ClickJoin();
                }));
                UnloadAndJoinButton = button;
            }

            if (VersionText.IsDestroyedOrNull())
            {
                VersionText = TMPTemplate.Create(
                    name: "HostVersionText",
                    alignment: TextAlignmentOptions.Center,
                    setActive: true,
                    parent: __instance.joinGamePassiveButton.transform.parent
                );

                VersionText.transform.localPosition += new Vector3(0, -1.62f);
            }

            VersionText.text = "";
            DownloadButton.gameObject.SetActive(false);
            UnloadAndJoinButton.gameObject.SetActive(false);
            CheckRestoreButton();
        }

        [HarmonyPatch(nameof(EnterCodeManager.FindGameResult)), HarmonyPostfix]
        public static void FindGameResultPostfix(EnterCodeManager __instance)
        {
            if (__instance.enterCodeField != null)
                SetGameId(__instance.enterCodeField.text);
            CheckRestoreButton();

            if (VersionText == null) return;

            VersionText.text = "";
            VersionText.fontSize =
            VersionText.fontSizeMin = 1.8f;
            VersionText.color = Color.red;
            DownloadButton?.gameObject.SetActive(false);
            UnloadAndJoinButton?.gameObject.SetActive(true);

            var hostVersion = CheckHostVersion(__instance);

            if (hostVersion != null)
            {
                if (!MatchVersions(hostVersion))
                {
                    VersionText.fontSize =
                    VersionText.fontSizeMin = 1f;
                    VersionText.text = $"バージョンがホストと合致しません\nホストが使用しているバージョン<color=green>{hostVersion.forkId}v{hostVersion.version}</color>";
                    var version = ModUpdater.snapshots.FirstOrDefault(x => hostVersion.version.ToString() == x.TagName.TrimStart('v')?.Trim('S')?.Trim('s'));
                    if (version != null)
                    {
                        DownloadButton.OnClick = new();
                        DownloadButton.OnClick.AddListener((Action)(() =>
                        {
                            ModScreen?.gameObject.SetActive(false);
                            ModUpdater.StartUpdate(version.DownloadUrl);
                        }));
                        DownloadButton?.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                VersionText.text = $"ホストが<color={Main.ModColor}>{Main.ForkId}</color>を導入していません。";
            }
        }

        private static void CheckRestoreButton()
            => RestoreButton?.SetButtonEnableState(GameId != "");
        private static void SetGameId(string id)
            => GameId = id.IsNullOrWhiteSpace() ? GameId : id;
        private static bool MatchVersions(PlayerVersion version)
            => Main.ForkId == version.forkId
            && Main.version.CompareTo(version.version) == 0;
        private static PlayerVersion CheckHostVersion(EnterCodeManager manager)
        {
            if (manager == null || manager.hostText == null) return null;
            var text = manager.hostText.text;
            string pattern = @"<size=0>([^:]+):(\d+\.\d+\.\d+\.\d+)</size>";

            // 正規表現で一致する部分を検索
            MatchCollection matches = Regex.Matches(text, pattern);

            // 最後の<size=0>タグの中身を取得
            if (!matches.Any()) return null;

            var groups = matches[^1].Groups;
            string id = groups[1].Value;        // ID部分
            string ver = groups[2].Value;   // バージョン部分
            return new PlayerVersion(ver, "", id);
        }
    }
}
