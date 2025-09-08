using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class YuruSheriff : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(YuruSheriff),
            player => new YuruSheriff(player),
            CustomRoles.YuruSheriff,
            () => RoleTypes.Impostor,            // インポ基盤UI（Killボタン等）
            CustomRoleTypes.Crewmate,
            30400,
            SetupOptionItem,
            "yr",
            "#ffb347",
            (2, 0),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.SheriffMod
        );

    public YuruSheriff(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
    }

    // ── Option 定義 ─────────────────────────────────────────────
    public static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    public static OptionItem ShotLimitOpt;
    public static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillLovers;

    public static OptionItem ShotMissRateOpt;           // 外れる確率（%）
    public static OptionItem HasteChanceOnSuccessOpt;   // 成功時：短縮確率（%）
    public static OptionItem HasteScaleOnSuccessOpt;    // 成功時：短縮倍率（x）
    public static OptionItem HasteOnMissOpt;            // 外れ時も短縮抽選を行うか（ON/OFF）
    public static OptionItem HasteChanceOnMissOpt;      // 外れ時：短縮確率（%）
    public static OptionItem HasteScaleOnMissOpt;       // 外れ時：短縮倍率（x）

    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKill,
        SheriffCanKillLovers,

        SheriffShotMissRate,          // 外す確率

        SheriffHasteChanceSuccess,    // 成功時：短縮確率
        SheriffHasteScaleSuccess,     // 成功時：短縮倍率

        SheriffHasteOnMiss,           // 外れ時にも短縮抽選するか
        SheriffHasteChanceMiss,       // 外れ時：短縮確率
        SheriffHasteScaleMiss         // 外れ時：短縮倍率
    }

    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<SchrodingerCat.TeamType, OptionItem> SchrodingerCatKillTargetOptions = new();

    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30f;

    public static readonly string[] KillOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately" // 0=全許可 / 1=個別設定
    };

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Crew;

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(
            RoleInfo, 10, GeneralOption.KillCooldown,
            new(0f, 990f, 0.5f), 30f, false
        ).SetValueFormat(OptionFormat.Seconds);

        OverrideKilldistance.Create(RoleInfo, 8);

        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);

        ShotLimitOpt = IntegerOptionItem.Create(
            RoleInfo, 12, OptionName.SheriffShotLimit,
            new(1, 15, 1), 15, false
        ).SetValueFormat(OptionFormat.Times);

        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);

        // ── 命中ロジック ─────────────────────────────────────────
        ShotMissRateOpt = IntegerOptionItem.Create(
            RoleInfo, 17, OptionName.SheriffShotMissRate,
            new(0, 100, 10), 0, false
        ).SetValueFormat(OptionFormat.Percent);

        // ── 成功時の短縮 ────────────────────────────────────────
        HasteChanceOnSuccessOpt = IntegerOptionItem.Create(
            RoleInfo, 18, OptionName.SheriffHasteChanceSuccess,
            new(0, 100, 5), 0, false
        ).SetValueFormat(OptionFormat.Percent);

        HasteScaleOnSuccessOpt = FloatOptionItem.Create(
            RoleInfo, 19, OptionName.SheriffHasteScaleSuccess,
            new(0.10f, 1.00f, 0.05f), 0.50f, false
        ).SetValueFormat(OptionFormat.Multiplier);

        // ── 外れ時の短縮（トグル＋子項目） ────────────────────────
        HasteOnMissOpt = BooleanOptionItem.Create(
            RoleInfo, 20, OptionName.SheriffHasteOnMiss, true, false
        );

        HasteChanceOnMissOpt = IntegerOptionItem.Create(
            RoleInfo, 21, OptionName.SheriffHasteChanceMiss,
            new(0, 100, 5), 0, false
        ).SetValueFormat(OptionFormat.Percent).SetParent(HasteOnMissOpt);

        HasteScaleOnMissOpt = FloatOptionItem.Create(
            RoleInfo, 22, OptionName.SheriffHasteScaleMiss,
            new(0.10f, 1.00f, 0.05f), 0.50f, false
        ).SetValueFormat(OptionFormat.Multiplier).SetParent(HasteOnMissOpt);

        // Madmate 個別ON/OFF
        SetUpKillTargetOption(CustomRoles.Madmate, 13);

        // Neutral 全体設定（0=All, 1=Separately）
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);

        // Neutral 個別ON/OFF ＆ 猫の各陣営
        SetUpNeutralOptions(30);

        // 恋人許可
        CanKillLovers = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SheriffCanKillLovers, true, false);
    }

    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllStandardRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (neutral is CustomRoles.SchrodingerCat) continue;
            SetUpKillTargetOption(neutral, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
        foreach (var catType in EnumHelper.GetAllValues<SchrodingerCat.TeamType>())
        {
            if ((byte)catType < 50) continue;
            SetUpSchrodingerCatKillTargetOption(catType, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
    }

    public static void SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;

        var roleName = UtilsRoleText.GetRoleName(role);
        Dictionary<string, string> replacementDic = new()
        {
            { "%role%", Utils.ColorString(UtilsRoleText.GetRoleColor(role), roleName) }
        };

        KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false)
            .SetParent(parent)
            .SetParentRole(CustomRoles.YuruSheriff);

        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }

    public static void SetUpSchrodingerCatKillTargetOption(SchrodingerCat.TeamType catType, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;

        var inTeam = GetString("In%team%", new Dictionary<string, string>() { ["%team%"] = GetRoleString(catType.ToString()) });
        var catInTeam = Utils.ColorString(SchrodingerCat.GetCatColor(catType), UtilsRoleText.GetRoleName(CustomRoles.SchrodingerCat) + inTeam);

        Dictionary<string, string> replacementDic = new() { ["%role%"] = catInTeam };

        SchrodingerCatKillTargetOptions[catType] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false)
            .SetParent(parent)
            .SetParentRole(CustomRoles.YuruSheriff);

        SchrodingerCatKillTargetOptions[catType].ReplacementDictionary = replacementDic;
    }

    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();
        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{PlayerCatch.GetPlayerById(playerId)?.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "YuruSheriff");
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        ShotLimit = reader.ReadInt32();
    }

    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;

    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;

    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    // ── キルクール管理 ──────────────────────────────────────────

    private float GetBaseCooldown() => Mathf.Max(0.1f, KillCooldown.GetFloat());

    // 成功/外れ それぞれの設定から次回CDを計算
    private float ComputeNextCooldown(bool isSuccess)
    {
        float baseCd = GetBaseCooldown();

        int chance = isSuccess
            ? Mathf.Clamp(HasteChanceOnSuccessOpt?.GetInt() ?? 0, 0, 100)
            : Mathf.Clamp(HasteChanceOnMissOpt?.GetInt() ?? 0, 0, 100);

        float scale = isSuccess
            ? (HasteScaleOnSuccessOpt is null ? 1f : Mathf.Clamp(HasteScaleOnSuccessOpt.GetFloat(), 0.10f, 1.00f))
            : (HasteScaleOnMissOpt is null ? 1f : Mathf.Clamp(HasteScaleOnMissOpt.GetFloat(), 0.10f, 1.00f));

        if (chance > 0 && UnityEngine.Random.Range(0, 100) < chance)
            return Mathf.Max(0.1f, baseCd * scale);

        return baseCd;
    }

    // バニラ/他Modの後書き上書きを潰すため、次フレームでもう一度適用
    private void ApplySheriffCooldown(PlayerControl killer, float seconds, bool writeLog = false)
    {
        float cd = Mathf.Max(0.1f, seconds);
        killer.SetKillCooldown(cd); // 即時

        if (writeLog)
        {
            UtilsGameLog.AddGameLog(
                "YuruSheriff",
                $"<color=#ffb347>{UtilsName.GetPlayerColor(killer.PlayerId)}</color> の次回キルCDが <b>{cd:0.##}秒</b> に設定されました"
            );
        }

        var host = HudManager.Instance;
        if (host != null)
            host.StartCoroutine(YS_DelaySetCooldownOnce(killer, cd, 0.05f));
    }

    private static System.Collections.IEnumerator YS_DelaySetCooldownOnce(PlayerControl killer, float cd, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (killer != null) killer.SetKillCooldown(cd);
    }

    // ── メインロジック ────────────────────────────────────────
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "YuruSheriff");

            if (ShotLimit <= 0)
            {
                info.DoKill = false;
                return;
            }

            // 弾を消費＆同期
            ShotLimit--;
            SendRPC();

            // ▼ ミス抽選
            int missRate = Mathf.Clamp(ShotMissRateOpt?.GetInt() ?? 0, 0, 100);
            if (missRate > 0 && UnityEngine.Random.Range(0, 100) < missRate)
            {
                // 外れ：誰も死なない。外れ時トグルで分岐
                info.DoKill = false;

                var nextCd = (HasteOnMissOpt?.GetBool() == true)
                    ? ComputeNextCooldown(isSuccess: false)
                    : GetBaseCooldown();

                ApplySheriffCooldown(killer, nextCd, writeLog: nextCd < GetBaseCooldown());

                UtilsGameLog.AddGameLog(
                    "YuruSheriff",
                    $"<color=#ffb347>{UtilsName.GetPlayerColor(killer.PlayerId)}</color> の発砲は <b>外れ</b>（{missRate}％）"
                );
                return;
            }

            // 返り討ち（Alien/Tairou系）検査
            bool AlienTairo = false;
            var targetroleclass = target.GetRoleClass();
            if ((targetroleclass as Alien)?.CheckSheriffKill(target) == true) AlienTairo = true;
            if ((targetroleclass as JackalAlien)?.CheckSheriffKill(target) == true) AlienTairo = true;
            if ((targetroleclass as AlienHijack)?.CheckSheriffKill(target) == true) AlienTairo = true;

            // 撃てない相手 or 返り討ち → シェリフ自決（道連れは設定次第）
            if (!CanBeKilledBy(target) || AlienTairo)
            {
                PlayerState.GetByPlayerId(killer.PlayerId).DeathReason =
                    target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Revenge1 :
                    target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                    (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                    (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire));

                killer.RpcMurderPlayer(killer);
                UtilsGameLog.AddGameLog("YuruSheriff", string.Format(GetString("SheriffMissLog"), UtilsName.GetPlayerColor(target.PlayerId)));

                if (!MisfireKillsTarget.GetBool())
                {
                    info.DoKill = false;

                    // 自決のみ（道連れ無し）→ 外れ扱いの短縮設定 or ベース
                    var nextCdMiss = (HasteOnMissOpt?.GetBool() == true)
                        ? ComputeNextCooldown(isSuccess: false)
                        : GetBaseCooldown();

                    ApplySheriffCooldown(killer, nextCdMiss, writeLog: nextCdMiss < GetBaseCooldown());
                    return;
                }
            }

            // ▼ 正常にキル成立 → 成功時短縮設定を適用
            var nextCdOnSuccess = ComputeNextCooldown(isSuccess: true);
            ApplySheriffCooldown(killer, nextCdOnSuccess, writeLog: nextCdOnSuccess < GetBaseCooldown());
        }
    }

    public override string GetProgressText(bool comms = false, bool gamelog = false)
        => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");

    public static bool CanBeKilledBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();

        // シュレ猫は現在の陣営で分岐
        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
            {
                Logger.Warn($"シェリフ({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", nameof(YuruSheriff));
                return false;
            }
            else
            {
                if (player.IsLovers() && CanKillLovers.GetBool()) return true;
            }

            return schrodingerCat.Team switch
            {
                SchrodingerCat.TeamType.Mad => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var optMad) && optMad.GetBool(),
                SchrodingerCat.TeamType.Crew => false,
                _ => CanKillNeutrals.GetValue() == 0
                     || (SchrodingerCatKillTargetOptions.TryGetValue(schrodingerCat.Team, out var optCat) && optCat.GetBool()),
            };
        }

        // 恋人特例
        if (player.IsLovers() && CanKillLovers.GetBool()) return true;

        // 個別例外
        if (cRole == CustomRoles.Jackaldoll)
            return CanKillNeutrals.GetValue() == 0
                || (KillTargetOptions.TryGetValue(CustomRoles.Jackal, out var optJackal) && optJackal.GetBool())
                || (KillTargetOptions.TryGetValue(CustomRoles.JackalMafia, out var optJM) && optJM.GetBool());

        if (cRole == CustomRoles.SKMadmate)
            return KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var optSkMad) && optSkMad.GetBool();

        if (player.Is(CustomRoles.Amanojaku))
            return CanKillNeutrals.GetValue() == 0;

        // デフォルト
        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => cRole is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var optMad2) && optMad2.GetBool(),
            CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0
                                        || (KillTargetOptions.TryGetValue(cRole, out var optN) && optN.GetBool()),
            CustomRoleTypes.Crewmate => cRole is CustomRoles.WolfBoy,
            _ => false,
        };
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Sheriff_Kill"; // 既存ローカライズキー
        return true;
    }
}
