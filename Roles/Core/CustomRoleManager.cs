using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Text;

using AmongUs.GameOptions;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Core;

public static class CustomRoleManager
{
    public static Type[] AllRolesClassType;
    public static Dictionary<CustomRoles, SimpleRoleInfo> AllRolesInfo = new(CustomRolesHelper.AllRoles.Length);
    public static Dictionary<byte, RoleBase> AllActiveRoles = new(15);

    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ContainsKey(role) ? AllRolesInfo[role] : null;
    public static RoleBase GetRoleClass(this PlayerControl player) => GetByPlayerId(player.PlayerId);
    public static RoleBase GetByPlayerId(byte playerId) => AllActiveRoles.TryGetValue(playerId, out var roleBase) ? roleBase : null;
    public static void Do<T>(this List<T> list, Action<T> action) => list.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    public static Dictionary<byte, MurderInfo> CheckMurderInfos = new();

    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptKiller">実際にキルを行ったプレイヤー 不変</param>
    /// <param name="attemptTarget">>Killerが実際にキルを行おうとしたプレイヤー 不変</param>
    public static bool OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget)
        => OnCheckMurder(attemptKiller, attemptTarget, attemptKiller, attemptTarget);
    /// <summary>
    ///
    /// </summary>
    /// <param name="attemptKiller">実際にキルを行ったプレイヤー 不変</param>
    /// <param name="attemptTarget">>Killerが実際にキルを行おうとしたプレイヤー 不変</param>
    /// <param name="appearanceKiller">見た目上でキルを行うプレイヤー 可変</param>
    /// <param name="appearanceTarget">見た目上でキルされるプレイヤー 可変</param>
    /// <returns></returns>
    public static bool OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        Logger.Info($"Attempt  :{attemptKiller.GetNameWithRole()} => {attemptTarget.GetNameWithRole()}", "CheckMurder");
        if (appearanceKiller != attemptKiller || appearanceTarget != attemptTarget)
            Logger.Info($"Apperance:{appearanceKiller.GetNameWithRole()} => {appearanceTarget.GetNameWithRole()}", "CheckMurder");

        var info = new MurderInfo(attemptKiller, attemptTarget, appearanceKiller, appearanceTarget);

        appearanceKiller.ResetKillCooldown();

        // 無効なキルをブロックする処理 必ず最初に実行する
        if (!CheckMurderPatch.CheckForInvalidMurdering(info))
        {
            return false;
        }

        var killerRole = attemptKiller.GetRoleClass();
        var targetRole = attemptTarget.GetRoleClass();

        // キラーがキル能力持ちなら
        if (killerRole is IKiller killer)
        {
            if (killer.IsKiller)//一応今は属性ガード有線にしてますが
            {
                if (targetRole != null)
                    if (Amnesia.CheckAbility(attemptTarget))
                        if (!targetRole.OnCheckMurderAsTarget(info))
                        {
                            CheckMurderPatch.TimeSinceLastKill[attemptKiller.PlayerId] = 0f;//タゲ側でガードされるときってキルガードだけのはずだから。
                            return false;
                        }
                if (AsistingAngel.Guard)
                {
                    if (attemptTarget == AsistingAngel.Asist)
                    {
                        CheckMurderPatch.TimeSinceLastKill[attemptKiller.PlayerId] = 0f;//タゲ側でガードされるときってキルガードだけのはずだから。
                        attemptKiller.SetKillCooldown(target: attemptTarget, delay: true);

                        Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [AsistingAngel]　" + Utils.GetPlayerColor(Main.AllPlayerControls.Where(x => x.Is(CustomRoles.AsistingAngel)).FirstOrDefault())
                        + ":  " + string.Format(Translator.GetString("GuardMaster.Guard"), Utils.GetPlayerColor(attemptKiller, true) + $"(<b>{Utils.GetTrueRoleName(attemptKiller.PlayerId, false)}</b>)");

                        Utils.NotifyRoles();

                        return false;
                    }
                }
            }
            //属性ガードがある場合はCankillのみ先にfalseで返す。
            if (Main.Guard.ContainsKey(attemptTarget.PlayerId))
                if (Main.Guard[attemptTarget.PlayerId] > 0)
                    info.CanKill = false;

            // キラーのキルチェック処理実行
            if (Amnesia.CheckAbility(attemptKiller)) killer.OnCheckMurderAsKiller(info);

            if (Main.Guard.ContainsKey(attemptTarget.PlayerId) && info.DoKill && info.CanKill)
            {
                if (Main.Guard[attemptTarget.PlayerId] > 0)
                {
                    CheckMurderPatch.TimeSinceLastKill[attemptKiller.PlayerId] = 0f;
                    Main.Guard[attemptTarget.PlayerId]--;
                    attemptKiller.SetKillCooldown(target: attemptTarget, delay: true);
                    Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Guard]　" + Utils.GetPlayerColor(attemptTarget) + ":  " + string.Format(Translator.GetString("GuardMaster.Guard"), Utils.GetPlayerColor(attemptKiller, true) + $"(<b>{Utils.GetTrueRoleName(attemptKiller.PlayerId, false)}</b>)");
                    Logger.Info($"{attemptTarget.GetNameWithRole()} : ガード残り{Main.Guard[attemptTarget.PlayerId]}回", "Guarding");
                    Utils.NotifyRoles();
                    return false;
                }
            }
        }

        //キル可能だった場合のみMurderPlayerに進む
        if (info.CanKill && info.DoKill)//ノイメ対応
        {
            if (appearanceTarget.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() == RoleTypes.Noisemaker)
            {
                if (AmongUsClient.Instance.AmHost)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == PlayerControl.LocalPlayer)
                            appearanceTarget.StartCoroutine(appearanceTarget.CoSetRole(RoleTypes.Noisemaker, true));
                        else
                            appearanceTarget.RpcSetRoleDesync(RoleTypes.Noisemaker, pc.GetClientId());
                    }
            }
            if (GhostNoiseSender.Nois.ContainsValue(appearanceTarget.PlayerId))
            {
                if (AmongUsClient.Instance.AmHost)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == PlayerControl.LocalPlayer)
                            appearanceTarget.StartCoroutine(appearanceTarget.CoSetRole(RoleTypes.Noisemaker, true));
                        else
                            appearanceTarget.RpcSetRoleDesync(RoleTypes.Noisemaker, pc.GetClientId());
                        appearanceTarget.SyncSettings();
                    }
            }

            Psychic.CanAbility(appearanceTarget);

            //MurderPlayer用にinfoを保存
            CheckMurderInfos[appearanceKiller.PlayerId] = info;
            appearanceKiller.RpcMurderPlayer(appearanceTarget);
            return true;
        }
        else
        {
            if (!info.CanKill) Logger.Info($"{appearanceTarget.GetNameWithRole()}をキル出来ない。", "CheckMurder");
            if (!info.DoKill) Logger.Info($"{appearanceKiller.GetNameWithRole()}はキルしない。", "CheckMurder");
            return false;
        }
    }
    /// <summary>
    /// MurderPlayer実行後の各役職処理
    /// </summary>
    /// <param name="appearanceKiller">見た目上でキルを行うプレイヤー 可変</param>
    /// <param name="appearanceTarget">見た目上でキルされるプレイヤー 可変</param>
    public static void OnMurderPlayer(PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        //MurderInfoの取得
        if (CheckMurderInfos.TryGetValue(appearanceKiller.PlayerId, out var info))
        {
            //参照出来たら削除
            CheckMurderInfos.Remove(appearanceKiller.PlayerId);
        }
        else
        {
            //CheckMurderを経由していない場合はappearanceで処理
            info = new MurderInfo(appearanceKiller, appearanceTarget, appearanceKiller, appearanceTarget);
        }

        Main.KillCount[appearanceKiller.PlayerId]++;

        (var attemptKiller, var attemptTarget) = info.AttemptTuple;

        Logger.Info($"Real Killer={attemptKiller.GetNameWithRole()}", "MurderPlayer");

        //キラーの処理

        if (Amnesia.CheckAbility(attemptKiller))
            (attemptKiller.GetRoleClass() as IKiller)?.OnMurderPlayerAsKiller(info);

        //ターゲットの処理
        var targetRole = attemptTarget.GetRoleClass();

        if (Amnesia.CheckAbility(attemptKiller))
            if (targetRole != null)
                targetRole.OnMurderPlayerAsTarget(info);

        //その他視点の処理があれば実行
        foreach (var onMurderPlayer in OnMurderPlayerOthers.ToArray())
        {
            onMurderPlayer(info);
        }

        //サブロール処理ができるまではラバーズをここで処理
        FixedUpdatePatch.ALoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.BLoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.CLoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.DLoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.FLoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.ELoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.GLoversSuicide(attemptTarget.PlayerId);
        FixedUpdatePatch.MadonnaLoversSuicide(attemptTarget.PlayerId);

        //以降共通処理
        var targetState = PlayerState.GetByPlayerId(attemptTarget.PlayerId);
        if (targetState.DeathReason == CustomDeathReason.etc)
        {
            //死因が設定されていない場合は死亡判定
            targetState.DeathReason = CustomDeathReason.Kill;
        }
        //あっ!死ぬ前にどこにいたかだけ教えてね!
        var room = "";
        if (Utils.GetPlayerById(appearanceTarget.PlayerId).GetPlainShipRoom() != null)
        {
            room = Translator.GetString($"{Utils.GetPlayerById(appearanceTarget.PlayerId).GetPlainShipRoom().RoomId}");
            room = $"〔{room}〕";
        }
        targetState.SetDead();
        attemptTarget.SetRealKiller(attemptKiller, true);

        GhostRoleAssingData.AssignAddOnsFromList(true);

        Utils.CountAlivePlayers(true);

        Utils.TargetDies(info);

        Utils.SyncAllSettings();
        Utils.NotifyRoles();
        //サブロールは表示めんどいしながいから省略★
        if (PlayerState.GetByPlayerId(appearanceTarget.PlayerId).DeathReason != CustomDeathReason.Guess && !GameStates.Meeting)
        {
            Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Kill]　{Utils.GetPlayerColor(appearanceTarget, true)}(<b>{Utils.GetTrueRoleName(appearanceTarget.PlayerId, false)}</b>) [{Utils.GetVitalText(appearanceTarget.PlayerId, true)}]　{room}";
            if (appearanceKiller != appearanceTarget) Main.gamelog += $"\n\t\t⇐ {Utils.GetPlayerColor(appearanceKiller, true)}(<b>{Utils.GetTrueRoleName(appearanceKiller.PlayerId, false)}</b>)";
        }

        if (Main.KillCount.ContainsKey(appearanceKiller.PlayerId))
            if (appearanceKiller.Is(CustomRoles.Amnesia) && Amnesia.TriggerKill.GetBool())
            {
                if (Amnesia.KillCount.GetInt() <= Main.KillCount[appearanceKiller.PlayerId])
                {
                    Amnesia.Kesu(appearanceKiller.PlayerId);

                    if (appearanceKiller.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                        appearanceKiller.RpcSetRoleDesync(appearanceKiller.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke(), appearanceKiller.GetClientId());
                    else
                    if (appearanceKiller.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        if (PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false && PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke() != RoleTypes.Impostor)
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke());
                        else if (appearanceKiller.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke() == RoleTypes.Shapeshifter)
                        {
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Shapeshifter);
                        }
                    }
                    appearanceKiller.ResetKillCooldown();
                    _ = new LateTask(() =>
                    {
                        appearanceKiller.RpcResetAbilityCooldown(kousin: true);
                        appearanceKiller.SetKillCooldown(delay: true);
                        Utils.NotifyRoles();
                    }, 0.2f, "SetKillCOolDown");
                }
            }
    }
    /// <summary>
    /// その他視点からのMurderPlayer処理
    /// 初期化時にOnMurderPlayerOthers+=で登録
    /// </summary>
    public static HashSet<Action<MurderInfo>> OnMurderPlayerOthers = new();

    public static void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask && !GameStates.Meeting)
        {
            if (Amnesia.CheckAbility(player))
                player.GetRoleClass()?.OnFixedUpdate(player);
            //その他視点処理があれば実行
            foreach (var onFixedUpdate in OnFixedUpdateOthers)
            {
                onFixedUpdate(player);
            }
        }
    }
    /// <summary>
    /// タスクターンに常時呼ばれる関数
    /// 他役職への干渉用
    /// Host以外も呼ばれるので注意
    /// 初期化時にOnFixedUpdateOthers+=で登録
    /// </summary>
    public static HashSet<Action<PlayerControl>> OnFixedUpdateOthers = new();

    public static bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
        bool cancel = false;
        foreach (var roleClass in AllActiveRoles.Values)
        {
            if (!roleClass.OnSabotage(player, systemType))
            {
                cancel = true;
            }
        }
        return !cancel;
    }
    // ==初期化関連処理 ==
    [GameModuleInitializer]
    public static void Initialize()
    {
        AllRolesInfo.Do(kvp => kvp.Value.IsEnable = kvp.Key.IsEnable());
        AllActiveRoles.Clear();
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        OnEnterVentOthers.Clear();
        CheckMurderInfos.Clear();
        OnMurderPlayerOthers.Clear();
        OnFixedUpdateOthers.Clear();
        OnCompleteTaskOthers.Clear();
    }
    public static void CreateInstance()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            CreateInstance(pc.GetCustomRole(), pc);
        }
    }
    public static void CreateInstance(CustomRoles role, PlayerControl player)
    {
        if (AllRolesInfo.TryGetValue(role, out var roleInfo))
        {
            roleInfo.CreateInstance(player).Add();
        }
        else
        {
            OtherRolesAdd(player);
        }
        if (player.Data.Role.Role == RoleTypes.Shapeshifter || player.GetCustomRole().GetRoleInfo()?.BaseRoleType?.Invoke() == RoleTypes.Shapeshifter)
        {
            Main.CheckShapeshift.TryAdd(player.PlayerId, false);
            (player.GetRoleClass() as IUseTheShButton)?.Shape(player);
        }
    }

    public static void OtherRolesAdd(PlayerControl pc)
    {
        foreach (var subRole in pc.GetCustomSubRoles())
        {
            switch (subRole)
            {
                case CustomRoles.watching: watching.Add(pc.PlayerId); break;
                case CustomRoles.Speeding: Speeding.Add(pc.PlayerId); break;
                case CustomRoles.Moon: Moon.Add(pc.PlayerId); break;
                case CustomRoles.Guesser: Guesser.Add(pc.PlayerId); break;
                case CustomRoles.Lighting: Lighting.Add(pc.PlayerId); break;
                case CustomRoles.Tiebreaker: Tiebreaker.Add(pc.PlayerId); break;
                case CustomRoles.Management: Management.Add(pc.PlayerId); break;
                case CustomRoles.Connecting: Connecting.Add(pc.PlayerId); break;
                case CustomRoles.Serial: Serial.Add(pc.PlayerId); break;
                case CustomRoles.PlusVote: PlusVote.Add(pc.PlayerId); break;
                case CustomRoles.Opener: Opener.Add(pc.PlayerId); break;
                case CustomRoles.Revenger: Revenger.Add(pc.PlayerId); break;
                case CustomRoles.seeing: seeing.Add(pc.PlayerId); break;
                case CustomRoles.Guarding: Guarding.Add(pc.PlayerId); break;
                case CustomRoles.Autopsy: Autopsy.Add(pc.PlayerId); break;

                case CustomRoles.SlowStarter: SlowStarter.Add(pc.PlayerId); break;
                case CustomRoles.Notvoter: Notvoter.Add(pc.PlayerId); break;
                case CustomRoles.Transparent: Transparent.Add(pc.PlayerId); break;
                case CustomRoles.NonReport: NonReport.Add(pc.PlayerId); break;
                case CustomRoles.Water: Water.Add(pc.PlayerId); break;
                case CustomRoles.Clumsy: Clumsy.Add(pc.PlayerId); break;
                case CustomRoles.Slacker: Slacker.Add(pc.PlayerId); break;
                case CustomRoles.Elector: Elector.Add(pc.PlayerId); break;
                case CustomRoles.Amnesia: Amnesia.Add(pc.PlayerId); break;

                case CustomRoles.Amanojaku: Amanojaku.Add(pc.PlayerId); break;

                case CustomRoles.Ghostbuttoner: Ghostbuttoner.Add(pc.PlayerId); break;
                case CustomRoles.GhostNoiseSender: GhostNoiseSender.Add(pc.PlayerId); break;
                case CustomRoles.GhostReseter: GhostReseter.Add(pc.PlayerId); break;
                case CustomRoles.DemonicTracker: DemonicTracker.Add(pc.PlayerId); break;
                case CustomRoles.DemonicVenter: DemonicVenter.Add(pc.PlayerId); break;
                case CustomRoles.DemonicCrusher: DemonicCrusher.Add(pc.PlayerId); break;
                case CustomRoles.AsistingAngel: AsistingAngel.Add(pc.PlayerId); break;
            }
        }
    }
    /// <summary>
    /// 受信したRPCから送信先を読み取ってRoleClassに配信する
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="rpcType"></param>
    public static void DispatchRpc(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        GetByPlayerId(playerId)?.ReceiveRPC(reader);
    }
    //NameSystem
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = new();
    //Vent
    public static HashSet<Func<PlayerPhysics, int, bool>> OnEnterVentOthers = new();
    public static HashSet<Action<PlayerControl, bool>> OnCompleteTaskOthers = new();
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するMark
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>結合したMark</returns>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するLowerText
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <param name="isForHud">ModでHudとして表示する場合</param>
    /// <returns>結合したLowerText</returns>
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }
        return sb.ToString();
    }
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するSuffix
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>結合したSuffix</returns>
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// ベントに入ることが確定した後に呼ばれる
    /// </summary>
    public static void OnEnterVent(PlayerPhysics physics, int ventId)
    {
        //bool check = false;
        foreach (var vent in OnEnterVentOthers)
        {
            /*var r = */
            vent(physics, ventId);
            //if (!r) check = false;
        }
        //return check;
    }
    /// <summary>
    /// OnCompleateTask時に呼ばれる
    /// </summary>
    public static void onCompleteTaskOthers(PlayerControl player, bool ret)
    {
        foreach (var cmptsk in OnCompleteTaskOthers)
            cmptsk(player, ret);
    }
    /// <summary>
    /// オブジェクトの破棄
    /// </summary>
    public static void Dispose()
    {
        Logger.Info($"Dispose ActiveRoles", "CustomRoleManager");
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        OnEnterVentOthers.Clear();
        CheckMurderInfos.Clear();
        OnMurderPlayerOthers.Clear();
        OnFixedUpdateOthers.Clear();
        OnCompleteTaskOthers.Clear();

        AllActiveRoles.Values.ToArray().Do(roleClass => roleClass.Dispose());
    }
}
public class MurderInfo
{
    /// <summary>実際にキルを行ったプレイヤー 不変</summary>
    public PlayerControl AttemptKiller { get; }
    /// <summary>Killerが実際にキルを行おうとしたプレイヤー 不変</summary>
    public PlayerControl AttemptTarget { get; }
    /// <summary>見た目上でキルを行うプレイヤー 可変</summary>
    public PlayerControl AppearanceKiller { get; set; }
    /// <summary>見た目上でキルされるプレイヤー 可変</summary>
    public PlayerControl AppearanceTarget { get; set; }

