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
            26200,
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
    }
    static OptionItem Kakusei;
    static OptionItem Task;
    bool kakusei;
    static HashSet<Psychic> Psychics = new();
    private static void SetupOptionItem()
    {
        Kakusei = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.TaskKakusei, true, false);
        Task = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Kakuseitask, new(0f, 255f, 1f), 5f, false, Kakusei);
    }
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished || MyTaskState.CompletedTasksCount >= Task.GetFloat()) kakusei = true;
        return true;
    }
    public override void Add()
    {
        kakusei = !Kakusei.GetBool();

        Psychics.Add(this);
    }

    public override void OnDestroy() => Psychics.Clear();
    public static void CanAbility(PlayerControl target)
    {
        foreach (var ps in Psychics)
        {
            if (ps.Player.IsAlive() && ps.kakusei)
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

}
