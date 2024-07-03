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
    public class LobbyInfoPanePatch
    {
        public static void Postfix()
        {
            var window = GameObject.Find("Main Camera/Hud/LobbyInfoPane/AspectSize/RulesPopOutWindow").GetComponent<LobbyViewSettingsPane>();
            var ModButton = Object.Instantiate(window.taskTabButton, window.transform);
            var ShowStgTMPButton = Object.Instantiate(HudManager.Instance.SettingsButton, window.transform).GetComponent<PassiveButton>();

            window.rolesTabButton.transform.localPosition += new Vector3(3.4938f, 0f);
            ModButton.transform.localPosition += new Vector3(3.4938f, 0f);
            ShowStgTMPButton.GetComponent<AspectPosition>().DistanceFromEdge += new Vector3(-0.35f, 1.2f);

            ModButton.buttonText.DestroyTranslator();
            ModButton.buttonText.text = "MOD";

            ShowStgTMPButton.ClickSound = ModButton.ClickSound;

            ModButton.OnClick = new();
            ModButton.OnClick.AddListener((Action)(() =>
            {
                window.rolesTabButton.SelectButton(false);
                window.taskTabButton.SelectButton(false);
                ModButton.SelectButton(true);

                for (int index2 = 0; index2 < window.settingsInfo.Count; ++index2)
                    Object.Destroy((Object)window.settingsInfo[index2].gameObject);
                window.settingsInfo.Clear();

                float y1 = 1.44f;

                CategoryHeaderMasked categoryHeaderMasked = Object.Instantiate<CategoryHeaderMasked>(window.categoryHeaderOrigin);
                setHeader(categoryHeaderMasked, GetString("TabGroup.MainSettings"));
                categoryHeaderMasked.transform.SetParent(window.settingsContainer);
                categoryHeaderMasked.transform.localScale = Vector3.one;
                categoryHeaderMasked.transform.localPosition = new Vector3(-9.77f, y1, -2f);
                window.settingsInfo.Add(categoryHeaderMasked.gameObject);

                float y2 = y1 - 0.85f;
                int index = 0;
                foreach (OptionItem option in OptionItem.AllOptions)
                {
                    if (option.Tab != TabGroup.MainSettings || option.IsHiddenOn(Options.CurrentGameMode) || (!option?.Parent?.GetBool() ?? false)) continue;
                    ViewSettingsInfoPanel settingsInfoPanel = Object.Instantiate<ViewSettingsInfoPanel>(window.infoPanelOrigin);
                    settingsInfoPanel.transform.SetParent(window.settingsContainer);
                    settingsInfoPanel.transform.localScale = Vector3.one;
                    float x;
                    if (index % 2 == 0)
                    {
                        x = -8.95f;
                        if (index > 0)
                            y2 -= 0.59f;
                    }
                    else
                        x = -3f;
                    settingsInfoPanel.transform.localPosition = new Vector3(x, y2, -2f);
                    setInfo(settingsInfoPanel, option.GetName(false), option.GetString());
                    window.settingsInfo.Add(settingsInfoPanel.gameObject);
                    y1 = y2 - 0.59f;
                    index++;
                }
                window.scrollBar.CalculateAndSetYBounds((float)(window.settingsInfo.Count + 10), 2f, 6f, 0.59f);
            }));

            ShowStgTMPButton.OnClick = new();
            ShowStgTMPButton.OnClick.AddListener((Action)(() =>
            {
                Main.ShowGameSettingsTMP.Value = !Main.ShowGameSettingsTMP.Value;
            }));
        }

        public static void setHeader(CategoryHeaderMasked infoPane, string name)
        {
            infoPane.Title.text = name;
            infoPane.Background.material.SetInt(PlayerMaterial.MaskLayer, 61);
            if ((Object)infoPane.Divider != (Object)null)
                infoPane.Divider.material.SetInt(PlayerMaterial.MaskLayer, 61);
            infoPane.Title.fontMaterial.SetFloat("_StencilComp", 3f);
            infoPane.Title.fontMaterial.SetFloat("_Stencil", (float)61);
        }

        public static void setInfo(ViewSettingsInfoPanel info, string title, string valueString)
        {
            info.titleText.text = title;
            info.settingText.text = valueString;
            info.disabledBackground.gameObject.SetActive(false);
            info.background.gameObject.SetActive(true);
            info.SetMaskLayer(61);
        }
        [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.DrawRolesTab))]
        class LobbyViewSettingsPaneDrawRolesTabPatch
        {
            public static bool Prefix(LobbyViewSettingsPane __instance)
            {
                float y = 0.95f;
                float x1 = -6.53f;
                CategoryHeaderMasked categoryHeaderMasked1 = Object.Instantiate<CategoryHeaderMasked>(__instance.categoryHeaderOrigin);
                categoryHeaderMasked1.SetHeader(StringNames.RoleQuotaLabel, 61);
                categoryHeaderMasked1.transform.SetParent(__instance.settingsContainer);
                categoryHeaderMasked1.transform.localScale = Vector3.one;
                categoryHeaderMasked1.transform.localPosition = new Vector3(-9.77f, 1.26f, -2f);
                __instance.settingsInfo.Add(categoryHeaderMasked1.gameObject);
                List<CustomRoles> roleRulesCategoryList = new();
                for (int index1 = 0; index1 < 5; ++index1)
                {
                    CategoryHeaderRoleVariant headerRoleVariant = Object.Instantiate<CategoryHeaderRoleVariant>(__instance.categoryHeaderRoleOrigin);
                    setHeader(headerRoleVariant, GetString($"{(TabGroup)index1 + 1}"));
                    headerRoleVariant.transform.SetParent(__instance.settingsContainer);
                    headerRoleVariant.transform.localScale = Vector3.one;
                    headerRoleVariant.transform.localPosition = new Vector3(0.09f, y, -2f);
                    __instance.settingsInfo.Add(headerRoleVariant.gameObject);
                    y -= 0.696f;
                    for (int index2 = 0; index2 < Options.CustomRoleSpawnChances.Count; ++index2)
                    {
                        CustomRoles role = Options.CustomRoleSpawnChances.Keys.ToList()[index2];
                        if ((TabGroup)index1 + 1 == role.GetRoleInfo().Tab)
                        {
                            int chancePerGame = Options.GetRoleChance(role);
                            int numPerGame = Options.GetRoleCount(role);
                            bool showDisabledBackground = numPerGame == 0;
                            ViewSettingsInfoPanelRoleVariant panelRoleVariant = Object.Instantiate<ViewSettingsInfoPanelRoleVariant>(__instance.infoPanelRoleOrigin);
                            panelRoleVariant.transform.SetParent(__instance.settingsContainer);
                            panelRoleVariant.transform.localScale = Vector3.one;
                            panelRoleVariant.transform.localPosition = new Vector3(x1, y, -2f);
                            if (!showDisabledBackground)
                                roleRulesCategoryList.Add(role);
                            _ = ColorUtility.TryParseHtmlString("#696969", out Color ncolor);
                            setInfo(panelRoleVariant, GetString($"{role}"), numPerGame, chancePerGame, 61, (Color32)((TabGroup)index1 + 1 == TabGroup.CrewmateRoles ? Palette.CrewmateRoleBlue : (TabGroup)index1 + 1 == TabGroup.NeutralRoles ? ncolor : Palette.ImpostorRoleRed), null, index1 == 0, showDisabledBackground);
                            __instance.settingsInfo.Add(panelRoleVariant.gameObject);
                            y -= 0.664f;
                        }
                    }
                }
                if (roleRulesCategoryList.Count > 0)
                {
                    CategoryHeaderMasked categoryHeaderMasked2 = Object.Instantiate<CategoryHeaderMasked>(__instance.categoryHeaderOrigin);
                    categoryHeaderMasked2.SetHeader(StringNames.RoleSettingsLabel, 61);
                    categoryHeaderMasked2.transform.SetParent(__instance.settingsContainer);
                    categoryHeaderMasked2.transform.localScale = Vector3.one;
                    categoryHeaderMasked2.transform.localPosition = new Vector3(-9.77f, y, -2f);
                    __instance.settingsInfo.Add(categoryHeaderMasked2.gameObject);
                    y -= 1.7f;
                    float num1 = 0.0f;
                    for (int index = 0; index < roleRulesCategoryList.Count; ++index)
                    {
                        float x2;
                        if (index % 2 == 0)
                        {
                            x2 = -5.8f;
                            if (index > 0)
                            {
                                y -= num1 + 0.59f;
                                num1 = 0.0f;
                            }
                        }
                        else
                            x2 = 0.149999619f;
                        AdvancedRoleViewPanel advancedRoleViewPanel = Object.Instantiate<AdvancedRoleViewPanel>(__instance.advancedRolePanelOrigin);
                        advancedRoleViewPanel.transform.SetParent(__instance.settingsContainer);
                        advancedRoleViewPanel.transform.localScale = Vector3.one;
                        advancedRoleViewPanel.transform.localPosition = new Vector3(x2, y, -2f);
                        float num2 = setUp(advancedRoleViewPanel, roleRulesCategoryList[index], 0.59f, 61);
                        if ((double)num2 > (double)num1)
                            num1 = num2;
                        __instance.settingsInfo.Add(advancedRoleViewPanel.gameObject);
                    }
                }
                __instance.scrollBar.SetYBoundsMax(-y);
                return false;
            }
        }

        public static void setInfo(
          ViewSettingsInfoPanelRoleVariant infoPanelRole,
          string name,
          int count,
          int chance,
          int maskLayer,
          Color32 color,
          Sprite roleIcon,
          bool crewmateTeam,
          bool showDisabledBackground = false)
        {
            infoPanelRole.titleText.text = name;
            infoPanelRole.settingText.text = count.ToString();
            infoPanelRole.chanceText.text = chance.ToString();
            infoPanelRole.iconSprite.sprite = roleIcon;
            if (showDisabledBackground)
            {
                infoPanelRole.titleText.color = Palette.White_75Alpha;
                infoPanelRole.chanceTitle.color = Palette.White_75Alpha;
                infoPanelRole.chanceBackground.sprite = infoPanelRole.disabledCube;
                infoPanelRole.background.sprite = infoPanelRole.disabledCube;
                infoPanelRole.labelBackground.color = Palette.DisabledGrey;
            }
            else
            {
                infoPanelRole.labelBackground.color = (Color)color;
                if (crewmateTeam)
                {
                    infoPanelRole.chanceBackground.sprite = infoPanelRole.crewmateCube;
                    infoPanelRole.background.sprite = infoPanelRole.crewmateCube;
                }
                else
                {
                    infoPanelRole.chanceBackground.sprite = infoPanelRole.impostorCube;
                    infoPanelRole.background.sprite = infoPanelRole.impostorCube;
                }
            }
            infoPanelRole.SetMaskLayer(maskLayer);
        }
        public static float setUp(AdvancedRoleViewPanel view, CustomRoles role, float spacingY, int maskLayer)
        {
            setHeader2(view.header, role);
            view.divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            float yPosStart = view.yPosStart;
            float num1 = 1.08f;
            OptionItem[] all = Options.CustomRoleSpawnChances[role].Children.ToArray();
            for (int index = 0; index < all.Length; ++index)
            {
                OptionItem option = all[index];
                ViewSettingsInfoPanel settingsInfoPanel = Object.Instantiate<ViewSettingsInfoPanel>(view.infoPanelOrigin);
                settingsInfoPanel.transform.SetParent(view.transform);
                settingsInfoPanel.transform.localScale = Vector3.one;
                settingsInfoPanel.transform.localPosition = new Vector3(view.xPosStart, yPosStart, -2f);
                setInfo(settingsInfoPanel, option.GetName(false), option.GetString());
                yPosStart -= spacingY;
                if (index > 0)
                    num1 += 0.8f;
            }
            return num1;
        }
        public static void setHeader2(CategoryHeaderRoleVariant roleVariant, CustomRoles role)
        {
            setHeader(roleVariant, GetString($"{role}"));
            _ = ColorUtility.TryParseHtmlString("#696969", out Color ncolor);
            var color = (Color32)(role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate ? Palette.CrewmateRoleBlue : role.GetCustomRoleTypes() == CustomRoleTypes.Neutral ? ncolor : Palette.ImpostorRoleRed);
            roleVariant.Background.color = color;
            roleVariant.Divider.color = color;
        }
    }
}