    /// <summary>
    /// targetがキル出来るか
    /// </summary>
    public bool CanKill = true;
    /// <summary>
    /// Killerが実際にキルするか
    /// </summary>
    public bool DoKill = true;
    /// <summary>
    ///転落死など事故の場合(キラー不在)
    /// </summary>
    public bool IsAccident = false;

    // 分解用 (killer, target) = info.AttemptTuple; のような記述でkillerとtargetをまとめて取り出せる
    public (PlayerControl killer, PlayerControl target) AttemptTuple => (AttemptKiller, AttemptTarget);
    public (PlayerControl killer, PlayerControl target) AppearanceTuple => (AppearanceKiller, AppearanceTarget);
    /// <summary>
    /// 本来の自殺
    /// </summary>
    public bool IsSuicide => AttemptKiller.PlayerId == AttemptTarget.PlayerId;
    /// <summary>
    /// 遠距離キル代わりの疑似自殺
    /// </summary>
    public bool IsFakeSuicide => AppearanceKiller.PlayerId == AppearanceTarget.PlayerId;
    public MurderInfo(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearancetarget)
    {
        AttemptKiller = attemptKiller;
        AttemptTarget = attemptTarget;
        AppearanceKiller = appearanceKiller;
        AppearanceTarget = appearancetarget;
    }
}

