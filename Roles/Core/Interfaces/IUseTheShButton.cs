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
        PlayerOutfitManager.Save(Player);
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
        var outfit = PlayerOutfitManager.Load(Player);
        if (outfit == null) return;
        var sender = CustomRpcSender.Create();

        Player.SetColor(outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetColor)
            .Write(Player.Data.NetId)
            .Write(outfit.color)
            .EndRpc();

        Player.SetHat(outfit.hat, outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetHatStr)
            .Write(outfit.hat)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        Player.SetSkin(outfit.skin, outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(outfit.skin)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        Player.SetVisor(outfit.visor, outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(outfit.visor)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

        Player.RpcSetPet(outfit.pet);
        Player.RpcSetName(outfit.name);

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
        var outfit = PlayerOutfitManager.Load(Player);
        var sender = CustomRpcSender.Create();

        Player.SetColor(outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetColor)
            .Write(Player.Data.NetId)
            .Write(outfit.color)
            .EndRpc();

        Player.SetHat(outfit.hat, outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetHatStr)
            .Write(outfit.hat)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

        Player.SetSkin(outfit.skin, outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(outfit.skin)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

        Player.SetVisor(outfit.visor, outfit.color);
        sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(outfit.visor)
            .Write(Player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

        if (Player.IsAlive()) Player.RpcSetPet(outfit.pet);
        if (!GameStates.Meeting) Player.RpcSetName(outfit.name);

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
