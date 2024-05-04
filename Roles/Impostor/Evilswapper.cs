/*using AmongUs.GameOptions;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;
using static TownOfHost.Modules.SelfVoteManager;
using TownOfHost.Roles.Core.Interfaces;


namespace TownOfHost.Roles.Impostor;
public sealed class Evilswapper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Evilswapper),
                player => new Evilswapper(player),
                CustomRoles.Evilswapper,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                13000,
                null,
                "es"
            //from: From.SuperNewRoles
            );
    public Evilswapper(PlayerControl player)
    : base(
    RoleInfo,
    player
    )
    {
        target1 = 255;
        target2 = 255;
        Target1 = 255;
        Target2 = 255;
    }
    public static byte target1 = 255, target2 = 255;
    public static byte Id = 255;
    byte Target1, Target2;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                //ターゲットの情報をリセット
                Target1 = 255;
                Target2 = 255;
                Utils.SendMessage("自己投票モードが有効です。", Player.PlayerId);
            }
            if (status is VoteStatus.Skip)
            {
                SetMode(Player, false);
                Utils.SendMessage("投票スキル終了", Player.PlayerId);
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
            if (votedForId == Player.PlayerId && ((Target1 != 255 && Target2 == 255) || (Target1 == 255 && Target2 != 255)))
            {
                Vote();
                return false;
            }
        }
        return true;

        void Vote()
        {
            //1一目が決まってないなら一人目を決める
            if (Target1 == 255)
                Target1 = votedForId;
            //二人目が決まってないなら二人目を決める
            else if (Target2 == 255)
                Target2 = votedForId;

            //同じ人なら二人目をリセット
            if (Target1 == Target2)
                Target2 = 255;

            //プレイヤーの状態を取得
            var p1 = Utils.GetPlayerById(Target1);
            var p2 = Utils.GetPlayerById(Target2);

            //切断or死んでいるならリセット
            if (!p1.IsAlive())
                Target1 = 255;
            if (!p2.IsAlive())
                Target2 = 255;

            //どちらかの情報があるならチャットで伝える
            if (Target1 != 255 || Target2 != 255)
            {
                //どちらかが決まっていなかったら一人目
                var n = (Target1 != 255 && Target2 != 255) ? "2人目" : "1人目";
                var s = string.Format("バランサースキル：{0}、{1}", n, Utils.GetPlayerColor(Utils.GetPlayerById(votedForId), true));
                Utils.SendMessage(s.ToString(), Player.PlayerId);
            }
            // 自投票してから2人に投票した人の票数を入れ替える処理
            if (Target1 != 255 && Target2 != 255)
            {
                if (votedForId == Player.PlayerId && Target1 != 255 && Target2 != 255)
                {
                    var temp = Target1;
                    Target1 = Target2;
                    Target2 = temp;
                    Utils.SendMessage("2人に投票した人の票数を入れ替えました。", Player.PlayerId);
                }
            }
        }

    }
}*/