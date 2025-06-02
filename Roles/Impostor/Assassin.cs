using System.Collections.Generic;
using AmongUs.GameOptions;
using InnerNet;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Assassin : RoleBase, IImpostor
{
    //ここに書いておこう！
    //この役職は実装しない予定だ！
    //えなんでかって？SHRとかにあるかｒ(((
    //配信とかで使ってあげてください(
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Assassin),
            player => new Assassin(player),
            CustomRoles.Assassin,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            40200,
            SetupOptionItem,
            "as",
            tab: TabGroup.Combinations,
            assignInfo: new RoleAssignInfo(CustomRoles.Assassin, CustomRoleTypes.Impostor)
            {
                AssignUnitRoles = [CustomRoles.Assassin, CustomRoles.Merlin]
            },
            combination: CombinationRoles.AssassinandMerlin
        );
    public Assassin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NowState = AssassinMeeting.WaitMeeting;
        MarlinIds = new();
        isDeadCache = new();
        NowUse = false;
        GuessId = byte.MaxValue;
    }
    byte GuessId;
    public static bool NowUse;
    public static List<byte> MarlinIds = new();
    AssassinMeeting NowState;
    static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();

    static OptionItem DieCallMeeting;
    enum AssassinMeeting
    {
        WaitMeeting,
        CallMetting,
        Guessing,
        Collected,
        EndMeeting
    }
    public static void SetupOptionItem()
    {
        DieCallMeeting = BooleanOptionItem.Create(RoleInfo, 10, "DieCallMeeting", false, false);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!DieCallMeeting.GetBool() || player.IsAlive() || player == null || GameStates.IsMeeting) return;

        if (NowState is AssassinMeeting.WaitMeeting)
        {
            NowState = AssassinMeeting.CallMetting;
            Logger.Info("死んじゃった。", "Assassin");
            foreach (var info in GameData.Instance.AllPlayers)
            {
                isDeadCache[info.PlayerId] = (info.PlayerId.GetPlayerState().IsDead, info.Disconnected);

                info.IsDead = false;
                info.Disconnected = false;
            }
            NowUse = true;
            NowState = AssassinMeeting.Guessing;
            AntiBlackout.SendGameData();
            _ = new LateTask(() =>
            ReportDeadBodyPatch.DieCheckReport(Player, null, false, "アサシン会議", "#ff1919"), 3, "", true);
        }
    }
    public override void OnSpawn(bool initialState = false)
    {
        NowUse = false;
        if (NowState is AssassinMeeting.Collected or AssassinMeeting.CallMetting)
        {
            //_ = new LateTask(() =>
            {
                if (NowState is AssassinMeeting.Collected)
                {
                    MyState.IsDead = false;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, Player.PlayerId, hantrole: CustomRoles.Assassin);
                    Logger.Info("まーりんぱりーん", "Assassin");
                }
                else if (NowState is AssassinMeeting.CallMetting)
                {
                    foreach (var info in GameData.Instance.AllPlayers)
                    {
                        isDeadCache[info.PlayerId] = (info.PlayerId.GetPlayerState().IsDead, info.Disconnected);

                        info.IsDead = false;
                        info.Disconnected = false;
                    }
                    NowUse = true;
                    NowState = AssassinMeeting.Guessing;
                    AntiBlackout.SendGameData();
                    _ = new LateTask(() =>
                    ReportDeadBodyPatch.DieCheckReport(Player, null, false, "アサシン会議", "#ff1919"), 3, "", true);
                }
            }//, 0.5f, "AssassinShori");
        }
    }
    public override void OnStartMeeting()
    {
        if (NowState is AssassinMeeting.Guessing)
        {
            _ = new LateTask(() =>
            {
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (info == null) continue;
                    if (isDeadCache.TryGetValue(info.PlayerId, out var val))
                    {
                        info.IsDead = val.isDead;
                        info.Disconnected = val.Disconnected;
                    }
                }
                isDeadCache.Clear();

                MeetingHudPatch.StartPatch.Serialize = true;
                AntiBlackout.SendGameData();
                MeetingHudPatch.StartPatch.Serialize = false;

                if (Options.ExHideChatCommand.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        var count = 0;
                        Dictionary<byte, bool> State = new();
                        foreach (var player in PlayerCatch.AllAlivePlayerControls)
                        {
                            State.TryAdd(player.PlayerId, player.Data.IsDead);
                        }
                        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (!Main.IsCs() && Options.ExRpcWeightR.GetBool()) count++;

                            if (!State.ContainsKey(pc.PlayerId)) continue;
                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                            if (pc.IsModClient()) continue;

                            _ = new LateTask(() =>
                            {
                                foreach (PlayerControl tg in PlayerCatch.AllAlivePlayerControls)
                                {
                                    if (tg.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                    if (tg.IsModClient()) continue;
                                    tg.Data.IsDead = true;
                                }
                                pc.Data.IsDead = false;
                                MeetingHudPatch.StartPatch.Serialize = true;
                                RPC.RpcSyncAllNetworkedPlayer(pc.GetClientId());
                                MeetingHudPatch.StartPatch.Serialize = false;
                            }, count * 0.1f, "SetDienoNaka", true);
                        }
                        _ = new LateTask(() =>
                        {
                            foreach (PlayerControl player in PlayerCatch.AllAlivePlayerControls)
                            {
                                player.Data.IsDead = State.TryGetValue(player.PlayerId, out var data) && data;
                            }
                        }, count * 0.1f, "SetDienoNaka", true);
                    }, 4f, "SetDie");
                }
            }, 3, "Assassin-SetDie", true);
        }
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (NowState is AssassinMeeting.EndMeeting or AssassinMeeting.CallMetting) return false;

        if (NowState is AssassinMeeting.Guessing)
        {
            var name = Camouflage.PlayerSkins.TryGetValue(Player.PlayerId, out var cos) ? cos.PlayerName : "^a^";
            var tage = Camouflage.PlayerSkins.TryGetValue(GuessId, out var tcos) ? tcos.PlayerName : "彼";

            if (GuessId is not byte.MaxValue)
            {
                NowState = AssassinMeeting.EndMeeting;
                Player.RpcSetName($"{tage}はマーリンではなかった...<size=0>");
                MeetingVoteManager.Voteresult = $"{tage}はマーリンではなかった...";
            }
            if (GuessId.GetPlayerState()?.MainRole is CustomRoles.Merlin)
            {
                NowState = AssassinMeeting.Collected;
                Player.RpcSetName($"{tage}はマーリンだった...<size=0>");
                MeetingVoteManager.Voteresult = $"{tage}はマーリンだった...";
            }
            else
            {
                NowState = AssassinMeeting.EndMeeting;
                Player.RpcSetName($"{tage}はマーリンではなかった...<size=0>");
                MeetingVoteManager.Voteresult = $"{tage}はマーリンではなかった...";
            }
            Exiled = Player.Data;
            _ = new LateTask(() => Player.RpcSetName(name), 6f, "AssassinSetName", true);
            return true;
        }
        else
        if (NowState is AssassinMeeting.WaitMeeting)
        {
            if (Exiled?.PlayerId == Player.PlayerId)
            {
                NowState = AssassinMeeting.CallMetting;
                Logger.Info("追放されちゃった！", "Assassin");
                Exiled = null;
                IsTie = false;
                ClearAndExile = true;
                return true;
            }
        }

        return false;
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (NowState is not AssassinMeeting.Guessing) return true;

        if (!Is(voter)) return false;
        if (votedForId is MeetingVoteManager.Skip
        || votedForId == Player.PlayerId
        || (votedForId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool())) return false;

        GuessId = votedForId;
        Logger.Info($"{votedForId.GetPlayerControl()?.Data?.GetLogPlayerName() ?? "???"} はマーリンかな?", "Assassin");
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, Player.PlayerId);
        return true;
    }
}