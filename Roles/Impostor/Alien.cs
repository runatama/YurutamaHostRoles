using AmongUs.GameOptions;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Linq;
using UnityEngine;
using TownOfHost.Modules;
using Hazel;
using System.Text;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

// コードがクッソ長い!!スパゲッティかよ!!まぁ処理が複雑な役職だからね。仕方ない。
//
// メモ
// ・キルクール増加,キルク減少はバグありなので一回封印。
// 追加したいなぁって思ってるの
//ペンギン
//マジシャン

public sealed class Alien : RoleBase, IMeetingTimeAlterable, IImpostor, INekomata
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Alien),
                player => new Alien(player),
                CustomRoles.Alien,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                3351,
                SetupOptionItem,
                "Al",
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
            );
    public Alien(PlayerControl player)
: base(
    RoleInfo,
    player
    )
    {
        RateVampire = OptionModeVampire.GetInt();
        RateEvilHacker = OptionModeEvilHacker.GetInt();
        RateLimiter = OptionModeLimiter.GetInt();
        RateNomal = OptionModeNomal.GetInt();
        RatePuppeteer = OptionModePuppeteer.GetInt();
        RateStealth = OptionModeStealth.GetInt();
        RateRemotekiller = OptionModeRemotekiller.GetInt();
        RateNotifier = OptionModeNotifier.GetInt();
        RateTimeThief = OptionModeTimeThief.GetInt();
        RateTairo = OptionModeTairo.GetInt();
        RateMayor = OptionModeMayor.GetInt();
        RateProgresskiller = OptionModeProgresskiller.GetInt();
        RateMole = OptionModeMole.GetInt();
        RateNekokabocha = OptionModeNekokabocha.GetInt();

        TimeThiefDecreaseMeetingTime = OptionTimeThiefDecreaseMeetingTime.GetInt();
        NotifierCance = OptionNotifierProbability.GetInt();
        VampireKillDelay = OptionVampireKillDelay.GetFloat();
        AlienHitoku = OptionAlienHitoku.GetBool();
        Limiterblastrange = Optionblastrange.GetFloat();
        TimeThiefReturnStolenTimeUponDeath = OptionTimeThiefReturnStolenTimeUponDeath.GetBool();
        StealthDarkenDuration = OptionStealthDarkenDuration.GetInt();
        TairoDeathReason = OptionTairoDeathReason.GetBool();
        AdditionalVote = OptionAdditionalVote.GetInt();
        ProgressKillerMadseen = OptionProgressKillerMadseen.GetBool();
        ProgressWorkhorseseen = OptionProgressWorkhorseseen.GetBool();
        impostorsGetRevenged = optionImpostorsGetRevenged.GetBool();
        madmatesGetRevenged = optionMadmatesGetRevenged.GetBool();
        NeutralsGetRevenged = optionNeutralsGetRevenged.GetBool();
        revengeOnExile = optionRevengeOnExile.GetBool();

        modeNone = true;
        modeVampire = false;
        modeEvilHacker = false;
        modeLimiter = false;
        modeNomal = false;
        modePuppeteer = false;
        modeStealth = false;
        modeRemotekiller = false;
        modeNotifier = false;
        modeTimeThief = false;
        modeTairo = false;
        modeMayor = false;
        modeProgresskiller = false;
        modeMole = false;
        modeNekokabocha = false;
        Count = 0;
        Remotekillertarget = 111;

        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void Add()
    {
        modeNone = true;
        modeVampire = false;
        modeEvilHacker = false;
        modeLimiter = false;
        modeNomal = false;
        modePuppeteer = false;
        modeStealth = false;
        modeRemotekiller = false;
        modeNotifier = false;
        modeTimeThief = false;
        modeTairo = false;
        modeMayor = false;
        modeProgresskiller = false;
        modeMole = false;
        modeNekokabocha = false;
        Count = 0;
        Remotekillertarget = 111;

        Aliens.Add(this);
    }
    public override void OnDestroy()
    {
        Aliens.Clear();
        Puppets.Clear();
    }
    public static HashSet<Alien> Aliens = new();
    //ヴァンパイア
    static OptionItem OptionModeVampire;
    static OptionItem OptionVampireKillDelay;
    static float VampireKillDelay;
    static int RateVampire;
    bool modeVampire;
    //イビルハッカー
    static OptionItem OptionModeEvilHacker;
    static int RateEvilHacker;
    bool modeEvilHacker;
    //リミッター
    static OptionItem OptionModeLimiter;
    static OptionItem Optionblastrange;
    static int RateLimiter;
    static float Limiterblastrange;
    bool modeLimiter;
    //ノーマル
    static OptionItem OptionModeNomal;
    static int RateNomal;
    bool modeNomal;
    //パペッティア
    static OptionItem OptionModePuppeteer;
    static int RatePuppeteer;
    bool modePuppeteer;
    //リモートキラー
    static OptionItem OptionModeRemotekiller;
    static int RateRemotekiller;
    bool modeRemotekiller;
    static byte Remotekillertarget;
    //ステルス
    static OptionItem OptionModeStealth;
    static int RateStealth;
    bool modeStealth;
    static OptionItem OptionStealthDarkenDuration;
    float StealthDarkenDuration;
    float darkenTimer;
    PlayerControl[] darkenedPlayers;
    SystemTypes? darkenedRoom = null;
    //ノーティファー
    static OptionItem OptionModeNotifier;
    static OptionItem OptionNotifierProbability;
    static int RateNotifier;
    bool modeNotifier;
    static int NotifierCance;
    //タイムシーフ
    static OptionItem OptionModeTimeThief;
    static OptionItem OptionTimeThiefDecreaseMeetingTime;
    static int RateTimeThief;
    bool modeTimeThief;
    static int TimeThiefDecreaseMeetingTime;
    static OptionItem OptionTimeThiefReturnStolenTimeUponDeath;
    static bool TimeThiefReturnStolenTimeUponDeath;
    public bool RevertOnDie => TimeThiefReturnStolenTimeUponDeath;
    static int Count;
    //大狼
    static OptionItem OptionModeTairo;
    static OptionItem OptionTairoDeathReason;
    static int RateTairo;
    public bool modeTairo;
    public static bool TairoDeathReason;
    //メイヤー
    static OptionItem OptionModeMayor;
    static OptionItem OptionAdditionalVote;
    static int RateMayor;
    bool modeMayor;
    static int AdditionalVote;
    //モグラ
    static OptionItem OptionModeMole;
    static int RateMole;
    bool modeMole;
    //プログレスキラー
    static OptionItem OptionModeProgresskiller;
    static OptionItem OptionProgressKillerMadseen;
    static OptionItem OptionProgressWorkhorseseen;
    static int RateProgresskiller;
    public bool modeProgresskiller;
    static bool ProgressKillerMadseen;
    public static bool ProgressWorkhorseseen;
    //ネコカボチャ
    static OptionItem OptionModeNekokabocha;
    static BooleanOptionItem optionImpostorsGetRevenged;
    static BooleanOptionItem optionMadmatesGetRevenged;
    static BooleanOptionItem optionNeutralsGetRevenged;
    static BooleanOptionItem optionRevengeOnExile;
    static int RateNekokabocha;
    bool modeNekokabocha;
    static bool impostorsGetRevenged;
    static bool madmatesGetRevenged;
    static bool NeutralsGetRevenged;
    static bool revengeOnExile;

    static readonly LogHandler logger = Logger.Handler(nameof(Alien));
    //秘匿設定
    static OptionItem OptionAlienHitoku;
    static bool AlienHitoku;
    bool modeNone;
    enum OptionName
    {
        AlienHitoku,
        AlienCVampire, VampireKillDelay,
        AlienCEvilHacker,
        AlienCLimiter, blastrange,
        AlienCPuppeteer,
        AlienCRemoteKiller,
        AlienCStealth, StealthDarkenDuration,
        AlienCNomal,
        AlienCNotifier, NotifierProbability,
        AlienCTimeThief, TimeThiefDecreaseMeetingTime, TimeThiefReturnStolenTimeUponDeath,
        AlienCTairo, TairoDeathReason,
        AlienCMayor, MayorAdditionalVote,
        AlienCMole,
        AlienCProgressKiller, ProgressKillerMadseen, ProgressWorkhorseseen,
        AlienCNekokabocha, NekoKabochaImpostorsGetRevenged, NekoKabochaMadmatesGetRevenged, NekoKabochaNeutralsGetRevenged, NekoKabochaRevengeOnExile,
    }
    Dictionary<byte, float> BittenPlayers = new(14);
    static void SetupOptionItem()
    {
        OptionAlienHitoku = BooleanOptionItem.Create(RoleInfo, 9, OptionName.AlienHitoku, false, false);
        OptionModeVampire = FloatOptionItem.Create(RoleInfo, 10, OptionName.AlienCVampire, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionVampireKillDelay = FloatOptionItem.Create(RoleInfo, 11, OptionName.VampireKillDelay, new(5, 100, 1), 10, false, OptionModeVampire).SetValueFormat(OptionFormat.Seconds);
        OptionModeEvilHacker = FloatOptionItem.Create(RoleInfo, 12, OptionName.AlienCEvilHacker, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeLimiter = FloatOptionItem.Create(RoleInfo, 13, OptionName.AlienCLimiter, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        Optionblastrange = FloatOptionItem.Create(RoleInfo, 14, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false, OptionModeLimiter);
        OptionModePuppeteer = FloatOptionItem.Create(RoleInfo, 15, OptionName.AlienCPuppeteer, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeStealth = FloatOptionItem.Create(RoleInfo, 18, OptionName.AlienCStealth, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionStealthDarkenDuration = FloatOptionItem.Create(RoleInfo, 19, OptionName.StealthDarkenDuration, new(0.5f, 5f, 0.5f), 1f, false, OptionModeStealth).SetValueFormat(OptionFormat.Seconds);
        OptionModeRemotekiller = FloatOptionItem.Create(RoleInfo, 20, OptionName.AlienCRemoteKiller, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeNotifier = FloatOptionItem.Create(RoleInfo, 21, OptionName.AlienCNotifier, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionNotifierProbability = FloatOptionItem.Create(RoleInfo, 22, OptionName.NotifierProbability, new(0, 100, 5), 50, false, OptionModeNotifier).SetValueFormat(OptionFormat.Percent);
        OptionModeTimeThief = FloatOptionItem.Create(RoleInfo, 23, OptionName.AlienCTimeThief, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTimeThiefDecreaseMeetingTime = FloatOptionItem.Create(RoleInfo, 24, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 5), 50, false, OptionModeTimeThief).SetValueFormat(OptionFormat.Seconds);
        OptionTimeThiefReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 25, OptionName.TimeThiefReturnStolenTimeUponDeath, false, false, OptionModeTimeThief);
        OptionModeTairo = FloatOptionItem.Create(RoleInfo, 26, OptionName.AlienCTairo, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionTairoDeathReason = BooleanOptionItem.Create(RoleInfo, 27, OptionName.TairoDeathReason, false, false, OptionModeTairo);
        OptionModeMayor = FloatOptionItem.Create(RoleInfo, 28, OptionName.AlienCMayor, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 29, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false, OptionModeMayor).SetValueFormat(OptionFormat.Votes);
        OptionModeMole = FloatOptionItem.Create(RoleInfo, 30, OptionName.AlienCMole, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionModeProgresskiller = FloatOptionItem.Create(RoleInfo, 31, OptionName.AlienCProgressKiller, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        OptionProgressKillerMadseen = BooleanOptionItem.Create(RoleInfo, 32, OptionName.ProgressKillerMadseen, false, false, OptionModeProgresskiller);
        OptionProgressWorkhorseseen = BooleanOptionItem.Create(RoleInfo, 33, OptionName.ProgressWorkhorseseen, false, false, OptionModeProgresskiller);
        OptionModeNekokabocha = FloatOptionItem.Create(RoleInfo, 34, OptionName.AlienCNekokabocha, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        optionImpostorsGetRevenged = BooleanOptionItem.Create(RoleInfo, 35, OptionName.NekoKabochaImpostorsGetRevenged, false, false, OptionModeNekokabocha);
        optionMadmatesGetRevenged = BooleanOptionItem.Create(RoleInfo, 36, OptionName.NekoKabochaMadmatesGetRevenged, false, false, OptionModeNekokabocha);
        optionNeutralsGetRevenged = BooleanOptionItem.Create(RoleInfo, 37, OptionName.NekoKabochaNeutralsGetRevenged, false, false, OptionModeNekokabocha);
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 38, OptionName.NekoKabochaRevengeOnExile, false, false, OptionModeNekokabocha);
        OptionModeNomal = FloatOptionItem.Create(RoleInfo, 8, OptionName.AlienCNomal, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
    }
    public override string GetProgressText(bool comms = false)
    {
        if (!Player.IsAlive()) return "";
        if (AlienHitoku || GameStates.Meeting)
        {
            return Mode();
        }
        return "";
    }
    public string Mode()
    {
        if (!Player.IsAlive()) return "";

        if (modeNone) return "<size=75%><color=#ff1919>mode:None</color></size>";
        if (modeVampire) return "<size=75%><color=#ff1919>mode:" + GetString("Vampire") + "</color></size>";
        if (modeEvilHacker) return "<size=75%><color=#ff1919>mode:" + GetString("EvilHacker") + "</color></size>";
        if (modeLimiter) return "<size=75%><color=#ff1919>mode:" + GetString("Limiter") + "</color></size>";
        if (modePuppeteer) return "<size=75%><color=#ff1919>mode:" + GetString("Puppeteer") + "</color></size>";
        if (modeStealth) return "<size=75%><color=#ff1919>mode:" + GetString("Stealth") + "</color></size>";
        if (modeRemotekiller) return "<size=75%><color=#8f00ce>mode:" + GetString("Remotekiller") + "</color></size>";
        if (modeNotifier) return "<size=75%><color=#ff1919>mode:" + GetString("Notifier") + "</color></size>";
        if (modeTimeThief) return "<size=75%><color=#ff1919>mode:" + GetString("TimeThief") + "</color></size>";
        if (modeTairo) return "<size=75%><color=#ff1919>mode:" + GetString("Tairou") + "</color></size>";
        if (modeMayor) return "<size=75%><color=#204d42>mode:" + GetString("Mayor") + "</color></size>";
        if (modeMole) return "<size=75%><color=#ff1919>mode:" + GetString("Mole") + "</color></size>";
        if (modeProgresskiller) return "<size=75%><color=#ff1919>mode:" + GetString("ProgressKiller") + "</color></size>";
        if (modeNekokabocha) return "<size=75%><color=#ff1919>mode:" + GetString("NekoKabocha") + "</color></size>";
        if (modeNomal) return "<size=75%><color=#ff1919>mode:Normal</color></size>";

        return "<size=75%><color=#ff1919>mode:？</color></size>";
    }
    public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo __)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!Player.IsAlive())//死んでたらこれより後の処理しない。
        {
            return;
        }
        //とりま状態リセット
        Remotekillertarget = 111;
        Puppets.Clear();
        SendRPC(byte.MaxValue, 0);
        foreach (var targetId in BittenPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            KillBitten(target, true);
            if (repo == target)
            {
                ReportDeadBodyPatch.DieCheckReport(repo, __);
            }
        }
        BittenPlayers.Clear();

        //modeEvilHacker
        if (modeEvilHacker)
        {
            var admins = AdminProvider.CalculateAdmin();
            var builder = new StringBuilder(512);

            // 送信するメッセージを生成
            foreach (var admin in admins)
            {
                var entry = admin.Value;
                if (entry.TotalPlayers <= 0)
                {
                    continue;
                }
                // インポスターがいるなら星マークを付ける
                if (entry.NumImpostors > 0)
                {
                    builder.Append('★');
                }
                // 部屋名と合計プレイヤー数を表記
                builder.Append(DestroyableSingleton<TranslationController>.Instance.GetString(entry.Room));
                builder.Append(": ");
                builder.Append(entry.TotalPlayers);
                // 死体があったら死体の数を書く
                if (entry.NumDeadBodies > 0)
                {
                    builder.Append('(').Append(Translator.GetString("Deadbody"));
                    builder.Append('×').Append(entry.NumDeadBodies).Append(')');
                }
                builder.Append('\n');
            }

            // 送信
            var message = builder.ToString();
            var title = Utils.ColorString(Color.green, Translator.GetString("LastAdminInfo"));

            _ = new LateTask(() =>
            {
                if (GameStates.IsInGame)
                {
                    Utils.SendMessage(message, Player.PlayerId, title, false);
                }
            }, 4f, "EvilHacker Admin Message");
            return;
        }
    }

    /// 次ターンの能力を決める。
    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!Player.IsAlive())//死んでたら処理しない。
        {
            return;
        }
        //とりま全モード一旦false
        modeNone = false;
        modeVampire = false;
        modeEvilHacker = false;
        modeLimiter = false;
        modeNomal = false;
        modePuppeteer = false;
        modeStealth = false;
        modeRemotekiller = false;
        modeTimeThief = false;
        modeNotifier = false;
        modeTairo = false;
        modeMayor = false;
        modeMole = false;
        modeProgresskiller = false;
        modeNekokabocha = false;

        int Count = RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer
                    + RateStealth + RateRemotekiller + RateNotifier + RateTimeThief
                    + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha + RateNomal;
        int chance = IRandom.Instance.Next(1, Count);
        //ランダム
        if (chance <= RateVampire)
        {
            modeVampire = true;
            Logger.Info("Alienはヴァンパイアになりました。", "Alien");
        }
        else
        if (chance <= RateVampire + RateEvilHacker)
        {
            Logger.Info("Alienはイビルハッカーになりました。", "Alien");
            modeEvilHacker = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter)
        {
            Logger.Info("Alienはリミッターになりました。", "Alien");
            modeLimiter = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer)
        {
            Logger.Info("Alienはパペッティアになりました。", "Alien");
            modePuppeteer = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth)
        {
            Logger.Info("Alienはステルスになりました。", "Alien");
            modeStealth = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller)
        {
            Logger.Info("Alienはリモートキラーになりました。", "Alien");
            modeRemotekiller = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier)
        {
            Logger.Info("Alienはノーティファーになりました。", "Alien");
            modeNotifier = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief)
        {
            Logger.Info("Alienはタイムシーフになりました。", "Alien");
            modeTimeThief = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo)
        {
            Logger.Info("Alienは大狼になりました。", "Alien");
            modeTairo = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor)
        {
            Logger.Info("Alienはメイヤーになりました。", "Alien");
            modeMayor = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole)
        {
            Logger.Info("Alienはモグラになりました。", "Alien");
            modeMole = true;
        }
        else
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller)
        {
            Logger.Info("Alienはプログレスキラーになりました。", "Alien");
            modeProgresskiller = true;
        }
        if (chance <= RateVampire + RateEvilHacker + RateLimiter + RatePuppeteer + RateStealth + RateRemotekiller
        + RateNotifier + RateTimeThief + RateTairo + RateMayor + RateMole + RateProgresskiller + RateNekokabocha)
        {
            Logger.Info("Alienはネコカボチャになりました。", "Alien");
            modeNekokabocha = true;
        }
        else//どれにもあてはまらないならとりあえずノーマル
        {
            Logger.Info("ｴｰﾘｱﾝﾜﾀｼｴｰﾘｱﾝ", "Alien");
            modeNomal = true;
        }
    }
    public override void OnStartMeeting()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            ResetDarkenState();
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (modeVampire)//ヴァンパイアの時のみ実行
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;
            if (target.Is(CustomRoles.Bait)) return;
            if (target.Is(CustomRoles.InSender)) return;
            if (info.IsFakeSuicide) return;
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                killer.SetKillCooldown();
                BittenPlayers.Add(target.PlayerId, 0f);
            }
            info.DoKill = false;
        }

        //リミッターの時のみ実行。
        if (modeLimiter)
        {
            var Targets = new List<PlayerControl>(Main.AllAlivePlayerControls);
            foreach (var tage in Targets)
            {
                info.DoKill = false;
                var distance = Vector3.Distance(Player.transform.position, tage.transform.position);
                if (distance > Limiterblastrange) continue;
                PlayerState.GetByPlayerId(tage.PlayerId).DeathReason = CustomDeathReason.Bombed;
                tage.SetRealKiller(tage);
                tage.RpcMurderPlayer(tage, true);
                RPC.PlaySoundRPC(tage.PlayerId, Sounds.KillSound);
            }
        }

        //パペッティアの時のみ実行。
        if (modePuppeteer)
        {
            var (puppeteer, target) = info.AttemptTuple;

            Puppets[target.PlayerId] = this;
            SendRPC(target.PlayerId, 1);
            puppeteer.SetKillCooldown();
            Utils.NotifyRoles(SpecifySeer: puppeteer);
            info.DoKill = false;
        }

        //ステルスの時のみ
        if (modeStealth)
        {
            // キルできない，もしくは普通のキルじゃないならreturn
            if (!info.CanKill || !info.DoKill || info.IsSuicide || info.IsAccident || info.IsFakeSuicide)
            {
                return;
            }
            var playersToDarken = FindPlayersInSameRoom(info.AttemptTarget);
            if (playersToDarken == null)
            {
                Logger.Info("部屋の当たり判定を取得できないため暗転を行いません", "Alien.S");
                return;
            }
            DarkenPlayers(playersToDarken);
        }
        //リモートキラー
        if (modeRemotekiller)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;
            if (target.Is(CustomRoles.Bait)) return;
            if (target.Is(CustomRoles.InSender)) return;
            if (info.IsFakeSuicide) return;
            //登録
            Remotekillertarget = target.PlayerId;
            killer.RpcProtectedMurderPlayer(target);
            info.DoKill = false;
        }
        //ノーティファー
        if (modeNotifier)
        {
            if (!info.IsSuicide)
            {
                (var killer, var target) = info.AttemptTuple;
                int chance = IRandom.Instance.Next(1, 101);
                if (chance <= NotifierCance)
                {
                    Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知成功", "Notifier");
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        player.KillFlash();
                    }
                }
                else
                {
                    Logger.Info($"{killer?.Data?.PlayerName}: フラ全体通知失敗", "Notifier");
                }
            }
        }
        if (modeTimeThief)//タイムシーフはタイムシーフモード中じゃないと会議時間を減らさない。
        {
            if (!info.IsSuicide)
            {
                Count++;//キルが成功したらカウントを1増やす。
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl _)
    {
        //ヴァンパイアの時のみ実行
        if (modeVampire)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BittenPlayers.ToArray())
            {
                if (timer >= VampireKillDelay)
                {
                    var target = Utils.GetPlayerById(targetId);
                    KillBitten(target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    BittenPlayers[targetId] += Time.fixedDeltaTime;
                }
            }
        }
        if (modeStealth)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                return;
            }
            // 誰かを暗転させているとき
            if (darkenedPlayers != null)
            {
                // タイマーを減らす
                darkenTimer -= Time.fixedDeltaTime;
                // タイマーが0になったらみんなの視界を戻してタイマーと暗転プレイヤーをリセットする
                if (darkenTimer <= 0)
                {
                    ResetDarkenState();
                }
            }
        }
    }
    /// <summary>
    /// そもそもヴァンパイアモードの時しか呼び出されないはず。
    /// </summary>
    /// <param name="target"></param>
    /// <param name="isButton"></param>
    void KillBitten(PlayerControl target, bool isButton = false)
    {
        var vampire = Player;
        if (target.IsAlive())
        {
            PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bite;
            target.SetRealKiller(vampire);
            CustomRoleManager.OnCheckMurder(
                vampire, target,
                target, target
            );
            Logger.Info($"Alienに噛まれている{target.name}を自爆させました。", "Alien.Va");
            if (!isButton && vampire.IsAlive())
            {
                RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
            }
        }
        else
        {
            Logger.Info($"Alienに噛まれている{target.name}はすでに死んでいました。", "Alien.Va");
        }
    }
    //これより↓パペッティア
    static Dictionary<byte, Alien> Puppets = new(15);
    enum RPC_type
    {
        SyncPuppet,
        StealthDarken
    }

    private void SendRPC(byte targetId, byte typeId)
    {
        using var sender = CreateSender();

        sender.Writer.Write((byte)RPC_type.SyncPuppet);
        sender.Writer.Write(typeId);
        sender.Writer.Write(targetId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_type)reader.ReadByte())
        {
            case RPC_type.StealthDarken:
                var roomId = reader.ReadByte();
                darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
                break;

            case RPC_type.SyncPuppet:
                var typeId = reader.ReadByte();
                var targetId = reader.ReadByte();

                switch (typeId)
                {
                    case 0: //Dictionaryのクリア
                        Puppets.Clear();
                        break;
                    case 1: //Dictionaryに追加
                        Puppets[targetId] = this;
                        break;
                    case 2: //DictionaryのKey削除
                        Puppets.Remove(targetId);
                        break;
                }
                break;
        }
    }
    public void OnFixedUpdateOthers(PlayerControl puppet)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (Puppets.TryGetValue(puppet.PlayerId, out var puppeteer))
            CheckPuppetKillA(puppet);
    }
    void CheckPuppetKillA(PlayerControl puppet)
    {
        if (!puppet.IsAlive())
        {
            Puppets.Remove(puppet.PlayerId);
            SendRPC(puppet.PlayerId, 2);
        }
        else
        {
            var puppetPos = puppet.transform.position;//puppetの位置
            Dictionary<PlayerControl, float> targetDistance = new();
            foreach (var pc in Main.AllAlivePlayerControls.ToArray())
            {
                if (pc.PlayerId != puppet.PlayerId && !pc.Is(CountTypes.Impostor))
                {
                    var dis = Vector2.Distance(puppetPos, pc.transform.position);
                    targetDistance.Add(pc, dis);
                }
            }
            if (targetDistance.Keys.Count <= 0) return;

            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
            var target = min.Key;
            var KillRange = NormalGameOptionsV08.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
            if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
            {
                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                target.SetRealKiller(Player);
                puppet.RpcMurderPlayer(target);
                Utils.MarkEveryoneDirtySettings();
                Puppets.Remove(puppet.PlayerId);
                SendRPC(puppet.PlayerId, 2);
                Utils.NotifyRoles();
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        if (modePuppeteer)
        {
            seen ??= seer;
            if (!(Puppets.ContainsValue(this) &&
                Puppets.ContainsKey(seen.PlayerId))) return "";
            return Utils.ColorString(RoleInfo.RoleColor, "◆");
        }
        else
        if (modeStealth)
        {
            if (seer != Player || seen != Player || !darkenedRoom.HasValue)
            {
                return base.GetSuffix(seer, seen);
            }
            return string.Format(Translator.GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value));
        }
        if (modeProgresskiller)
        {
            seen ??= seer;
            if (ProgressKillerMadseen && seen.Is(CustomRoleTypes.Madmate) && seer.Is(CustomRoles.Alien) && seer != seen)
            {
                if (seen.GetPlayerTaskState().IsTaskFinished)
                    return Utils.ColorString(RoleInfo.RoleColor, "☆");
            }
            return "";
        }
        else return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        foreach (var al in Aliens)
        {
            if (al.modeProgresskiller && al.Player == seen)
            {
                if (seer.Is(CustomRoles.Alien) && !seen.Is(CustomRoleTypes.Madmate) && seer != seen)
                {
                    if (seen.GetPlayerTaskState().IsTaskFinished)
                        return Utils.ColorString(RoleInfo.RoleColor, "〇");
                }
                return "";
            }
            if (al.Player != seer && seen == al.Player && !seer.IsAlive() && !AlienHitoku && !GameStates.Meeting && !MeetingStates.FirstMeeting)
            {
                return $"<size=50%>{al.Mode()}</size>";
            }
        }

        return "";
    }
    //これより↓ステルス
    IEnumerable<PlayerControl> FindPlayersInSameRoom(PlayerControl killedPlayer)
    {
        var room = killedPlayer.GetPlainShipRoom();
        if (room == null)
        {
            return null;
        }
        var roomArea = room.roomArea;
        var roomName = room.RoomId;
        RpcDarken(roomName);
        return Main.AllAlivePlayerControls.Where(player => player != Player && player.Collider.IsTouching(roomArea));
    }
    /// <summary>渡されたプレイヤーを<see cref="darkenDuration"/>秒分視界ゼロにする</summary>
    void DarkenPlayers(IEnumerable<PlayerControl> playersToDarken)
    {
        darkenedPlayers = playersToDarken.ToArray();
        foreach (var player in playersToDarken)
        {
            PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = true;
            player.MarkDirtySettings();
        }
    }
    void RpcDarken(SystemTypes? roomType)
    {
        Logger.Info($"暗転させている部屋を{roomType?.ToString() ?? "null"}に設定", "Alien.S");
        darkenedRoom = roomType;
        using var sender = CreateSender();
        sender.Writer.Write((byte)RPC_type.StealthDarken);
        sender.Writer.Write((byte?)roomType ?? byte.MaxValue);
    }
    void ResetDarkenState()
    {
        if (darkenedPlayers != null)
        {
            foreach (var player in darkenedPlayers)
            {
                PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = false;
                player.MarkDirtySettings();
            }
            darkenedPlayers = null;
        }
        darkenTimer = StealthDarkenDuration;
        RpcDarken(null);
        Utils.NotifyRoles(SpecifySeer: Player);
    }
    //リモートキラー
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (modeRemotekiller)
        {
            var user = physics.myPlayer;
            if (Remotekillertarget != 111 && Player.PlayerId == user.PlayerId)
            {
                var target = Utils.GetPlayerById(Remotekillertarget);
                if (!target.IsAlive()) return true;
                _ = new LateTask(() =>
                {
                    target.SetRealKiller(user);
                    user.RpcMurderPlayer(target, true);
                }, 1f);
                RPC.PlaySoundRPC(user.PlayerId, Sounds.KillSound);
                Remotekillertarget = 111;
                return false;
            }
        }
        if (modeMole)//モグラ
        {
            _ = new LateTask(() =>
        {
            int chance = IRandom.Instance.Next(0, ShipStatus.Instance.AllVents.Count);
            Player.RpcSnapToForced((Vector2)ShipStatus.Instance.AllVents[chance].transform.position + new Vector2(0f, 0.1f));
        }, 0.7f, "TP");
            return true;

        }
        return true;
    }
    public override bool CantVentIdo(PlayerPhysics physics, int ventId)
    {
        if (modeMole) return false;
        return true;
    }
    //タイムシーフ
    public int CalculateMeetingTimeDelta()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return 0;
        var sec = -(TimeThiefDecreaseMeetingTime * Count);
        return sec;
    }
    //大老
    public override CustomRoles GetFtResults(PlayerControl player) => modeTairo ? CustomRoles.Alien : CustomRoles.Crewmate;
    //メイヤー
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId && modeMayor)
        {
            numVotes = AdditionalVote + 1;
        }
        return (votedForId, numVotes, doVote);
    }
    //ここから先ネコカボチャ
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (modeNekokabocha)
        {
            // 普通のキルじゃない．もしくはキルを行わない時はreturn
            if (GameStates.IsMeeting || info.IsAccident || info.IsSuicide || !info.CanKill || !info.DoKill)
            {
                return;
            }
            // 殺してきた人を殺し返す
            if (!GameStates.Meeting && PlayerState.GetByPlayerId(Player.PlayerId).DeathReason is CustomDeathReason.Revenge) return;
            logger.Info("ネコカボチャの仕返し");
            var killer = info.AttemptKiller;
            if (!IsCandidate(killer))
            {
                logger.Info("キラーは仕返し対象ではないので仕返しされません");
                return;
            }
            killer.SetRealKiller(Player);
            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Revenge;
            Player.RpcMurderPlayer(killer);
        }
    }
    public bool DoRevenge(CustomDeathReason deathReason) => modeNekokabocha && revengeOnExile && deathReason == CustomDeathReason.Vote;
    public bool IsCandidate(PlayerControl player)
    {
        return player.GetCustomRole().GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => impostorsGetRevenged,
            CustomRoleTypes.Madmate => madmatesGetRevenged,
            CustomRoleTypes.Neutral => NeutralsGetRevenged,
            _ => true,
        };
    }
    public bool CheckSheriffKill(PlayerControl target)
    {
        if (target == Player) return modeTairo;
        return false;
    }
}