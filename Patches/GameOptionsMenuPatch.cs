using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using Object = UnityEngine.Object;
using TownOfHost.Roles.Core;
using System.Reflection.Metadata;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.OpenMenu))]
    [HarmonyPriority(Priority.First)]
    public static class GameOptionsMenuPatch
    {
        public static TMPro.TextMeshPro Timer; //ここのクラスは関係ない
        public static void Postfix(GameOptionsMenu __instance)
        {
            foreach (var ob in __instance.Children)
            {
                switch (ob.Title)
                {
                    case StringNames.GameShortTasks:
                    case StringNames.GameLongTasks:
                    case StringNames.GameCommonTasks:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                        break;
                    case StringNames.GameKillCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.GameNumImpostors:
                        if (DebugModeManager.IsDebugMode)
                        {
                            ob.Cast<NumberOption>().ValidRange.min = 0;
                        }
                        break;
                    default:
                        break;
                }
            }
            var template = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
            if (template == null) return;

            var gameSettings = GameObject.Find("Game Settings");
            if (gameSettings == null) return;
            gameSettings.transform.FindChild("GameGroup").GetComponent<Scroller>().ScrollWheelSpeed = 1f;

            var gameSettingMenu = Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
            if (gameSettingMenu == null) return;
            List<GameObject> menus = new() { gameSettingMenu.GameSettingsTab.gameObject, gameSettingMenu.RoleSettingsTab.gameObject };
            List<SpriteRenderer> highlights = new() { gameSettingMenu.GameSettingsButton.HeldButtonSprite, gameSettingMenu.RoleSettingsButton.HeldButtonSprite };

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");
            List<GameObject> tabs = new() { gameTab, roleTab };

            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                var obj = gameSettings.transform.parent.Find(tab + "Tab");
                if (obj != null)
                {
                    obj.transform.FindChild("../../GameGroup/Text").GetComponent<TMPro.TextMeshPro>().SetText(GetString("TabGroup." + tab));
                    continue;
                }

                var tohkSettings = Object.Instantiate(gameSettings, gameSettings.transform.parent);
                tohkSettings.name = tab + "Tab";
                tohkSettings.transform.FindChild("BackPanel").transform.localScale =
                tohkSettings.transform.FindChild("Bottom Gradient").transform.localScale = new Vector3(1.2f, 1f, 1f);
                tohkSettings.transform.FindChild("Background").transform.localScale = new Vector3(1.3f, 1f, 1f);
                tohkSettings.transform.FindChild("UI_Scrollbar").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                tohkSettings.transform.FindChild("UI_ScrollbarTrack").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                tohkSettings.transform.FindChild("GameGroup/SliderInner").transform.localPosition += new Vector3(-0.15f, 0f, 0f);
                var tohkMenu = tohkSettings.transform.FindChild("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();

                //OptionBehaviourを破棄
                tohkMenu.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));

                var scOptions = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
                foreach (var option in OptionItem.AllOptions)
                {
                    if (option.Tab != (TabGroup)tab) continue;
                    if (option.OptionBehaviour == null)
                    {
                        var stringOption = Object.Instantiate(template, tohkMenu.transform);
                        scOptions.Add(stringOption);
                        stringOption.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
                        stringOption.TitleText.text = option.Name;
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = option.GetString();
                        stringOption.name = option.Name;
                        stringOption.transform.FindChild("Background").localScale = new Vector3(1.2f, 1f, 1f);
                        stringOption.transform.FindChild("Plus_TMP").localPosition += new Vector3(option.HideValue ? 100f : 0.3f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                        stringOption.transform.FindChild("Minus_TMP").localPosition += new Vector3(option.HideValue ? 100f : 0.3f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                        stringOption.transform.FindChild("Value_TMP").localPosition += new Vector3(0.9709f, 0f, 0f);
                        stringOption.transform.FindChild("Title_TMP").localPosition += new Vector3(0.15f, 0f, 0f);
                        stringOption.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.5f, 0.37f);

                        option.OptionBehaviour = stringOption;
                    }
                    option.OptionBehaviour.gameObject.SetActive(true);
                }
                tohkMenu.Children = scOptions;
                tohkSettings.gameObject.SetActive(false);
                menus.Add(tohkSettings.gameObject);

                var tohkTab = Object.Instantiate(roleTab, roleTab.transform.parent);
                tohkTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite($"TownOfHost.Resources.TabIcon_{tab}.png", 100f);
                tabs.Add(tohkTab);
                var tohkTabHighlight = tohkTab.transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();
                highlights.Add(tohkTabHighlight);
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                tabs[i].transform.localPosition = new(0.8f * (i - 0.9f) - tabs.Count / 3f - (i <= 1 ? 0 : 0.5f), tabs[i].transform.localPosition.y, tabs[i].transform.localPosition.z);
                //無限の彼方へ!!
                tabs[1].transform.localScale = new(0f, 0f);
                tabs[1].transform.localPosition = new(999f, 999f);

                var button = tabs[i].GetComponentInChildren<PassiveButton>();
                if (button == null) continue;
                var copiedIndex = i;
                button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                Action value = () =>
                {
                    for (var j = 0; j < menus.Count; j++)
                    {
                        menus[j].SetActive(j == copiedIndex);
                        highlights[j].enabled = j == copiedIndex;
                    }
                };
                button.OnClick.AddListener(value);
            }
            /*
                        // ボタン生成
                        CreateButton("OptionReset", Color.red, new Vector2(-4f, 10.8f), new Action(() =>
                        {
                            OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue));
                        }));
                        CreateButton("OptionCopy", Color.green, new Vector2(-4f, -10.2f), new Action(() =>
                        {
                            OptionSerializer.SaveToClipboard();
                        }));
                        CreateButton("OptionLoad", Color.green, new Vector2(-4f, -10.5f), new Action(() =>
                        {
                            OptionSerializer.LoadFromClipboard();
                        }));

                        var timergobj = GameObject.Find("Timer");
                        if (timergobj != null)
                        {
                            Timer = GameObject.Instantiate(GameObject.Find("Timer"), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainSettingsTab").transform.parent)
                                .GetComponent<TMPro.TextMeshPro>();
                            Timer.name = "MenuTimer";
                            Timer.transform.localPosition = new Vector3(-4.85f, 2.1f, -30f);
                            Timer.transform.localScale = new Vector2(0.8f, 0.8f);
                        }

                        static void CreateButton(string text, Color color, Vector2 position, Action action)
                        {
                            var ToggleButton = Object.Instantiate(GameObject.Find("Main Camera/Hud/Menu/GeneralTab/ControlGroup/JoystickModeButton"), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainSettingsTab").transform);
                            ToggleButton.transform.localPosition = new Vector3(position.x, position.y, -15f);
                            ToggleButton.transform.localScale = new Vector2(0.6f, 0.6f);
                            ToggleButton.name = text;
                            ToggleButton.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = Translator.GetString(text);
                            ToggleButton.transform.FindChild("Background").GetComponent<SpriteRenderer>().color = color;
                            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
                            passiveButton.OnClick = new();
                            passiveButton.OnClick.AddListener(action);
                        }*/
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;
        public static string find = "";

        public static void Postfix(GameOptionsMenu __instance)
        {
            //タイマー表示
            if (GameOptionsMenuPatch.Timer != null)
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
                    //起動時以外で表示/非表示を切り替える際に使う
                    /*if (enabled)
                    {
                        switch (option.Name)
                        {
                            case "KickModClient":
                                if (!ModUpdater.nothostbug)
                                {
                                    if (Options.KickModClient.GetBool())
                                        Options.KickModClient.SetValue(0, true);
                                }
                                else
                                {
                                    if (!Options.KickModClient.GetBool() && Main.LastKickModClient.Value)
                                        Options.KickModClient.SetValue(1, true);
                                }
                                enabled = ModUpdater.nothostbug;
                                break;
                        }
                    }*/
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
                    if (parent == null) opt.color = new Color32(180, 180, 180, 255);

                    while (parent != null && enabled)
                    {
                        //Memo.なんか濁った感じのあんまり綺麗じゃない色だからどうにかしたい。
                        //それか文字縁取りかな...見辛い。
                        enabled = parent.GetBool();
                        parent = parent.Parent;
                        opt.color = new Color32(90, 100, 120, 255);

                        opt.size = new(4.6f, 0.68f);
                        option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.8566f, 0f);
                        option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.4f, 0.6f);
                        if (option.Parent?.Parent != null)
                        {
                            opt.color = new Color32(90, 140, 110, 255);
                            opt.size = new(4.4f, 0.68f);
                            option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.7566f, 0f);
                            option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.35f, 0.6f);
                            if (option.Parent?.Parent?.Parent != null)
                            {
                                opt.color = new Color32(140, 80, 110, 255);
                                opt.size = new(4.2f, 0.68f);
                                option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.6566f, 0f);
                                option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.3f, 0.6f);
                                if (option.Parent?.Parent?.Parent?.Parent != null)
                                {
                                    opt.color = new Color32(140, 120, 80, 255);
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
            /*string mark = "";
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

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = "<b>" + option.GetName() /*+ mark */ + option.Fromtext + "</b>"/* + addinfo*/;
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
    /*
    [HarmonyPatch(typeof(NormalGameOptionsV08), nameof(NormalGameOptionsV08.SetRecommendations))]
    public static class SetRecommendationsPatch
    {
        public static bool Prefix(NormalGameOptionsV08 __instance, int numPlayers, bool isOnline)
        {
            numPlayers = Mathf.Clamp(numPlayers, 4, 15);
            __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
            __instance.CrewLightMod = 0.5f;
            __instance.ImpostorLightMod = 1.75f;
            __instance.KillCooldown = 25f;
            __instance.NumCommonTasks = 2;
            __instance.NumLongTasks = 4;
            __instance.NumShortTasks = 6;
            __instance.NumEmergencyMeetings = 1;
            if (!isOnline)
                __instance.NumImpostors = NormalGameOptionsV08.RecommendedImpostors[numPlayers];
            __instance.KillDistance = 0;
            __instance.DiscussionTime = 0;
            __instance.VotingTime = 150;
            __instance.IsDefaults = true;
            __instance.ConfirmImpostor = false;
            __instance.VisualTasks = false;

            __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
            __instance.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Phantom);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Tracker);
            __instance.roleOptions.SetRoleRecommended(RoleTypes.Noisemaker);

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
            {
                __instance.PlayerSpeedMod = 1.75f;
                __instance.CrewLightMod = 5f;
                __instance.ImpostorLightMod = 0.25f;
                __instance.NumImpostors = 1;
                __instance.NumCommonTasks = 0;
                __instance.NumLongTasks = 0;
                __instance.NumShortTasks = 10;
                __instance.KillCooldown = 10f;
            }
            if (Options.IsStandardHAS) //StandardHAS
            {
                __instance.PlayerSpeedMod = 1.75f;
                __instance.CrewLightMod = 5f;
                __instance.ImpostorLightMod = 0.25f;
                __instance.NumImpostors = 1;
                __instance.NumCommonTasks = 0;
                __instance.NumLongTasks = 0;
                __instance.NumShortTasks = 10;
                __instance.KillCooldown = 10f;
            }
            return false;
        }
    }*/

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
            GamePresetButtons.gameObject.SetActive(false);

            var ModStgButton = GameObject.Instantiate(RoleSettingsButton, RoleSettingsButton.transform.parent);

            RoleSettingsButton.gameObject.SetActive(false);

            ModSettingsButton = ModStgButton.GetComponent<PassiveButton>();
            ModSettingsButton.buttonText.text = "MODの設定";
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
                    stringOption.transform.FindChild("LabelBackground").localScale = new Vector3(1.3f, 1f, 1f);
                    stringOption.transform.FindChild("LabelBackground").SetLocalX(-2.2695f);
                    stringOption.transform.FindChild("PlusButton (1)").localPosition += new Vector3(option.HideValue ? 100f : 1.1434f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
                    stringOption.transform.FindChild("MinusButton (1)").localPosition += new Vector3(option.HideValue ? 100f : 0.3463f, option.HideValue ? 100f : 0f, option.HideValue ? 100f : 0f);
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
                        if (hozon2[0] != null)
                            hozon2[0].GetComponent<PassiveButton>().OnClick.Invoke();
                    }, 0.05f, "");
                }
            }));

            GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB").SetActive(false);

            //var copyButton = GameObject.Instantiate(HudManager.Instance.Chat.chatButton, __instance.transform);
            //copyButton.transform.localPosition = HudManager.Instance.Chat.chatButton.transform.localPosition - new Vector3(20, 0);
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class Prisetkesu
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            __instance.ChangeTab(1, false);
            GameSettingMenuChangeTabPatch.Hima = 0;
            /*(オプションロード云々)
            //ここだけけーわいさんお願いしますorz

            // ボタン生成
            CreateButton("OptionReset", Color.red, new Vector2(-4f, 10.8f), new Action(() =>
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue));
            }));
            CreateButton("OptionCopy", Color.green, new Vector2(-4f, -10.2f), new Action(() =>
            {
                OptionSerializer.SaveToClipboard();
            }));
            CreateButton("OptionLoad", Color.green, new Vector2(-4f, -10.5f), new Action(() =>
            {
                OptionSerializer.LoadFromClipboard();
            }));

            var timergobj = GameObject.Find("Timer");
            if (timergobj != null)
            {
                GameOptionsMenuPatch.Timer = GameObject.Instantiate(GameObject.Find("Timer"), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB").transform.parent)
                    .GetComponent<TMPro.TextMeshPro>();
                GameOptionsMenuPatch.Timer.name = "MenuTimer";
                GameOptionsMenuPatch.Timer.transform.localPosition = new Vector3(-4.85f, 2.1f, -30f);
                GameOptionsMenuPatch.Timer.transform.localScale = new Vector2(10.8f, 10.8f);
            }

            static void CreateButton(string text, Color color, Vector2 position, Action action)
            {
                var ToggleButton = Object.Instantiate(GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB"), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB").transform);
                ToggleButton.transform.localPosition = new Vector3(position.x, position.y, -15f);
                ToggleButton.transform.localScale = new Vector2(0.6f, 0.6f);
                ToggleButton.name = text;
                ToggleButton.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = Translator.GetString(text);
                ToggleButton.transform.FindChild("Background").GetComponent<SpriteRenderer>().color = color;
                var passiveButton = ToggleButton.GetComponent<PassiveButton>();
                passiveButton.OnClick = new();
                passiveButton.OnClick.AddListener(action);
            }*/
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
    class GameSettingMenuChangeTabPatch
    {
        public static string meg;
        public static void Prefix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (!previewOnly)
            {
                var ModSettingsTab = GameSettingMenuStartPatch.ModSettingsTab;
                if (!ModSettingsTab) return;
                ModSettingsTab.gameObject.SetActive(false);
                GameSettingMenuStartPatch.ModSettingsButton.SelectButton(false);
                if (tabNum != 3) return;
                ModSettingsTab.gameObject.SetActive(true);

                __instance.MenuDescriptionText.text = meg;

                __instance.ToggleLeftSideDarkener(true);
                __instance.ToggleRightSideDarkener(false);
                ControllerManager.Instance.OpenOverlayMenu(__instance.name, __instance.BackButton, __instance.DefaultButtonSelected, __instance.ControllerSelectable);
                GameSettingMenuStartPatch.ModSettingsButton.SelectButton(true);
            }
        }
        static int Last = 0;
        public static int Hima = 0;
        public static void Postfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (previewOnly) return;

            var l = Last;
            if (tabNum == Last && tabNum == 3) Hima++;

            if (tabNum != Last)
            {
                GameSettingMenuStartPatch.dasu = true;
                Last = tabNum;
            }
            if (100 > Hima)
            {
                var rand = IRandom.Instance;
                int rect = IRandom.Instance.Next(1, 101);
                if (rect < 50)
                    meg = GetString("ModSettingInfo0");
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
                                ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.GameNumImpostors:
                                if (DebugModeManager.IsDebugMode)
                                {
                                    ob.Cast<NumberOption>().ValidRange.min = 0;
                                }
                                break;
                            default:
                                break;
                        }
                    }
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
            _ = new LateTask(() =>
            {
                var dd = GameSettingMenuStartPatch.ModSettingsTab.AllButton.transform.parent.GetComponentsInChildren<RoleSettingsTabButton>();
                foreach (Component aaa in dd)
                {
                    Object.Destroy(aaa.gameObject);
                }
                foreach (var option in GameSettingMenuStartPatch.ModSettingsTab.roleChances.ToArray())
                    Object.Destroy(option.gameObject);
                GameSettingMenuStartPatch.ModSettingsTab.roleChances = new();
                Object.Destroy(GameSettingMenuStartPatch.ModSettingsTab?.AllButton?.gameObject);
            }, 0.02f);
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
