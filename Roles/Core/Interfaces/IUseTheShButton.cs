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
        if (!Player.IsModClient() && Player.PlayerId != 0) Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
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
        if (!Player.IsModClient() && Player.PlayerId != 0) Player.RpcShapeshift(PlayerControl.LocalPlayer, false);
        var (name, color, hat, skin, visor, nameplate, level, pet) = PlayerSkinPatch.Load(Player);
        var sender = CustomRpcSender.Create();

        Player.SetColor(color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetColor)
            .Write(Player.Data.NetId)
            .Write(color)
            .EndRpc();

        Player.SetHat(hat, color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetHatStr)
            .Write(hat)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        Player.SetSkin(skin, color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(skin)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        Player.SetVisor(visor, color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(visor)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

        Player.RpcSetPet(pet);
        Player.RpcSetName(name);

        _ = new LateTask(() =>
        {
            sender.SendMessage();
            if (Options.Onlyseepet.GetBool()) PlayerCatch.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.Colorchnge());
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(NoCache: true), 0.2f, "", true);
        }, 0.23f);
    }
    public void ResetS(PlayerControl Player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Camouflage.IsCamouflage) return;
        if (Player.inVent) return;
        var (name, color, hat, skin, visor, nameplate, level, pet) = PlayerSkinPatch.Load(Player);
        var sender = CustomRpcSender.Create();

        Player.SetColor(color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetColor)
            .Write(Player.Data.NetId)
            .Write(color)
            .EndRpc();

        Player.SetHat(hat, color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetHatStr)
            .Write(hat)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        Player.SetSkin(skin, color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(skin)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        Player.SetVisor(visor, color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(visor)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

        if (Player.IsAlive()) Player.RpcSetPet(pet);
        if (!GameStates.Meeting) Player.RpcSetName(name);

        _ = new LateTask(() =>
        {
            sender.SendMessage();
            if (Options.Onlyseepet.GetBool()) PlayerCatch.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.Colorchnge());
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(NoCache: true), 0.2f, "", true);
        }, 0.23f);
    }

    public void OnClick()
    { }
    /// <summary>ワンクリックボタンが使えるか</summary>
    public bool UseOCButton => true;
}
