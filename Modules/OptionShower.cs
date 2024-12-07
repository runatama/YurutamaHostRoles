using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;

using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class OptionShower
    {
        public static int currentPage = 0;
        public static List<string> pages = new();
        static OptionShower()
        {

        }
        public static string GetText()
        {
            //初期化
            StringBuilder sb = new();
            pages = new()
            {
                //1ページに基本ゲーム設定を格納
                GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10) + "\n\n"
            };
            //ゲームモードの表示
            sb.Append($"{Options.GameMode.GetName()}: {Options.GameMode.GetString()}\n\n");
            //sb.AppendFormat("{0}: {1}\n\n", RoleAssignManager.OptionAssignMode.GetName(), RoleAssignManager.OptionAssignMode.GetString());
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                sb.Append($"<color=#ff0000>{GetString("Message.HideGameSettings")}</color>");
            }
            else
            {
                //Standardの時のみ実行
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                {
                    var roleType = CustomRoleTypes.Impostor;
                    var farst = true;
                    var (imp, mad, crew, neu, addon, lover, gorst) = UtilsShowOption.GetRoleTypesCountInt();
                    //有効な役職一覧
                    sb.Append($"<color={UtilsRoleText.GetRoleColorCode(CustomRoles.GM)}>{UtilsRoleText.GetRoleName(CustomRoles.GM)}:</color> {Options.EnableGM.GetString()}\n\n");
                    sb.Append(GetString("ActiveRolesList")).Append("<size=90%>");
                    var count = -1;
                    var co = 0;
                    var las = "";
                    var a = Options.CustomRoleSpawnChances.Where(r => r.Key.IsImpostor())?.ToArray();
                    var b = Options.CustomRoleSpawnChances.Where(r => r.Key.IsMadmate())?.ToArray();
                    var cc = Options.CustomRoleSpawnChances.Where(r => r.Key.IsCrewmate())?.ToArray();
                    var d = Options.CustomRoleSpawnChances.Where(r => r.Key.IsNeutral())?.ToArray();
                    var e = Options.CustomRoleSpawnChances.Where(r => !r.Key.IsImpostor() && !r.Key.IsCrewmate() && !r.Key.IsMadmate() && !r.Key.IsNeutral()).ToArray();
                    var addoncheck = false;
                    foreach (var kvp in a.AddRangeToArray(b).AddRangeToArray(cc).AddRangeToArray(d).AddRangeToArray(e))
                        if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All && kvp.Value.GetBool()) //スタンダードか全てのゲームモードで表示する役職
                        {
                            var role = kvp.Key;
                            if (farst && role.IsImpostor())
                            {
                                var maxtext = $"({imp})";
                                var (che, max, min) = RoleAssignManager.CheckRoleTypeCount(role.GetCustomRoleTypes());
                                if (che)
                                {
                                    maxtext += $"　[Min : {min}|Max : {max} ]";
                                }
                                las = Utils.ColorString(Palette.ImpostorRed, "\n<u>☆Impostors☆" + maxtext + "</u>\n");
                                sb.Append(Utils.ColorString(Palette.ImpostorRed, "\n<u>☆Impostors☆" + maxtext + "</u>\n"));
                            }
                            farst = false;
                            if ((!addoncheck && roleType == CustomRoleTypes.Crewmate && role.IsSubRole()) || (role.GetCustomRoleTypes() != roleType && role.GetCustomRoleTypes() != CustomRoleTypes.Impostor))
                            {
                                var s = "";
                                var c = 0;
                                var cor = Color.white;
                                if (role.IsSubRole())
                                {
                                    s = "☆Add-ons☆";
                                    c = addon + lover + gorst;
                                    cor = ModColors.AddonsColor;
                                    count = -1;
                                    addoncheck = true;
                                }
                                else
                                    switch (role.GetCustomRoleTypes())
                                    {
                                        case CustomRoleTypes.Crewmate: count = -1; s = "☆CrewMates☆"; c = crew; cor = ModColors.CrewMateBlue; break;
                                        case CustomRoleTypes.Madmate: count = -1; s = "☆MadMates☆"; c = mad; cor = StringHelper.CodeColor("#ff7f50"); break;
                                        case CustomRoleTypes.Neutral: count = -1; s = "☆Neutrals☆"; c = neu; cor = ModColors.NeutralGray; break;
                                    }
                                var maxtext = $"({c})";
                                var (che, max, min) = RoleAssignManager.CheckRoleTypeCount(role.GetCustomRoleTypes());
                                if (che && !role.IsSubRole())
                                {
                                    maxtext += $"　[Min : {min}|Max : {max} ]";
                                }
                                las = Utils.ColorString(cor, $"\n<u>{s + maxtext}</u>\n");
                                sb.Append(Utils.ColorString(cor, $"\n<u>{s + maxtext}</u>\n"));
                                roleType = role.GetCustomRoleTypes();
                            }
                            var m = role.IsImpostor() ? Utils.ColorString(Palette.ImpostorRed, "Ⓘ") : (role.IsCrewmate() ? Utils.ColorString(Palette.CrewmateBlue, "Ⓒ") : (role.IsMadmate() ? "<color=#ff7f50>Ⓜ</color>" : (role.IsNeutral() ? Utils.ColorString(ModColors.NeutralGray, "Ⓝ") : "<color=#cccccc>⦿</color>")));

                            if (role.IsBuffAddon()) m = Utils.AdditionalWinnerMark;
                            if (role.IsRiaju()) m = Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Lovers), "♥");
                            if (role.IsDebuffAddon()) m = Utils.ColorString(Palette.DisabledGrey, "☆");
                            if (role.IsGorstRole()) m = "<color=#8989d9>■</color>";

                            if (count == 0) sb.Append($"\n{m}{UtilsRoleText.GetCombinationCName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}");
                            else if (count == -1) sb.Append($"{m}{UtilsRoleText.GetCombinationCName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}");
                            else sb.Append($"<pos=39%>{m}{UtilsRoleText.GetCombinationCName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}</pos>");

                            if (count == 0) co++;
                            count = count is 0 or -1 ? 1 : 0;

                            if (co >= 27)
                            {
                                co = 0;
                                count = -1;
                                pages.Add(sb.ToString() + "\n\n");
                                sb.Clear();
                                sb.Append("<size=90%>" + las);
                            }
                        }
                    pages.Add(sb.ToString() + "\n\n</size>");
                    sb.Clear();
                }
                //有効な役職と詳細設定一覧
                pages.Add("");
                nameAndValue(Options.EnableGM);
                foreach (var kvp in Options.CustomRoleSpawnChances)
                {
                    if (!kvp.Key.IsEnable() || kvp.Value.IsHiddenOn(Options.CurrentGameMode)) continue;
                    sb.Append('\n');
                    sb.Append($"</size><size=100%>{UtilsRoleText.GetCombinationCName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}</size>\n<size=80%>");
                    ShowChildren(kvp.Value, ref sb, UtilsRoleText.GetRoleColor(kvp.Key).ShadeColor(-0.5f), 1);
                    string rule = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┣ ");
                    string ruleFooter = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┗ ");

                    if (kvp.Key.CanMakeMadmate()) //シェイプシフター役職の時に追加する詳細設定
                    {
                        sb.Append($"{ruleFooter}{Options.CanMakeMadmateCount.GetName()}: {Options.CanMakeMadmateCount.GetString()}\n");
                    }
                }
                sb.Append("</size><size=90%>");
                foreach (var opt in OptionItem.AllOptions.Where(x => x.Id >= 90000 && !x.IsHiddenOn(Options.CurrentGameMode) && x.Parent == null))
                {
                    if (opt.IsHeader) sb.Append('\n');
                    sb.Append($"{opt.GetName()}: {opt.GetString().RemoveSN()}\n");
                    if (opt.GetBool())
                        ShowChildren(opt, ref sb, Color.white, 1);
                }
                //Onの時に子要素まで表示するメソッド
                void nameAndValue(OptionItem o) => sb.Append($"{o.GetName()}: {o.GetString().RemoveSN()}\n");
            }
            //1ページにつき35行までにする処理
            List<string> tmp = new(sb.ToString().Split("\n\n"));
            for (var i = 0; i < tmp.Count; i++)
            {
                if (pages[^1].Count(c => c == '\n') + 1 + tmp[i].Count(c => c == '\n') + 1 > 35)
                    pages.Add(tmp[i] + "\n\n");
                else pages[^1] += tmp[i] + "\n\n";
            }
            if (currentPage >= pages.Count) currentPage = pages.Count - 1; //現在のページが最大ページ数を超えていれば最後のページに修正
            return $"{pages[currentPage]}{GetString("PressTabToNextPage")}({currentPage + 1}/{pages.Count})";
        }

        public static void Next()
        {
            currentPage++;
            if (currentPage >= pages.Count) currentPage = 0; //現在のページが最大ページを超えていれば最初のページに
        }
        private static void ShowChildren(OptionItem option, ref StringBuilder sb, Color color, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "GiveGuesser" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveWatching" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveManagement" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "Giveseeing" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveAutopsy" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveTiebreaker" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveMagicHand" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GivePlusVote" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveRevenger" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveOpener" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveLighting" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveMoon" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveElector" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveInfoPoor" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveNonReport" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveTransparent" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveNotvoter" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveWater" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveSpeeding" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveGuarding" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveClumsy" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveSlacker" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "FixedRole") continue;
                if (opt.Value.Name == "DisableSkeldDevices" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "SkeldReactorTimeLimit" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "SkeldO2TimeLimit" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "MiraReactorTimeLimit" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "MiraO2TimeLimit" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisableMiraHQDevices" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisablePolusDevices" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "PolusReactorTimeLimit" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "DisableAirshipDevices" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "AirshipReactorTimeLimit" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableFungleDevices" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "FungleReactorTimeLimit" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "FungleMushroomMixupDuration" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "DisableFungleSporeTrigger" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "CantUseZipLineTotop" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "CantUseZipLineTodown" && !Options.IsActiveFungle) continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveFungle || Options.IsActiveAirship || Options.IsActivePolus)) continue;
                if (opt.Value.Name == "AirShipVariableElectrical" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipMovingPlatform" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipViewingDeckLightsPanel" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipCargoLightsPanel" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "DisableAirshipGapRoomLightsPanel" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveSkeld || Options.IsActiveMiraHQ || Options.IsActiveAirship || Options.IsActivePolus)) continue;
                sb.Append("<line-height=80%><size=70%>");
                if (deep > 0)
                {
                    sb.Append(string.Concat(Enumerable.Repeat(Utils.ColorString(color, "┃"), deep - 1)));
                    sb.Append(Utils.ColorString(color, opt.Index == option.Children.Count ? "┗ " : "┣ "));
                }
                sb.Append($"{opt.Value.GetName()}: {opt.Value.GetString().RemoveSN()}</size></line-height>\n");
                if (opt.Value.GetBool()) ShowChildren(opt.Value, ref sb, color, deep + 1);
            }
        }
        public static bool? Checkenabled(OptionItem opt)
        {
            if (opt.Name == "GiveGuesser" && !opt.GetBool()) return false;
            if (opt.Name == "GiveWatching" && !opt.GetBool()) return false;
            if (opt.Name == "GiveManagement" && !opt.GetBool()) return false;
            if (opt.Name == "Giveseeing" && !opt.GetBool()) return false;
            if (opt.Name == "GiveAutopsy" && !opt.GetBool()) return false;
            if (opt.Name == "GiveTiebreaker" && !opt.GetBool()) return false;
            if (opt.Name == "GivePlusVote" && !opt.GetBool()) return false;
            if (opt.Name == "GiveRevenger" && !opt.GetBool()) return false;
            if (opt.Name == "GiveOpener" && !opt.GetBool()) return false;
            if (opt.Name == "GiveLighting" && !opt.GetBool()) return false;
            if (opt.Name == "GiveMoon" && !opt.GetBool()) return false;
            if (opt.Name == "GiveElector" && !opt.GetBool()) return false;
            if (opt.Name == "GiveInfoPoor" && !opt.GetBool()) return false;
            if (opt.Name == "GiveNonReport" && !opt.GetBool()) return false;
            if (opt.Name == "GiveTransparent" && !opt.GetBool()) return false;
            if (opt.Name == "GiveNotvoter" && !opt.GetBool()) return false;
            if (opt.Name == "GiveWater" && !opt.GetBool()) return false;
            if (opt.Name == "GiveSpeeding" && !opt.GetBool()) return false;
            if (opt.Name == "GiveGuarding" && !opt.GetBool()) return false;
            if (opt.Name == "GiveClumsy" && !opt.GetBool()) return false;
            if (opt.Name == "GiveSlacker" && !opt.GetBool()) return false;

            if (opt.Name == "DisableSkeldDevices" && !Options.IsActiveSkeld) return null;
            if (opt.Name == "DisableMiraHQDevices" && !Options.IsActiveMiraHQ) return null;
            if (opt.Name == "DisablePolusDevices" && !Options.IsActivePolus) return null;
            if (opt.Name == "PolusReactorTimeLimit" && !Options.IsActivePolus) return null;
            if (opt.Name == "DisableAirshipDevices" && !Options.IsActiveAirship) return null;
            if (opt.Name == "AirshipReactorTimeLimit" && !Options.IsActiveAirship) return null;
            if (opt.Name == "DisableFungleDevices" && !Options.IsActiveFungle) return null;
            if (opt.Name == "FungleReactorTimeLimit" && !Options.IsActiveFungle) return null;
            if (opt.Name == "CantUseZipLineTotop" && !Options.IsActiveFungle) return null;
            if (opt.Name == "CantUseZipLineTodown" && !Options.IsActiveFungle) return null;
            if (opt.Name == "SkeldReactorTimeLimit" && !Options.IsActiveSkeld) return null;
            if (opt.Name == "SkeldO2TimeLimit" && !Options.IsActiveSkeld) return null;
            if (opt.Name == "MiraReactorTimeLimit" && !Options.IsActiveMiraHQ) return null;
            if (opt.Name == "MiraO2TimeLimit" && !Options.IsActiveMiraHQ) return null;
            if (opt.Name == "FungleMushroomMixupDuration" && !Options.IsActiveFungle) return null;
            if (opt.Name == "DisableFungleSporeTrigger" && !Options.IsActiveFungle) return null;
            if (opt.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveFungle || Options.IsActiveAirship || Options.IsActivePolus)) return null;
            if (opt.Name == "AirShipVariableElectrical" && !Options.IsActiveAirship) return null;
            if (opt.Name == "DisableAirshipMovingPlatform" && !Options.IsActiveAirship) return null;
            if (opt.Name == "DisableAirshipViewingDeckLightsPanel" && !Options.IsActiveAirship) return null;
            if (opt.Name == "DisableAirshipCargoLightsPanel" && !Options.IsActiveAirship) return null;
            if (opt.Name == "DisableAirshipGapRoomLightsPanel" && !Options.IsActiveAirship) return null;
            if (opt.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveSkeld || Options.IsActiveMiraHQ || Options.IsActiveAirship || Options.IsActivePolus)) return null;
            return true;
        }
    }
}