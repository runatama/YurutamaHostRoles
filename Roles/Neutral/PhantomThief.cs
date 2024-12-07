using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;
public sealed class PhantomThief : RoleBase, IKiller, IKillFlashSeeable, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(PhantomThief),
            player => new PhantomThief(player),
            CustomRoles.PhantomThief,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            35500,
            SetupOptionItem,
            "PT",
            "#3c1f56",
            true,
            introSound: () => GetIntroSound(RoleTypes.Phantom)
        );
    public PhantomThief(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Target = null;
        roletarget = byte.MaxValue;
        tagerole = CustomRoles.NotAssigned;
        MeetingNotice = false;
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionCantSetCount;
    static OptionItem OptionTandokuWin;
    static OptionItem OptionNotice;
    static OptionItem OptionNoticetype;
    byte roletarget;
    CustomRoles tagerole;
    PlayerControl Target;
    bool MeetingNotice;
    enum OptionName
    {
        PhantomThiefFarstCoolDown,
        PhantomThiefCantSetCount,
        PhantomThiefTandokuWin,
        PhantomThiefNotice,
        PhantomThiefNoticeType
    }
    enum Notice
    {
        NoticeNone, //なし
        NoticeTeam,//陣営のみ
        NoticePlayer,//個人
    }
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.PhantomThiefFarstCoolDown, new(0f, 180f, 0.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionCantSetCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.PhantomThiefCantSetCount, new(1, 15, 1), 8, false).SetValueFormat(OptionFormat.Players);
        OptionNotice = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PhantomThiefNotice, true, false);
        OptionNoticetype = StringOptionItem.Create(RoleInfo, 14, OptionName.PhantomThiefNoticeType, EnumHelper.GetAllNames<Notice>(), 1, false, OptionNotice);
        OptionTandokuWin = BooleanOptionItem.Create(RoleInfo, 12, OptionName.PhantomThiefTandokuWin, false, false);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (roletarget == byte.MaxValue) return;

        if ((PlayerCatch.GetPlayerById(roletarget)?.IsAlive() ?? false) && player != Target) return;

        roletarget = byte.MaxValue;
        Player.KillFlash();
        MeetingNotice = false;
        Player.SetKillCooldown();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.2f, "PhantomThief Target");
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        Target = null;
        roletarget = byte.MaxValue;
        tagerole = CustomRoles.NotAssigned;
        MeetingNotice = false;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        info.DoKill = false;

        if (OptionCantSetCount.GetFloat() > PlayerCatch.AllAlivePlayerControls.Count()) return;
        if (roletarget != byte.MaxValue) return;

        killer.ResetKillCooldown();
        roletarget = target.PlayerId;
        Target = target;
        tagerole = target.GetCustomRole();
        MeetingNotice = true;
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.2f, "PhantomThief Target");
        killer.SetKillCooldown(target: target, delay: true);
        return;
    }
    public bool CanUseKillButton() => true;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen == seer && seer == Player)
        {
            if (!seer.IsAlive()) return "";
            var notage = "<size=60%>" + GetString("PhantomThieftarget") + "</size>";
            var akiramero = "<size=60%>" + GetString("PhantomThiefakiarmero") + "</size>";
            if (roletarget == byte.MaxValue)
                if (OptionCantSetCount.GetFloat() > PlayerCatch.AllAlivePlayerControls.Count())
                {
                    return isForHud ? akiramero.RemoveSizeTags() : akiramero;
                }
                else return isForHud ? notage.RemoveSizeTags() : notage;

            var hehhehhe = "<size=60%>" + Utils.GetPlayerColor(roletarget) + Translator.GetString("PhantomThiefwoitadakuze") + "</size>";

            return isForHud ? hehhehhe.RemoveSizeTags() : hehhehhe;
        }
        return "";
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        seen ??= Player;
        if (seen.PlayerId == roletarget)
        {
            enabled = true;
            addon = true;
        }
    }
    public override void OnStartMeeting()
    {
        if (!MeetingNotice) return;
        MeetingNotice = false;
        if (!OptionNotice.GetBool()) return;
        if (!Target.IsAlive()) return;
        if (!Player.IsAlive()) return;
        var sendmeg = "";

        switch ((Notice)OptionNoticetype.GetValue())
        {
            case Notice.NoticeNone:
                sendmeg = GetString("PhantomThiefNoticeemail0");
                break;
            case Notice.NoticeTeam:
                var team = Target.GetCustomRole().GetCustomRoleTypes();
                if (Target.Is(CustomRoles.Amanojaku) || Target.IsRiaju()) team = CustomRoleTypes.Neutral;
                if (team == CustomRoleTypes.Madmate) team = CustomRoleTypes.Impostor;
                Color color = team is CustomRoleTypes.Crewmate ? Palette.Blue : (team is CustomRoleTypes.Impostor ? ModColors.ImpostorRed : ModColors.NeutralGray);

                sendmeg = string.Format(GetString("PhantomThiefNoticeTeam0"), Utils.ColorString(color, $"<u>{GetString($"PT.{team}")}</u>"));
                break;
            case Notice.NoticePlayer:
                var colorid = Target.Data.DefaultOutfit.ColorId;
                var playername = Utils.ColorString(Palette.PlayerColors[colorid], $"<u>{GetString($"PhantomThiefmeg{colorid}")}</u>");
                sendmeg = string.Format(GetString("PhantomThiefNoticePlayer0"), playername);
                break;
        }
        if (sendmeg.RemoveHtmlTags() != "") _ = new LateTask(() => Utils.SendMessage(sendmeg, title: GetString("PhantomThiefTitle").Color(UtilsRoleText.GetRoleColor(CustomRoles.PhantomThief))), 5f, "SendPhantom", true);
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("PhantomThiefButtonText");
        return true;
    }
    public bool OverrideKillButton(out string text)
    {
        text = "PhantomThief_Kill";
        return true;
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public bool CheckKillFlash(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        return target.PlayerId == roletarget;
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (roletarget == byte.MaxValue || !Player.IsAlive()) return false;
        if (CustomWinnerHolder.WinnerIds.Contains(roletarget))
        {
            var Targetrole = Target.GetCustomRole();
            if (Targetrole != tagerole && Targetrole != CustomRoles.NotAssigned) tagerole = Targetrole;
            Player.RpcSetCustomRole(tagerole, log: null);
            Target.RpcSetCustomRole(CustomRoles.Emptiness, log: null);
            CustomWinnerHolder.WinnerIds.Remove(roletarget);
            UtilsGameLog.AddGameLog($"PhantomThief", string.Format(Translator.GetString("Log.PhantomThief"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(roletarget, true)));
            if (OptionTandokuWin.GetBool())
            {
                CustomWinnerHolder.ResetAndSetWinner((CustomWinner)CustomRoles.PhantomThief);
                CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
                return false;
            }
            return true;
        }
        return false;
    }
}