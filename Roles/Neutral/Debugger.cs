/*
using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;


namespace TownOfHost.Roles.Neutral;
public sealed class Debugger : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Debugger),
            player => new Debugger(player),
            CustomRoles.Debugger,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            80000,
            null,
            "dg",
            "#8f00ce",
            true
        );
    public Debugger(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        page = 0;
        playerId = 255;
    }

    private byte page;
    private byte playerId;

    public static bool Prefix()
    {
        switch (page)
        {
            case 0:
                page = 1;
                playerId = target.PlayerId;
                break;

        }
        return false;
    }
    //Ikillerの奴一個入れないとエラー吐く
    public bool CanUseSabotageButton() => true;
}
*/