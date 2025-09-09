using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Modules;              // PlayerCatch, LateTask
using static TownOfHost.Translator;    // GetString

namespace TownOfHost.Roles.Crewmate
{
    /// <summary>
    /// 誰かがベントを使うとフラッシュが見える役職。
    /// ・会議（またはHUD）に直前ラウンド中のベント回数を表示
    /// ・ランダム遅延（最小/最大）対応
    /// ・Impostor / Madmate / Crewmate / Neutral を内訳表示（個別ON/OFF）
    /// ・Comms中の有効/無効を設定可能
    /// ※ フラッシュは互換性重視で標準 KillFlash() に統一
    /// </summary>
    public sealed class VentSeer : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(VentSeer),
                player => new VentSeer(player),
                CustomRoles.VentSeer,                 // ★ enum 追加が必要
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                11050,
                SetupOptionItem,
                "vs",
                "#61b26c",
                (6, 3)
            );

        public VentSeer(PlayerControl player) : base(RoleInfo, player)
        {
            CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
            if (!Counters.ContainsKey(Player.PlayerId))
                Counters[Player.PlayerId] = new VentCounters();
        }

        // ===== オプション =====
        private static OptionItem OptActiveComms;      // 通信妨害中でも有効
        private static OptionItem OptUseDelay;         // ランダム遅延を使う
        private static OptionItem OptMinDelay;         // 最小遅延(s)
        private static OptionItem OptMaxDelay;         // 最大遅延(s)
        private static OptionItem OptSplitByFaction;   // 陣営別に分ける
        private static OptionItem OptShowImpostor;     // インポスターをカウント
        private static OptionItem OptShowMadmate;      // マッドメイトをカウント
        private static OptionItem OptShowCrewmate;     // クルーをカウント
        private static OptionItem OptShowNeutral;      // 第三をカウント

        private static bool ActiveComms;
        private static bool UseDelay;
        private static float MinDelay;
        private static float MaxDelay;
        private static bool SplitByFaction;
        private static bool ShowImp, ShowMad, ShowCrew, ShowNeut;

        private enum OptName
        {
            VentSeerUseDelay,
            VentSeerMinDelay,
            VentSeerMaxDelay,
            VentSeerSplitByFaction,
            VentSeerShowImpostor,
            VentSeerShowMadmate,
            VentSeerShowCrewmate,
            VentSeerShowNeutral
        }

        private static void SetupOptionItem()
        {
            // 既存の共通キー（Doctor等と同じ使い方）
            OptActiveComms = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanUseActiveComms, true, false);

            // 遅延
            OptUseDelay = BooleanOptionItem.Create(RoleInfo, 11, OptName.VentSeerUseDelay, false, false);
            OptMinDelay = FloatOptionItem.Create(RoleInfo, 12, OptName.VentSeerMinDelay, new(0f, 60f, 0.5f), 0f, false, OptUseDelay)
                                .SetValueFormat(OptionFormat.Seconds);
            OptMaxDelay = FloatOptionItem.Create(RoleInfo, 13, OptName.VentSeerMaxDelay, new(0f, 60f, 0.5f), 3f, false, OptUseDelay)
                                .SetValueFormat(OptionFormat.Seconds);

            // 陣営別
            OptSplitByFaction = BooleanOptionItem.Create(RoleInfo, 20, OptName.VentSeerSplitByFaction, true, false);
            OptShowImpostor = BooleanOptionItem.Create(RoleInfo, 21, OptName.VentSeerShowImpostor, true, false, OptSplitByFaction);
            OptShowMadmate = BooleanOptionItem.Create(RoleInfo, 22, OptName.VentSeerShowMadmate, true, false, OptSplitByFaction);
            OptShowCrewmate = BooleanOptionItem.Create(RoleInfo, 23, OptName.VentSeerShowCrewmate, true, false, OptSplitByFaction);
            OptShowNeutral = BooleanOptionItem.Create(RoleInfo, 24, OptName.VentSeerShowNeutral, true, false, OptSplitByFaction);
        }

        public override void Add()
        {
            ActiveComms = OptActiveComms.GetBool();
            UseDelay = OptUseDelay.GetBool();
            MinDelay = OptMinDelay.GetFloat();
            MaxDelay = OptMaxDelay.GetFloat();
            SplitByFaction = OptSplitByFaction.GetBool();
            ShowImp = OptShowImpostor.GetBool();
            ShowMad = OptShowMadmate.GetBool();
            ShowCrew = OptShowCrewmate.GetBool();
            ShowNeut = OptShowNeutral.GetBool();
        }

        // ===== カウンタと表示 =====
        class VentCounters
        {
            public int Total; public int Imp; public int Mad; public int Crew; public int Neut;
        }
        private static readonly Dictionary<byte, VentCounters> Counters = new();

        // ★ 会議中 or HUD の両方で表示できるように（環境差対策）
        public string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (seen != seer) return "";
            if (!(isForMeeting || isForHud)) return "";   // どちらかで表示OK

            if (!Counters.TryGetValue(seer.PlayerId, out var c)) return "";

            if (SplitByFaction)
            {
                var parts = new List<string>();
                if (ShowImp) parts.Add($"Imp:{c.Imp}");
                if (ShowMad) parts.Add($"Mad:{c.Mad}");
                if (ShowCrew) parts.Add($"Crew:{c.Crew}");
                if (ShowNeut) parts.Add($"Neut:{c.Neut}");
                var detail = string.Join(" / ", parts);
                return $"<size=50%><color=#61b26c>Vents</color>: {c.Total}  <alpha=#AA>({detail})</alpha></size>";
            }
            else
            {
                return $"<size=50%><color=#61b26c>Vents</color>: {c.Total}</size>";
            }
        }

        // 会議後に直前ラウンドのカウントをクリア
        public override void AfterMeetingTasks()
        {
            if (Counters.TryGetValue(Player.PlayerId, out var c))
                c.Total = c.Imp = c.Mad = c.Crew = c.Neut = 0;
        }

        // ===== ベント検知 → フラッシュ通知 =====
        public static void NotifyVentUsed(PlayerControl venter)
        {
            try
            {
                if (venter == null) return;

                // 種別だけ先に確定（色分けは廃止、カウントのみ）
                var rtype = venter.GetCustomRole().GetCustomRoleTypes();

                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pc == null) continue;
                    if (pc.GetCustomRole() != CustomRoles.VentSeer) continue;

                    // Comms中の可否
                    var allowed = !Utils.IsActive(SystemTypes.Comms) || ActiveComms;
                    if (!allowed) continue;

                    // 陣営別フィルタ（Split時のみ／表示とフラッシュを同じ基準で）
                    if (SplitByFaction)
                    {
                        if (rtype == CustomRoleTypes.Impostor && !ShowImp) continue;
                        if (rtype == CustomRoleTypes.Madmate && !ShowMad) continue;
                        if (rtype == CustomRoleTypes.Crewmate && !ShowCrew) continue;
                        if (rtype == CustomRoleTypes.Neutral && !ShowNeut) continue;
                    }

                    // カウント（Total は常に加算）
                    if (!Counters.TryGetValue(pc.PlayerId, out var cnt))
                        Counters[pc.PlayerId] = cnt = new VentCounters();

                    cnt.Total++;
                    if (rtype == CustomRoleTypes.Impostor) cnt.Imp++;
                    else if (rtype == CustomRoleTypes.Madmate) cnt.Mad++;
                    else if (rtype == CustomRoleTypes.Crewmate) cnt.Crew++;
                    else if (rtype == CustomRoleTypes.Neutral) cnt.Neut++;

                    // フラッシュ（互換性重視。色付きは未使用）
                    void doFlash()
                    {
                        try { if (!GameStates.CalledMeeting && pc.IsAlive()) pc.KillFlash(); }
                        catch { }
                    }

                    if (UseDelay)
                    {
                        float extra = 0f;
                        if (MaxDelay > 0f)
                        {
                            int ti = IRandom.Instance.Next(0, (int)(MaxDelay * 10f));
                            extra = ti * 0.1f;
                        }
                        var delay = MinDelay + extra;
                        _ = new LateTask(doFlash, delay, "VentSeerDelayFlash", null);
                    }
                    else
                    {
                        doFlash();
                    }
                }
            }
            catch (Exception ex)
            {
                TownOfHost.Logger.Exception(ex, "VentSeer.NotifyVentUsed");
            }
        }
    }

    // ===== ベント使用フック（実環境に合わせて使う方を有効化） =====
    [HarmonyPatch]
    static class VentSeer_VentHook
    {
        [HarmonyPatch(typeof(Vent), "EnterVent")]
        [HarmonyPostfix]
        static void Postfix_EnterVent(Vent __instance, PlayerControl pc)
        {
            try { if (pc != null) TownOfHost.Roles.Crewmate.VentSeer.NotifyVentUsed(pc); } catch { }
        }

        // 必要ならRPCの方も
        /*
        [HarmonyPatch(typeof(PlayerControl), "RpcEnterVent")]
        [HarmonyPostfix]
        static void Postfix_RpcEnterVent(PlayerControl __instance, int ventId)
        {
            try { if (__instance != null) TownOfHost.Roles.Crewmate.VentSeer.NotifyVentUsed(__instance); } catch { }
        }
        */
    }
}
