using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Mole : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Mole),
                player => new Mole(player),
                CustomRoles.Mole,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                5800,
                null,
                "ml",
                OptionSort: (6, 1),
                from: From.TownOfHost_K
            );
        public Mole(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
        }
        public override bool CanVentMoving(PlayerPhysics physics, int ventId) => false;
        public override bool OnEnterVent(PlayerPhysics physics, int ventId)
        {
            _ = new LateTask(() =>
            {
                //簡単の方がええやろ(拗)
                int chance = IRandom.Instance.Next(0, ShipStatus.Instance.AllVents.Count);
                Player.RpcSnapToForced((Vector2)ShipStatus.Instance.AllVents[chance].transform.position + new Vector2(0f, 0.1f));
                if (Patches.ISystemType.VentilationSystemUpdateSystemPatch.NowVentId.ContainsKey(Player.PlayerId))
                {
                    Patches.ISystemType.VentilationSystemUpdateSystemPatch.NowVentId[Player.PlayerId] = (byte)ShipStatus.Instance.AllVents[chance].Id;
                }
            }, 0.7f, "TP");
            return true;
        }
        public bool OverrideImpVentButton(out string text)
        {
            text = "Mole_Vent";
            return true;
        }
    }
}
