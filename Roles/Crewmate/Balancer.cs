using AmongUs.GameOptions;
using System;
using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;

using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Modules.MeetingVoteManager;
using static TownOfHost.Modules.MeetingTimeManager;

namespace TownOfHost.Roles.Crewmate;
public sealed class Balancer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Balancer),
            player => new Balancer(player),
            CustomRoles.Balancer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22215,
            SetupOptionItem,
            "bal",
            "#cff100",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Balancer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        meetingtime = OptionMeetingTime.GetInt();
        target1 = 255;
        target2 = 255;
        used = false;
        BalancerChecker.Balancer = 255;
        nickname = null;
    }

    static OptionItem OptionMeetingTime;

    public static byte target1, target2;
    static bool used;
    public static int meetingtime;
    static string nickname;

    enum Option
    {
        meetingtime
    }

    private static void SetupOptionItem()
    {
        OptionMeetingTime = IntegerOptionItem.Create(RoleInfo, 10, Option.meetingtime, new(15, 120, 1), 30, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
        => AddS(Player);
    public override void OnDestroy()
        => BalancerChecker.Balancer = 255;

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        //誰かが天秤を発動していて、自分ではないなら実行しない
        if (BalancerChecker.Balancer is not 255 && BalancerChecker.Balancer != Player.PlayerId) return true;
        //発動してるなら～
        if (BalancerChecker.Balancer is not 255)
        {
            //投票先が天秤のターゲットではないなら投票しない
            if (votedForId == target1 || votedForId == target2)
                return true;
            return false;
        }

        //通常会議の処理 投票した人が自分ではない or 能力使用済みならここから先は実行しない
        if (voter.PlayerId != Player.PlayerId || used)
            return true;

        //天秤モードかチェック
        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                //ターゲットの情報をリセット
                target1 = 255;
                target2 = 255;
                Utils.SendMessage("天秤モードになりました！\n天秤に掛けたいプレイヤー2人に投票する\nスキップでキャンセル、\nもう一度自投票することで自身に票が入る", Player.PlayerId);
            }
            if (status is VoteStatus.Skip)
            {
                SetMode(Player, false);
                Utils.SendMessage("天秤モードをキャンセルしました", Player.PlayerId);
            }
            //選ぶ処理
            if (status is VoteStatus.Vote)
            {
                Vote();
            }
            return false;
        }
        else
        {
            if (votedForId == Player.PlayerId && ((target1 != 255 && target2 == 255) || (target1 == 255 && target2 != 255)))
            {
                Vote();
                return false;
            }
        }
        return true;

        void Vote()
        {
            //1一目が決まってないなら一人目を決める
            if (target1 == 255)
                target1 = votedForId;
            //二人目が決まってないなら二人目を決める
            else if (target2 == 255)
                target2 = votedForId;

            //同じ人なら二人目をリセット
            if (target1 == target2)
                target2 = 255;

            //プレイヤーの状態を取得
            var p1 = Utils.GetPlayerById(target1);
            var p2 = Utils.GetPlayerById(target2);

            //切断or死んでいるならリセット
            if (!p1.IsAlive())
                target1 = 255;
            if (!p2.IsAlive())
                target2 = 255;

            //どちらかの情報があるならチャットで伝える
            if (target1 != 255 || target2 != 255)
            {
                //どちらかが決まっていなかったら一人目
                var n = (target1 != 255 && target2 != 255) ? "二人目を" : "一人目を";
                Utils.SendMessage($"{n}{Main.AllPlayerNames[votedForId]}にしました", Player.PlayerId);
            }

            //二人決まったなら会議を終了
            if (target1 != 255 && target2 != 255)
            {
                used = true;
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;
                MeetingHud.Instance.RpcClose();
            }
        }
    }

    public override bool VotingResults(ref GameData.PlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        //天秤モードじゃないor自分の天秤じゃないなら実行しない
        if (BalancerChecker.Balancer != Player.PlayerId) return false;

        //ディクテーターなどの強制的に会議を終わらせるものなら生存確認の処理スキップ
        if (!ClearAndExile)
        {
            var d1 = Utils.GetPlayerById(target1);
            var d2 = Utils.GetPlayerById(target2);

            //二人とも切断or死んでいるなら同数
            if (!d1.IsAlive() && !d2.IsAlive())
            {
                IsTie = true;
                Exiled = null;
                return true;
            }
            IsTie = false;
            //チェック
            if (!d1.IsAlive())
            {
                Exiled = d2.Data;
                return true;
            }
            if (!d2.IsAlive())
            {
                Exiled = d1.Data;
                return true;
            }
        }

        var rand = new Random();
        Dictionary<byte, int> data = new(2)
        {
            //セット
            { target1, 0 },
            { target2, 0 }
        };

        //投票をカウント、投票してない場合はどちらかに投票させる
        foreach (var voteData in Instance.AllVotes)
        {
            var voted = voteData.Value;

            //死んでたらスキップ
            if (!Utils.GetPlayerById(voted.Voter).IsAlive()) continue;

            //ディクテーターなどの強制的に会議を終わらせるものではないならランダム投票
            if (!voted.HasVoted && !ClearAndExile)
            {
                var id = rand.Next(0, 2) is 0 ? target1 : target2;
                Instance.SetVote(voted.Voter, id, isIntentional: false);
                data[id] += 1;
            }
            else if (voted.VotedFor is not NoVote) //投票なし(死亡時、会議強制終了時など)の人はスキップ
            {
                data[voted.VotedFor] += voted.NumVotes;
            }
        }
        //暗転対策の追放リセット
        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;

        //ランダムで追放者を決める 同数ならどちらも追放
        var exileId = data.Where(kv => kv.Value == data.Values.Max())
                        .Select(kv => kv.Key)
                        .OrderBy(x => Guid.NewGuid())
                        .FirstOrDefault();
        Exiled = GameData.Instance.GetPlayerById(exileId);
        if (data.Values.Distinct().Count() == 1)
        {
            //追放画面が出てくるちょい前に名前を変える
            _ = new LateTask(() =>
            {
                //ホストなら別の処理
                if (exileId is 0)
                {
                    nickname = Main.nickName;
                    Main.nickName = "どちらも追放された。<size=0>";
                }
                else
                    Utils.GetPlayerById(exileId).RpcSetName("どちらも追放された。<size=0>");
            }, 4f, "dotiramotuihou☆");
            var toExile = data.Keys.ToArray();
            foreach (var playerId in toExile)
            {
                Utils.GetPlayerById(playerId)?.SetRealKiller(null);
            }
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, toExile);
        }
        return true;
    }

    public override void AfterMeetingTasks()
    {
        //天秤会議になってない状態なら
        if (BalancerChecker.Balancer == 255 && target1 is not 255 && target2 is not 255)
        {
            //天秤会議にする
            BalancerChecker.Balancer = Player.PlayerId;
            //対象の名前を天秤の色に
            foreach (var pc in Main.AllPlayerControls.Where(pc => pc.PlayerId == target1 || pc.PlayerId == target2))
                pc.RpcSetName("<color=red>★" + Utils.ColorString(RoleInfo.RoleColor, Main.AllPlayerNames[pc.PlayerId]) + "<color=red>★");
            Balancer(meetingtime);
            PlayerControl.LocalPlayer.NoCheckStartMeeting(PlayerControl.LocalPlayer.Data);
            //アナウンス
            Utils.SendMessage($"{Main.AllPlayerNames[target1]}と{Main.AllPlayerNames[target2]}が天秤に掛けられました！\n\nどちらかに投票せよ！");

            _ = new LateTask(() =>
            {
                //名前を戻す
                Utils.GetPlayerById(target1).RpcSetName(Main.AllPlayerNames[target1]);
                Utils.GetPlayerById(target2).RpcSetName(Main.AllPlayerNames[target2]);
            }, 0.5f);

            return;
        }
        //自分の天秤会議じゃないなら実行しない
        else if (BalancerChecker.Balancer != Player.PlayerId)
            return;

        //名前を戻す
        Utils.GetPlayerById(target1).RpcSetName(Main.AllPlayerNames[target1]);
        Utils.GetPlayerById(target2).RpcSetName(Main.AllPlayerNames[target2]);

        if (nickname != null)
            Main.nickName = nickname;
        nickname = null;

        //名前にロールとかのを適用
        _ = new LateTask(() => Utils.NotifyRoles(isForMeeting: false, ForceLoop: true, NoCache: true), 0.2f);

        //リセット
        BalancerChecker.Balancer = 255;
        target1 = 255;
        target2 = 255;
    }
}

//天秤会議かチェックする
class BalancerChecker
{
    public static byte Balancer = 255;
}