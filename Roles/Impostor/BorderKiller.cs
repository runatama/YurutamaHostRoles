using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class BorderKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(BorderKiller),
            player => new BorderKiller(player),
            CustomRoles.BorderKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            7000,
            SetupOptionItem,
            "Br",
            Desc: () =>
            {
                return string.Format(GetString("BorderKillerDesc"), OptionMissionKillcount.GetInt());
            }
        );
    public BorderKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionMissionKillcount;
    enum OptionName
    {
        BorderKillerMissionKillcount
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionMissionKillcount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.BorderKillerMissionKillcount, new(1, 14, 1), 3, false).SetValueFormat(OptionFormat.Players);
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"({MyState.GetKillCount(false)}/{OptionMissionKillcount.GetInt()})";

    public override void CheckWinner()
    {
        //目標キルカウント ＞ 現在のキルカウント
        if (OptionMissionKillcount.GetInt() > MyState.GetKillCount(false) && CustomWinnerHolder.WinnerTeam is CustomWinner.Impostor)
        {
            CustomWinnerHolder.IdRemoveLovers.Add(Player.PlayerId);
            CustomWinnerHolder.WinnerIds.Remove(Player.PlayerId);
        }
    }
}