using AmongUs.GameOptions;
using UnityEngine;
using System;
using TownOfHost.Roles.Core;
namespace TownOfHost.Roles.Crewmate;
public sealed class Shyboy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shyboy),
            player => new Shyboy(player),
            CustomRoles.Shyboy,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            23000,
            SetupOptionItem,
            "Sy",
            "#00fa9a",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Shyboy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        tuuti = true;
        Shytime = OptionShytime.GetFloat();
        Notshy = OptionNotShy.GetFloat();
        Shydeath = 0;
        AfterMeeting = 0;
    }
    private static OptionItem OptionShytime;
    private static OptionItem OptionNotShy;
    float Shydeath;
    float Cool;
    float AfterMeeting;
    bool tuuti;
    float Last;
    private static float Notshy;
    float Shydeathdi;
    enum OptionName
    {
        ShyboyShytime,
        ShyboyAfterMeetingNotShytime
    }
    private static float Shytime;
    public override bool CanClickUseVentButton => false;
    private static void SetupOptionItem()
    {
        OptionShytime = FloatOptionItem.Create(RoleInfo, 10, OptionName.ShyboyShytime, new(0f, 15f, 0.5f), 5f, false);
        OptionNotShy = FloatOptionItem.Create(RoleInfo, 11, OptionName.ShyboyAfterMeetingNotShytime, new(0f, 30f, 1f), 10f, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        //ししゃごにゅー
        double Coold = Math.Round(Shytime + 1 / 4 - Shydeath);
        AURoleOptions.EngineerCooldown = (float)Coold;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override void StartGameTasks() => Shydeathdi = Player.Is(CustomRoles.Lighting) ? 5 * Main.DefaultImpostorVision : 5 * Main.DefaultCrewmateVision;
    public override void OnStartMeeting() => StartGameTasks();
    public override bool AllEnabledColor => true;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => false;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.Meeting || GameStates.Tuihou) return;
        if (!Player.IsAlive()) return;
        Cool += Time.fixedDeltaTime;
        if (0.25 < Cool)
        {
            Cool = 0;
            //シャイのクールｳﾙｾｪからログださない()()バグ起こったらここtrueか削除して探そう!!((((
            //30回に1回だけとかlog残すか考えたけど余計重くなりそう。
            var cooldown = (float)Math.Round(Shytime + 1 / 4 - Shydeath);
            if (Last != cooldown) //必要な時だけ送る
            {
                Last = cooldown;
                Player.MarkDirtySettings();
            }
            Player.RpcResetAbilityCooldown(log: false);
        }
        AfterMeeting += Time.fixedDeltaTime;

        if (GameStates.IsInTask && Notshy <= AfterMeeting - 5)
        {
            if (tuuti)
            {
                tuuti = false;
                Player.RpcProtectedMurderPlayer();
            }

            Vector2 GSpos = player.transform.position;
            bool Hito = false;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc != player)
                {
                    float HitoDistance = Vector2.Distance(GSpos, pc.transform.position);
                    if (HitoDistance <= Shydeathdi)
                    {
                        Hito = true;
                        break;
                    }
                }
            }
            if (Hito)//周囲に人がいる状況
            {
                Shydeath += Time.fixedDeltaTime;
            }
            else
            {
                Shydeath -= Time.fixedDeltaTime * 1 / 4;//周囲に人がいないとカウントをちょっとずつ減らす
            }

            if (Shydeath <= -0.25f)//値がマイナスにならないようにする
            {
                Shydeath = 0;
            }

            if (Shytime <= Shydeath)
            {
                Logger.Info("もぉみんなかまうからシャイ君しんぢゃったぁ～!", "Shyboy");
                MyState.DeathReason = CustomDeathReason.Suicide;
                Player.RpcMurderPlayer(Player);//一定時間周囲に人がいたら恥ずかしくて死ぬ。
                Shydeath = 0;//0sの無限キル防止(おきないだろうけど)
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        tuuti = true;
        Shydeath = 0;//会議明け修正
        AfterMeeting = 0;
    }

    public override string GetAbilityButtonText() => GetString("ShyBoyText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "ShyBoy_Ability";
        return true;
    }
    //いつか可視化を顔文字でしたい!!!
}