using System;
using System.Linq;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.AddOns.Common
{
    /// <summary>
    /// インポスター・マッドメイトのみが付与される属性。
    /// 後ついでに狼少年にも。
    /// </summary>
    public class AddOnsAssignDataTeamImp
    {
        static Dictionary<CustomRoles, AddOnsAssignDataTeamImp> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        OptionItem CrewmateMaximum;
        OptionItem ImpostorMaximum;
        OptionItem ImpostorFixedRole;
        OptionItem ImpostorAssignTarget;
        OptionItem MadmateMaximum;
        OptionItem MadmateFixedRole;
        OptionItem MadmateAssignTarget;
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
        static CustomRoles[] ImpostorRoles = ValidRoles.Where(role => role.IsImpostor()).ToArray();
        static CustomRoles[] MadmateRoles = ValidRoles.Where(role => role.IsMadmate()).ToArray();

        public AddOnsAssignDataTeamImp(int idStart, CustomRoles role, bool assignCrew, bool assignMadmate, bool assignImpostor)
        {
            this.IdStart = idStart;
            this.Role = role;
            if (assignCrew)
            {
                CrewmateMaximum = IntegerOptionItem.Create(idStart++, "WoMaximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
            }
            if (assignImpostor)
            {
                ImpostorMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 3, 1), 3, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                ImpostorMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("TeamImpostor")) } };
                ImpostorFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(ImpostorMaximum);
                var impostorStringArray = ImpostorRoles.Select(role => role.ToString()).ToArray();
                ImpostorAssignTarget = StringOptionItem.Create(idStart++, "Role", impostorStringArray, 0, TabGroup.Addons, false)
                    .SetParent(ImpostorFixedRole);
            }
            if (assignMadmate)
            {
                MadmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                MadmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("Madmate")) } };
                MadmateFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(MadmateMaximum);
                var MadmateStringArray = MadmateRoles.Select(role => role.ToString()).ToArray();
                MadmateAssignTarget = StringOptionItem.Create(idStart++, "Role", MadmateStringArray, 0, TabGroup.Addons, false)
                    .SetParent(MadmateFixedRole);
            }

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataTeamImpが作成されました", "AddOnsAssignDataTeamImp");
        }
        public static AddOnsAssignDataTeamImp Create(int idStart, CustomRoles role, bool assignCrew, bool assignMadmate, bool assignImpostor)
            => new(idStart, role, assignCrew, assignMadmate, assignImpostor);
        ///<summary>
        ///AddOnsAssignDataTeamImpが存在する属性を一括で割り当て
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
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AddOnsAssignDataTeamImp data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = Main.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));
            if (data.CrewmateMaximum != null)
            {
                var CrewmateMaximum = data.CrewmateMaximum.GetInt();
                if (CrewmateMaximum > 0)
                {
                    var Crewmates = validPlayers.Where(pc
                        => pc.Is(CustomRoles.WolfBoy)).ToList();
                    for (var i = 0; i < CrewmateMaximum; i++)
                    {
                        if (Crewmates.Count == 0) break;
                        var selectedImpostor = Crewmates[rnd.Next(Crewmates.Count)];
                        candidates.Add(selectedImpostor);
                        Crewmates.Remove(selectedImpostor);
                    }
                }
            }
            if (data.ImpostorMaximum != null)
            {
                var impostorMaximum = data.ImpostorMaximum.GetInt();
                if (impostorMaximum > 0)
                {
                    var impostors = validPlayers.Where(pc
                        => data.ImpostorFixedRole.GetBool() ? pc.Is(ImpostorRoles[data.ImpostorAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Impostor)).ToList();
                    for (var i = 0; i < impostorMaximum; i++)
                    {
                        if (impostors.Count == 0) break;
                        var selectedImpostor = impostors[rnd.Next(impostors.Count)];
                        candidates.Add(selectedImpostor);
                        impostors.Remove(selectedImpostor);
                    }
                }
            }

            if (data.MadmateMaximum != null)
            {
                var MadmateMaximum = data.MadmateMaximum.GetInt();
                if (MadmateMaximum > 0)
                {
                    var Madmates = validPlayers.Where(pc
                        => data.MadmateFixedRole.GetBool() ? pc.Is(MadmateRoles[data.MadmateAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Madmate)).ToList();
                    for (var i = 0; i < MadmateMaximum; i++)
                    {
                        if (Madmates.Count == 0) break;
                        var selectedMadmate = Madmates[rnd.Next(Madmates.Count)];
                        candidates.Add(selectedMadmate);
                        Madmates.Remove(selectedMadmate);
                    }
                }
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
    /// <summary>
    /// マフィング用
    /// </summary>
    public class AddOnsAssignDataOnlyImp
    {
        static Dictionary<CustomRoles, AddOnsAssignDataOnlyImp> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        OptionItem ImpostorMaximum;
        OptionItem ImpostorFixedRole;
        OptionItem ImpostorAssignTarget;
        static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.Jackaldoll,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
            CustomRoles.Mafia//マフィングの他に必要になれば。うん。
        };
        static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));
        static CustomRoles[] ImpostorRoles = ValidRoles.Where(role => role.IsImpostor()).ToArray();

        public AddOnsAssignDataOnlyImp(int idStart, CustomRoles role, bool assignImpostor)
        {
            this.IdStart = idStart;
            this.Role = role;
            if (assignImpostor)
            {
                ImpostorMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 3, 1), 3, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                ImpostorMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("TeamImpostor")) } };
                ImpostorFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(ImpostorMaximum);
                var impostorStringArray = ImpostorRoles.Select(role => role.ToString()).ToArray();
                ImpostorAssignTarget = StringOptionItem.Create(idStart++, "Role", impostorStringArray, 0, TabGroup.Addons, false)
                    .SetParent(ImpostorFixedRole);
            }

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataOnlyImpが作成されました", "AddOnsAssignDataOnlyImp");
        }
        public static AddOnsAssignDataOnlyImp Create(int idStart, CustomRoles role, bool assignImpostor)
            => new(idStart, role, assignImpostor);
        ///<summary>
        ///AddOnsAssignDataOnlyImpが存在する属性を一括で割り当て
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
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AddOnsAssignDataOnlyImp data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = Main.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));
            if (data.ImpostorMaximum != null)
            {
                var impostorMaximum = data.ImpostorMaximum.GetInt();
                if (impostorMaximum > 0)
                {
                    var impostors = validPlayers.Where(pc
                        => data.ImpostorFixedRole.GetBool() ? pc.Is(ImpostorRoles[data.ImpostorAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Impostor)).ToList();
                    for (var i = 0; i < impostorMaximum; i++)
                    {
                        if (impostors.Count == 0) break;
                        var selectedImpostor = impostors[rnd.Next(impostors.Count)];
                        candidates.Add(selectedImpostor);
                        impostors.Remove(selectedImpostor);
                    }
                }
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
}