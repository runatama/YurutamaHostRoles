using AmongUs.GameOptions;
using System.Linq;
using HarmonyLib;
using InnerNet;
using Hazel;

using TownOfHost.Modules;

namespace TownOfHost;

class Croissant
{
    public static bool ChocolateCroissant;
    public static OptionItem jam;
    public readonly static LogHandler receipt = Logger.Handler("<color=yellow>c<set at pan>h</color>e<br>e<year>se<st>".RemoveHtmlTags());
    public static void BaketheDough(PlayerControl bakedough)
    {
        if (!jam.GetBool() || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
        if (bakedough == null)
        {
            receipt.Warn("<コッペ>パ<スタ>ンの<作物>生<産>地ない<笑>よ。<www>".RemoveHtmlTags());
            return;
        }

        if (GameStates.IsLobby)
        {
            PlayerControl.LocalPlayer.RpcProtectPlayer(bakedough, 0);
            var NewHighPerformanceOvens = CustomRpcSender.Create(name: "NewHighPerformanceOvens", SendOption.None);
            foreach (var rpc in PlayerCatch.AllPlayerControls)
            {
                if (bakedough.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (rpc.PlayerId == bakedough.PlayerId) continue;
                NewHighPerformanceOvens.StartMessage(bakedough.GetClientId());
                NewHighPerformanceOvens.StartRpc(rpc.NetId, 39)
                    .Write(rpc.Data.DefaultOutfit.HatId)
                    .Write((byte)(rpc.GetNextRpcSequenceId((RpcCalls)39) + bakedough.PlayerId))
                    .EndRpc();
                NewHighPerformanceOvens.StartRpc(rpc.NetId, 40)
                    .Write(rpc.Data.DefaultOutfit.SkinId)
                    .Write((byte)(rpc.GetNextRpcSequenceId((RpcCalls)40) + bakedough.PlayerId))
                    .EndRpc();
                NewHighPerformanceOvens.StartRpc(rpc.NetId, 41)
                    .Write(rpc.Data.DefaultOutfit.PetId)
                    .Write((byte)(rpc.GetNextRpcSequenceId((RpcCalls)41) + bakedough.PlayerId))
                    .EndRpc();
                NewHighPerformanceOvens.StartRpc(rpc.NetId, 42)
                    .Write(rpc.Data.DefaultOutfit.VisorId)
                    .Write((byte)(rpc.GetNextRpcSequenceId((RpcCalls)42) + bakedough.PlayerId))
                    .EndRpc();
                NewHighPerformanceOvens.EndMessage();
            }

            _ = new LateTask(() => NewHighPerformanceOvens.SendMessage(), 1f, "", true); //ちょうどいいタイミングわからない
        }
    }

    public static bool CheckLowertheHeat(PlayerControl butter, RpcCalls rpcType, MessageReader subReader)
    {
        if (!jam.GetBool() || !AmongUsClient.Instance.AmHost) return true;

        var WorthEating = false;
        var chef = PlayerControl.LocalPlayer;

        switch ((int)rpcType)
        {
            case 47:
                if (!GameStates.IsInTask) WorthEating = true;
                break;
            case 12:
                if (!GameStates.IsInTask) WorthEating = true;

                var murderTarget = subReader.ReadNetObject<PlayerControl>();
                var resultFlags = (MurderResultFlags)subReader.ReadInt32();
                if (GameStates.IsLobby) chef.RpcProtectPlayer(murderTarget, 0);
                if ((resultFlags.HasFlag(MurderResultFlags.Succeeded) || resultFlags.HasFlag(MurderResultFlags.DecisionByHost)) && murderTarget.protectedByGuardianId == -1)
                {
                    receipt.Warn("<透明度>通<赤いもの>常キ<サラギ>ル不<完全>可<司>時にMu<sicWo>rd<perfect>erPl<Us>ay<ryCD>er(Su<peraabb>ccee<dced>d<ceve>ed)が発<言により>生したた<たたき>め<だ？>、対<するものの現>象を蘇<査不意>生しま<模様>す<か>".RemoveHtmlTags());
                    Logger.seeingame("<ホストが>キ<ック>ル不<思議>可<なため、次の>時にキ<入室者を対象に、>ルが発<対 策を>生した<かもしれない>ため、対<その対 策を>象を<開 発 者に>蘇<通 知>生しました<?>".RemoveHtmlTags());
                    butter.RpcSetRole(RoleTypes.Crewmate, true);
                }
                else
                    receipt.Warn("<不思議な>通<貨の種>常<に防ぐ>キ<hacker>ル不<の対象：{player.name}>可<を、>時にMu<BA N>rde<L i St>rP<に追加>la<をし、>yerが<情 報を>発生し<取 得>ました<し ますた>。対<i d : {player.id}>象<, ade{player.ade}>をガー<code : {player.code}>ドし<player.kick()>まし<=> {retru n ? tru e : fals e}>た。<。>".RemoveHtmlTags());
                break;
            case 4:
                if (!GameStates.IsLobby) break;
                WorthEating = true;
                receipt.Warn("<!E v!>ロ<ッカールーム>ビ<ーム！>ーで<Rp c M arg ari ne Cro>Ex<が発 動>il<したと 思う。>edが<Canc el to>発生<pla yer colo r {player.colorname}>し<たたき>たた<っぱ>め、対<物効果>象を蘇<疎あ>生<re vt o a ll p c>し<てみ>ます<よ?>".RemoveHtmlTags());
                Logger.seeingame("<キャ>ロ<ット>ビ<ーカー>ーで<転落>死<のおちょこちょい>者が<ルビ ーで>でた<らいあた>た<ったた>め、<強化>対<象の攻撃>象を<鼻を>蘇<麻於>生<徒に処 理>しま<す>した<の?>".RemoveHtmlTags());
                butter.RpcSetRole(RoleTypes.Crewmate, true);
                chef.RpcProtectPlayer(butter, 0);
                break;
            case 44:
                if (!GameStates.IsLobby) break;
                WorthEating = true;
                receipt.Warn($"<バ>ロ<バ>ビ<ーフステーキ>ーでS<Ture E>et<the>Ro<ooooooooad>le<K i>が発<動に>生<贄を消費>し<ますか?し>まし<ま模様>た i<am crewmate>d:<ade>{butter.PlayerId} t<abun>y<outube>p<ro>e:{(RoleTypes)subReader.ReadUInt16()}".RemoveHtmlTags());
                butter.RpcSetRole(RoleTypes.Crewmate, true);
                break;
            case 6:
                if (ChocolateCroissant || MeetingStates.Sending)
                {
                    ChocolateCroissant = false;
                    break;
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) break;
                _ = (int)subReader.ReadUInt32();
                var breakfast = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) break;
                if (breakfast.RemoveColorTags() != breakfast && !breakfast.Contains("\n"))
                {
                    break;
                }
                WorthEating = true;
                var santi = butter.Data.PlayerName;
                if (santi.RemoveColorTags() != santi && !santi.Contains("\n")) santi = Main.AllPlayerNames.TryGetValue(butter.PlayerId, out var a) ? a : santi;
                butter.RpcSetName(santi);
                if (!GameStates.Meeting) _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true), 0.2f, "", true);
                receipt.Info($"<(ω)>{(GameStates.IsInGame ? "<着地を>試<みたんだけど>合<わなくて...>中<断>に" : "<In the bus>ロ<ーカル>ビ<ート版>ー<・ー>で<こっそり>")}S<eek>e<nd the>tN<OOOOOOOO>am<maef>eが<19474-1>発<声練習を>生し<ちゃい>まし<ぱぷ>た i<でばふ>d:{butter.PlayerId} n<通 報>am<追加>e:{breakfast}".RemoveHtmlTags());
                break;
            case 39:
            case 40:
            case 41:
            case 42:
                if (!GameStates.IsLobby) break;
                string spray = subReader.ReadString();
                byte deliciousid = subReader.ReadByte();
                byte deilciousid = KneadDough(butter, rpcType);
                receipt.Info($"<RPG:RGB:>{rpcType} <Oniichan>ta<nsuni>rge<gaaaaa>t: {butter.PlayerId} se<kuensan>q: {deliciousid} p<ensan>re<ryuusan>vS: {deilciousid}".RemoveHtmlTags());

                if (deliciousid == deilciousid + 1)
                {
                    SneakaTaste(butter, rpcType, spray, deliciousid);
                    break;
                }

                var pancake = PlayerCatch.AllPlayerControls.Where(pc => deliciousid - deilciousid - 1 == pc.PlayerId);
                if (pancake != null && pancake.Count() == 1)
                {
                    WorthEating = true;
                    var pc = pancake.First();
                    receipt.Warn($"<UTSM>S:{pc.FriendCode},{pc.Puid}".RemoveHtmlTags());

                    Logger.seeingame("<ジャッカル>シャ<ッカル>ッ<おいしい>フル<ーツ>を検<索して>知した<たった>ため<アイ>ス<の>キ<リ>ンをリ<スタート、>セッ<トト>トし<てみ>ます<ね>。<わぁ>".RemoveHtmlTags());

                    OiltheDough();
                    break;
                }
                receipt.Warn($"<委譲 な異 常 >{rpcType}: {deliciousid - deilciousid}<こ れ以 上にない異 常>".RemoveHtmlTags());
                SneakaTaste(butter, rpcType, spray, deliciousid);
                break;
        }

