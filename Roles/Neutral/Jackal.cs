using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Jackal : RoleBase, ILNKiller, ISchrodingerCatOwner, IUseTheShButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Jackal),
                player => new Jackal(player),
                CustomRoles.Jackal,
                () => CanmakeSK.GetBool() ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                51000,
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
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            SK = CanmakeSK.GetBool();
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        static OptionItem CanmakeSK;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        bool SK;

        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            CanmakeSK = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.CanCreateSideKick, true, false);
            RoleAddAddons.Create(RoleInfo, 15);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = 1f;
            AURoleOptions.ShapeshifterDuration = 1f;
            opt.SetVision(HasImpostorVision);
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public bool UseOCButton => SK;
        public override bool CanUseAbilityButton() => SK;

        public void OnClick()
        {
            if (!SK) return;
            if (JackalDoll.sidekick.GetInt() <= JackalDoll.side)
            {
                SK = false;
                return;
            }
            var target = Player.GetKillTarget();
            if (target == null || target.Is(CustomRoles.Jackaldoll) || target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.JackalMafia) || target.GetCustomRole().IsImpostor() || target.Is(CustomRoles.Egoist)) return;
            SK = false;
            Player.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(Player);
            target.RpcProtectedMurderPlayer(target);
            Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Sidekick]　" + string.Format(Translator.GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({Utils.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true) + $"({Utils.GetTrueRoleName(Player.PlayerId)})");
            target.RpcSetCustomRole(CustomRoles.Jackaldoll);
            JackalDoll.Sidekick(target);
            Main.FixTaskNoPlayer.Add(target);
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
            Utils.DelTask();
            JackalDoll.side++;
            Main.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(Utils.GetRoleColor(target.GetCustomRole()), Translator.GetString($"{target.GetCustomRole()}")) + "</b>" + Utils.GetSubRolesText(target.PlayerId);
        }
    }
}