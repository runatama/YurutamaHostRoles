using HarmonyLib;
using UnityEngine;
using static TownOfHost.GameSettingMenuStartPatch;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;
        public static void Postfix(GameOptionsMenu __instance)
        {
            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            if (priset)
            {
                priset.transform.localPosition = new Vector3(0f, 3.2f);
                search.transform.localPosition = new Vector3(0f, 3.5f);
                search.transform.localScale = priset.transform.localScale = new Vector3(0.4f, 0.4f, 0f);

                activeonly.transform.localPosition = new Vector3(-2.05f, 3.3f);
                activeonly.transform.localScale = new Vector3(0.6f, 0.6f, 0f);

                searchtext.enabled = search.textArea.text == "";
                prisettext.enabled = priset.textArea.text == "";

                var active = ModSettingsButton?.selected ?? false;

                searchtext.gameObject.SetActive(active);
                prisettext.gameObject.SetActive(active);
                search.gameObject.SetActive(active);
                priset.gameObject.SetActive(active);
                activeonly.gameObject.SetActive(active);
            }

            if (__instance.transform.name == "GAME SETTINGS TAB") return;
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                if (__instance.gameObject.name != tab + "-Stg") continue;

                float numItems = __instance.Children.Count;
                var offset = 2.7f;
                var y = 0.713f;

                foreach (var option in OptionItem.AllOptions)
                {
                    if ((TabGroup)tab != option.Tab) continue;
                    if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;
                    var enabled = true;
                    var parent = option.Parent;

                    enabled = AmongUsClient.Instance.AmHost && !option.IsHiddenOn(Options.CurrentGameMode);

                    //起動時以外で表示/非表示を切り替える際に使う
                    var isroleoption = Options.CustomRoleSpawnChances.ContainsValue(option as IntegerOptionItem);
                    if (enabled)
                    {
                        if (ActiveOnlyMode)
                        {
                            if (isroleoption)
                            {
                                enabled = option.GetBool();
                            }
                            if (OptionShower.Checkenabled(option) is false or null)
                            {
                                var v = OptionShower.Checkenabled(option);
                                enabled = v is not null && option.GetBool();
                            }
                            if (option.Parent is not null)
                                if (OptionShower.Checkenabled(option.Parent) is false or null)
                                {
                                    var v = OptionShower.Checkenabled(option.Parent);
                                    enabled = v is not null && option.Parent.GetBool();
                                }
                        }
                        if (!Event.Special)
                        {
                            if (isroleoption)
                            {
                                if (Event.OptionLoad.Contains(option.Name)) enabled = false;
                            }
                        }
                    }

                    var opt = option.OptionBehaviour.LabelBackground;

                    opt.color = new Color32(200, 200, 200, 255);
                    opt.size = new(5.0f * w, 0.68f * h);

                    if (option.Tab is TabGroup.MainSettings && (option.NameColor != Color.white || option.NameColorCode != "#ffffff"))
                    {
                        var color = option.NameColor == Color.white ? StringHelper.CodeColor(option.NameColorCode) : option.NameColor;

                        opt.color = color.ShadeColor(-6);
                    }
                    if (isroleoption)
                    {
                        opt.color = option.NameColor.ShadeColor(-5);
                    }

                    Transform titleText = option.OptionBehaviour.transform.Find("Title Text");
                    RectTransform titleTextRect = titleText.GetComponent<RectTransform>();

                    var i = 0;
                    while (parent != null && enabled)
                    {
                        i++;
                        enabled = parent.GetBool();
                        parent = parent.Parent;
                    }

                    switch (i)
                    {
                        case 0:
                            break;
                        case 1:
                            opt.color = new Color32(40, 50, 80, 255);
                            opt.size = new(4.6f * w, 0.68f * h);
                            titleText.transform.localPosition = new Vector3(-1.8566f * w, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.4f, 0.6f);
                            break;
                        case 2:
                            opt.color = new Color32(20, 60, 40, 255);
                            opt.size = new(4.4f * w, 0.68f * h);
                            titleText.transform.localPosition = new Vector3(-1.7566f * w, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.35f, 0.6f);
                            break;
                        case 3:
                            opt.color = new Color32(60, 20, 40, 255);
                            opt.size = new(4.2f * w, 0.68f * h);
                            titleText.transform.localPosition = new Vector3(-1.6566f * w, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.3f, 0.6f);
                            break;
                        case 4:
                            opt.color = new Color32(60, 40, 10, 255);
                            opt.size = new(4.0f * w, 0.68f * h);
                            titleText.transform.localPosition = new Vector3(-1.6566f * w, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.25f, 0.6f);
                            break;
                    }

                    option.OptionBehaviour.gameObject.SetActive(enabled);
                    if (enabled)
                    {
                        offset -= option.IsHeader ? (0.68f * h) : (0.45f * h);
                        option.OptionBehaviour.transform.localPosition = new Vector3(
                            option.OptionBehaviour.transform.localPosition.x,//0.952f,
                            offset - (1.5f * h),//y,
                            option.OptionBehaviour.transform.localPosition.z);//-120f);
                        y -= option.IsHeader ? (0.68f * h) : (0.45f * h);

                        if (option.IsHeader)
                        {
                            numItems += (0.5f * h);
                        }
                    }
                    else
                    {
                        numItems--;
                    }
                }
                __instance.GetComponentInParent<Scroller>().ContentYBounds.max = -(offset * 3 - (h * offset * (h == 1 ? 2f : 2.2f))) + 0.75f;
            }
        }
    }
    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.FixedUpdate))]
    class NumberOptionFixUpdataPatch
    {
        public static void Postfix(NumberOption __instance)
        {
            if (__instance?.PlusBtn == null || __instance?.MinusBtn == null) return;
            __instance.PlusBtn.isInteractable = true;
            __instance.MinusBtn.isInteractable = true;
        }
    }
    [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
    class StringOptionFixUpdataPatch
    {
        public static void Postfix(StringOption __instance)
        {
            if (__instance?.PlusBtn == null || __instance?.MinusBtn == null) return;
            __instance.PlusBtn.isInteractable = true;
            __instance.MinusBtn.isInteractable = true;
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Update))]
    class GameSettingMenuUpdataPatch
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            __instance.MenuDescriptionText.text = GameSettingMenuChangeTabPatch.meg;
            var settingsButton = __instance.GameSettingsTab?.gameObject;
            var allButton = ModSettingsTab?.AllButton?.gameObject;
            if (settingsButton?.active == true && (ModSettingsButton?.selected ?? false) && __instance?.GameSettingsTab is not null)
            {
                __instance.GameSettingsTab?.gameObject?.SetActive(false);
            }
            if (allButton?.active == true)
                allButton?.SetActive(false);
        }
    }
}