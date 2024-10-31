using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class Dictator : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Dictator),
            player => new Dictator(player),
            CustomRoles.Dictator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            18700,
            SetupOptionItem,
            "dic",
            "#df9b00",
            from: From.TownOfHost
        );
    public Dictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    enum OptionName
    {
        DictatorSelfVote
    }
    static void SetupOptionItem()
    {
        OptionSelfVote = BooleanOptionItem.Create(RoleInfo, 10, OptionName.DictatorSelfVote, false, false);
    }
    public override void Add()
        => AddS(Player);
    static OptionItem OptionSelfVote;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (!OptionSelfVote.GetBool()) return true;

        if (Is(voter))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Dictator"), GetString("Vote.Dictator")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                {
                    MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
                    PlayerCatch.GetPlayerById(votedForId).SetRealKiller(Player);
                    MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, votedForId);
                    UtilsGameLog.AddGameLog($"Dictator", string.Format(GetString("Dictator.log"), Utils.GetPlayerColor(Player)));
                }
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }

        return true;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || !Canuseability() || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        if (!OptionSelfVote.GetBool())
        {
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            PlayerCatch.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
            UtilsGameLog.AddGameLog($"Dictator", string.Format(Translator.GetString("Dictator.log"), Utils.GetPlayerColor(Player)));
        }
        return (votedForId, numVotes, false);
    }
}
