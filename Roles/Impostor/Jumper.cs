using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Jumper : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jumper),
            player => new Jumper(player),
            CustomRoles.Jumper,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            3000,
            SetupOptionItem,
            "Jm"
        );
    public Jumper(PlayerControl player)
    : base(
        RoleInfo,
        player
        )
    {
        position = new Vector2(999f, 999f);
        nowposition = new Vector2(999f, 999f);
        timer = 0;
        x = 0;
        y = 0;
        count = 0;
        ability = false;
        aname = false;
        speed = Main.AllPlayerSpeed[Player.PlayerId];
        PlayerColor = player.Data.DefaultOutfit.ColorId;
        jampdis = Jampdis.GetFloat();
        jampcount = Jampcount.GetInt();
        onecooltime = Onecooltime.GetFloat();
        jampcooltime = Jampcooltime.GetFloat();
        killcool = OptionKillCoolDown.GetFloat();
        jampdistance = JampDistance.GetInt();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem Jampcount;
    static OptionItem Onecooltime;
    static OptionItem Jampcooltime;
    static OptionItem Jampdis;
    static OptionItem JampDistance;
    Vector2 position;
    Vector2 nowposition;
    static float killcool;
    static float onecooltime;
    static float jampcooltime;
    static float jampdis;
    static int jampcount;
    static int jampdistance;
    int PlayerColor;
    float x;
    float y;
    float addx;
    float addy;
    float timer;
    float speed;
    int count;
    public bool ability;
    bool aname;
    enum Op
    {
        JumperOneCoolTime, JumperCCoolTime, JumperJampcount, JumperJampDis, JumperDistance
    }

    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        Jampcount = FloatOptionItem.Create(RoleInfo, 11, Op.JumperJampcount, new(1f, 30f, 1f), 4f, false);
        JampDistance = IntegerOptionItem.Create(RoleInfo, 15, Op.JumperDistance, new(1, 3, 1), 1, false);
        Onecooltime = FloatOptionItem.Create(RoleInfo, 12, Op.JumperOneCoolTime, new(0f, 180f, 0.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
        Jampcooltime = FloatOptionItem.Create(RoleInfo, 13, Op.JumperCCoolTime, new(0f, 180f, 0.5f), 25f, false).SetValueFormat(OptionFormat.Seconds);
        Jampdis = FloatOptionItem.Create(RoleInfo, 14, Op.JumperJampDis, new(0.2f, 3, 0.1f), 1.5f, false).SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = position == new Vector2(999f, 999f) ? onecooltime : jampcooltime;
    }
    public float CalculateKillCooldown() => killcool;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => !ability;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!ability) return;

        timer += Time.fixedDeltaTime;

        if (timer > jampdis)
        {
            if (count == 1) aname = true;
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            player.RpcSnapToForced(count == jampcount ? position : new Vector2(nowposition.x + addx * count, nowposition.y + addy * count));
            _ = new LateTask(() =>
            {
                if (!GameStates.IsMeeting && player.IsAlive())
                {
                    foreach (var target in PlayerCatch.AllAlivePlayerControls)
                    {
                        if (target.Is(CustomRoles.King) || target.PlayerId == player.PlayerId) continue;

                        float Distance = Vector2.Distance(player.transform.position, target.transform.position);
                        if (Distance <= (jampdistance == 1 ? 1.22f : (jampdistance == 2 ? 1.82f : 2.2)))
                        {
                            PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bombed;
                            target.SetRealKiller(player);
                            CustomRoleManager.OnCheckMurder(
                                player, target,
                                target, target, true
                            );
                        }
                    }
                }
            }, jampdis - 0.19f, "abo-n", null);
            if (jampcount <= count)
            {
                ability = false;
                aname = false;
                x = 0;
                y = 0;
                position = new Vector2(999f, 999f);
                nowposition = new Vector2(999f, 999f);
                Main.AllPlayerSpeed[Player.PlayerId] = speed;
                _ = new LateTask(() => player.RpcResetAbilityCooldown(kousin: true), 0.2f, "Jampowari", null);
                player.SetKillCooldown();
                Player.RpcSetColor((byte)PlayerColor);
                _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true), 0.2f, "jampnamemoosu", null);
            }
            count++;
            timer = 0;
        }
    }
    public override void OnStartMeeting()
    {
        timer = 0;
        ability = false;
        aname = false;
        x = 0;
        y = 0;
        position = new Vector2(999f, 999f);
        nowposition = new Vector2(999f, 999f);
        Main.AllPlayerSpeed[Player.PlayerId] = speed;
        _ = new LateTask(() => Player.RpcResetAbilityCooldown(kousin: true), 0.2f, "Jampokyouseiowari");
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __) => Player.RpcSetColor((byte)PlayerColor);
    public bool CanUseKillButton() => !ability;
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        if (ability) return;
        fall = false;
        if (position == new Vector2(999f, 999f))
        {
            position = Player.transform.position;
            resetkillcooldown = false;
            Player.RpcSpecificRejectShapeshift(Player, false);
            Player.RpcResetAbilityCooldown(kousin: true);
            Logger.Info($"Set:{position.x}-{position.y} (${Player.PlayerId})", "Jumper");
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.2f, "Jumperset", true);
            return;
        }
        timer = 0;
        count = 1;
        resetkillcooldown = true;
        ability = true;
        nowposition = Player.transform.position;
        x = position.x - nowposition.x;
        y = position.y - nowposition.y;
        addx = x / Jampcount.GetInt();
        addy = y / Jampcount.GetInt();
        Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
        Logger.Info($"{Player?.Data?.GetLogPlayerName()}Jump!", "Jumper");
        _ = new LateTask(() =>
        {
            Player.RpcSetPet("");
            Player.SyncSettings();
            int chance = IRandom.Instance.Next(0, 18);
            PlayerCatch.AllPlayerControls.Do(pc => Player.RpcChColor(pc, (byte)chance));
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        }, 0.4f, "Jumper0Speed", true);

    }
    public override bool GetTemporaryName(ref string name, ref bool NoMarker, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (Player == seen && aname && !GameStates.Meeting)
        {
            name = jampdistance == 1 ? "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n<size=1200%><color=#ff1919>●</color></size></line-height>"
            : (jampdistance == 2 ? "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n<size=2100%><color=#ff1919>●</color></size></line-height>"
                : "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n<size=2800%><color=#ff1919>●</color></size></line-height>"
            );
            NoMarker = true;
            return true;
        }
        return false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return "";
        if (Player.IsAlive())
            return position == new Vector2(999f, 999f) ? GetString("Jumper_setti") : GetString("Jumper_Jamp");
        return "";
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Jumper_Ability";
        return true;
    }
    public override string GetAbilityButtonText()
    {
        return GetString("Jumpertext");
    }
}