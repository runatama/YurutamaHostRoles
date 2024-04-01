using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;
using System.Linq;
using TownOfHost.Attributes;
using TownOfHost.Roles.Madmate;

namespace TownOfHost.Roles.Neutral;
public sealed class Madonna : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Madonna),
            player => new Madonna(player),
            CustomRoles.Madonna,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            52000,
            SetupOptionItem,
            "Ma",
            "#f09199",
            introSound: () => GetIntroSound(RoleTypes.Scientist),
            assignInfo: new RoleAssignInfo(CustomRoles.Madonna, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public Madonna(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        limit = Optionlimit.GetFloat();
        LoverChenge = ChangeRoles[OptionLoverChenge.GetValue()];
        limitcount = 0;
        Limitd = true;
        Hangyaku = false;
        Wakarero = false;
    }
    private static OptionItem Optionlimit;
    private static OptionItem OptionLoverChenge;
    public static CustomRoles LoverChenge;
    public float limit;
    int limitcount;
    bool Limitd;
    bool Hangyaku;
    bool Wakarero;
    enum Option
    {
        limit,
        LoverChenge
    }

    [GameModuleInitializer]
    public static void Mareset()
    {
        Main.MaMaLoversPlayers.Clear();
        Main.isMaLoversDead = false;
    }
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Madmate
    };
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        Optionlimit = FloatOptionItem.Create(RoleInfo, 10, Option.limit, new(1f, 10f, 1f), 3f, false);
        OptionLoverChenge = StringOptionItem.Create(RoleInfo, 11, Option.LoverChenge, cRolesString, 4, false);
    }

    public override void Add()
        => AddS(Player);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (!Player.Is(CustomRoles.MaLovers) && Limitd)
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage("告白モードになりました！\n\n告白するプレイヤーに投票→告白\n" + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    MadonnaL(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    public void MadonnaL(byte votedForId)
    {
        var target = Utils.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (!target.Is(CustomRoles.ALovers) && !target.Is(CustomRoles.BLovers) && !target.Is(CustomRoles.CLovers) && !target.Is(CustomRoles.DLovers) &&
            !target.Is(CustomRoles.ELovers) && !target.Is(CustomRoles.FLovers) && !target.Is(CustomRoles.GLovers))
        {
            Limitd = false;
            Logger.Info($"Player: {Player.name},Target: {target.name}", "Madonna");
            Utils.SendMessage(Utils.GetPlayerColor(target, true) + "に告白しました。\n彼とマドンナラバーズになりました!\n" + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(Utils.GetPlayerColor(Player, true) + "から告白された...\n" + Player.name + "と\nマドンナラバーズになりました!", target.PlayerId);
            target.RpcSetCustomRole(CustomRoles.MaLovers);
            Player.RpcSetCustomRole(CustomRoles.MaLovers);
            Main.MaMaLoversPlayers.Add(Player);
            Main.MaMaLoversPlayers.Add(target);
            target.RpcProtectedMurderPlayer();
        }
        else
        {
            Limitd = false;
            Hangyaku = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　相手がラバーズなので断わられた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(Utils.GetPlayerColor(target, true) + $"に告白しました。\n彼には彼女がいるので断られました...\n次ターンからあなたのロールは{GetString($"{LoverChenge}")}になります。\n" + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(Utils.GetPlayerColor(Player, true) + "から告白された...\nが君には大切な彼女がいるため断りました。", target.PlayerId);
            target.RpcProtectedMurderPlayer();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (Player.Is(CustomRoles.MaLovers))
        {
            Wakarero = true;//リア充ならバクハフラグを立てる
        }
        else
        if (Player.IsAlive() && !Player.Is(CustomRoles.MaLovers) && Wakarero)
        {//生きててラバーズ状態が解消されててる状態なら実行
            Utils.SendMessage($"君の大事な彼ピッピに別れを告げられた(相手が回線切断しました。)\nあなたのロールは{GetString($"{LoverChenge}")}になります。", Player.PlayerId);
            Player.RpcSetCustomRole(LoverChenge);
            Wakarero = false;
        }
        if (Hangyaku)
        {
            Hangyaku = false;
            Player.RpcSetCustomRole(LoverChenge);
        }
        else
        if (limit <= limitcount && Limitd && Player.IsAlive())
        {
            limitcount = -1;//0の時に永遠と死なれちゃ困るで
            PlayerState state = PlayerState.GetByPlayerId(Player.PlayerId);
            PlayerState.GetByPlayerId(Player.PlayerId);
            Player.RpcExileV2();
            state.SetDead();
            state.DeathReason = CustomDeathReason.Suicide;
            Logger.Info($"{Player.GetNameWithRole()}は指定ターン経過したため自殺。", "Madonna");
        }
        else
        {
            limitcount += 1;
        }
    }
}