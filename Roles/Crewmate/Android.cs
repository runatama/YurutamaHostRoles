using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Android : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Android),
            player => new Android(player),
            CustomRoles.Android,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            26100,
            SetupOptionItem,
            "And",
            "#8a99b7",
            false
        );
    public Android(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        time = 0;
        Battery = 0;
        NowVent = 999;
        bat = zero;
    }
    static OptionItem TaskAddBattery;
    static OptionItem CoolTime;
    static OptionItem InVentTime;
    static OptionItem RemoveBattery;
    static OptionItem Remove;//減る値
    static OptionItem RemoveTime;//減る時間
    string bat;
    float Battery;
    float time;
    int NowVent;
    enum OptionName
    {
        AndroidRemoveBattery, AndroidRemove, AndroidRemoveTime, AndroidAddTaskBattery
    }

    private static void SetupOptionItem()
    {
        CoolTime = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false).SetValueFormat(OptionFormat.Seconds);
        InVentTime = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.EngineerInVentMaxTime, new(1f, 60f, 0.5f), 7.5f, false).SetValueFormat(OptionFormat.Seconds);
        TaskAddBattery = FloatOptionItem.Create(RoleInfo, 12, OptionName.AndroidAddTaskBattery, new(1f, 100f, 1f), 10f, false, RemoveBattery).SetValueFormat(OptionFormat.Percent);
        RemoveBattery = BooleanOptionItem.Create(RoleInfo, 13, OptionName.AndroidRemoveBattery, true, false);
        Remove = FloatOptionItem.Create(RoleInfo, 14, OptionName.AndroidRemove, new(1f, 100f, 0.1f), 7.5f, false, RemoveBattery).SetValueFormat(OptionFormat.Percent);
        RemoveTime = FloatOptionItem.Create(RoleInfo, 15, OptionName.AndroidRemoveTime, new(1f, 180f, 0.5f), 4.0f, false, RemoveBattery).SetValueFormat(OptionFormat.Seconds);
        Options.OverrideTasksData.Create(RoleInfo, 16);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        var InMax = Battery * InVentTime.GetFloat();
        if (InMax <= 1f) InMax = 1f;

        AURoleOptions.EngineerCooldown = Battery == 0 ? 200f : ((CoolTime.GetFloat() * 3) - (Battery * CoolTime.GetFloat() * 2));
        AURoleOptions.EngineerInVentMaxTime = Battery == 0 ? 1f : InMax;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        var lastbatt = Battery;

        Battery += TaskAddBattery.GetFloat() * 0.01f;

        //0なら更新入れる
        if (lastbatt <= 0)
            Player.RpcResetAbilityCooldown(kousin: true);

        bat = Now();
        Player.MarkDirtySettings();
        return true;
    }

    //サボタージュ来たら通信障害起きるんでベントから強制排出
    public override bool OnSabotage(PlayerControl __, SystemTypes systemType)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return true;

        if (Player.inVent && NowVent != 999)
            Player.MyPhysics.RpcExitVent(NowVent);
        return true;
    }
    public override void AfterSabotage(SystemTypes systemType) => Player.RpcResetAbilityCooldown(kousin: true);
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => !Main.NowSabotage;//サボタージュ中なら入れないよっ!

    public override void OnFixedUpdate(PlayerControl player)
    {
        //ホストじゃないなら
        if (!AmongUsClient.Instance.AmHost) return;
        //もう充電がパンパンなら
        if (Battery > 1)
        {
            Battery = 1;
            return;
        }

        //もうすでに充電切れなら
        if (Battery <= 0) return;
        //減らさないなら
        if (!RemoveBattery.GetBool()) return;
        //タスクターンじゃないなら
        if (GameStates.Intro || GameStates.Meeting) return;

        time += Time.fixedDeltaTime;

        if (time >= RemoveTime.GetFloat())
        {
            Battery -= Remove.GetFloat() * 0.01f;//1/100にして小数に対応させる
            time = 0;
            if (Battery < 0) Battery = 0;

            if (Battery <= 0)//追い出す
            {
                if (Player.inVent && NowVent != 999)
                    Player.MyPhysics.RpcExitVent(NowVent);
            }
            if (Now() != bat)
            {
                Utils.NotifyRoles(SpecifySeer: Player);
                bat = Now();
                Player.MarkDirtySettings();
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen)
            return "Now: <u>" + Now() + "</u>";

        return "";
    }
    string Now()//バッテリー量の表示
    {
        var b = Battery * 100;
        if (b <= 0) return zero;
        if (b <= 5) return "<mark=#d95327><color=#000000>||</mark>           </size></color>";
        if (b <= 10) return "<mark=#d96e27><color=#000000>|||</mark>          </size></color>";
        if (b <= 20) return "<mark=#d9b827><color=#000000>||||</mark>         </size></color>";
        if (b <= 30) return "<mark=#d6d927><color=#000000>|||||</mark>        </size></color>";
        if (b <= 40) return "<mark=#b8d13b><color=#000000>||||||</mark>       </size></color>";
        if (b <= 50) return "<mark=#a7ba47><color=#000000>|||||||</mark>      </size></color>";
        if (b <= 60) return "<mark=#96ba47><color=#000000>||||||||</mark>     </size></color>";
        if (b <= 70) return "<mark=#84ba47><color=#000000>|||||||||</mark>    </size></color>";
        if (b <= 80) return "<mark=#75ba47><color=#000000>||||||||||</mark>   </size></color>";
        if (b <= 90) return "<mark=#3fb81d><color=#000000>|||||||||||</mark>  </size></color>";
        else return "<mark=#03ff4a><color=#000000>||||||||||</mark> </size></color>";
    }
    const string zero = "<mark=#676767><color=#000000>|</mark>                  </size></color>";
}