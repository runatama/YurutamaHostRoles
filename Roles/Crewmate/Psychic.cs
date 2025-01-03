using System.Collections.Generic;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Psychic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Psychic),
            player => new Psychic(player),
            CustomRoles.Psychic,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20400,
            SetupOptionItem,
            "Ps",
            "#a34fee",
            false,
            from: From.None
        );
    public Psychic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        callrate = OptionCallRate.GetFloat();
        taskaddrate = OptionTaskAddRate.GetBool();
    }
    static OptionItem Kakusei;
    static OptionItem Task;
    static OptionItem OptionCallRate;
    static OptionItem OptionTaskAddRate;
    static float callrate;
    static bool taskaddrate;
    bool kakusei;
    static HashSet<Psychic> Psychics = new();
    enum OptionName
    {
        PsychicCallRate,
        PsychicTaskAddrate
    }
    private static void SetupOptionItem()
    {
        OptionCallRate = FloatOptionItem.Create(RoleInfo, 12, OptionName.PsychicCallRate, new(0, 100, 1), 50, false).SetValueFormat(OptionFormat.Percent);
        OptionTaskAddRate = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PsychicTaskAddrate, false, false);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.TaskKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Kakuseitask, new(0f, 255f, 1f), 5f, false, Kakusei);
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(Task.GetInt()))
        {
            if (kakusei == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            kakusei = true;
        }
        return true;
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override void Add()
    {
        kakusei = !Kakusei.GetBool() || Task.GetInt() < 1; ;

        Psychics.Add(this);
    }

    public override void OnDestroy() => Psychics.Clear();
    public float GetChance()
    {
        var mokuhyo = callrate;
        float wariai = MyTaskState.CompletedTasksCount * 100 / MyTaskState.AllTasksCount;

        if (taskaddrate)
        {
            mokuhyo = callrate * wariai;
        }

        return mokuhyo / 100;
    }
    public static void CanAbility(PlayerControl target)
    {
        foreach (var ps in Psychics)
        {
            var random = IRandom.Instance.Next(100);
            if (ps.Player.IsAlive() && ps.kakusei && ps.GetChance() > random)
            {
                if (AmongUsClient.Instance.AmHost)
                    if (ps.Player == PlayerControl.LocalPlayer)
                        target.StartCoroutine(target.CoSetRole(RoleTypes.Noisemaker, true));
                    else
                        target.RpcSetRoleDesync(RoleTypes.Noisemaker, ps.Player.GetClientId());
                target.SyncSettings();
            }
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!GameLog && comms) return "<color=#cccccc> (??)</color>";

        return $"<color={RoleInfo.RoleColorCode}>({GetChance()}%)</color>";
    }
}
