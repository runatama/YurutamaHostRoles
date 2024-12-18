using AmongUs.GameOptions;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class GrimReaper : RoleBase, ILNKiller//死神はにゃんこを仲間にできない
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(GrimReaper),
                player => new GrimReaper(player),
                CustomRoles.GrimReaper,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                32200,
                SetupOptionItem,
                "GR",
                "#4b0082",
                true,
                countType: CountTypes.GrimReaper,//こいつ生存カウント分ける。(生存カウント入れないため)
                assignInfo: new RoleAssignInfo(CustomRoles.GrimReaper, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public GrimReaper(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            GrimReaperCanButtom = OptionGrimReaperCanButtom.GetBool();
            cankill = true;
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionGrimReaperCanButtom;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool GrimReaperCanButtom;
        bool cankill;
        enum OptionName
        {
            GrimReaperCanButtom,
        }

        Dictionary<byte, float> GrimPlayers = new(14);
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);//正味サボ使用不可でもいい気がする
            OptionGrimReaperCanButtom = BooleanOptionItem.Create(RoleInfo, 14, OptionName.GrimReaperCanButtom, false, false);
            RoleAddAddons.Create(RoleInfo, 15);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            {
                info.DoKill = false;

                if (!info.IsSuicide)
                {
                    (var kille, var taret) = info.AttemptTuple;
                    {
                        Logger.Info($"{kille?.Data?.PlayerName}:キル", "GrimReaper");
                        Main.AllPlayerKillCooldown[kille.PlayerId] = 999;
                        kille.SyncSettings();//もう君はキルできないよ...!
                    }
                }

                var (killer, target) = info.AttemptTuple;

                if (info.IsFakeSuicide) return;

                if (target.Is(CustomRoles.King))
                {
                    info.DoKill = false;
                    return;
                }

                if (cankill)
                    if (!GrimPlayers.ContainsKey(target.PlayerId))
                    {
                        killer.SetKillCooldown(delay: true, kousin: false);
                        GrimPlayers.Add(target.PlayerId, 0f);
                        cankill = false;
                    }
            }
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo __)
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            foreach (var targetId in GrimPlayers.Keys)
            {
                var target = PlayerCatch.GetPlayerById(targetId);
                KillBitten(target, true);
                if (repo == target)
                {
                    ReportDeadBodyPatch.DieCheckReport(repo, __);
                }
            }
            GrimPlayers.Clear();
        }
        public override void AfterMeetingTasks()
        {
            cankill = true;
            if (Player.Is(CustomRoles.Amnesia) && AddOns.Common.Amnesia.defaultKillCool.GetBool()) return;
            Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
            Player.SyncSettings();
        }
        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            var Grim = Player;
            if (target.IsAlive())
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Grim;
                target.SetRealKiller(Grim);
                CustomRoleManager.OnCheckMurder(Grim, target, target, target, true, true);
                if (!isButton && Grim.IsAlive()) RPC.PlaySoundRPC(Grim.PlayerId, Sounds.KillSound);
            }
        }
        public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reportreson)
        {
            if (reporter.Is(CustomRoles.GrimReaper) && target != null)//死体通報はデフォでさせない。
            {
                reportreson = DontReportreson.Other;
                return true;
            }
            if (reporter.Is(CustomRoles.GrimReaper) && target == null && !GrimReaperCanButtom)//ボタン使用不可でボタンであろう場面のみ
            {
                reportreson = DontReportreson.Other;
                return true;
            }
            return false;
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("WarlockCurseButtonText");
            return true;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Grim_Kill";
            return true;
        }
    }
}