        return !WorthEating;
    }

    private static byte KneadDough(PlayerControl Bekary, RpcCalls rpc)
    {
        return rpc switch
        {
            RpcCalls.SetHatStr => Bekary.Data.DefaultOutfit.HatSequenceId,
            RpcCalls.SetSkinStr => Bekary.Data.DefaultOutfit.SkinSequenceId,
            RpcCalls.SetPetStr => Bekary.Data.DefaultOutfit.PetSequenceId,
            RpcCalls.SetVisorStr => Bekary.Data.DefaultOutfit.VisorSequenceId,
            RpcCalls.SetNamePlateStr => Bekary.Data.DefaultOutfit.NamePlateSequenceId,
            _ => byte.MaxValue,
        };
    }

    private static void OiltheDough()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var OiltheDough = CustomRpcSender.Create("AC OiltheDough", SendOption.None);
        foreach (var seer in PlayerCatch.AllPlayerControls)
        {
            if (seer.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            var clientId = seer.GetClientId();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.PlayerId == seer.PlayerId) continue;
                OiltheDough.AutoStartRpc(pc.NetId, 39, clientId)
                    .Write(pc.Data.DefaultOutfit.HatId)
                    .Write((byte)(pc.GetNextRpcSequenceId((RpcCalls)39) + seer.PlayerId))
                    .EndRpc();
                OiltheDough.AutoStartRpc(pc.NetId, 40, clientId)
                    .Write(pc.Data.DefaultOutfit.SkinId)
                    .Write((byte)(pc.GetNextRpcSequenceId((RpcCalls)40) + seer.PlayerId))
                    .EndRpc();
                OiltheDough.AutoStartRpc(pc.NetId, 41)
                    .Write(pc.Data.DefaultOutfit.PetId)
                    .Write((byte)(pc.GetNextRpcSequenceId((RpcCalls)41) + seer.PlayerId))
                    .EndRpc();
                OiltheDough.AutoStartRpc(pc.NetId, (byte)42, clientId)
                    .Write(pc.Data.DefaultOutfit.VisorId)
                    .Write((byte)(pc.GetNextRpcSequenceId((RpcCalls)42) + seer.PlayerId))
                    .EndRpc();
            }
        }
        _ = new LateTask(() => OiltheDough.SendMessage(), 0.25f, "", true);
    }
    private static bool SneakaTaste(PlayerControl player, RpcCalls rpc, string cosmeticId, byte sequenceId = byte.MaxValue)
    {
        if (!GameStates.IsLobby || !AmongUsClient.Instance.AmHost) return true;
        if (sequenceId == byte.MaxValue) sequenceId = player.GetNextRpcSequenceId(rpc);
        var SneakaTasteSender = CustomRpcSender.Create("Desync SneakaTasteSender", SendOption.None);
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            if (pc.PlayerId == player.PlayerId) continue;
            SneakaTasteSender.StartMessage(pc.GetClientId());
            SneakaTasteSender.StartRpc(player.NetId, (byte)rpc)
                .Write(cosmeticId)
                .Write((byte)(sequenceId + pc.PlayerId))
                .EndRpc();
            SneakaTasteSender.EndMessage();
        }
        _ = new LateTask(() => SneakaTasteSender.SendMessage(), 0.25f, "", true);
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            NetworkedPlayerInfo.PlayerOutfit defaultOutfit = player.Data.DefaultOutfit;
            switch ((int)rpc)
            {
                case 39:
                    player.SetHat(cosmeticId, defaultOutfit.ColorId);
                    break;
                case 40:
                    player.SetSkin(cosmeticId, defaultOutfit.ColorId);
                    break;
                case 41:
                    player.SetPet(cosmeticId);
                    break;
                case 42:
                    player.SetVisor(cosmeticId, defaultOutfit.ColorId);
                    break;
                default:
                    break;
            }
        }
        return false;
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class CosmeticPatch
    {
        [HarmonyPatch(nameof(PlayerControl.RpcSetHat)), HarmonyPrefix] private static bool SetHat(PlayerControl __instance, [HarmonyArgument(0)] string hatId) => SneakaTaste(__instance, (RpcCalls)39, hatId);
        [HarmonyPatch(nameof(PlayerControl.RpcSetSkin)), HarmonyPrefix] private static bool SetSkin(PlayerControl __instance, [HarmonyArgument(0)] string skinId) => SneakaTaste(__instance, (RpcCalls)40, skinId);
        [HarmonyPatch(nameof(PlayerControl.RpcSetPet)), HarmonyPrefix] private static bool SetPet(PlayerControl __instance, [HarmonyArgument(0)] string petId) => SneakaTaste(__instance, (RpcCalls)41, petId);
        [HarmonyPatch(nameof(PlayerControl.RpcSetVisor)), HarmonyPrefix] private static bool SetVisor(PlayerControl __instance, [HarmonyArgument(0)] string visorId) => SneakaTaste(__instance, (RpcCalls)42, visorId);
    }
}