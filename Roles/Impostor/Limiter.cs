using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Limiter : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Limiter),
                player => new Limiter(player),
                CustomRoles.Limiter,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                1404,
                SetupOptionItem,
                "Lm"
            );
        public Limiter(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            LimiterTarnLimit = OptionLimiterTarnLimit.GetFloat();
            blastrange = Optionblastrange.GetFloat();
            KillCooldown = OptionKillCooldown.GetFloat();
        }

        static OptionItem OptionLimiterTarnLimit;
        static OptionItem OptionLastTarnKillcool;
        static OptionItem Optionblastrange;
        static OptionItem OptionKillCooldown;
        enum OptionName
        {
            LimiterTarnLimit,
            LimiterLastTarnKillCool,
            blastrange,
        }

        float LimiterTarnLimit;
        float blastrange;
        float KillCooldown;
        bool Limit;

        public bool CanBeLastImpostor { get; } = false;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLastTarnKillcool = FloatOptionItem.Create(RoleInfo, 10, OptionName.LimiterLastTarnKillCool, new(0f, 180f, 2.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLimiterTarnLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.LimiterTarnLimit, new(1f, 5f, 1f), 3f, false).SetValueFormat(OptionFormat.day);
            Optionblastrange = FloatOptionItem.Create(RoleInfo, 12, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var Targets = new List<PlayerControl>(Main.AllAlivePlayerControls);//.Where(pc => !Player)
            foreach (var tage in Targets)
                if (Limit)
                {
                    info.DoKill = false;
                    var distance = Vector3.Distance(Player.transform.position, tage.transform.position);
                    if (distance > blastrange) continue;
                    PlayerState.GetByPlayerId(tage.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    tage.SetRealKiller(tage);
                    tage.RpcMurderPlayer(tage, true);
                    RPC.PlaySoundRPC(tage.PlayerId, Sounds.KillSound);
                }
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("FireWorksBomberExplosionButtonText");
            return Limit;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Limiter_Kill";
            return Limit;
        }

        public override void AfterMeetingTasks()//一旦はアムネシア中なら回避してるけどリミッターは削除してあげてもいいかも
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;

            if (Main.day >= LimiterTarnLimit && Player.IsAlive())
            {
                Limit = true;
                _ = new LateTask(() => Player.SetKillCooldown(OptionLastTarnKillcool.GetFloat()), 5f, "Limiter Limit Kill cool");
            }
        }
        public override string GetProgressText(bool comms = false)
        {
            if (Limit && Player.IsAlive())
            {
                return Utils.ColorString(Color.red, "\n" + GetString("LimiterBom"));
            }
            else
                return Utils.ColorString(Color.red, "");
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo __)
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            if (Limit && Player.IsAlive())
            {
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Bombed;
                Player.SetRealKiller(Player);
                Player.RpcMurderPlayer(Player, true);
                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
            }
            if (Limit && repo == Player)
            {
                ReportDeadBodyPatch.DieCheckReport(Player, __);
            }
        }
    }
}
