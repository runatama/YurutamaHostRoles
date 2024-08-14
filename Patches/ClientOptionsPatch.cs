using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules.ClientOptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientActionItem ForceJapanese;
        private static ClientActionItem JapaneseRoleName;
        private static ClientActionItem UnloadMod;
        private static ClientActionItem DumpLog;
        private static ClientActionItem ForceEnd;
        private static ClientActionItem WebHookD;
        private static ClientActionItem Yomiage;
        private static ClientActionItem UseZoom;
        private static ClientActionItem SyncYomiage;
        private static ClientActionItem CustomName;
        private static ClientActionItem CustomSprite;
        private static ClientActionItem HideSomeFriendCodes;
        private static ToggleButtonBehaviour soundSettingsButton;
        private static ClientActionItem ViewPingDetails;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null)
            {
                return;
            }

            if (ForceJapanese == null || ForceJapanese.ToggleButton == null)
            {
                ForceJapanese = ClientOptionItem.Create("ForceJapanese", Main.ForceJapanese, __instance);
            }
            if (JapaneseRoleName == null || JapaneseRoleName.ToggleButton == null)
            {
                JapaneseRoleName = ClientOptionItem.Create("JapaneseRoleName", Main.JapaneseRoleName, __instance);
            }
            if (UnloadMod == null || UnloadMod.ToggleButton == null)
            {
                UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
            }
            if (DumpLog == null || DumpLog.ToggleButton == null)
            {
                DumpLog = ClientActionItem.Create("DumpLog", Utils.DumpLog, __instance);
            }
            if ((ForceEnd == null || ForceEnd.ToggleButton == null) && AmongUsClient.Instance.AmHost)
            {
                ForceEnd = ClientActionItem.Create("ForceEnd", ForceEndProcess, __instance);
            }
            if (WebHookD == null || WebHookD.ToggleButton == null)
            {
                WebHookD = ClientOptionItem.Create("UseWebHook", Main.UseWebHook, __instance);
            }
            if (Yomiage == null || Yomiage.ToggleButton == null)
            {
                Yomiage = ClientOptionItem.Create("UseYomiage", Main.UseYomiage, __instance);
            }
            if (UseZoom == null || UseZoom.ToggleButton == null)
            {
                UseZoom = ClientOptionItem.Create("UseZoom", Main.UseZoom, __instance);
            }
            if (SyncYomiage == null || SyncYomiage.ToggleButton == null)
            {
                SyncYomiage = ClientOptionItem.Create("SyncYomiage", Main.SyncYomiage, __instance);
            }
            if ((CustomName == null || CustomName.ToggleButton == null) && (Main.IsHalloween || Main.IsChristmas || Main.White || Main.GoldenWeek || Main.April))
            {
                CustomName = ClientOptionItem.Create("CustomName", Main.CustomName, __instance);
            }
            if (CustomSprite == null || CustomSprite.ToggleButton == null)
            {
                CustomSprite = ClientOptionItem.Create("CustomSprite", Main.CustomSprite, __instance);
            }
            if (HideSomeFriendCodes == null || HideSomeFriendCodes.ToggleButton == null)
            {
                HideSomeFriendCodes = ClientOptionItem.Create("HideSomeFriendCodes", Main.HideSomeFriendCodes, __instance);
            }
#if DEBUG
            if (ViewPingDetails == null || ViewPingDetails.ToggleButton == null)
            {
                ViewPingDetails = ClientOptionItem.Create("ViewPingDetails", Main.ViewPingDetails, __instance);
            }
#endif
            if (ModUnloaderScreen.Popup == null)
            {
                ModUnloaderScreen.Init(__instance);
            }
            if (SoundSettingsScreen.Popup == null)
            {
                SoundSettingsScreen.Init(__instance);
            }
            if (soundSettingsButton.IsDestroyedOrNull())
            {
                soundSettingsButton = Object.Instantiate(__instance.DisableMouseMovement, __instance.transform.FindChild("GeneralTab/MiscGroup"));
                soundSettingsButton.transform.localPosition = new(1.9546f, 1.5297f, __instance.DisableMouseMovement.transform.localPosition.z);//左側:-1.3127f,1.5588f
                soundSettingsButton.transform.localScale = new(0.5f, 0.5f);
                soundSettingsButton.name = "SoundStgButton";
                soundSettingsButton.Text.text = "サウンド設定";
                soundSettingsButton.Background.color = Palette.DisabledGrey;
                var soundSettingsPassiveButton = soundSettingsButton.GetComponent<PassiveButton>();
                soundSettingsPassiveButton.OnClick = new();
                soundSettingsPassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    SoundSettingsScreen.Show();
                }));
            }

            if (!AmongUsClient.Instance.AmHost && ForceEnd != null)
                ForceEnd = null;

        }
        private static void ForceEndProcess()
        {
            //左シフトが押されているなら強制廃村
            if (Input.GetKey(KeyCode.LeftShift) || (Main.FeColl != 0) && !GameStates.IsLobby)
            {
                GameManager.Instance.enabled = false;
                CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                return;
            }
            if (!GameStates.IsLobby) Main.FeColl++;
            Logger.Info($"廃村コール{Main.FeColl}回目", "fe");
            if (!GameStates.IsInGame) return;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
            GameManager.Instance.LogicFlow.CheckEndCriteria();
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    public static class OptionsMenuBehaviourClosePatch
    {
        public static void Postfix()
        {
            if (ClientActionItem.CustomBackground != null)
            {
                ClientActionItem.CustomBackground.gameObject.SetActive(false);
            }
            ModUnloaderScreen.Hide();
            SoundSettingsScreen.Hide();
        }
    }
}
