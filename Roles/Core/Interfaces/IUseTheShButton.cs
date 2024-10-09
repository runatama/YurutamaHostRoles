using HarmonyLib;

namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
///ワンクリックシェイプボタンを使う役職
/// <summary>
public interface IUseTheShButton
{
    public void Shape(PlayerControl Player)
    {
        if (!AmongUsClient.Instance.AmHost || Player.shapeshifting || !UseOCButton) return;
        PlayerSkinPatch.Save(Player);
        Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        ResetSkin(Player);
    }

    public bool CheckShapeshift(PlayerControl Player, PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (target.PlayerId == Player.PlayerId && UseOCButton)
        {
            if (GameStates.IsInTask && !Utils.IsActive(SystemTypes.MushroomMixupSabotage))
                OnClick();
            Player.RpcRejectShapeshift();
            return false;
        }
        return !UseOCButton;
    }
    public void ResetSkin(PlayerControl Player)
    {
        Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        var sd = PlayerSkinPatch.Load(Player);
        var sender = CustomRpcSender.Create();

        Player.SetColor(sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetColor)
            .Write(Player.Data.NetId)
            .Write(sd.color)
            .EndRpc();

        Player.SetHat(sd.hat, sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetHatStr)
            .Write(sd.hat)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        Player.SetSkin(sd.skin, sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(sd.skin)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        Player.SetVisor(sd.visor, sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(sd.visor)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

        Player.RpcSetPet(sd.pet);
        Player.RpcSetName(sd.name);

        _ = new LateTask(() =>
        {
            sender.SendMessage();
            if (Options.Onlyseepet.GetBool()) Main.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.Colorchnge());
            new LateTask(() => Utils.NotifyRoles(false, NoCache: true), 0.2f, "");
        }, 0.3f);
    }
    public void ResetS(PlayerControl Player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var sd = PlayerSkinPatch.Load(Player);
        var sender = CustomRpcSender.Create();

        Player.SetColor(sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetColor)
            .Write(Player.Data.NetId)
            .Write(sd.color)
            .EndRpc();

        Player.SetHat(sd.hat, sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetHatStr)
            .Write(sd.hat)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        Player.SetSkin(sd.skin, sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(sd.skin)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        Player.SetVisor(sd.visor, sd.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(sd.visor)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

        Player.RpcSetPet(sd.pet);
        Player.RpcSetName(sd.name);

        _ = new LateTask(() =>
        {
            sender.SendMessage();
            if (Options.Onlyseepet.GetBool()) Main.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.Colorchnge());
            new LateTask(() => Utils.NotifyRoles(false, NoCache: true), 0.2f, "");
        }, 0.3f);
    }

    public void OnClick()
    { }
    /// <summary>ワンクリックボタンが使えるか</summary>
    public bool UseOCButton => true;
}
