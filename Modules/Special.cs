using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using UnityEngine;
using TownOfHost.Attributes;

namespace TownOfHost;
static class Event
{
    public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
    public static bool White = DateTime.Now.Month == 3 && DateTime.Now.Day is 14;
    public static bool IsInitialRelease = DateTime.Now.Month == 10 && DateTime.Now.Day is 31;
    public static bool IsHalloween = (DateTime.Now.Month == 10 && DateTime.Now.Day is 31) || (DateTime.Now.Month == 11 && DateTime.Now.Day is 1 or 2 or 3 or 4 or 5 or 6 or 7);
    public static bool GoldenWeek = DateTime.Now.Month == 5 && DateTime.Now.Day is 3 or 4 or 5;
    public static bool April = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public static bool IsEventDay => IsChristmas || White || IsInitialRelease || IsHalloween || GoldenWeek || April;
    public static bool Special = false;
    public static List<string> OptionLoad = new();
    public static bool IsE(this CustomRoles role) => role is CustomRoles.SpeedStar or CustomRoles.EvilTeller;
}


























//やぁ。気付いちゃった...?( ᐛ )
//ファイル作っちゃうとばれちゃうからね。
//ｶ ｸ ｼ ﾃ ﾙ ﾅ ﾗ ﾏ ｧ ｺ ｺ ｼﾞ ｬ ﾝ ?
public sealed class SpeedStar : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpeedStar),
            player => new SpeedStar(player),
            CustomRoles.SpeedStar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            6400,
            SetUpOptionItem,
            "SS",
            from: From.Speyrp
        );
    public SpeedStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        allplayerspeed.Clear();
        speed = optSpeed.GetFloat();
        abilitytime = optabilitytime.GetFloat();
        cooldown = optcooldown.GetFloat();
        killcooldown = optkillcooldown.GetFloat();
    }
    static OptionItem optabilitytime;
    static OptionItem optcooldown;
    static OptionItem optkillcooldown;
    static OptionItem optSpeed;
    static float speed;
    static float abilitytime;
    static float cooldown;
    static float killcooldown;
    Dictionary<byte, float> allplayerspeed = new();
    public override void StartGameTasks()
    {
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            allplayerspeed.TryAdd(pc.PlayerId, Main.AllPlayerSpeed[pc.PlayerId]);
        }
    }
    static void SetUpOptionItem()
    {
        optkillcooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optcooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optabilitytime = FloatOptionItem.Create(RoleInfo, 12, "GhostNoiseSenderTime", new(1, 300, 1f), 10f, false).SetValueFormat(OptionFormat.Seconds);
        optSpeed = FloatOptionItem.Create(RoleInfo, 13, "SpeedStarSpeed", new(0, 10, 0.05f), 3f, false).SetValueFormat(OptionFormat.Multiplier);
    }
    public float CalculateKillCooldown() => killcooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = cooldown;
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        fall = false;
        resetkillcooldown = false;
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            Main.AllPlayerSpeed[pc.PlayerId] = speed;
        }
        UtilsOption.MarkEveryoneDirtySettings();
        _ = new LateTask(() =>
        {
            if (GameStates.InGame)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    Main.AllPlayerSpeed[pc.PlayerId] = allplayerspeed[pc.PlayerId];
                }
                _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 0.2f, "", true);
                Player.RpcResetAbilityCooldown();
            }
        }, abilitytime, "", true);
    }
    public override void OnStartMeeting()
    {
        if (GameStates.InGame)
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                Main.AllPlayerSpeed[pc.PlayerId] = allplayerspeed[pc.PlayerId];
            }
            _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 0.2f, "", true);
        }
    }
}
public sealed class EvilTeller : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilTeller),
            player => new EvilTeller(player),
            CustomRoles.EvilTeller,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            6500,
            SetUpOptionItem,
            "Et",
            from: From.Speyrp
        );
    public EvilTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Tellnow.Clear();
        seentarget.Clear();
        nowuse = false;
        fall = false;
        usekillcool = optusekillcoool.GetBool();
        cooldown = optcooldown.GetFloat();
        killcooldown = optkillcooldown.GetFloat();
        telltime = opttelltime.GetFloat();
        distance = optDistance.GetFloat();
        tellroleteam = opttellroleteam.GetBool();
        tellrole = opttellrole.GetBool();
    }
    static OptionItem optcooldown;
    static OptionItem optkillcooldown;
    static OptionItem opttelltime;
    static OptionItem optDistance;
    static OptionItem opttellroleteam;
    static OptionItem opttellrole;
    static OptionItem optusekillcoool;
    static float cooldown;
    static float killcooldown;
    static float telltime;
    static float distance;
    static bool tellroleteam;
    static bool tellrole;
    static bool usekillcool;
    static Dictionary<byte, float> Tellnow = new();
    bool nowuse;
    bool fall;
    static Dictionary<byte, CustomRoles> seentarget = new();
    enum OptionName { EvilTellerTellTime, EvilTellerDistance, EvilTellertellrole }
    static void SetUpOptionItem()
    {
        optkillcooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optcooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        opttelltime = FloatOptionItem.Create(RoleInfo, 12, OptionName.EvilTellerTellTime, new(0, 100, 0.5f), 5, false).SetValueFormat(OptionFormat.Seconds);
        optDistance = FloatOptionItem.Create(RoleInfo, 13, OptionName.EvilTellerDistance, new(1f, 30f, 0.25f), 1.75f, false);
        opttellroleteam = BooleanOptionItem.Create(RoleInfo, 14, "tRole", false, false);
        opttellrole = BooleanOptionItem.Create(RoleInfo, 15, OptionName.EvilTellertellrole, false, false);
        optusekillcoool = BooleanOptionItem.Create(RoleInfo, 16, "OptionSetKillcooldown", false, false);
    }
    public float CalculateKillCooldown() => killcooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = fall ? 1 : (nowuse ? telltime : cooldown);
    public void OnClick(ref bool resetkillcooldown, ref bool falla)
    {
        resetkillcooldown = true;
        falla = true;
        var target = Player.GetKillTarget();
        if (target == null) { fall = true; return; }
        if (target.Is(CustomRoleTypes.Impostor)) { fall = true; return; }

        if (seentarget.ContainsKey(target.PlayerId)) { fall = true; return; }
        Tellnow.TryAdd(target.PlayerId, 0);
        nowuse = true;
        fall = false;
        _ = new LateTask(() =>
        {
            Player.RpcResetAbilityCooldown(kousin: true);
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }, 0.2f, "", true);
    }
    [PluginModuleInitializer]
    public static void Load()
    {
        Event.OptionLoad.Add("SpeedStar");
        Event.OptionLoad.Add("EvilTeller");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!seer.IsAlive()) return "";

        if (Tellnow.ContainsKey(seen.PlayerId)) return "<color=#ff1919>◆</color>";
        return "";
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) { fall = false; Tellnow.Clear(); nowuse = false; }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (!seen) return;
        if (!Player.IsAlive()) return;

        if (seentarget.TryGetValue(seen.PlayerId, out var role))
        {
            enabled = true;
            addon = false;
            if (tellrole) role = seen.GetCustomRole();
            if (!tellroleteam)
            {
                switch (seen.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                    case CustomRoleTypes.Madmate:
                        roleColor = Palette.CrewmateBlue;
                        roleText = GetString("Crewmate");
                        break;
                    case CustomRoleTypes.Impostor:
                        roleColor = ModColors.ImpostorRed;
                        roleText = GetString("Impostor");
                        break;
                    case CustomRoleTypes.Neutral:
                        roleColor = ModColors.NeutralGray;
                        roleText = GetString("Neutral");
                        break;
                }
            }
            roleText = GetString($"{role}");
            roleColor = UtilsRoleText.GetRoleColor(role);
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask)
        {
            if (Tellnow.Count == 0) return;
            List<byte> del = new();
            foreach (var data in Tellnow)
            {
                var target = PlayerCatch.GetPlayerById(data.Key);
                if (!target)
                {
                    del.Add(target.PlayerId);
                    fall = true;
                    continue;
                }
                if (!target.IsAlive())
                {
                    del.Add(target.PlayerId);
                    fall = true;
                    continue;
                }
                if (telltime <= data.Value)//超えたなら消して追加
                {
                    fall = false;
                    seentarget.TryAdd(target.PlayerId, target.GetCustomRole());
                    del.Add(target.PlayerId);
                    continue;
                }

                float dis;
                dis = Vector2.Distance(Player.transform.position, target.transform.position);//距離を出す
                if (dis <= distance)//一定の距離にターゲットがいるならば時間をカウント
                    Tellnow[data.Key] += Time.fixedDeltaTime;
                else//それ以外は削除
                { del.Add(target.PlayerId); fall = true; }
            }
            if (del.Count != 0)
            {
                nowuse = false;
                del.ForEach(task => Tellnow.Remove(task));
                _ = new LateTask(() =>
                {
                    Player.RpcResetAbilityCooldown(kousin: true);
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                    if (usekillcool && !fall) Player.SetKillCooldown();
                }, 0.2f, "", true);
            }
        }
    }
}