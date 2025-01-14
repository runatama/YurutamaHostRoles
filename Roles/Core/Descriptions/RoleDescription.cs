using System.Linq;
using System.Text;
using UnityEngine;

namespace TownOfHost.Roles.Core.Descriptions;

public abstract class RoleDescription
{
    public RoleDescription(SimpleRoleInfo roleInfo)
    {
        RoleInfo = roleInfo;
    }

    public SimpleRoleInfo RoleInfo { get; }
    /// <summary>イントロなどで表示される短い文</summary>
    public abstract string Blurb { get; }
    /// <summary>
    /// ヘルプコマンドで使用される長い説明文<br/>
    /// AmongUs2023.7.12時点で，Impostor, Crewmateに関してはバニラ側でロング説明文が未実装のため「タスクを行う」と表示される
    /// </summary>
    public abstract string Description { get; }
    public string FullFormatHelp
    {
        get
        {
            var builder = new StringBuilder(256);
            //役職とイントロ
            builder.AppendFormat("<b><line-height=2.0pic><size={0}>{1}\n", FirstHeaderSize, Translator.GetRoleString(RoleInfo.RoleName.ToString()).Color(RoleInfo.RoleColor.ToReadableColor()));
            builder.AppendFormat("<line-height=1.8pic><size={0}>{1}</b>\n", InfoSize, Blurb.Color(RoleInfo.RoleColor.ToReadableColor()));
            // 陣営
            builder.AppendFormat("<size={0}>{1}:", SecondSize, Translator.GetString("Team"));
            var roleTeam = RoleInfo.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : RoleInfo.CustomRoleType;
            builder.AppendFormat("<b><size={0}>{1}</b>    ", SecondSize, Translator.GetString($"CustomRoleTypes.{roleTeam}"));
            // バニラ置き換え役職
            builder.AppendFormat("<size={0}>{1}", SecondSize, Translator.GetString("Basis"));
            builder.AppendFormat("<line-height=1.3pic><size={0}>:{1}\n", SecondSize, Translator.GetString(RoleInfo.BaseRoleType.Invoke().ToString()));
            //From
            if (RoleInfo.From != From.None) builder.AppendFormat("<line-height=1.3pic><size={0}>{1}\n", SecondSize, UtilsOption.GetFrom(RoleInfo).RemoveSizeTags());

            //説明
            builder.AppendFormat("<line-height=1.3pic><size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Description);
            //設定
            var sb = new StringBuilder();
            if (Options.CustomRoleSpawnChances.TryGetValue(RoleInfo.RoleName, out var op)) UtilsShowOption.ShowChildrenSettings(op, ref sb);
            else if (RoleInfo.RoleName is CustomRoles.Braid) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Driver], ref sb);
            if (RoleInfo.CustomRoleType == CustomRoleTypes.Madmate)
            {
                string rule = "┣ ";
                string ruleFooter = "┗ ";
                sb.Append($"{Options.MadMateOption.GetName()}: {Options.MadMateOption.GetString().Color(Palette.ImpostorRed)}\n");
                if (Options.MadMateOption.GetBool())
                {
                    sb.Append($"{rule}{Options.MadmateCanFixLightsOut.GetName()}: {Options.MadmateCanFixLightsOut.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanFixComms.GetName()}: {Options.MadmateCanFixComms.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateHasLighting.GetName()}: {Options.MadmateHasLighting.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateHasMoon.GetName()}: {Options.MadmateHasMoon.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanSeeKillFlash.GetName()}: {Options.MadmateCanSeeKillFlash.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanSeeOtherVotes.GetName()}: {Options.MadmateCanSeeOtherVotes.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanSeeDeathReason.GetName()}: {Options.MadmateCanSeeDeathReason.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateRevengeCrewmate.GetName()}: {Options.MadmateRevengeCrewmate.GetString()}\n");
                    if (Options.MadmateRevengeCrewmate.GetBool())
                    {
                        sb.Append($"┃ {rule}{Options.MadNekomataCanImp.GetName()}: {Options.MadNekomataCanImp.GetString()}\n");
                        sb.Append($"┃ {rule}{Options.MadNekomataCanMad.GetName()}: {Options.MadNekomataCanMad.GetString()}\n");
                        sb.Append($"┃ {rule}{Options.MadNekomataCanCrew.GetName()}: {Options.MadNekomataCanCrew.GetString()}\n");
                        sb.Append($"┃ {ruleFooter}{Options.MadNekomataCanNeu.GetName()}: {Options.MadNekomataCanNeu.GetString()}\n");
                    }
                    sb.Append($"{rule}{Options.MadCanSeeImpostor.GetName()}: {Options.MadCanSeeImpostor.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateVentCooldown.GetName()}: {Options.MadmateVentCooldown.GetString()}\n");
                    sb.Append($"{rule}{Options.MadmateVentMaxTime.GetName()}: {Options.MadmateVentMaxTime.GetString()}\n");
                    sb.Append($"{ruleFooter}{Options.MadmateCanMovedByVent.GetName()}: {Options.MadmateCanMovedByVent.GetString()}\n");
                }
            }
            if (sb.ToString() != "")
            {
                builder.AppendFormat("<size={0}>\n", BlankLineSize);
                builder.AppendFormat("<size={0}>{1}\n", InfoSize, Translator.GetString("Settings"));
                builder.AppendFormat("<line-height=1.0pic><size={0}>{1}", "50%", sb);
            }
            var temp = TemplateManager.GetTemplate($"{RoleInfo.RoleName}");
            if (temp != "")
            {
                builder.AppendFormat("</line-height><line-height=1.3pic>\n<size={0}>{1}</size>\n", InfoSize, Translator.GetString("RoleTemplate"));
                builder.Append($"{temp}");
            }
            return builder.ToString();
        }
    }
    public string WikiText
    {
        get
        {
            var builder = new StringBuilder(256);
            //役職とイントロ
            builder.Append("# ").Append(Translator.GetRoleString(RoleInfo.RoleName.ToString()));
            var roleTeam = RoleInfo.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : RoleInfo.CustomRoleType;

            if (roleTeam is CustomRoleTypes.Neutral)
            {
                builder.Append($"\n陣営　　：{Translator.GetString($"CustomRoleTypes.{roleTeam}")}<br>\n");
                builder.Append($"判定　　：<br>\n");
                builder.Append($"カウント：<br>\n");
            }
            else
            {
                builder.Append($"\n陣営：{Translator.GetString($"CustomRoleTypes.{roleTeam}")}<br>\n");
                builder.Append($"判定：<br>\n");
            }
            builder.Append($"\n\n\n");
            builder.Append($"## 参考/移植元\n").Append("[]()より<br>\n");
            builder.Append($"\n## 役職概要\n{Description.Changebr(true)}<br>\n");
            builder.Append($"\n## 能力\n ()<br>\n◎→ワンクリ\n△→キル\n★→常時発動\n◇→ベント\n※→自投票\n▽→タスク\n▶→その他のアビリティ\n");

            //設定
            var sb = new StringBuilder();
            if (Options.CustomRoleSpawnChances.TryGetValue(RoleInfo.RoleName, out var op))
                wikiOption(op, ref sb);

            if (sb.ToString().RemoveHtmlTags() is not null and not "")
            {
                builder.Append($"\n## 設定\n").Append("|設定名|(設定値 / デフォルト値)|説明|\n").Append("|-----|----------------------|----|\n");
                builder.Append($"{sb.ToString().RemoveHtmlTags()}\n");
            }

            builder.Append($"\n## 補足説明/仕様\n");
            builder.Append($"\n## 勝利条件\n");

            return builder.ToString().RemoveColorTags();
        }
    }

    public const string FirstHeaderSize = "150%";
    public const string InfoSize = "90%";
    public const string SecondHeaderSize = "80%";
    public const string SecondSize = "70%";
    public const string BodySize = "60%";
    public const string BlankLineSize = "30%";

    public static void wikiOption(OptionItem option, ref StringBuilder sb, int deep = 0)
    {
        foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
        {
            switch (opt.Value.Name)
            {
                case "Maximum": continue;
                case "GiveGuesser": continue;
                case "GiveWatching": continue;
                case "GiveManagement": continue;
                case "Giveseeing": continue;
                case "GiveAutopsy": continue;
                case "GiveTiebreaker": continue;
                case "GiveMagicHand": continue;
                case "GivePlusVote": continue;
                case "GiveRevenger": continue;
                case "GiveOpener": continue;
                case "GiveLighting": continue;
                case "GiveMoon": continue;
                case "GiveElector": continue;
                case "GiveInfoPoor": continue;
                case "GiveNonReport": continue;
                case "GiveTransparent": continue;
                case "GiveNotvoter": continue;
                case "GiveWater": continue;
                case "GiveSpeeding": continue;
                case "GiveGuarding": continue;
                case "GiveClumsy": continue;
                case "GiveSlacker": continue;
            }

            sb.Append("|");
            if (deep > 0)
            {
                sb.Append(string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0))));
                sb.Append(opt.Index == option.Children.Count ? "┗ " : "┣ ");
            }
            var val = "";
            var tani = "";
            if (opt.Value is BooleanOptionItem boolean)
            {
                val = $"On/Off ({(opt.Value.GetBool() ? "On" : "Off")})";
            }
            if (opt.Value is IntegerOptionItem integer)
            {
                val = $"{integer.Rule.MinValue}/{integer.Rule.MaxValue}/{integer.Rule.Step} ({opt.Value.GetInt()})";
            }
            if (opt.Value is FloatOptionItem floatOption)
            {
                var i = opt.Value.Infinity;
                var min = $"{floatOption.Rule.MinValue}";
                var step = $"{floatOption.Rule.Step}";
                if (i is not false)
                {
                    if (min == "0") min = i == true ? "∞" : "ー";
                    if (step == "0") step = i == true ? "∞" : "ー";
                }

                val = $"{min}/{floatOption.Rule.MaxValue}/{step} ({opt.Value.GetFloat()})";
            }
            if (opt.Value is StringOptionItem stringOption)
            {
                val = $" ({opt.Value.GetString()})";
            }
            if (opt.Value.ValueFormat is not OptionFormat.None)
            {
                //かっこの色がおかしくなるけどご愛敬。とると判定が狂って削除エラー吐く
                tani = $"({Translator.GetString("Format." + opt.Value.ValueFormat).RemoveDeltext("{0").RemoveDeltext("}")})";
            }

            sb.Append($"{opt.Value.GetName(true)}{tani}|{val}||\n");
            if (opt.Value is not null) wikiOption(opt.Value, ref sb, deep + 1);
        }
    }
}
