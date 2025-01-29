using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Eraser : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Eraser),
            player => new Eraser(player),
            CustomRoles.Eraser,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            6900,
            SetupOptionItem,
            "Er"
        );
    public Eraser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        AbilityCoolDown = OptionAbilityCoolDown.GetFloat();
        MaxUseCount = OptionUseCount.GetInt();
        CanDelCrew = OptionCanDelCrewmate.GetBool();
        CanDelSheriffRole = OptionCanDelSheriffRole.GetBool();
        CanDelMad = OptionCanDelMadmate.GetBool();
        CanDelNeu = OptionCanDelNeutral.GetBool();
        DeltimingAfterMeeting = OptionDeltimingAfterMeeting.GetBool();

        UseCount = 0;
        ErasePlayers.Clear();
        Eraseds.Clear();
        MEraseds.Clear();
    }
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionAbilityCoolDown; static float AbilityCoolDown;
    static OptionItem OptionUseCount; static int MaxUseCount;
    static OptionItem OptionCanDelCrewmate; static bool CanDelCrew;
    static OptionItem OptionCanDelSheriffRole; static bool CanDelSheriffRole;
    static OptionItem OptionCanDelMadmate; static bool CanDelMad;
    static OptionItem OptionCanDelNeutral; static bool CanDelNeu;

    static OptionItem OptionDeltimingAfterMeeting; static bool DeltimingAfterMeeting;

    int UseCount;//使用回数
    List<byte> ErasePlayers = new();//消す用。
    List<byte> Eraseds = new();//消す予定の人のマーク
    List<byte> MEraseds = new();//消したと思ってる人のマーク

    static List<CustomRoles> IsSheriffRole =
    [CustomRoles.Sheriff, CustomRoles.SwitchSheriff, CustomRoles.MeetingSheriff, CustomRoles.WolfBoy];

    enum OptionName
    {
        EraserDelTimingAfterMeeting,
        EraserCanDelCrewmate,
        EraserCanDelSheriffRole,
        EraserCanDelMadmate,
        EraserCanDelNeutral
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionUseCount = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.OptionCount, new(1, 20, 1), 2, false).SetValueFormat(OptionFormat.Times);
        OptionDeltimingAfterMeeting = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EraserDelTimingAfterMeeting, false, false);
        OptionCanDelCrewmate = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EraserCanDelCrewmate, true, false);
        OptionCanDelSheriffRole = BooleanOptionItem.Create(RoleInfo, 15, OptionName.EraserCanDelSheriffRole, true, false);
        OptionCanDelMadmate = BooleanOptionItem.Create(RoleInfo, 16, OptionName.EraserCanDelMadmate, false, false);
        OptionCanDelNeutral = BooleanOptionItem.Create(RoleInfo, 17, OptionName.EraserCanDelNeutral, false, false);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = AbilityCoolDown;
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        resetkillcooldown = false;
        fall = true;

        var target = Player.GetKillTarget(true);
        if (!target.IsAlive()) return;
        if (Eraseds.Contains(target.PlayerId)) return;
        if (MaxUseCount <= UseCount) return;

        resetkillcooldown = true;
        fall = false;
        UseCount++;

        Eraseds.Add(target.PlayerId);//マークつける用
        ErasePlayers.Add(target.PlayerId);//消す予定の人

        _ = new LateTask(() =>
            Player.SetKillCooldown(target: target, kousin: true), Main.LagTime, "EraserNoyatu", true);

        if (!DeltimingAfterMeeting) ErasePlayer();

        UtilsNotifyRoles.NotifyRoles(Player);
    }
    void ErasePlayer()
    {
        if (!Player.IsAlive()) return;
        foreach (var player in PlayerCatch.AllPlayerControls.Where(x => ErasePlayers.Contains(x.PlayerId)))
        {
            if (player == null) continue;
            Logger.Info($"{Player?.Data?.PlayerName} => {player?.Data?.PlayerName}を消そうとしてる！", "Eraser");

            var role = player.GetCustomRole();
            //インポスターならキャンセル
            if (role.IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode) continue;

            //消したと思ってるリスト
            MEraseds.Add(player.PlayerId);
            //クルーでクルー消せないなら
            if (role.IsCrewmate() && !CanDelCrew) continue;
            //シェリフ系役職で消せないなら
            if (IsSheriffRole.Contains(role) && !CanDelSheriffRole) continue;
            //マッドメイト系でマッドメイト消せないなら
            if (role.IsMadmate() && !CanDelMad) continue;
            //ニュートラルでニュートラル消せないなら
            if (role.IsNeutral() && !CanDelNeu) continue;

            UtilsGameLog.AddGameLog("Eraser", string.Format(GetString("EraserMeg"), Utils.GetPlayerColor(Player), Utils.GetPlayerColor(player)));
            Logger.Info($"{Player?.Data?.PlayerName} => {player?.Data?.PlayerName}をのロールをクルーに", "Eraser");

            player.RpcSetCustomRole(SuddenDeathMode.NowSuddenDeathMode ? CustomRoles.Impostor : CustomRoles.Crewmate, true, null);
        }
        ErasePlayers.Clear();
    }
    public override void AfterMeetingTasks()
    {
        if (DeltimingAfterMeeting) ErasePlayer();
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color=#{(MaxUseCount <= UseCount ? "758593" : "ff1919")}>({UseCount}/{MaxUseCount})</color>";
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        //消された人
        if (MEraseds.Contains(seen.PlayerId)) return "<color=#ff1919>□</color>";

        //死んでたら消す予定の人の処理をしない
        if (!Player.IsAlive()) return "";

        //消す予定の人
        if (Eraseds.Contains(seen.PlayerId)) return "<color=#ff1919>■</color>";
        return "";
    }
    public override string GetAbilityButtonText() => GetString("EraserAbility");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || MaxUseCount <= UseCount || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonKilltargetLowertext");
        return $"<size=50%>{GetString("PhantomButtonKilltargetLowertext")}</size>";
    }
}