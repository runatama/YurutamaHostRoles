namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
///ワンクリックシェイプボタンを使う役職
/// <summary>
public interface IUseTheShButton
{
    public void Shape(PlayerControl Player)
    {
        if (!AmongUsClient.Instance.AmHost || Player.shapeshifting) return;
        PlayerSkinPatch.Save(Player);
        Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        ResetSkin(Player);
    }

    public bool CheckShapeshift(PlayerControl Player, PlayerControl target)
    {
        if (target.PlayerId == Player.PlayerId)
        {
            if (GameStates.IsInTask)
                OnClick();
            Player.RpcRejectShapeshift();
            return false;
        }
        return true;
    }

    public void ResetSkin(PlayerControl Player)
    {
        Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        var sd = PlayerSkinPatch.Load(Player);
        Player.RpcSetColor((byte)sd.Item2);
        Player.RpcSetHat(sd.Item3);
        Player.RpcSetSkin(sd.Item4);
        Player.RpcSetVisor(sd.Item5);
        Player.RpcSetPet(sd.Item8);
        Player.RpcSetName(sd.Item1);
        _ = new LateTask(() =>
        {
            Player.RpcSetHat(sd.Item3);
            Player.RpcSetSkin(sd.Item4);
            Player.RpcSetVisor(sd.Item5);
            Utils.NotifyRoles(false, Player, NoCache: true);
        }, 0.3f);
    }
    public void ResetS(PlayerControl Player)
    {
        var sd = PlayerSkinPatch.Load(Player);
        Player.RpcSetHat(sd.Item3);
        Player.RpcSetSkin(sd.Item4);
        Player.RpcSetVisor(sd.Item5);
    }

    public void OnClick()
    { }
}
