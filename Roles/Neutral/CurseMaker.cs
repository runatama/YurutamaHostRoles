using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;
public sealed class CurseMaker : RoleBase, IKiller, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CurseMaker),
            player => new CurseMaker(player),
            CustomRoles.CurseMaker,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Neutral,
            34300,
            SetupOptionItem,
            "Cm",
            "#554d59",
            true,
            introSound: () => GetIntroSound(RoleTypes.Phantom)
        );
    public CurseMaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        count = 0;
        Noroi.Clear();
        Norotteruto.Clear();
        CanWin = false;
        fall = false;
    }
    static OptionItem Distance;
    static OptionItem NoroiTime;
    static OptionItem OptionDelTarn;
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionShepeCooldown;

    static Dictionary<byte, float> Norotteruto = new();//呪おうとしてる人達。
    static Dictionary<byte, int> Noroi = new();
    public bool CanWin;
    float count;
    bool fall;
    public static HashSet<CurseMaker> curseMakers = new();
    enum OptionName
    {
        CueseMakerDelTarn,
        CueseMakerNoroiTime,
        CueseMakerDicstance
    }
    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionShepeCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionDelTarn = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CueseMakerDelTarn, new(1, 30, 1), 4, false);
        NoroiTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.CueseMakerNoroiTime, new(0f, 30f, 0.5f), 3f, false)
                .SetValueFormat(OptionFormat.Seconds);
        Distance = FloatOptionItem.Create(RoleInfo, 14, OptionName.CueseMakerDicstance, new(1f, 30f, 0.25f), 1.75f, false);
        Options.OverrideKilldistance.Create(RoleInfo, 15);
    }
    public override void Add()
    {
        curseMakers.Add(this);
    }
    public override void OnDestroy()
    {
        curseMakers.Clear();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        fall = false;
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;
        if (Noroi.ContainsKey(target.PlayerId)) return;

        Norotteruto.TryAdd(target.PlayerId, 0);
        Player.SetKillCooldown(target: target, delay: true);
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.4f, "CueseMaker");
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask)
        {
            if (Norotteruto.Count == 0) return;
            List<byte> del = new();
            foreach (var NR in Norotteruto)
            {
                var np = PlayerCatch.GetPlayerById(NR.Key);
                if (!np)
                {
                    del.Add(np.PlayerId);
                    fall = true;
                    continue;
                }
                if (!np.IsAlive())
                {
                    del.Add(np.PlayerId);
                    fall = true;
                    continue;
                }
                if (NoroiTime.GetFloat() <= NR.Value)//超えたなら消して追加
                {
                    Noroi.TryAdd(np.PlayerId, 0);
                    del.Add(np.PlayerId);
                    count++;
                    continue;
                }

                float dis;
                dis = Vector2.Distance(Player.transform.position, np.transform.position);//距離を出す
                if (dis <= Distance.GetFloat())//一定の距離にターゲットがいるならば時間をカウント
                    Norotteruto[NR.Key] += Time.fixedDeltaTime;
                else//それ以外は削除
                { del.Add(np.PlayerId); fall = true; }
            }
            if (del.Count != 0)
            {
                del.Do(x => Norotteruto.Remove(x));
                _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.4f, "CueseMaker");
                Player.SetKillCooldown(delay: true);
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl a, NetworkedPlayerInfo target)
    {
        CanWin = false;
        if (Noroi.Count == 0) return;
        List<byte> DelList = new();
        foreach (var nr in Noroi)
        {
            var np = PlayerCatch.GetPlayerById(nr.Key);
            if (!np) DelList.Add(nr.Key);
            if (!np.IsAlive()) DelList.Add(nr.Key);
            if (OptionDelTarn.GetInt() <= nr.Value + 1) DelList.Add(nr.Key);

            Noroi[nr.Key] = nr.Value + 1;
        }
        if (DelList.Count != 0)
            DelList.Do(x => Noroi.Remove(x));
    }
    public override string MeetingMeg()
    {
        if (count == 0) return "";
        if (!Player.IsAlive()) return "";

        return string.Format(Translator.GetString("CurseMakerMeetingMeg"), count);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public bool CanUseKillButton() => true;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = OptionShepeCooldown.GetFloat();
        opt.SetVision(false);
    }
    public float CalculateKillCooldown() => fall ? 0.00000000001f : OptionKillCoolDown.GetFloat();
    public void OnClick(ref bool resetkillcooldown, ref bool fall)
    {
        fall = true;
        if (!Player.IsAlive()) return;
        if (Noroi.Count == 0) return;
        resetkillcooldown = true;
        fall = false;

        Noroi.Add(Player.PlayerId, 0);
        foreach (var nr in Noroi)
        {
            var np = PlayerCatch.GetPlayerById(nr.Key);
            var st = PlayerState.GetByPlayerId(nr.Key);
            st.DeathReason = CustomDeathReason.Spell;
            CustomRoleManager.OnCheckMurder(Player, np, np, np, true, true);
        }

        CanWin = true;
        _ = new LateTask(() => CanWin = false, 2f, "ResetCanWin");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Noroi.ContainsKey(seen.PlayerId))
            return "<color=#554d59>†</color>";
        if (Norotteruto.ContainsKey(seen.PlayerId))
            return "<color=#554d59>◇</color>";
        return "";
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("WarlockCurseButtonText");
        return true;
    }
    public override string GetAbilityButtonText() => Translator.GetString("CurseMakerbooom");
    public bool OverrideKillButton(out string text)
    {
        text = "CurseMaker_Kill";
        return true;
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "CurseMaker_Ability";
        return true;
    }
}
