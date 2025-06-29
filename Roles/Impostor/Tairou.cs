using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Tairou : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tairou),
                player => new Tairou(player),
                CustomRoles.Tairou,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                6000,
                (6, 5),
                SetupOptionItem,
                "t"
            );
        public Tairou(PlayerControl player)
            : base(
                RoleInfo,
                player
            )
        {
            TairoDeathReason = OptionTairoDeathReason.GetBool();
            Tairouhoukoku = OptionTairouhoukoku.GetBool();
        }
        public static OptionItem OptionTairoDeathReason;
        public static OptionItem OptionTairouhoukoku;
        enum OptionName
        {
            TairoDeathReason,
            Tairouhoukoku
        }
        public static bool TairoDeathReason;
        public static bool Tairouhoukoku;
        private static void SetupOptionItem()
        {
            OptionTairoDeathReason = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TairoDeathReason, true, false);
            OptionTairouhoukoku = BooleanOptionItem.Create(RoleInfo, 11, OptionName.Tairouhoukoku, true, false);
        }
        public override CustomRoles GetFtResults(PlayerControl player) => CustomRoles.Crewmate;
        public override string MeetingMeg()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return "";
            if (Player.IsAlive() && Tairouhoukoku)
            {
                string TairouTitle = $"<size=90%><color=#ff0000>{GetString("Message.TairouTitle")}</size></color>";
                return TairouTitle + "\n<size=70%>" + GetString("Message.TairouAlive") + "</size>\n";
            }
            return "";
        }
    }
}