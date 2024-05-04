using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
            sb.AppendFormat("{0}: {1}\n\n", RoleAssignManager.OptionAssignMode.GetName(), RoleAssignManager.OptionAssignMode.GetString());
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                sb.Append($"<color=#ff0000>{GetString("Message.HideGameSettings")}</color>");
            }
            else
            {
                //Standardの時のみ実行
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                {
                    //有効な役職一覧
                    sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.GM)}>{Utils.GetRoleName(CustomRoles.GM)}:</color> {Options.EnableGM.GetString()}\n\n");
                    sb.Append(GetString("ActiveRolesList")).Append("\n<size=90%>");
                    foreach (var kvp in Options.CustomRoleSpawnChances)
                        if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All && kvp.Value.GetBool()) //スタンダードか全てのゲームモードで表示する役職
                            sb.Append($"{Utils.GetCombinationCName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n");
                    pages.Add(sb.ToString() + "\n\n</size>");
                    sb.Clear();
                }
                //有効な役職と詳細設定一覧
                pages.Add("");
                if (RoleAssignManager.OptionAssignMode.GetBool())
                {
                    ShowChildren(RoleAssignManager.OptionAssignMode, ref sb, Color.white);
                    sb.Append('\n');
                }
                nameAndValue(Options.EnableGM);
                foreach (var kvp in Options.CustomRoleSpawnChances)
                {
                    if (!kvp.Key.IsEnable() || kvp.Value.IsHiddenOn(Options.CurrentGameMode)) continue;
                    sb.Append('\n');
                    sb.Append($"</size>{Utils.GetCombinationCName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n<size=80%>");
                    ShowChildren(kvp.Value, ref sb, Utils.GetRoleColor(kvp.Key).ShadeColor(-0.5f), 1);
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
                    sb.Append($"{opt.GetName()}: {opt.GetString()}\n");
                    if (opt.GetBool())
                        ShowChildren(opt, ref sb, Color.white, 1);
                }
                //Onの時に子要素まで表示するメソッド
                void nameAndValue(OptionItem o) => sb.Append($"{o.GetName()}: {o.GetString()}\n");
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
                if (opt.Value.Name == "GivePlusVote" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveRevenger" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveOpener" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveLighting" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveMoon" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveElector" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveNonReport" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveTransparent" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveNotvoter" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveWater" && !opt.Value.GetBool()) continue;
                if (opt.Value.Name == "GiveSpeeding" && !opt.Value.GetBool()) continue;
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
                sb.Append($"{opt.Value.GetName()}: {opt.Value.GetString()}</size></line-height>\n");
                if (opt.Value.GetBool()) ShowChildren(opt.Value, ref sb, color, deep + 1);
            }
        }
    }
}