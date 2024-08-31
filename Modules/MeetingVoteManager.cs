using System;
using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using AmongUs.GameOptions;

namespace TownOfHost.Modules;

public class MeetingVoteManager
{
    public IReadOnlyDictionary<byte, VoteData> AllVotes => allVotes;
    private static Dictionary<byte, VoteData> allVotes = new(15);
    private readonly MeetingHud meetingHud;

    public static MeetingVoteManager Instance => _instance;
    private static MeetingVoteManager _instance;
    private static LogHandler logger = Logger.Handler(nameof(MeetingVoteManager));

    private MeetingVoteManager()
    {
        meetingHud = MeetingHud.Instance;
        ClearVotes();
    }

    public static void Start()
    {
        _instance = new();
    }

    /// <summary>
    /// 投票を初期状態にします
    /// </summary>
    public void ClearVotes()
    {
        foreach (var voteArea in meetingHud.playerStates)
        {
            allVotes[voteArea.TargetPlayerId] = new(voteArea.TargetPlayerId);
        }
    }
    /// <summary>
    /// 今までに行われた投票をすべて削除し，特定の投票先に1票投じられた状態で会議を強制終了します
    /// </summary>
    /// <param name="voter">投票を行う人</param>
    /// <param name="exiled">追放先</param>
    public void ClearAndExile(byte voter, byte exiled)
    {
        logger.Info($"{Utils.GetPlayerById(voter).GetNameWithRole().RemoveHtmlTags()} によって {GetVoteName(exiled)} が追放されます");
        ClearVotes();
        var vote = new VoteData(voter);
        vote.DoVote(exiled, 1);
        allVotes[voter] = vote;
        EndMeeting(false, true);
    }
    /// <summary>
    /// 投票を行います．投票者が既に投票している場合は票を上書きします
    /// </summary>
    /// <param name="voter">投票者</param>
    /// <param name="voteFor">投票先</param>
    /// <param name="numVotes">票数</param>
    /// <param name="isIntentional">投票者自身の投票操作による自発的な投票かどうか</param>
    public void SetVote(byte voter, byte voteFor, int numVotes = 1, bool isIntentional = true)
    {
        if (!allVotes.TryGetValue(voter, out var vote))
        {
            logger.Warn($"ID: {voter}の投票データがありません。新規作成します");
            vote = new(voter);
        }
        if (vote.HasVoted)
        {
            logger.Info($"ID: {voter}の投票を上書きします");
        }

        bool doVote = true;
        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            var (roleVoteFor, roleNumVotes, roleDoVote) = ((byte?)voteFor, (int?)numVotes, isIntentional);
            var player = Utils.GetPlayerById(voter);
            if (Amnesia.CheckAbility(player))
                (roleVoteFor, roleNumVotes, roleDoVote) = role.ModifyVote(voter, voteFor, isIntentional);

            if (roleVoteFor.HasValue)
            {
                logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} が {Utils.GetPlayerById(voter).GetNameWithRole().RemoveHtmlTags()} の投票先を {GetVoteName(roleVoteFor.Value)} に変更します");
                voteFor = roleVoteFor.Value;
            }
            var pc = Utils.GetPlayerById(voteFor);
            if (!pc.IsAlive() && voteFor != Skip && voteFor != NoVote)
            {
                logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} 相手が死んでいるので投票は取り消されます");
                doVote = false;
            }

            //追加投票
            if (roleNumVotes.HasValue)//追加投票訳
            {
                if (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GivePlusVote.GetBool())
                {
                    if (player.Is(CustomRoles.PlusVote)) numVotes = roleNumVotes.Value + data.AdditionalVote.GetInt() + PlusVote.AdditionalVote.GetInt();
                    else numVotes = roleNumVotes.Value + data.AdditionalVote.GetInt();
                }
                else if (player.Is(CustomRoles.PlusVote)) numVotes = roleNumVotes.Value + PlusVote.AdditionalVote.GetInt();
                else numVotes = roleNumVotes.Value;
                logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} が {Utils.GetPlayerById(voter).GetNameWithRole().RemoveHtmlTags()} の投票数を {numVotes} に変更します");
            }
            else if (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GivePlusVote.GetBool())
            {
                if (player.Is(CustomRoles.PlusVote)) numVotes = data.AdditionalVote.GetInt() + PlusVote.AdditionalVote.GetInt() + 1;
                else numVotes = data.AdditionalVote.GetInt() + 1;
            }
            else if (player.Is(CustomRoles.PlusVote))//プラスポート
            {
                logger.Info($"プラスポート:{role.Player.GetNameWithRole().RemoveHtmlTags()} が {Utils.GetPlayerById(voter).GetNameWithRole().RemoveHtmlTags()} の投票数を {numVotes}+{PlusVote.AdditionalVote.GetInt()}  に変更します");
                numVotes = PlusVote.AdditionalVote.GetInt();
            }

            if (player.Is(CustomRoles.Notvoter))
            {
                logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} の {Utils.GetPlayerById(voter).GetNameWithRole().RemoveHtmlTags()} の投票数を 0 に変更します");
                numVotes = 0;
            }
            else
            if (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GiveNotvoter.GetBool())
            {
                logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} の {Utils.GetPlayerById(voter).GetNameWithRole().RemoveHtmlTags()} の投票数を 0 に変更します");
                numVotes = 0;
            }

            if (player.Is(CustomRoles.Elector) && voteFor == Skip)
            {
                logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} スキップ投票は取り消されます");
                doVote = false;
            }
            else
            if (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var da))
            {
                if (da.GiveElector.GetBool() && voteFor == Skip)
                {
                    logger.Info($"{role.Player.GetNameWithRole().RemoveHtmlTags()} スキップ投票は取り消されます");
                    doVote = false;
                }
            }
        }

        if (doVote)
        {
            vote.DoVote(voteFor, numVotes);
        }
    }
    /// <summary>
    /// 議論時間が終わってる or 全員が投票を終えていれば会議を終了します
    /// </summary>
    public void CheckAndEndMeeting()
    {
        if (meetingHud.discussionTimer - (float)Main.NormalOptions.DiscussionTime >= Main.NormalOptions.VotingTime || AllVotes.Values.All(vote => vote.HasVoted))
        {
            EndMeeting(Roles.Crewmate.Balancer.Id == 255);
        }
    }
    public static string Voteresult;
    /// <summary>
    /// 無条件で会議を終了します
    /// </summary>
    /// <param name="applyVoteMode">スキップと同数投票の設定を適用するかどうか</param>
    public void EndMeeting(bool applyVoteMode = true, bool ClearAndExile = false)
    {
        GameStates.Tuihou = true;
        var result = CountVotes(applyVoteMode, ClearAndExile);
        var logName = result.Exiled == null ? (result.IsTie ? "同数" : "スキップ") : result.Exiled.Object.GetNameWithRole().RemoveHtmlTags();
        logger.Info($"追放者: {logName} で会議を終了します");

        var r = result.Exiled == null ? (result.IsTie ? Translator.GetString("votetie") : Translator.GetString("voteskip")) : Utils.GetPlayerColor(Utils.GetPlayerById(result.Exiled.Object.PlayerId)) + Translator.GetString("fortuihou");
        if (Voteresult == "")
        {
            Voteresult = r;
            Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Vote]　" + r;
        }
        var states = new List<MeetingHud.VoterState>();
        foreach (var voteArea in meetingHud.playerStates)
        {
            var voteData = AllVotes.TryGetValue(voteArea.TargetPlayerId, out var value) ? value : null;
            if (voteData == null)
            {
                logger.Warn($"{Utils.GetPlayerById(voteArea.TargetPlayerId).GetNameWithRole().RemoveHtmlTags()} の投票データがありません");
                continue;
            }
            for (var i = 0; i < voteData.NumVotes; i++)
            {
                states.Add(new()
                {
                    VoterId = voteArea.TargetPlayerId,
                    VotedForId = voteData.VotedFor,
                });
            }
        }
        if (!AntiBlackout.OverrideExiledPlayer)
        {
            var ch = true;
            if (AmongUsClient.Instance.AmHost)
            {
                if (result.Exiled != null)
                    if (result.Exiled == PlayerControl.LocalPlayer.Data)
                    {
                        foreach (var Player in Main.AllPlayerControls)
                        {
                            var taishou = Player;
                            foreach (var pc in Main.AllPlayerControls)
                            {
                                taishou = pc;
                                var List = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(x => x && x != pc && x != PlayerControl.LocalPlayer));
                                taishou = List.OrderBy(x => x.PlayerId).FirstOrDefault();
                                if (pc == PlayerControl.LocalPlayer) continue;
                                Player.RpcSetRoleDesync(Player == taishou ? RoleTypes.Impostor : RoleTypes.Crewmate, pc.GetClientId());
                            }
                            Logger.Info($"{Player.name} => {taishou.name} , Ch = false!", "NotAntenEx");
                        }
                        ch = false;
                    }

                if (ch)
                    foreach (var Player in Main.AllPlayerControls)
                    {
                        var taishou = PlayerControl.LocalPlayer;
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            if (pc == PlayerControl.LocalPlayer) continue;
                            var t = byte.MaxValue;
                            if (!PlayerControl.LocalPlayer.IsAlive())
                            {
                                if (result.Exiled != null) t = result.Exiled.PlayerId;
                                var List = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(x => x && x != pc && x.PlayerId != t && x != PlayerControl.LocalPlayer));
                                taishou = List.OrderBy(x => x.PlayerId).FirstOrDefault();
                            }
                            Player?.RpcSetRoleDesync(Player == taishou ? RoleTypes.Impostor : RoleTypes.Crewmate, pc.GetClientId());
                        }
                        Logger.Info($"{Player.name} => {taishou.name} , Ch = true!", "NotAntenEx");
                    }
            }
        }

        if (AntiBlackout.OverrideExiledPlayer)
        {
            meetingHud.RpcVotingComplete(states.ToArray(), null, true);
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = result.Exiled;
        }
        else
        {
            meetingHud.RpcVotingComplete(states.ToArray(), result.Exiled, result.IsTie);
            //AntiBlackout.SetRole(result.Exiled?.Object);
        }
        if (result.Exiled != null)
        {
            MeetingHudPatch.CheckForDeathOnExile(CustomDeathReason.Vote, result.Exiled.PlayerId);
        }
        Destroy();
    }
    /// <summary>
    /// <see cref="AllVotes"/>から投票をカウントします
    /// </summary>
    /// <param name="applyVoteMode">スキップと同数投票の設定を適用するかどうか</param>
    /// <returns>([Key: 投票先,Value: 票数]の辞書, 追放される人, 同数投票かどうか)</returns>
    public VoteResult CountVotes(bool applyVoteMode, bool ClearAndExile = false)
    {
        // 投票モードに従って投票を変更
        if (applyVoteMode && Options.VoteMode.GetBool())
        {
            ApplySkipAndNoVoteMode();
        }

        // Key: 投票された人
        // Value: 票数
        Dictionary<byte, int> votes = new();
        Dictionary<byte, int> Tie = new();
        foreach (var voteArea in meetingHud.playerStates)
        {
            votes[voteArea.TargetPlayerId] = 0;
            Tie[voteArea.TargetPlayerId] = 0;
        }
        votes[Skip] = 0;
        Tie[Skip] = 0;
        foreach (var vote in AllVotes.Values)
        {
            if (vote.VotedFor == NoVote)
            {
                continue;
            }

            if (Utils.GetPlayerById(vote.Voter) == null) continue;

            votes[vote.VotedFor] += vote.NumVotes;

            if (vote.NumVotes != 0)
            {
                if (Utils.GetPlayerById(vote.Voter).Is(CustomRoles.Tiebreaker)
                || (Utils.GetPlayerById(vote.Voter).Is(CustomRoles.LastImpostor) && LastImpostor.GiveTiebreaker.GetBool())
                || (Utils.GetPlayerById(vote.Voter).Is(CustomRoles.LastNeutral) && LastNeutral.GiveTiebreaker.GetBool())
                || (RoleAddAddons.AllData.TryGetValue(Utils.GetPlayerById(vote.Voter).GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GiveTiebreaker.GetBool())
                )//タイブレ投票は1固定
                { Tie[vote.VotedFor] += 1; }
            }
        }

        return new VoteResult(votes, Tie, ClearAndExile);
    }
    /// <summary>
    /// スキップモードと無投票モードに応じて，投票を上書きしたりプレイヤーを死亡させたりします
    /// </summary>
    private void ApplySkipAndNoVoteMode()
    {
        var ignoreSkipModeDueToFirstMeeting = MeetingStates.FirstMeeting && Options.WhenSkipVoteIgnoreFirstMeeting.GetBool();
        var ignoreSkipModeDueToNoDeadBody = !MeetingStates.IsExistDeadBody && Options.WhenSkipVoteIgnoreNoDeadBody.GetBool();
        var ignoreSkipModeDueToEmergency = MeetingStates.IsEmergencyMeeting && Options.WhenSkipVoteIgnoreEmergency.GetBool();
        var ignoreSkipMode = ignoreSkipModeDueToFirstMeeting || ignoreSkipModeDueToNoDeadBody || ignoreSkipModeDueToEmergency;

        var skipMode = Options.GetWhenSkipVote();
        var noVoteMode = Options.GetWhenNonVote();
        foreach (var voteData in AllVotes)
        {
            var vote = voteData.Value;
            if (!vote.HasVoted)
            {
                var voterName = Utils.GetPlayerById(vote.Voter).GetNameWithRole().RemoveHtmlTags();
                switch (noVoteMode)
                {
                    case VoteMode.Suicide:
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, vote.Voter);
                        logger.Info($"無投票のため {voterName} に自殺させます");
                        break;
                    case VoteMode.Skip:
                        SetVote(vote.Voter, Skip, isIntentional: false);
                        logger.Info($"無投票のため {voterName} にスキップさせます");
                        break;
                    case VoteMode.SelfVote:
                        SetVote(vote.Voter, vote.Voter, isIntentional: false);
                        logger.Info($"無投票のため {voterName} に自投票させます");
                        break;
                }
            }
            else if (!ignoreSkipMode && vote.IsSkip)
            {
                var voterName = Utils.GetPlayerById(vote.Voter).GetNameWithRole().RemoveHtmlTags();
                switch (skipMode)
                {
                    case VoteMode.Suicide:
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, vote.Voter);
                        logger.Info($"スキップしたため {voterName} を自殺させます");
                        break;
                    case VoteMode.SelfVote:
                        SetVote(vote.Voter, vote.Voter, isIntentional: false);
                        logger.Info($"スキップしたため {voterName} に自投票させます");
                        break;
                }
            }
        }
    }
    public void Destroy()
    {
        _instance = null;
    }

    public static string GetVoteName(byte num, bool color = false)
    {
        string name = "invalid";
        var player = Utils.GetPlayerById(num);
        if (num < 15 && player != null)
        {
            if (color)
                name = Utils.GetPlayerColor(player);
            else
            {
                name = player?.GetNameWithRole().RemoveHtmlTags();
            }
        }
        else if (num == Skip) name = "Skip";
        else if (num == NoVote) name = "None";
        else if (num == 255) name = "Dead";
        return name;
    }

    public class VoteData
    {
        public byte Voter { get; private set; } = byte.MaxValue;
        public byte VotedFor { get; private set; } = NoVote;
        public int NumVotes { get; private set; } = 1;
        public bool IsSkip => IsSkipCh();
        public bool IsSkipCh()
        {
            if (PlayerState.GetByPlayerId(Voter) == null) return false;
            return VotedFor == Skip && !PlayerState.GetByPlayerId(Voter).IsDead;
        }
        //ミーテやゲッサーいるからSKip入れてるorスキップ以外の誰か(死んでない)人に入れてるor死んでるだとtrueになる。
        public bool HasVoted => HasVotedCheck();
        public bool HasVotedCheck()
        {
            if (PlayerState.GetByPlayerId(Voter) == null) return true;
            if (PlayerState.GetByPlayerId(Voter).IsDead || VotedFor == Skip) return true;
            if (VotedFor is /*Skip or*/ NoVote) return false;//ここのスキップいらなくね?
            if (Utils.GetPlayerById(VotedFor) != null) return !PlayerState.GetByPlayerId(VotedFor).IsDead;
            return false;
        }

        public VoteData(byte voter) => Voter = voter;

        public void DoVote(byte voteTo, int numVotes)
        {
            logger.Info($"投票: {Utils.GetPlayerById(Voter).GetNameWithRole().RemoveHtmlTags()} => {GetVoteName(voteTo)} x {numVotes}");
            VotedFor = voteTo;
            NumVotes = numVotes;
        }
    }

    public readonly struct VoteResult
    {
        /// <summary>
        /// Key: 投票された人<br/>
        /// Value: 得票数
        /// </summary>
        public IReadOnlyDictionary<byte, int> VotedCounts => votedCounts;
        private readonly Dictionary<byte, int> votedCounts;
        private readonly Dictionary<byte, int> Tievotecount;
        /// <summary>
        /// 追放されるプレイヤー
        /// </summary>
        public readonly NetworkedPlayerInfo Exiled;
        /// <summary>
        /// 同数投票かどうか
        /// </summary>
        public readonly bool IsTie;

        public VoteResult(Dictionary<byte, int> votedCounts, Dictionary<byte, int> Tievotecount, bool ClearAndExile = false)
        {
            this.votedCounts = votedCounts;

            // 票数順に整列された投票
            var orderedVotes = votedCounts.OrderByDescending(vote => vote.Value);
            // 最も票を得た人の票数
            var maxVoteNum = orderedVotes.FirstOrDefault().Value;
            // 最多票数のプレイヤー全員
            var mostVotedPlayers = votedCounts.Where(vote => vote.Value == maxVoteNum).Select(vote => vote.Key).ToArray();
            //最多投票以外のプレイヤー
            var NotmostVotedPlayers = votedCounts.Where(vote => vote.Value != maxVoteNum).Select(vote => vote.Key).ToArray();

            //タイブレ投票
            this.Tievotecount = Tievotecount;
            foreach (var pc in NotmostVotedPlayers)
            {//最多投票以外のプレイヤーはタイブレ投票0にする
                Tievotecount[pc] = 0;
            }

            // 票数順に整列された投票
            var TSe = Tievotecount.OrderByDescending(vote => vote.Value);
            // 最も票を得た人の票数
            var TCo = TSe.FirstOrDefault().Value;
            // 最多票数のプレイヤー全員
            var TMost = Tievotecount.Where(vote => vote.Value == TCo).Select(vote => vote.Key).ToArray();

            // 最多票数のプレイヤーが複数人いる場合
            if (mostVotedPlayers.Length > 1)
            {
                if (TMost.Length == 1)//タイブレ投票
                {
                    IsTie = false;
                    Exiled = GameData.Instance.GetPlayerById(TMost[0]);
                    logger.Info($"-タイブレ-最多得票者: {GetVoteName(TMost[0])}");
                }
                else
                {
                    IsTie = true;
                    Exiled = null;
                    logger.Info($"{string.Join(',', mostVotedPlayers.Select(id => GetVoteName(id)))} が同数");
                }
            }
            else
            {
                IsTie = false;
                Exiled = GameData.Instance.GetPlayerById(mostVotedPlayers[0]);
                logger.Info($"最多得票者: {GetVoteName(mostVotedPlayers[0])}");
            }

            var c = false;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.GetRoleClass()?.VotingResults(ref Exiled, ref IsTie, votedCounts, mostVotedPlayers, ClearAndExile) ?? false)
                    c = true; //どれかがtrueを返すと以下の特殊モードが実行されなくなる
            }

            // 同数投票時の特殊モード
            if (IsTie && Options.VoteMode.GetBool() && !c)
            {
                var tieMode = (TieMode)Options.WhenTie.GetValue();
                switch (tieMode)
                {
                    case TieMode.All:
                        Voteresult = "";

                        var toExile = mostVotedPlayers.Where(id => id != Skip).ToArray();
                        foreach (var playerId in toExile)
                        {
                            Utils.GetPlayerById(playerId)?.SetRealKiller(null);
                        }
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, toExile);
                        Voteresult = string.Join(',', mostVotedPlayers.Select(id => GetVoteName(id, true))) + Translator.GetString("fortuihou");
                        Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Vote]　" + Voteresult;

                        Exiled = null;
                        logger.Info("全員追放します");
                        break;
                    case TieMode.Random:
                        var exileId = mostVotedPlayers.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                        Exiled = GameData.Instance.GetPlayerById(exileId);
                        IsTie = false;
                        logger.Info($"ランダム追放: {GetVoteName(exileId)}");
                        var player = Utils.GetPlayerById(exileId);
                        Voteresult = Utils.GetPlayerColor(player) + Translator.GetString("fortuihou");
                        Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Vote]　" + Utils.GetPlayerColor(player) + Translator.GetString("fortuihou");

                        break;
                }
            }
        }
    }

    public const byte Skip = 253;
    public const byte NoVote = 254;
}
