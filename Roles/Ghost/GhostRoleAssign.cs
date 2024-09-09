using System.Linq;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using AmongUs.GameOptions;
using System;

using static TownOfHost.Translator;

namespace TownOfHost.Roles.Ghost
{
    public class GhostRoleAssingData
    {
        public static Dictionary<CustomRoles, GhostRoleAssingData> AllData = new();
        public static Dictionary<CustomRoles, int> GhostAssingCount = new();
        public CustomRoles Role { get; private set; }
        public CustomRoleTypes RoleType { get; private set; }
        public CustomRoleTypes kottinimofuyo { get; private set; }
        public int IdStart { get; private set; }

        public GhostRoleAssingData(int idStart, CustomRoles role, CustomRoleTypes roleTypes, CustomRoleTypes k)
        {
            IdStart = idStart;
            Role = role;
            RoleType = roleTypes;
            kottinimofuyo = k;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするGhostRoleAssingDataが作成されました", "GhostRoleAssingData");
        }
        /// <summary>
        /// 幽霊役職のアサイン
        /// </summary>
        /// <param name="idStart">Id</param>
        /// <param name="role">配布役職</param>
        /// <param name="roleTypes">配布する陣営</param>
        /// <param name="kottinimofuyo">配布陣営その二</param>
        /// <returns></returns>
        public static GhostRoleAssingData Create(int idStart, CustomRoles role, CustomRoleTypes roleTypes, CustomRoleTypes kottinimofuyo) => new(idStart, role, roleTypes, kottinimofuyo);

        /// <summary>
        /// 幽霊役職のアサイン
        /// </summary>
        /// <param name="idStart">Id</param>
        /// <param name="role">配布役職</param>
        /// <param name="roleTypes">配布する陣営</param>
        /// <returns></returns>
        public static GhostRoleAssingData Create(int idStart, CustomRoles role, CustomRoleTypes roleTypes) => new(idStart, role, roleTypes, roleTypes);
        ///<summary>
        ///GhostRoleAssingDataが存在する幽霊役職を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList(bool d = false)
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                foreach (var pc in assignTargetList)
                {
                    if (GhostAssingCount[data.Role] >= data.Role.GetRealCount()) continue;
                    GhostAssingCount[data.Role]++;

                    PlayerState.GetByPlayerId(pc.PlayerId).SetGhostRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "GhostRoleAssingData");

                    Utils.AddGameLog($"{role}", string.Format(GetString("GhostRole.log"), Utils.GetPlayerColor(pc), Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role))));
                    Main.LastLogRole[pc.PlayerId] += $"<size=45%>=> {Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role))}</size>";

                    if (!d)
                    {
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.GuardianAngel);
                        else pc.RpcSetRoleDesync(RoleTypes.GuardianAngel, pc.GetClientId());

                        _ = new LateTask(() => pc.RpcResetAbilityCooldown(kousin: true), 0.5f, "GhostRoleResetAbilty");
                    }
                    else
                    {
                        _ = new LateTask(() =>
                            {
                                if (!GameStates.Meeting)
                                {
                                    if (pc == PlayerControl.LocalPlayer)
                                        RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.GuardianAngel);
                                    else pc.RpcSetRoleDesync(RoleTypes.GuardianAngel, pc.GetClientId());

                                    _ = new LateTask(() => pc.RpcResetAbilityCooldown(kousin: true), 0.5f, "GhostRoleResetAbilty");
                                }
                            }, 1.4f, "Fix sabotage");
                    }
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(GhostRoleAssingData data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var AP = new List<PlayerControl>(Main.AllPlayerControls.Where(x => !x.IsGorstRole() && !x.IsAlive() && (x.Is(data.RoleType) || x.Is(data.kottinimofuyo))));
            var APc = new List<PlayerControl>(Main.AllPlayerControls.Where(x => !x.IsGorstRole() && !x.IsAlive() && (x.Is(data.RoleType) || x.Is(data.kottinimofuyo))));

            if (!GhostAssingCount.ContainsKey(data.Role))//データ内なら0
            {
                GhostAssingCount[data.Role] = 0;
            }

            for (var i = 0; i < APc.Count; i++)
            {
                if (AP.Count == 0) continue;
                var pc = AP[rnd.Next(AP.Count)];

                //配布対象外ならサヨナラ...
                if (pc == null || pc.IsGorstRole() || pc.Is(CustomRoles.GM) || pc.IsAlive() || PlayerState.GetByPlayerId(pc.PlayerId) == null)
                {
                    AP.Remove(pc);
                    continue;
                }
                if (PlayerState.GetByPlayerId(pc.PlayerId).DeathReason == CustomDeathReason.Disconnected)
                {
                    AP.Remove(pc);
                    continue;
                }

                candidates.Add(pc);
                AP.Remove(pc);
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
}