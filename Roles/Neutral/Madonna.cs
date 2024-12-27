using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using System.Linq;
using TownOfHost.Attributes;

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
            35200,
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
        Limitd = true;
        Hangyaku = false;
        Wakarero = false;
    }
    private static OptionItem Optionlimit;
    private static OptionItem OptionLoverChenge;
    public static CustomRoles LoverChenge;
    public static OptionItem MadonnaLoverAddwin;
    public static OptionItem MaLoversSolowin3players;
    public float limit;
    bool Limitd;
    bool Hangyaku;
    bool Wakarero;
    enum Option
    {
        Madonnalimit,
        MadonnaFallChenge,
        LoversRoleAddwin,
        LoverSoloWin3players
    }

    [GameModuleInitializer]
    public static void Mareset()
    {
        Lovers.MaMadonnaLoversPlayers.Clear();
        Lovers.isMadonnaLoversDead = false;
    }
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Madmate,CustomRoles.Monochromer
    };
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        Optionlimit = FloatOptionItem.Create(RoleInfo, 10, Option.Madonnalimit, new(1f, 10f, 1f), 3f, false).SetValueFormat(OptionFormat.day);
        OptionLoverChenge = StringOptionItem.Create(RoleInfo, 11, Option.MadonnaFallChenge, cRolesString, 4, false);
        MadonnaLoverAddwin = BooleanOptionItem.Create(RoleInfo, 12, Option.LoversRoleAddwin, false, false);
        MaLoversSolowin3players = BooleanOptionItem.Create(RoleInfo, 13, Option.LoverSoloWin3players, false, false);
    }

    public override void Add()
        => AddS(Player);
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && Limitd)
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (!Player.Is(CustomRoles.MadonnaLovers) && Limitd && Is(voter))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Madoonna"), GetString("Vote.Madonna")) + GetString("VoteSkillMode"), Player.PlayerId);
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
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (target.Is(CustomRoles.OneLove))
        {
            Limitd = false;
            Hangyaku = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　相手が片思いで断わられた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadonnaOneLover"), Utils.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadonnaOneLoverNo"), Utils.GetPlayerColor(Player, true)), target.PlayerId);

            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadoonaFa"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)));
            target.RpcProtectedMurderPlayer();
        }
        else
        if (Lovers.OneLovePlayer.Ltarget == target.PlayerId)
        {
            Limitd = false;
            Hangyaku = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　視線が凄いからやめといた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadonnnaOneLovertarget"), Utils.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);

            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadoonaFa"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)));
        }
        else
        if (!target.IsRiaju())
        {
            Limitd = false;
            Logger.Info($"Player: {Player.name},Target: {target.name}", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaMyCollect"), Utils.GetPlayerColor(target, true)) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaCollect"), Utils.GetPlayerColor(Player, true)), target.PlayerId);
            target.RpcSetCustomRole(CustomRoles.MadonnaLovers);
            Player.RpcSetCustomRole(CustomRoles.MadonnaLovers);
            Lovers.MaMadonnaLoversPlayers.Add(Player);
            Lovers.MaMadonnaLoversPlayers.Add(target);
            RPC.SyncMadonnaLoversPlayers();
            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadonnaCo"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)));

            target.RpcProtectedMurderPlayer();
        }
        else
        {
            Limitd = false;
            Hangyaku = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　相手がラバーズなので断わられた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaMynotcollect"), Utils.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadonnaOneLoverNo"), Utils.GetPlayerColor(Player, true)), target.PlayerId);

            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadoonaFa"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)));
            target.RpcProtectedMurderPlayer();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Player.Is(CustomRoles.MadonnaLovers))
        {
            Wakarero = true;//リア充ならバクハフラグを立てる
        }
        else
        if (Player.IsAlive() && !Player.Is(CustomRoles.MadonnaLovers) && Wakarero)
        {//生きててラバーズ状態が解消されててる状態なら実行
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaHAMETU"), GetString($"{LoverChenge}")), Player.PlayerId);
            Wakarero = false;
            if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
            Player.RpcSetCustomRole(LoverChenge, true, log: true);
        }
        if (Hangyaku)
        {
            Hangyaku = false;
            Player.RpcSetCustomRole(LoverChenge, true, log: true);
        }
        else
        if (limit <= UtilsGameLog.day && Limitd && Player.IsAlive())
        {
            Player.RpcExileV2();
            MyState.SetDead();
            MyState.DeathReason = CustomDeathReason.Suicide;
            ReportDeadBodyPatch.Musisuruoniku[Player.PlayerId] = false;
            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("log.AM"), Utils.GetPlayerColor(PlayerCatch.GetPlayerById(Player.PlayerId)), UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)));
            Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}は指定ターン経過したため自殺。", "Madonna");
        }
    }
}