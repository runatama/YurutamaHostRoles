using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using Object = UnityEngine.Object;
using TownOfHost.Roles.Core;
using AmongUs.GameOptions;
using Il2CppSystem.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;
        public static string find = "";

        public static void Postfix(GameOptionsMenu __instance)
        {
            //タイマー表示
            /*if (GameOptionsMenuPatch.Timer != null)
            {
                var rtimer = GameStartManagerPatch.GetTimer();
                rtimer = Mathf.Max(0f, rtimer -= Time.deltaTime);
                int minutes = (int)rtimer / 60;
                int seconds = (int)rtimer % 60;

                string Color = "<color=#ffffff>";
                if (minutes <= 4) Color = "<color=#9acd32>";//5分切ったら
                if (minutes <= 2) Color = "<color=#ffa500>";//3分切ったら。
                if (minutes <= 0) Color = "<color=red>";//1分切ったら。
                GameOptionsMenuPatch.Timer.text = $"{Color}{minutes:00}:{seconds:00}";
            }
            */

            if (__instance.transform.name == "GAME SETTINGS TAB") return;
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                if (__instance.gameObject.name != tab + "-Stg") continue;

                _timer += Time.deltaTime;
                if (_timer < 0.1f) return;
                _timer = 0f;

                float numItems = __instance.Children.Count;
                var offset = 2.7f;
                var y = 0.713f;

                foreach (var option in OptionItem.AllOptions)
                {
                    if ((TabGroup)tab != option.Tab) continue;
                    if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;
                    var enabled = true;
                    var parent = option.Parent;

                    enabled = AmongUsClient.Instance.AmHost &&
                        !option.IsHiddenOn(Options.CurrentGameMode);
                    if (enabled && find != "")
                    {
                        enabled = option.Name.ToLower().Contains(find.ToLower())
                        || (Enum.TryParse(typeof(CustomRoles), option.Name, true, out var role)
                         ? Utils.GetCombinationCName((CustomRoles)role, false).ToLower().Contains(find.ToLower())
                         : GetString(option.Name).ToLower().Contains(find.ToLower()));
                    }
                    var opt = option.OptionBehaviour.transform.Find("LabelBackground").GetComponent<SpriteRenderer>();

                    opt.size = new(5.0f, 0.68f);
                    //opt.enabled = false;
                    if (parent == null) opt.color = new Color32(200, 200, 200, 255);
                    if (option.Tab is TabGroup.MainSettings && (option.NameColor != Color.white || option.NameColorCode != "#ffffff"))
                    {
                        var color = option.NameColor == Color.white ? StringHelper.CodeColor(option.NameColorCode) : option.NameColor;

                        opt.color = color.ShadeColor(-6);
                    }
                    if (Options.CustomRoleSpawnChances.ContainsValue(option as IntegerOptionItem))
                    {
                        opt.color = option.NameColor.ShadeColor(-5);
                    }
                    while (parent != null && enabled)
                    {
                        enabled = parent.GetBool();
                        parent = parent.Parent;
                        opt.color = new Color32(40, 50, 80, 255);

                        opt.size = new(4.6f, 0.68f);
                        option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.8566f, 0f);
                        option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.4f, 0.6f);
                        if (option.Parent?.Parent != null)
                        {
                            opt.color = new Color32(20, 60, 40, 255);
                            opt.size = new(4.4f, 0.68f);
                            option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.7566f, 0f);
                            option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.35f, 0.6f);
                            if (option.Parent?.Parent?.Parent != null)
                            {
                                opt.color = new Color32(60, 20, 40, 255);
                                opt.size = new(4.2f, 0.68f);
                                option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.6566f, 0f);
                                option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.3f, 0.6f);
                                if (option.Parent?.Parent?.Parent?.Parent != null)
                                {
                                    opt.color = new Color32(60, 40, 10, 255);
                                    opt.size = new(4.0f, 0.68f);
                                    option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.6566f, 0f);
                                    option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.25f, 0.6f);
                                }
                            }
                        }
                    }

                    option.OptionBehaviour.gameObject.SetActive(enabled);
                    if (enabled)
                    {
                        offset -= option.IsHeader ? 0.68f : 0.45f;
                        option.OptionBehaviour.transform.localPosition = new Vector3(
                            option.OptionBehaviour.transform.localPosition.x,//0.952f,
                            offset - 1.5f,//y,
                            option.OptionBehaviour.transform.localPosition.z);//-120f);
                        y -= option.IsHeader ? 0.68f : 0.45f;

                        if (option.IsHeader)
                        {
                            numItems += 0.5f;
                        }
                    }
                    else
                    {
                        numItems--;
                    }
                }
                __instance.GetComponentInParent<Scroller>().ContentYBounds.max = -offset + 0.75f;
            }
        }
    }
    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    public class StringOptionInitializePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            /*
            string mark = "";
            string addinfo = "";

            if (Enum.TryParse(typeof(CustomRoles), option.Name, false, out var id))
            {
                var role = (CustomRoles)id;
                if (role.IsAddOn())
                {
                    List<CustomRoles> list = new(1) { role };
                    mark = Utils.GetSubRoleMarks(list, CustomRoles.NotAssigned);
                    addinfo = "\n<size=1> " + GetString($"{role}InfoLong").Split('\n')[1] + "</size>";
                }
            }*/

            var role = CustomRoles.NotAssigned;
            var size = "<size=105%>";
            string mark = "";
            if (Enum.TryParse(typeof(CustomRoles), option.Name, false, out var id))
            {
                role = (CustomRoles)id;
                size = "<size=125%>";
                if (role.IsAddOn())
                {
                    List<CustomRoles> list = new(1) { role };
                    mark = $" {Utils.GetSubRoleMarks(list, CustomRoles.NotAssigned)}";
                }
            }
            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = size + "<b>" + option.GetName(isoption: true) + mark + option.Fromtext + "</b></size>"/* + addinfo*/;
            /*if (!(option.NameColor == Color.white && option.NameColorCode == "#ffffff"))
            {
                ColorUtility.TryParseHtmlString(option.NameColorCode, out var colorcode);
                __instance.TitleText.color.linear.ShadeColor(1);// = Color.black;
                __instance.TitleText.SetOutlineColor((option.NameColor == Color.white ? colorcode : option.NameColor).ShadeColor(-0.75f));
                __instance.TitleText.SetOutlineThickness(0.15f);
            }*/
            __instance.Value = __instance.oldValue = option.CurrentValue;
            __instance.ValueText.text = option.GetString();

            return false;
        }
    }
    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
    public class NumberOptionIncreasePatch
    {
        public static bool Prefix(NumberOption __instance)
        {
            if (__instance.floatOptionName is FloatOptionNames.ImpostorLightMod or FloatOptionNames.CrewLightMod)
            {
                if (__instance.Value < 2f)
                {
                    var che = true;
                    switch (__instance.Value)
                    {
                        case 0.25f: __instance.Value = 0.38f; break;
                        case 0.38f: __instance.Value = 0.5f; break;
                        case 0.5f: __instance.Value = 0.63f; break;
                        case 0.63f: __instance.Value = 0.75f; break;
                        case 0.75f: __instance.Value = 0.88f; break;
                        case 0.88f: __instance.Value = 1f; break;
                        case 1.00f: __instance.Value = 1.13f; break;
                        case 1.13f: __instance.Value = 1.25f; break;
                        case 1.25f: __instance.Value = 1.38f; break;
                        case 1.38f: __instance.Value = 1.5f; break;
                        case 1.5f: __instance.Value = 1.63f; break;
                        case 1.63f: __instance.Value = 1.75f; break;
                        case 1.75f: __instance.Value = 1.88f; break;
                        case 1.88f: __instance.Value = 2f; break;
                        default: che = false; break;
                    }
                    if (!che) return true;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    var v = __instance.Increment * 5 + __instance.Value;
                    if (__instance.ValidRange.max <= v) v = __instance.ValidRange.max;
                    __instance.Value = v;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            //if (option.Id == 1 && option.CurrentValue == 1 && !Main.TaskBattleOptionv) option.CurrentValue++;
            if (option.Name == "KickModClient") Main.LastKickModClient.Value = true;
            option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }
    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
    public class NumberOptionDecreasePatch
    {
        public static bool Prefix(NumberOption __instance)
        {
            if (__instance.floatOptionName is FloatOptionNames.ImpostorLightMod or FloatOptionNames.CrewLightMod)
            {
                if (__instance.Value <= 2f)
                {
                    var che = true;
                    switch (__instance.Value)
                    {
                        case 0.25f: __instance.Value = 0.25f; break;
                        case 0.38f: __instance.Value = 0.25f; break;
                        case 0.5f: __instance.Value = 0.38f; break;
                        case 0.63f: __instance.Value = 0.5f; break;
                        case 0.75f: __instance.Value = 0.63f; break;
                        case 0.88f: __instance.Value = 0.75f; break;
                        case 1f: __instance.Value = 0.88f; break;
                        case 1.13f: __instance.Value = 1f; break;
                        case 1.25f: __instance.Value = 1.13f; break;
                        case 1.38f: __instance.Value = 1.25f; break;
                        case 1.5f: __instance.Value = 1.38f; break;
                        case 1.63f: __instance.Value = 1.5f; break;
                        case 1.75f: __instance.Value = 1.63f; break;
                        case 1.88f: __instance.Value = 1.75f; break;
                        case 2.00f: __instance.Value = 1.88f; break;
                        default: che = false; break;
                    }
                    if (!che) return true;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    var v = __instance.Increment * -5 + __instance.Value;
                    if (__instance.ValidRange.min >= v) v = __instance.ValidRange.min;
                    __instance.Value = v;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            //if (option.Id == 1 && option.CurrentValue == 0 && !Main.TaskBattleOptionv) option.CurrentValue--;
            if (option.Name == "KickModClient") Main.LastKickModClient.Value = false;
            option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class GameSettingMenuStartPatch
    {
        public static bool dasu;
        public static PassiveButton ModSettingsButton;
        public static RolesSettingsMenu ModSettingsTab;
        static Il2CppSystem.Collections.Generic.List<PassiveButton> hozon2;
        public static void Postfix(GameSettingMenu __instance)
        {
            var GamePresetButton = GameObject.Find("LeftPanel/GamePresetButton");
            var GameSettingsButton = GameObject.Find("LeftPanel/GameSettingsButton");
            var RoleSettingsButton = GameObject.Find("LeftPanel/RoleSettingsButton");

            var GamePresetButtons = GamePresetButton.GetComponent<PassiveButton>();

            var ModStgButton = GameObject.Instantiate(RoleSettingsButton, RoleSettingsButton.transform.parent);

            GamePresetButtons.gameObject.SetActive(false);
            RoleSettingsButton.gameObject.SetActive(false);

            ModSettingsButton = ModStgButton.GetComponent<PassiveButton>();
            ModSettingsButton.buttonText.text = "TownOfHost-Kの設定";
            var activeSprite = ModSettingsButton.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = ModSettingsButton.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = StringHelper.CodeColor(Main.ModColor);
            selectedSprite.color = StringHelper.CodeColor(Main.ModColor).ShadeColor(-0.2f);
            ModSettingsButton.buttonText.DestroyTranslator();//翻訳破壊☆

            var roleTab = GameObject.Find("ROLES TAB");
            ModSettingsTab = GameObject.Instantiate(__instance.RoleSettingsTab, __instance.RoleSettingsTab.transform.parent).GetComponent<RolesSettingsMenu>();

            Dictionary<TabGroup, GameObject> menus = new();

            GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB").SetActive(true);

            var template = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)").GetComponent<StringOption>();
            if (template == null) return;

            Dictionary<TabGroup, GameOptionsMenu> list = new();
            Dictionary<TabGroup, Il2CppSystem.Collections.Generic.List<OptionBehaviour>> scOptions = new();

            foreach (var tb in EnumHelper.GetAllValues<TabGroup>())
            {
                var s = new GameObject($"{tb}-Stg");
                s.AddComponent<GameOptionsMenu>();
                s.transform.SetParent(ModSettingsTab.AdvancedRolesSettings.transform.parent);
                s.transform.localPosition = new Vector3(0.7789f, -0.5101f);
                list.Add(tb, s.GetComponent<GameOptionsMenu>());
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
                    stringOption.transform.FindChild("LabelBackground").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.LabelBackground.png");
                    stringOption.transform.FindChild("LabelBackground").localScale = new Vector3(1.3f, 1.14f, 1f);
                    stringOption.transform.FindChild("LabelBackground").SetLocalX(-2.2695f);
                    stringOption.transform.FindChild("PlusButton").localPosition += new Vector3(option.HideValue ? 100f : 1.1434f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                    stringOption.transform.FindChild("MinusButton").localPosition += new Vector3(option.HideValue ? 100f : 0.3463f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                    stringOption.transform.FindChild("Value_TMP (1)").localPosition += new Vector3(0.7322f, 0f, 0f);
                    stringOption.transform.FindChild("ValueBox").localScale += new Vector3(0.2f, 0f, 0f);
                    stringOption.transform.FindChild("ValueBox").localPosition += new Vector3(0.7322f, 0f, 0f);
                    stringOption.transform.FindChild("Title Text").localPosition += new Vector3(-1.096f, 0f, 0f);
                    stringOption.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.5f, 0.37f);
                    stringOption.transform.FindChild("Title Text").GetComponent<TMPro.TextMeshPro>().alignment = TMPro.TextAlignmentOptions.MidlineLeft;
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
                tabButton.transform.position = templateTabButton.transform.position + new Vector3(0.762f * i, 0f);
                Object.Destroy(tabButton.buttonText.gameObject);
                tabButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_{tab}.png", 60);
                tabButton.activeSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_S_{tab}.png", 120);
                tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_{tab}.png", 120);

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
                        hozon[j].selectedSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_{n}.png", 120);
                    }
                    tabButton.SelectButton(true);
                    tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_S_{tab}.png", 120);
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

            GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB").SetActive(false);

            // ボタン生成
            CreateButton("OptionReset", Color.red, new Vector2(8.5f, 0f), new Action(() =>
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue));
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
            }), Utils.LoadSprite("TownOfHost.Resources.RESET-STG.png", 150f));
            CreateButton("OptionCopy", Color.green, new Vector2(7.3f, -0.035f), new Action(() =>
            {
                OptionSerializer.SaveToClipboard();
            }), Utils.LoadSprite("TownOfHost.Resources.COPY-STG.png", 180f), true);
            CreateButton("OptionLoad", Color.green, new Vector2(7.3f + 0.125f, 0), new Action(() =>
            {
                OptionSerializer.LoadFromClipboard();
            }), Utils.LoadSprite("TownOfHost.Resources.LOAD-STG.png", 180f));

            static void CreateButton(string text, Color color, Vector2 position, Action action, Sprite sprite = null, bool csize = false)
            {
                var ToggleButton = Object.Instantiate(csize ? HudManager.Instance.Chat.chatButton : HudManager.Instance.SettingsButton.GetComponent<PassiveButton>(), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)").transform);
                ToggleButton.GetComponent<AspectPosition>().DistanceFromEdge += new Vector3(position.x, position.y, 0f);
                ToggleButton.transform.localScale -= new Vector3(0.25f, 0.25f);
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
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class Prisetkesu
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            __instance.ChangeTab(1, false);
            GameSettingMenuChangeTabPatch.Hima = 0;
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
    class GameSettingMenuChangeTabPatch
    {
        public static string meg;
        public static bool Prefix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (!previewOnly)
            {
                var ModSettingsTab = GameSettingMenuStartPatch.ModSettingsTab;
                if (!ModSettingsTab) return true;
                ModSettingsTab.gameObject.SetActive(false);
                GameSettingMenuStartPatch.ModSettingsButton.SelectButton(false);
                if (tabNum != 3) return true;
                ModSettingsTab.gameObject.SetActive(true);

                __instance.MenuDescriptionText.text = meg;

                __instance.ToggleLeftSideDarkener(true);
                __instance.ToggleRightSideDarkener(false);
                ControllerManager.Instance.OpenOverlayMenu(__instance.name, __instance.BackButton, __instance.DefaultButtonSelected, __instance.ControllerSelectable);
                GameSettingMenuStartPatch.ModSettingsButton.SelectButton(true);
            }
            return true;
        }
        static int Last = 0;
        public static int Hima = 0;
        public static void Postfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            Logger.Info($"{tabNum}", "OPH");
            if (previewOnly) return;

            var l = Last;
            if (tabNum == Last && tabNum == 3)
            {
                Hima++;
                GameSettingMenuStartPatch.dasu = false;
            }
            if (tabNum != Last)
            {
                if (tabNum == 3) GameSettingMenuStartPatch.dasu = true;
                else GameSettingMenuStartPatch.dasu = false;
                Last = tabNum;
            }
            if (100 > Hima)
            {
                var rand = IRandom.Instance;
                int rect = IRandom.Instance.Next(1, 101);
                if (rect < 40)
                    meg = GetString("ModSettingInfo0");
                else if (rect < 50)
                    meg = GetString("ModSettingInfo10");
                else if (rect < 60)
                    meg = GetString("ModSettingInfo1");
                else if (rect < 70)
                    meg = GetString("ModSettingInfo2");
                else if (rect < 80)
                    meg = GetString("ModSettingInfo3");
                else if (rect < 90)
                    meg = GetString("ModSettingInfo4");
                else if (rect < 95)
                    meg = GetString("ModSettingInfo5");
                else if (rect < 99)
                    meg = GetString("ModSettingInfo6");
                else
                    meg = GetString("ModSettingInfo7");
            }
            else meg = GetString("ModSettingInfo9");

            if (tabNum == 1)
            {
                _ = new LateTask(() =>
                {
                    foreach (var ob in __instance.GameSettingsTab.Children)
                    {
                        switch (ob.Title)
                        {
                            case StringNames.GameShortTasks:
                            case StringNames.GameLongTasks:
                            case StringNames.GameCommonTasks:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                                break;
                            case StringNames.GameKillCooldown:
                                ob.Cast<NumberOption>().Increment = 0.5f;
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.GameNumImpostors:
                                if (DebugModeManager.IsDebugMode)
                                {
                                    ob.Cast<NumberOption>().ValidRange.min = 0;
                                }
                                break;
                            case StringNames.GameTaskBarMode:
                                ob.transform.position = new Vector3(999f, 999f);
                                break;
                            case StringNames.GameConfirmImpostor:
                                ob.transform.position = new Vector3(999f, 999f);
                                break;
                            case StringNames.GameVotingTime:
                            case StringNames.GameEmergencyCooldown:
                            case StringNames.GameDiscussTime:
                                ob.Cast<NumberOption>().Increment = 1f;
                                break;
                            default:
                                break;
                        }
                    }
                    GameOptionsSender.RpcSendOptions();
                }, 0.02f);
            }
            if (tabNum == 2)
            {
                _ = new LateTask(() =>
                {
                    foreach (var ob in __instance.RoleSettingsTab.advancedSettingChildren)
                    {
                        switch (ob.Title)
                        {
                            case StringNames.GuardianAngelRole:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 0);
                                ob.enabled = false;
                                break;
                            case StringNames.EngineerCooldown:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.ShapeshifterCooldown:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.ScientistCooldown:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.ScientistBatteryCharge:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.TrackerCooldown:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.TrackerDelay:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.TrackerDuration:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.PhantomCooldown:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.PhantomDuration:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.NoisemakerAlertDuration:
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            default:
                                break;
                        }
                    }
                }, 0.02f);
            }

            if (tabNum != 3 || l == tabNum) return;
            //var length = GameSettingMenuStartPatch.ModSettingsTab.roleChances.ToArray().Length;
            /*_ = new LateTask(() =>
            {*/
            Logger.Info("!", "!");
            var dd = GameSettingMenuStartPatch.ModSettingsTab.AllButton.transform.parent.GetComponentsInChildren<RoleSettingsTabButton>();
            Logger.Info("2!", "!");
            foreach (Component aaa in dd)
            {
                Object.Destroy(aaa.gameObject);
            }
            Logger.Info("3!", "!");
            if (GameSettingMenuStartPatch.ModSettingsTab.roleChances != null)
                foreach (var option in GameSettingMenuStartPatch.ModSettingsTab.roleChances?.ToArray())
                    Object.Destroy(option?.gameObject);
            Logger.Info("4!", "!");
            GameSettingMenuStartPatch.ModSettingsTab.roleChances = new();
            Logger.Info("5!", "!");
            Object.Destroy(GameSettingMenuStartPatch.ModSettingsTab?.AllButton?.gameObject);
            //}, 0.02f, "", true);
            /*_ = new LateTask(() =>
            {
                if (length != 0) //動かない
                {
                    GameSettingMenuStartPatch.ModSettingsTab.transform.FindChild("HeaderButtons/MainSettings").GetComponent<PassiveButton>().OnClick.Invoke();
                }
            }, 0.02f);*/
        }
    }
}