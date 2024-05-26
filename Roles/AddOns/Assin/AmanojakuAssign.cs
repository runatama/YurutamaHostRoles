using System;
using System.Linq;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.AddOns.Common
{
    /// <summary>
    /// 天邪鬼専用assign
    /// </summary>
    public class AmanojakuAssing
    {
        static Dictionary<CustomRoles, AmanojakuAssing> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        OptionItem CrewmateMaximum;
        OptionItem CrewmateFixedRole;
        OptionItem CrewmateAssignTarget;
        OptionItem NeutralMaximum;
        OptionItem NeutralFixedRole;
        OptionItem NeutralAssignTarget;
        static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.Jackaldoll,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
        };
        static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));
        static CustomRoles[] CrewmateRoles = ValidRoles.Where(role => role.IsCrewmate()).ToArray();
        static CustomRoles[] NeutralRoles = ValidRoles.Where(role => role.IsNeutral()).ToArray();

        public AmanojakuAssing(int idStart, CustomRoles role, bool assignCrewmate, bool assignNeutral)
        {
            this.IdStart = idStart;
            this.Role = role;
            if (assignCrewmate)
            {
                CrewmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                CrewmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.CrewmateBlue, GetString("TeamCrewmate")) } };
                CrewmateFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(CrewmateMaximum);
                var crewmateStringArray = CrewmateRoles.Select(role => role.ToString()).ToArray();
                CrewmateAssignTarget = StringOptionItem.Create(idStart++, "Role", crewmateStringArray, 0, TabGroup.Addons, false)
                    .SetParent(CrewmateFixedRole);
            }

            if (assignNeutral)
            {
                NeutralMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                NeutralMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.AcceptedGreen, GetString("Neutral")) } };
                NeutralFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(NeutralMaximum);
                var neutralStringsArray = NeutralRoles.Select(role => role.ToString()).ToArray();
                NeutralAssignTarget = StringOptionItem.Create(idStart++, "Role", neutralStringsArray, 0, TabGroup.Addons, false)
                    .SetParent(NeutralFixedRole);
            }

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするAmanojakuAssingが作成されました", "AmanojakuAssing");
        }
        public static AmanojakuAssing Create(int idStart, CustomRoles role, bool assignCrewmate, bool assignNeutral)
            => new(idStart, role, assignCrewmate, assignNeutral);
        ///<summary>
        ///AmanojakuAssingが存在する属性を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList()
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                foreach (var pc in assignTargetList)
                {
                    Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Amanojaku]　" + string.Format(GetString("Log.Amanojaku"), Utils.GetPlayerColor(pc) + $"({Utils.GetTrueRoleName(pc.PlayerId, false)})");
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                    Amanojaku.Add(pc.PlayerId);
                    Main.LastLogRole[pc.PlayerId] = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Amanojaku), GetString("Amanojaku") + GetString($"{pc.GetCustomRole()}"));
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AmanojakuAssing data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = Main.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));

            if (data.CrewmateMaximum != null)
            {
                var crewmateMaximum = data.CrewmateMaximum.GetInt();
                if (crewmateMaximum > 0)
                {
                    var crewmates = validPlayers.Where(pc
                        => pc.IsAlive() && data.CrewmateFixedRole.GetBool() ? pc.Is(CrewmateRoles[data.CrewmateAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Crewmate)).ToList();
                    for (var i = 0; i < crewmateMaximum; i++)
                    {
                        if (crewmates.Count == 0) break;
                        var selectedCrewmate = crewmates[rnd.Next(crewmates.Count)];
                        candidates.Add(selectedCrewmate);
                        crewmates.Remove(selectedCrewmate);
                    }
                }
            }

            if (data.NeutralMaximum != null)
            {
                var neutralMaximum = data.NeutralMaximum.GetInt();
                if (neutralMaximum > 0)
                {
                    var neutrals = validPlayers.Where(pc
                        => pc.IsAlive() && data.NeutralFixedRole.GetBool() ? pc.Is(NeutralRoles[data.NeutralAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Neutral)).ToList();
                    for (var i = 0; i < neutralMaximum; i++)
                    {
                        if (neutrals.Count == 0) break;
                        var selectedNeutral = neutrals[rnd.Next(neutrals.Count)];
                        candidates.Add(selectedNeutral);
                        neutrals.Remove(selectedNeutral);
                    }
                }
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
}