public enum CustomRoles
{
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    Phantom,
    //Impostor
    BountyHunter,
    FireWorks,
    Mafia,
    SerialKiller,
    ShapeMaster,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Mare,
    Penguin,
    Puppeteer,
    TimeThief,
    EvilTracker,
    Stealth,
    NekoKabocha,
    EvilHacker,
    Insider,
    //TOH-k
    Bomber,
    TeleportKiller,
    AntiReporter,
    Tairou,
    Evilgambler,
    Notifier,
    Magician,
    Decrescendo,
    Alien,
    Limiter,
    ProgressKiller,
    Mole,
    EvilAddoer,
    Reloader,
    //Madmate
    MadGuardian,
    Madmate,
    MadSnitch,
    MadAvenger,
    SKMadmate,
    //TOH-k
    MadJester,
    MadTeller,
    MadBait,
    MadReduced,
    MadWorker,
    MadTracker,
    MadChanger,
    //Crewmate(Vanilla)
    Engineer,
    GuardianAngel,
    Scientist,
    Tracker,
    Noisemaker,
    //Crewmate
    Bait,
    Lighter,
    Mayor,
    SabotageMaster,
    Sheriff,
    Snitch,
    SpeedBooster,
    Trapper,
    Dictator,
    Doctor,
    Seer,
    TimeManager,
    //TOH-K
    VentMaster,
    ToiletFan,
    Bakery,
    FortuneTeller,
    TaskStar,
    PonkotuTeller,
    UltraStar,
    MeetingSheriff,
    GuardMaster,
    Shyboy,
    Balancer,
    ShrineMaiden,
    Comebacker,
    WhiteHacker,
    WolfBoy,
    NiceAddoer,
    InSender,
    Staff,
    Efficient,
    Psychic,
    SwitchSheriff,
    NiceLogger,
    Android,
    //Neutral
    Arsonist,
    Egoist,
    Jester,
    Opportunist,
    PlagueDoctor,
    SchrodingerCat,
    Terrorist,
    Executioner,
    Jackal,
    //TOHk
    Remotekiller,
    Chef,
    JackalMafia,
    CountKiller,
    GrimReaper,
    Madonna,
    Jackaldoll,
    Workaholic,
    Monochromer,
    DoppelGanger,
    MassMedia,
    Banker,
    BakeCat,
    TaskPlayerB,
    //HideAndSeek
    HASFox,
    HASTroll,
    //GM
    GM,
    //Combination
    Driver,
    Braid,
    // Sub-roll after 500
    NotAssigned = 500,
    LastImpostor,
    LastNeutral,
    Workhorse,
    //第三属性
    ALovers, BLovers, CLovers, DLovers, ELovers, FLovers, GLovers,
    MaLovers, Amanojaku,

    //バフ
    Guesser,
    Serial,
    Connecting,
    watching,
    PlusVote,
    Tiebreaker,
    Autopsy,
    Revenger,
    Speeding,
    Management,
    Opener,
    seeing,
    Lighting,
    Moon,
    Guarding,
    //デバフ
    Amnesia,
    Notvoter,
    Elector,
    NonReport,
    Transparent,
    Water,
    Clumsy,
    Slacker,
    SlowStarter,
    InfoPoor,

    //GhostRoles

    //MadmateGhost

    DemonicCrusher,
    DemonicTracker,
    DemonicVenter,
    //CrewMateGhost
    Ghostbuttoner,
    GhostNoiseSender,
    GhostReseter,
    //NeutralGhost
    AsistingAngel,
}
public enum CustomRoleTypes
{
    Crewmate,
    Impostor,
    Neutral,
    Madmate
}
public enum HasTask
{
    True,
    False,
    ForRecompute
}
