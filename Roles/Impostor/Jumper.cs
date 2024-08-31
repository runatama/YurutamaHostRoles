using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;
public sealed class Jumper : RoleBase, IImpostor, IUseTheShButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jumper),
            player => new Jumper(player),
            CustomRoles.Jumper,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            36000,
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
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem Jampcount;
    static OptionItem Onecooltime;
    static OptionItem Jampcooltime;
    Vector2 position;
    Vector2 nowposition;
    int PlayerColor;
    float x;
    float y;
    float addx;
    float addy;
    float timer;
    float speed;
    int count;
    bool ability;
    bool aname;
    enum Op
    {
        JamperOneCoolTime, JamperCCoolTime, JamperJampcount
    }

    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        Jampcount = FloatOptionItem.Create(RoleInfo, 11, Op.JamperJampcount, new(1f, 10f, 1f), 4f, false);
        Onecooltime = FloatOptionItem.Create(RoleInfo, 12, Op.JamperOneCoolTime, new(0f, 180f, 2.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
        Jampcooltime = FloatOptionItem.Create(RoleInfo, 13, Op.JamperCCoolTime, new(0f, 10f, 2.5f), 25f, false).SetValueFormat(OptionFormat.Seconds);

    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = position == new Vector2(999f, 999f) ? Onecooltime.GetFloat() : Jampcooltime.GetFloat();
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public override bool CanDesyncShapeshift => true;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => !ability;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!ability) return;

        timer += Time.fixedDeltaTime;

        if (timer > 1.5f)
        {
            if (count == 1) aname = true;
            int chance = IRandom.Instance.Next(0, 18);
            Main.AllPlayerControls.Do(pc => Player.RpcChColor(pc, (byte)chance));
            Utils.NotifyRoles();
            player.RpcSnapToForced(new Vector2(nowposition.x + addx * count, nowposition.y + addy * count));
            player.RpcProtectedMurderPlayer();
            _ = new LateTask(() =>
            {
                if (!GameStates.Meeting && player.IsAlive())
                {
                    foreach (var target in Main.AllAlivePlayerControls)
                    {
                        if (target.Is(CustomRoles.King)) continue;
                        if (target != player)
                        {
                            float Distance = Vector2.Distance(player.transform.position, target.transform.position);
                            if (Distance <= 1.22f)
                            {
                                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                target.SetRealKiller(player);
                                CustomRoleManager.OnCheckMurder(
                                    player, target,
                                    target, target, true
                                );
                                player.KillFlash();
                            }
                        }
                    }
                }
                if (!ability)
                {
                    Player.RpcSetColor((byte)PlayerColor);
                    aname = false;
                    _ = new LateTask(() => Utils.NotifyRoles(), 0.2f, "jampnamemoosu");
                }
            }, 1.3f, "abo-n");
            if (count == Jampcount.GetInt())
            {
                ability = false;
                x = 0;
                y = 0;
                position = new Vector2(999f, 999f);
                nowposition = new Vector2(999f, 999f);
                Main.AllPlayerSpeed[Player.PlayerId] = speed;
                _ = new LateTask(() => player.RpcResetAbilityCooldown(kousin: true), 0.2f, "Jampowari");
                Utils.NotifyRoles();
                player.SetKillCooldown();
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
    public void OnClick()
    {
        if (ability) return;
        if (position == new Vector2(999f, 999f))
        {
            position = Player.transform.position;
            Player.RpcResetAbilityCooldown(kousin: true);
            _ = new LateTask(() => Utils.NotifyRoles(), 0.2f, "Jamperset");
            return;
        }
        timer = 0;
        count = 1;
        ability = true;
        nowposition = Player.transform.position;
        x = position.x - nowposition.x;
        y = position.y - nowposition.y;
        addx = x / Jampcount.GetInt();
        addy = y / Jampcount.GetInt();
        Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
        _ = new LateTask(() =>
        {
            Player.SyncSettings();
            Utils.NotifyRoles();
        }, 0.4f, "Jamper0Speed");

    }
    public override bool GetTemporaryName(ref string name, ref bool NoMarker, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (Player == seen && aname && !GameStates.Meeting)
        {
            name = "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n<size=1200%><color=#ff1919>●</color></size></line-height>";
            NoMarker = true;
            return true;
        }
        return false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return "";
        if (Player.IsAlive())
            return position == new Vector2(999f, 999f) ? Translator.GetString("Jumper_setti") : Translator.GetString("Jumper_Jamp");
        return "";
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Jamper_Ability";
        return true;
    }
    public override string GetAbilityButtonText()
    {
        return Translator.GetString("Jumper_text");
    }
}