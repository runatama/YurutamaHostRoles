using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppSystem.Linq;
using HarmonyLib;
using UnityEngine;

using Object = UnityEngine.Object;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using static TownOfHost.GameSettingMenuStartPatch;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
    class GameSettingMenuClosePatch
    {
        public static void Postfix()
        {
            ModSettingsButton = null;
            ModSettingsTab = null;
            activeonly = null;
            ActiveOnlyMode = false;
            priset = null;
            prisettext = null;
            search = null;
            searchtext = null;
        }
    }


    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class GameSettingMenuStartPatch
    {
        public static float w = 1;
        public static float h = 1;
        public static bool dasu;
        public static PassiveButton ModSettingsButton;
        public static RolesSettingsMenu ModSettingsTab;
        public static PassiveButton activeonly;
        public static bool ActiveOnlyMode;
        public static FreeChatInputField priset;
        public static TMPro.TextMeshPro prisettext;
        public static FreeChatInputField search;
        public static TMPro.TextMeshPro searchtext;
        static Il2CppSystem.Collections.Generic.List<PassiveButton> hozon2;
        public static void Postfix(GameSettingMenu __instance)
        {
            ActiveOnlyMode = false;
            var GamePresetButton = __instance.GamePresetsButton;
            var GameSettingsButton = __instance.GameSettingsButton;
            var RoleSettingsButton = __instance.RoleSettingsButton;

            ModSettingsButton = Object.Instantiate(RoleSettingsButton, RoleSettingsButton.transform.parent);
            activeonly = Object.Instantiate(GamePresetButton, __instance.RoleSettingsTab.transform.parent);

            activeonly.buttonText.text = "有効なMap設定/役職のみ表示する <size=5>(OFF)</size>";

            activeonly.inactiveSprites.GetComponent<SpriteRenderer>().color =
            activeonly.activeSprites.GetComponent<SpriteRenderer>().color =
            activeonly.selectedSprites.GetComponent<SpriteRenderer>().color = ModColors.bluegreen;
            activeonly.buttonText.DestroyTranslator();

            GamePresetButton.gameObject.SetActive(false);
            RoleSettingsButton.gameObject.SetActive(false);

            ModSettingsButton.buttonText.text = "TownOfHost-Kの設定";
            var activeSprite = ModSettingsButton.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = ModSettingsButton.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = StringHelper.CodeColor(Main.ModColor);
            selectedSprite.color = StringHelper.CodeColor(Main.ModColor).ShadeColor(-0.2f);
            ModSettingsButton.buttonText.DestroyTranslator();//翻訳破壊☆

            activeonly.OnClick = new();
            activeonly.OnClick.AddListener((Action)(() =>
            {
                if (ModSettingsButton.selected)
                {
                    ActiveOnlyMode = !ActiveOnlyMode;
                    activeonly.inactiveSprites.GetComponent<SpriteRenderer>().color =
                    activeonly.activeSprites.GetComponent<SpriteRenderer>().color =
                    activeonly.selectedSprites.GetComponent<SpriteRenderer>().color = ActiveOnlyMode ? ModColors.GhostRoleColor : ModColors.bluegreen;
                    var now = ActiveOnlyMode ? "ON" : "OFF";
                    activeonly.buttonText.text = $"有効なMap設定/役職のみ表示する <size=5>({now})</size>";
                    activeonly.selected = false;
                    ModSettingsTab.scrollBar.velocity = Vector2.zero;
                    ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, 0, ModSettingsTab.scrollBar.Inner.localPosition.z);
                    ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                }
            }));
            activeonly.gameObject.SetActive(false);

            ModSettingsTab = Object.Instantiate(__instance.RoleSettingsTab, __instance.RoleSettingsTab.transform.parent).GetComponent<RolesSettingsMenu>();

            if (priset == null)
            {
                try
                {
                    priset = Object.Instantiate(HudManager.Instance.Chat.freeChatField, __instance.RoleSettingsTab.transform.parent);
                    search = Object.Instantiate(HudManager.Instance.Chat.freeChatField, __instance.RoleSettingsTab.transform.parent);

                    prisettext = Object.Instantiate(HudManager.Instance.TaskPanel.taskText, priset.transform);
                    prisettext.text = "<size=120%><color=#cccccc><b>プリセット名編集</b></color></size>";
                    prisettext.transform.localPosition = new Vector3(-2f, -1.1f);
                    searchtext = Object.Instantiate(HudManager.Instance.TaskPanel.taskText, priset.transform);
                    searchtext.text = "<size=120%><color=#ffa826><b>検索</b></color></size>";
                    searchtext.transform.localPosition = new Vector3(-2f, -0.3f);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "OptionsManager");
                }
            }
            priset.transform.localPosition = new Vector3(0f, 3.2f);
            priset.transform.localScale = new Vector3(0.4f, 0.4f, 0f);
            priset?.gameObject?.SetActive(true);
            priset.submitButton.OnPressed = (Action)(() =>
            {
                if (priset.textArea.text != "")
                {
                    var pr = OptionItem.AllOptions.Where(op => op.Id == 0).FirstOrDefault();
                    switch (pr.CurrentValue)
                    {
                        case 0: Main.Preset1.Value = priset.textArea.text; break;
                        case 1: Main.Preset2.Value = priset.textArea.text; break;
                        case 2: Main.Preset3.Value = priset.textArea.text; break;
                        case 3: Main.Preset4.Value = priset.textArea.text; break;
                        case 4: Main.Preset5.Value = priset.textArea.text; break;
                        case 5: Main.Preset6.Value = priset.textArea.text; break;
                        case 6: Main.Preset7.Value = priset.textArea.text; break;
                    }
                    priset.textArea.Clear();
                }
            });
            Dictionary<TabGroup, GameObject> menus = new();

            __instance.GameSettingsTab.gameObject.SetActive(true);
            GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/ROLES TAB(Clone)/Gradient").SetActive(false);

            var template = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)").GetComponent<StringOption>();

            if (template == null) return;

            Dictionary<TabGroup, GameOptionsMenu> list = new();
            Dictionary<TabGroup, Il2CppSystem.Collections.Generic.List<OptionBehaviour>> scOptions = new();

            foreach (var tb in EnumHelper.GetAllValues<TabGroup>())
            {
                var s = new GameObject($"{tb}-Stg").AddComponent<GameOptionsMenu>();
                s.transform.SetParent(ModSettingsTab.AdvancedRolesSettings.transform.parent);
                s.transform.localPosition = new Vector3(0.7789f, -0.5101f);
                list.Add(tb, s);
            }
            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption = Object.Instantiate(template, list[option.Tab].transform);
                    if (!scOptions.ContainsKey(option.Tab))
                        scOptions[option.Tab] = new();
                    scOptions[option.Tab].Add(stringOption);
                    stringOption.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = $"<b>{option.Name}</b>";
                    stringOption.TitleText.SetOutlineColor(Color.black);
                    stringOption.TitleText.SetOutlineThickness(0.125f);
                    stringOption.Value = stringOption.oldValue = option.CurrentValue;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.name = option.Name;

                    Vector3 pos = new();
                    Vector3 scale = new();

                    //Background
                    var label = stringOption.LabelBackground;
                    {
                        label.transform.localScale = new Vector3(1.3f, 1.14f, 1f);
                        label.transform.SetLocalX(-2.2695f * w);
                        label.sprite = LabelBackground.OptionLabelBackground(option.Name) ?? UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.LabelBackground.png");
                    }
                    //プラスボタン
                    var plusButton = stringOption.PlusBtn.transform;
                    {
                        pos = plusButton.localPosition;
                        plusButton.localPosition = new Vector3(option.HideValue ? 100f : (pos.x + 1.1434f) * w, option.HideValue ? 100f : pos.y * h, option.HideValue ? 100f : pos.z);
                        scale = plusButton.localScale;
                        plusButton.localScale = new Vector3(scale.x * w, scale.y * h);
                    }
                    //マイナスボタン
                    var minusButton = stringOption.MinusBtn.transform;
                    {
                        pos = minusButton.localPosition;
                        minusButton.localPosition = new Vector3(option.HideValue ? 100f : (pos.x + 0.3463f) * w, option.HideValue ? 100f : (pos.y * h), option.HideValue ? 100f : pos.z);
                        scale = minusButton.localScale;
                        minusButton.localScale = new Vector3(scale.x * w, scale.y * h);
                    }
                    //値を表示するテキスト
                    var valueTMP = stringOption.ValueText.transform;
                    {
                        pos = valueTMP.localPosition;
                        valueTMP.localPosition = new Vector3((pos.x + 0.7322f) * w, pos.y * h, pos.z);
                        scale = valueTMP.localScale;
                        valueTMP.localScale = new Vector3(scale.x * w, scale.y * h, scale.z);
                    }
                    //上のテキストを囲む箱(ﾀﾌﾞﾝ)
                    var valueBox = stringOption.transform.FindChild("ValueBox");
                    {
                        pos = valueBox.localScale;
                        valueBox.localScale = new Vector3((pos.x + 0.2f) * w, pos.y * h, pos.z);
                        scale = valueBox.localPosition;
                        valueBox.localPosition = new Vector3((scale.x + 0.7322f) * w, scale.y * h, scale.z);
                    }
                    //タイトル(設定名)
                    var titleText = stringOption.TitleText;
                    {
                        pos = titleText.transform.localPosition;
                        titleText.transform.localPosition = new Vector3((pos.x + -1.096f) * w, pos.y * h, pos.z);
                        scale = titleText.transform.localScale;
                        titleText.transform.localScale = new Vector3(scale.x * w, scale.y * h, scale.z);
                        titleText.rectTransform.sizeDelta = new Vector2(6.5f, 0.37f);
                        titleText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                    }

                    stringOption.SetClickMask(list[option.Tab].ButtonClickMask);
                    option.OptionBehaviour = stringOption;
                }
                option.OptionBehaviour.gameObject.SetActive(true);
            }

            for (var t = 0; t < EnumHelper.GetAllValues<TabGroup>().Length; t++)
            {
                var tab = (TabGroup)t;
                list[tab].Children = scOptions[tab];
                list[tab].gameObject.SetActive(false);
                list[tab].enabled = true;
                menus.Add(tab, list[tab].gameObject);
            }

            var templateTabButton = ModSettingsTab.AllButton;

            ModSettingsTab.roleTabs = new();
            var hozon = new Il2CppSystem.Collections.Generic.List<PassiveButton>();
            hozon2 = new();

            for (var i = 0; i < EnumHelper.GetAllValues<TabGroup>().Length; i++)
            {
                var tab = EnumHelper.GetAllValues<TabGroup>()[i];

                var tabButton = Object.Instantiate(templateTabButton, templateTabButton.transform.parent);
                tabButton.name = tab.ToString();
                tabButton.transform.position = templateTabButton.transform.position + new Vector3((0.762f * i * 0.8f) + (0.762f * i * w * 0.2f), 0, -300f);
                Object.Destroy(tabButton.buttonText.gameObject);
                tabButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{tab}.png", 60);
                tabButton.activeSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_S_{tab}.png", 120);
                tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{tab}.png", 120);

                hozon.Add(tabButton);
            }

            //一旦全部作ってから
            for (var i = 0; i < EnumHelper.GetAllValues<TabGroup>().Length; i++)
            {
                var tab = EnumHelper.GetAllValues<TabGroup>()[i];
                var tabButton = hozon[i];
                if (tabButton == null) continue;

                tabButton.OnClick = new();
                tabButton.OnClick.AddListener((Action)(() =>
                {
                    for (var j = 0; j < EnumHelper.GetAllValues<TabGroup>().Length; j++)
                    {
                        var n = EnumHelper.GetAllValues<TabGroup>()[j];
                        menus[(TabGroup)j].SetActive(false);
                        hozon[j].SelectButton(false);
                        hozon[j].selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{n}.png", 120);
                    }
                    tabButton.SelectButton(true);
                    tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_S_{tab}.png", 120);
                    menus[tab].SetActive(true);
                    var tabtitle = ModSettingsTab.transform.FindChild("Scroller/SliderInner/ChancesTab/CategoryHeaderMasked").GetComponent<CategoryHeaderMasked>();
                    CategoryHeaderEditRole[] tabsubtitle = tabtitle.transform.parent.GetComponentsInChildren<CategoryHeaderEditRole>();
                    tabtitle.Title.DestroyTranslator();
                    tabtitle.Title.text = GetString("TabGroup." + tab);
                    tabtitle.Title.color = Color.white;

                    ModSettingsTab.scrollBar.velocity = Vector2.zero;
                    ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, 0, ModSettingsTab.scrollBar.Inner.localPosition.z);
                    ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                    foreach (var sub in tabsubtitle)
                    {
                        Object.Destroy(sub.gameObject);
                    }
                }));

                ModSettingsTab.roleTabs.Add(tabButton);
                hozon2.Add(tabButton);
            }

            search.transform.localPosition = new Vector3(0f, 3.5f);
            search.transform.localScale = new Vector3(0.4f, 0.4f, 0f);
            search?.gameObject?.SetActive(true);
            search.submitButton.OnPressed = (Action)(() =>
            {
                bool ch = false;
                foreach (var op in OptionItem.AllOptions)
                {
                    var name = op.GetName().RemoveHtmlTags();

                    if (name == search.textArea.text)
                    {
                        scroll(op);
                        ch = true;
                        break;
                    }
                }

                if (!ch)//完全一致したものがないなら部分一致を検索
                {
                    foreach (var op in OptionItem.AllOptions)
                    {
                        var name = op.GetName().RemoveHtmlTags();
                        if (name.Contains(search.textArea.text))
                        {
                            scroll(op);
                            break;
                        }
                    }
                }
                search.textArea.Clear();

                //スクロール処理
                void scroll(OptionItem op)
                {
                    var opt = op;
                    while (opt.Parent != null && !opt.GetBool())
                    {
                        opt = opt.Parent;
                    }

                    int tabIndex = (int)opt.Tab;

                    if (tabIndex >= 0 && tabIndex < hozon2.Count && hozon2[tabIndex] != null)
                    {
                        hozon2[tabIndex].OnClick.Invoke();
                    }

                    _ = new LateTask(() =>
                    {
                        if (!(ModSettingsTab?.gameObject?.active ?? false)) return;
                        ModSettingsTab.scrollBar.velocity = Vector2.zero;
                        var relativePosition = ModSettingsTab.scrollBar.transform.InverseTransformPoint(opt.OptionBehaviour.transform.FindChild("Title Text").transform.position);// Scrollerのローカル空間における座標に変換
                        var scrollAmount = 1 - relativePosition.y;
                        ModSettingsTab.scrollBar.Inner.localPosition = ModSettingsTab.scrollBar.Inner.localPosition + Vector3.up * scrollAmount;  // 強制スクロール
                        ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                    }, 0.1f, "", true);
                }
            });

            ModSettingsButton.OnClick = new();
            ModSettingsButton.OnClick.AddListener((Action)(() =>
            {
                __instance.ChangeTab(3, false);

                if (dasu)
                {
                    _ = new LateTask(() =>
                    {
                        if (!(ModSettingsTab?.gameObject?.active ?? false)) return;
                        dasu = false;
                        if (hozon2[0] != null)
                            hozon2[0].GetComponent<PassiveButton>().OnClick.Invoke();
                    }, 0.05f, "", true);
                }
            }));

            __instance.GameSettingsTab.gameObject.SetActive(false);

            // ボタン生成
            CreateButton("OptionReset", Color.red, new Vector2(8.5f, 0f), new Action(() =>
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0 && x.Id is not 2 and not 3 && 1_000_000 > x.Id && x.CurrentValue != x.DefaultValue).Do(x => x.SetValue(x.DefaultValue));
                var pr = OptionItem.AllOptions.Where(op => op.Id == 0).FirstOrDefault();
                switch (pr.CurrentValue)
                {
                    case 0: Main.Preset1.Value = GetString("Preset_1"); break;
                    case 1: Main.Preset2.Value = GetString("Preset_2"); break;
                    case 2: Main.Preset3.Value = GetString("Preset_3"); break;
                    case 3: Main.Preset4.Value = GetString("Preset_4"); break;
                    case 4: Main.Preset5.Value = GetString("Preset_5"); break;
                    case 5: Main.Preset6.Value = GetString("Preset_6"); break;
                    case 6: Main.Preset7.Value = GetString("Preset_7"); break;
                }
                GameSettingMenuChangeTabPatch.meg = GetString("OptionResetMeg");
                reset();
            }), UtilsSprite.LoadSprite("TownOfHost.Resources.RESET-STG.png", 150f));
            CreateButton("OptionCopy", Color.green, new Vector2(7.89f, 0), new Action(() =>
            {
                OptionSerializer.SaveToClipboard();
                GameSettingMenuChangeTabPatch.meg = GetString("OptionCopyMeg");
                reset();
            }), UtilsSprite.LoadSprite("TownOfHost.Resources.COPY-STG.png", 180f), true);
            CreateButton("OptionLoad", Color.green, new Vector2(7.28f, 0), new Action(() =>
            {
                OptionSerializer.LoadFromClipboard();
                GameSettingMenuChangeTabPatch.meg = GetString("OptionLoadMeg");
                reset();

            }), UtilsSprite.LoadSprite("TownOfHost.Resources.LOAD-STG.png", 180f));

            void CreateButton(string text, Color color, Vector2 position, Action action, Sprite sprite = null, bool csize = false)
            {
                var ToggleButton = Object.Instantiate(HudManager.Instance.SettingsButton.GetComponent<PassiveButton>(), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)").transform);
                ToggleButton.GetComponent<AspectPosition>().DistanceFromEdge += new Vector3(position.x, 0, 200f);
                ToggleButton.transform.localScale -= new Vector3(0.25f * w, 0.25f * h);
                ToggleButton.name = text;
                if (sprite != null)
                {
                    ToggleButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                    ToggleButton.activeSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                    ToggleButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                }
                var textTMP = new GameObject("Text_TMP").AddComponent<TMPro.TextMeshPro>();
                textTMP.text = Utils.ColorString(color, GetString(text));
                textTMP.transform.SetParent(ToggleButton.transform);
                textTMP.transform.localPosition = new Vector3(0.8f, 0.8f);
                textTMP.transform.localScale = new Vector3(0, -0.5f);
                textTMP.alignment = TMPro.TextAlignmentOptions.Top;
                textTMP.fontSize = 10f;
                ToggleButton.OnClick = new();
                ToggleButton.OnClick.AddListener(action);
            }
            static void reset()
            {
                _ = new LateTask(() =>
                {
                    var rand = IRandom.Instance;
                    int rect = IRandom.Instance.Next(1, 101);
                    if (rect < 40)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo0");
                    else if (rect < 50)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo10");
                    else if (rect < 60)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo1");
                    else if (rect < 70)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo2");
                    else if (rect < 80)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo3");
                    else if (rect < 90)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo4");
                    else if (rect < 95)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo5");
                    else if (rect < 99)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo6");
                    else
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo7");
                }, 3, "SetModSettingInfo", true);
            }
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
    class PrisetNamechengePatch
    {
        public static void Postfix(StringOption __instance)
        {
            if (ModSettingsTab == null) return;

            var option = OptionItem.AllOptions.Where(opt => opt.Id == 0).FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return;

            __instance.ValueText.text = option.GetString();
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class Prisetkesu
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            __instance.ChangeTab(1, false);
            _ = new LateTask(() => __instance.ChangeTab(1, false), 0.2f, "", true);
            GameSettingMenuChangeTabPatch.Hima = 0;
        }
    }
    class LabelBackground
    {
        public static Sprite OptionLabelBackground(string OptionName)
        {
            var path = "TownOfHost.Resources.Label.";
            var la = "LabelBackground.png";
            return OptionName switch
            {
                "MapModification" => UtilsSprite.LoadSprite($"{path}MapModification{la}"),
                "MadmateOption" => UtilsSprite.LoadSprite($"{path}MadmateOption{la}"),
                "Sabotage" => UtilsSprite.LoadSprite($"{path}Sabotage{la}"),
                "RandomSpawn" => UtilsSprite.LoadSprite($"{path}RandomSpawn{la}"),
                "Preset" => UtilsSprite.LoadSprite($"{path}Preset{la}"),
                "GameMode" => UtilsSprite.LoadSprite($"{path}GameMode{la}"),
                "Shyboy" => UtilsSprite.LoadSprite($"{path}Shyboy{la}"),
                "MadTeller" => UtilsSprite.LoadSprite($"{path}Madteller{la}"),
                "PonkotuTeller" => UtilsSprite.LoadSprite($"{path}PonkotuTeller{la}"),
                "FortuneTeller" => UtilsSprite.LoadSprite($"{path}FortuneTeller{la}"),
                "AmateurTeller" => UtilsSprite.LoadSprite($"{path}AmateurTeller{la}"),
                "NiceAddoer" => UtilsSprite.LoadSprite($"{path}NiceAddoer{la}"),
                "EvilAddoer" => UtilsSprite.LoadSprite($"{path}EvilAddoer{la}"),
                "Alien" => UtilsSprite.LoadSprite($"{path}Alien{la}"),
                "JackalAlien" => UtilsSprite.LoadSprite($"{path}JackalAlien{la}"),
                "DevicesOption" => UtilsSprite.LoadSprite($"{path}Device{la}"),
                "Jumper" => UtilsSprite.LoadSprite($"{path}Jumper{la}"),
                "LadderDeath" => UtilsSprite.LoadSprite($"{path}LadderDeath{la}"),
                "ONspecialMode" => UtilsSprite.LoadSprite($"{path}ONspecialMode{la}"),
                "UltraStar" => UtilsSprite.LoadSprite($"{path}UltraStar{la}"),
                _ => null,
            };
        }
    }
}