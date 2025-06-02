using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.AddOns.Common
{
    /// <summary>
    /// 全陣営が付与される属性。
    /// </summary>
    public class AddOnsAssignData
    {
        public static Dictionary<CustomRoles, AddOnsAssignData> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem CrewmateMaximum;
        OptionItem CrewmateFixedRole;
        FilterOptionItem CrewmateAssignTarget;
        FilterOptionItem CrewmateAssignTarget2;
        public OptionItem ImpostorMaximum;
        OptionItem ImpostorFixedRole;
        FilterOptionItem ImpostorAssignTarget;
        FilterOptionItem ImpostorAssignTarget2;
        public OptionItem MadmateMaximum;
        OptionItem MadmateFixedRole;
        FilterOptionItem MadmateAssignTarget;
        FilterOptionItem MadmateAssignTarget2;
        public OptionItem NeutralMaximum;
        OptionItem NeutralFixedRole;
        FilterOptionItem NeutralAssignTarget;
        FilterOptionItem NeutralAssingTarget2;
        static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.Emptiness,
            CustomRoles.Phantom,
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.Jackaldoll,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
        };
        static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));

        public AddOnsAssignData(int idStart, CustomRoles role, bool assignCrewmate, bool assignMadmate, bool assignImpostor, bool assignNeutral)
        {
            this.IdStart = idStart;
            this.Role = role;
            if (assignCrewmate)
            {
                CrewmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
                CrewmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.CrewmateBlue, GetString("TeamCrewmate")) } };
                CrewmateFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(CrewmateMaximum).SetParentRole(role);
                CrewmateAssignTarget = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, crew: true, notassing: InvalidRoles)
                    .SetParent(CrewmateFixedRole).SetParentRole(role);
                CrewmateAssignTarget2 = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, crew: true, notassing: InvalidRoles)
                    .SetParent(CrewmateFixedRole).SetParentRole(role).SetCansee(() => CrewmateAssignTarget.GetBool());
            }

            if (assignImpostor)
            {
                ImpostorMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 3, 1), 3, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players).SetParentRole(role);
                ImpostorMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("TeamImpostor")) } };
                ImpostorFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(ImpostorMaximum).SetParentRole(role);
                ImpostorAssignTarget = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, imp: true, notassing: InvalidRoles)
                    .SetParent(ImpostorFixedRole).SetParentRole(role);
                ImpostorAssignTarget2 = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, imp: true, notassing: InvalidRoles)
                    .SetParent(ImpostorFixedRole).SetParentRole(role).SetCansee(() => ImpostorAssignTarget.GetBool());
            }
            if (assignMadmate)
            {
                MadmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
                MadmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("Madmate")) } };
                MadmateFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(MadmateMaximum).SetParentRole(role);
                MadmateAssignTarget = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, mad: true, notassing: InvalidRoles)
                    .SetParent(MadmateFixedRole).SetParentRole(role);
                MadmateAssignTarget2 = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, mad: true, notassing: InvalidRoles)
                    .SetParent(MadmateFixedRole).SetParentRole(role).SetCansee(() => MadmateAssignTarget.GetBool());
            }

            if (assignNeutral)
            {
                NeutralMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
                NeutralMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.AcceptedGreen, GetString("Neutral")) } };
                NeutralFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(NeutralMaximum).SetParentRole(role);
                NeutralAssignTarget = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, neu: true, notassing: InvalidRoles)
                    .SetParent(NeutralFixedRole).SetParentRole(role);
                NeutralAssingTarget2 = (FilterOptionItem)FilterOptionItem.Create(idStart++, "Role", 0, TabGroup.Addons, false, neu: true, notassing: InvalidRoles)
                    .SetParent(NeutralFixedRole).SetParentRole(role).SetCansee(() => NeutralAssignTarget.GetBool());
            }

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataが作成されました", "AddOnsAssignData");
        }
        public static AddOnsAssignData Create(int idStart, CustomRoles role, bool assignCrewmate, bool assignMadmate, bool assignImpostor, bool assignNeutral)
            => new(idStart, role, assignCrewmate, assignMadmate, assignImpostor, assignNeutral);
        ///<summary>
        ///AddOnsAssignDataが存在する属性を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList()
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                if (SuddenDeathMode.GetBool() && SuddenAllRoleonaji.GetBool() && assignTargetList.Count != 0)
                {
                    assignTargetList.Clear();
                    PlayerCatch.AllPlayerControls.Do(p => assignTargetList.Add(p));
                }
                foreach (var pc in assignTargetList)
                {
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.GetLogPlayerName() + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AddOnsAssignData data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = PlayerCatch.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));

            if (data.CrewmateMaximum != null)
            {
                var crewmateMaximum = data.CrewmateMaximum.GetInt();
                if (crewmateMaximum > 0)
                {
                    var crewmates = validPlayers.Where(pc
                        => data.CrewmateFixedRole.GetBool() ? (pc.Is(data.CrewmateAssignTarget.GetRole()) || pc.Is(data.CrewmateAssignTarget2.GetRole()))
                        : pc.Is(CustomRoleTypes.Crewmate)).ToList();
                    for (var i = 0; i < crewmateMaximum; i++)
                    {
                        if (crewmates.Count == 0) break;
                        var selectedCrewmate = crewmates[rnd.Next(crewmates.Count)];
                        if (data.Role is CustomRoles.Amnesia && selectedCrewmate.Is(CustomRoles.King))
                        {
                            crewmates.Remove(selectedCrewmate);
                            continue;
                        }
                        candidates.Add(selectedCrewmate);
                        crewmates.Remove(selectedCrewmate);
                    }
                }
            }

            if (data.ImpostorMaximum != null)
            {
                var impostorMaximum = data.ImpostorMaximum.GetInt();
                if (impostorMaximum > 0)
                {
                    var impostors = validPlayers.Where(pc
                        => data.ImpostorFixedRole.GetBool() ? (pc.Is(data.ImpostorAssignTarget.GetRole()) || pc.Is(data.ImpostorAssignTarget2.GetRole()))
                        : pc.Is(CustomRoleTypes.Impostor)).ToList();
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
                        => data.MadmateFixedRole.GetBool() ? (pc.Is(data.MadmateAssignTarget.GetRole()) || pc.Is(data.MadmateAssignTarget2.GetRole()))
                        : pc.Is(CustomRoleTypes.Madmate)).ToList();
                    for (var i = 0; i < MadmateMaximum; i++)
                    {
                        if (Madmates.Count == 0) break;
                        var selectedMadmate = Madmates[rnd.Next(Madmates.Count)];
                        candidates.Add(selectedMadmate);
                        Madmates.Remove(selectedMadmate);
                    }
                }
            }

            if (data.NeutralMaximum != null)
            {
                var neutralMaximum = data.NeutralMaximum.GetInt();
                if (neutralMaximum > 0)
                {
                    var neutrals = validPlayers.Where(pc
                        => data.NeutralFixedRole.GetBool() ? (pc.Is(data.NeutralAssignTarget.GetRole()) || pc.Is(data.NeutralAssingTarget2.GetRole()))
                        : pc.Is(CustomRoleTypes.Neutral)).ToList();
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