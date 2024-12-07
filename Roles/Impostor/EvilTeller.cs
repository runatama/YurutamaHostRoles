using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilTeller : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilTeller),
            player => new EvilTeller(player),
            CustomRoles.EvilTeller,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            4500,
            SetUpOptionItem,
            "Et"
        );
    public EvilTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Tellnow.Clear();
        seentarget.Clear();
        nowuse = false;
        fall = false;
        usekillcool = optusekillcoool.GetBool();
        cooldown = optcooldown.GetFloat();
        killcooldown = optkillcooldown.GetFloat();
        telltime = opttelltime.GetFloat();
        distance = optDistance.GetFloat();
        tellroleteam = opttellroleteam.GetBool();
        tellrole = opttellrole.GetBool();
    }
    static OptionItem optcooldown;
    static OptionItem optkillcooldown;
    static OptionItem opttelltime;
    static OptionItem optDistance;
    static OptionItem opttellroleteam;
    static OptionItem opttellrole;
    static OptionItem optusekillcoool;
    static float cooldown;
    static float killcooldown;
    static float telltime;
    static float distance;
    static bool tellroleteam;
    static bool tellrole;
    static bool usekillcool;
    static Dictionary<byte, float> Tellnow = new();
    bool nowuse;
    bool fall;
    static Dictionary<byte, CustomRoles> seentarget = new();
    enum OptionName { EvilTellerTellTime, EvilTellerDistance, EvilTellertellrole }
    static void SetUpOptionItem()
    {
        optkillcooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optcooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        opttelltime = FloatOptionItem.Create(RoleInfo, 12, OptionName.EvilTellerTellTime, new(0, 100, 0.5f), 5, false).SetValueFormat(OptionFormat.Seconds);
        optDistance = FloatOptionItem.Create(RoleInfo, 13, OptionName.EvilTellerDistance, new(1f, 30f, 0.25f), 1.75f, false);
        opttellroleteam = BooleanOptionItem.Create(RoleInfo, 14, "tRole", false, false);
        opttellrole = BooleanOptionItem.Create(RoleInfo, 15, OptionName.EvilTellertellrole, false, false);
        optusekillcoool = BooleanOptionItem.Create(RoleInfo, 16, "OptionSetKillcooldown", false, false);
    }
    public float CalculateKillCooldown() => killcooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = fall ? 1 : (nowuse ? telltime : cooldown);
    public void OnClick(ref bool resetkillcooldown, ref bool falla)
    {
        resetkillcooldown = true;
        falla = true;
        var target = Player.GetKillTarget();
        if (target == null) { fall = true; return; }
        if (target.Is(CustomRoleTypes.Impostor)) { fall = true; return; }

        if (seentarget.ContainsKey(target.PlayerId)) { fall = true; return; }
        Tellnow.TryAdd(target.PlayerId, 0);
        nowuse = true;
        fall = false;
        _ = new LateTask(() =>
        {
            Player.RpcResetAbilityCooldown(kousin: true);
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }, 0.2f, "", true);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!seer.IsAlive()) return "";

        if (Tellnow.ContainsKey(seen.PlayerId)) return "<color=#ff1919>◆</color>";
        return "";
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) { fall = false; Tellnow.Clear(); nowuse = false; }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (!seen) return;
        if (!Player.IsAlive()) return;

        if (seentarget.TryGetValue(seen.PlayerId, out var role))
        {
            enabled = true;
            addon = false;
            if (tellrole) role = seen.GetCustomRole();
            if (!tellroleteam)
            {
                switch (seen.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                    case CustomRoleTypes.Madmate:
                        roleColor = Palette.CrewmateBlue;
                        roleText = GetString("Crewmate");
                        break;
                    case CustomRoleTypes.Impostor:
                        roleColor = ModColors.ImpostorRed;
                        roleText = GetString("Impostor");
                        break;
                    case CustomRoleTypes.Neutral:
                        roleColor = ModColors.NeutralGray;
                        roleText = GetString("Neutral");
                        break;
                }
            }
            roleText = GetString($"{role}");
            roleColor = UtilsRoleText.GetRoleColor(role);
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask)
        {
            if (Tellnow.Count == 0) return;
            List<byte> del = new();
            foreach (var data in Tellnow)
            {
                var target = PlayerCatch.GetPlayerById(data.Key);
                if (!target)
                {
                    del.Add(target.PlayerId);
                    fall = true;
                    continue;
                }
                if (!target.IsAlive())
                {
                    del.Add(target.PlayerId);
                    fall = true;
                    continue;
                }
                if (telltime <= data.Value)//超えたなら消して追加
                {
                    fall = false;
                    seentarget.TryAdd(target.PlayerId, target.GetCustomRole());
                    del.Add(target.PlayerId);
                    continue;
                }

                float dis;
                dis = Vector2.Distance(Player.transform.position, target.transform.position);//距離を出す
                if (dis <= distance)//一定の距離にターゲットがいるならば時間をカウント
                    Tellnow[data.Key] += Time.fixedDeltaTime;
                else//それ以外は削除
                { del.Add(target.PlayerId); fall = true; }
            }
            if (del.Count != 0)
            {
                nowuse = false;
                del.ForEach(task => Tellnow.Remove(task));
                _ = new LateTask(() =>
                {
                    Player.RpcResetAbilityCooldown(kousin: true);
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                    if (usekillcool && !fall) Player.SetKillCooldown();
                }, 0.2f, "", true);
            }
        }
    }
}