using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Jackal : RoleBase, ILNKiller, ISchrodingerCatOwner, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Jackal),
                player => new Jackal(player),
                CustomRoles.Jackal,
                () => CanmakeSK.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                30000,
                SetupOptionItem,
                "jac",
                "#00b4eb",
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.Jackal, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                },
                from: From.TheOtherRoles
            );
        public Jackal(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            Cooldown = OptionCooldown.GetFloat();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            SK = CanmakeSK.GetBool();
            Fall = false;
        }

        public static OptionItem OptionKillCooldown;
        private static OptionItem OptionCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        static OptionItem CanmakeSK;
        static OptionItem CanImpSK;
        //サイドキックが元仲間の色を見える
        public static OptionItem SKcanImp;
        //元仲間impがサイドキック相手の名前の色を見える
        public static OptionItem SKimpwocanimp;
        public static OptionItem OptionDoll;
        private static float KillCooldown;
        private static float Cooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        bool SK;
        bool Fall;

        enum opt { JackaldollShoukaku, JackaldollCanimp, JackalbeforeImpCanSeeImp, Jackaldollimpgaimpnimieru }

        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            CanmakeSK = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.CanCreateSideKick, true, false);
            CanImpSK = BooleanOptionItem.Create(RoleInfo, 15, opt.JackaldollCanimp, false, false, CanmakeSK);
            SKcanImp = BooleanOptionItem.Create(RoleInfo, 16, opt.JackalbeforeImpCanSeeImp, false, false, CanImpSK);
            SKimpwocanimp = BooleanOptionItem.Create(RoleInfo, 17, opt.Jackaldollimpgaimpnimieru, false, false, CanImpSK);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, CanmakeSK)
                .SetValueFormat(OptionFormat.Seconds);
            OptionDoll = BooleanOptionItem.Create(RoleInfo, 19, opt.JackaldollShoukaku, false, false, CanmakeSK);
            RoleAddAddons.Create(RoleInfo, 20, NeutralKiller: true);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.PhantomCooldown = Fall ? 0f : Cooldown;
            AURoleOptions.ShapeshifterDuration = 1f;
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public bool UseOneclickButton => SK;
        public override bool CanUseAbilityButton() => SK;
        public override void AfterMeetingTasks()
        {
            Fall = false;
            Player.MarkDirtySettings();
        }
        public void OnClick(ref bool resetkillcooldown, ref bool fall)
        {
            resetkillcooldown = false;
            if (!SK) return;

            if (JackalDoll.sidekick.GetInt() <= JackalDoll.side)
            {
                SK = false;
                return;
            }
            var ch = Fall;
            var target = Player.GetKillTarget();
            if (target == null || target.Is(CustomRoles.King) || target.Is(CustomRoles.Jackaldoll) || target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.JackalMafia) || ((target.GetCustomRole().IsImpostor() || target.Is(CustomRoles.Egoist)) && !CanImpSK.GetBool()))
            {
                fall = true;
                /*
                Fall = true;
                if (!ch)
                {
                    _ = new LateTask(() => Player.MarkDirtySettings(), Main.LagTime, "", true);
                    _ = new LateTask(() => Player.RpcResetAbilityCooldown(), 0.4f + Main.LagTime, "", true);
                }*/
                return;
            }
            SK = false;
            Player.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(Player);
            target.RpcProtectedMurderPlayer(target);
            UtilsGameLog.AddGameLog($"SideKick", string.Format(Translator.GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true) + $"({UtilsRoleText.GetTrueRoleName(Player.PlayerId)})"));
            target.RpcSetCustomRole(CustomRoles.Jackaldoll);
            JackalDoll.Sidekick(target, Player);
            Main.FixTaskNoPlayer.Add(target);
            UtilsOption.MarkEveryoneDirtySettings();
            UtilsTask.DelTask();
            JackalDoll.side++;
            Main.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(target.GetCustomRole()), Translator.GetString($"{target.GetCustomRole()}")) + "</b>";
        }
        public override string GetAbilityButtonText() => Translator.GetString("Sidekick");
        public override bool OverrideAbilityButton(out string text)
        {
            text = "SideKick";
            return true;
        }
    }
}