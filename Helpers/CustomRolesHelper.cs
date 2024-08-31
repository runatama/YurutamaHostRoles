using AmongUs.GameOptions;
using System.Linq;

using TownOfHost.Roles.Core;

namespace TownOfHost
{
    static class CustomRolesHelper
    {
        /// <summary>すべての役職(属性は含まない)</summary>
        public static readonly CustomRoles[] AllRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).ToArray();
        /// <summary>すべての属性</summary>
        public static readonly CustomRoles[] AllAddOns = EnumHelper.GetAllValues<CustomRoles>().Where(role => role > CustomRoles.NotAssigned).ToArray();
        /// <summary>スタンダードモードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllStandardRoles = AllRoles.Where(role => role is not (CustomRoles.HASFox or CustomRoles.HASTroll)).ToArray();
        /// <summary>HASモードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllHASRoles = { CustomRoles.HASFox, CustomRoles.HASTroll };
        public static readonly CustomRoleTypes[] AllRoleTypes = EnumHelper.GetAllValues<CustomRoleTypes>();

        public static bool IsImpostor(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Impostor;

            return false;
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Madmate;
            return role == CustomRoles.SKMadmate;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Neutral || role == CustomRoles.Jackaldoll;
            return role is CustomRoles.HASTroll or CustomRoles.HASFox;
        }
        public static bool IsCrewmate(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType == CustomRoleTypes.Crewmate || (!role.IsImpostorTeam() && !role.IsNeutral());
        public static bool IsVanilla(this CustomRoles role)
        {
            return
                role is CustomRoles.Crewmate or
                CustomRoles.Engineer or
                CustomRoles.Scientist or
                CustomRoles.Noisemaker or
                CustomRoles.Tracker or
                CustomRoles.GuardianAngel or
                CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.Phantom;
        }
        public static bool IsAddOn(this CustomRoles roles)
        {
            return
                roles is
                //ラスト系
                CustomRoles.LastImpostor or
                CustomRoles.LastNeutral or
                CustomRoles.Workhorse or
                //バフ
                CustomRoles.Moon or
                CustomRoles.Guesser or
                CustomRoles.Speeding or
                CustomRoles.Guarding or
                CustomRoles.watching or
                CustomRoles.Lighting or
                CustomRoles.Management or
                CustomRoles.Connecting or
                CustomRoles.Serial or
                CustomRoles.PlusVote or
                CustomRoles.Opener or
                CustomRoles.seeing or
                CustomRoles.Revenger or
                CustomRoles.Autopsy or
                CustomRoles.Tiebreaker or
                //デバフ
                CustomRoles.NonReport or
                CustomRoles.Notvoter or
                CustomRoles.Elector or
                CustomRoles.Water or
                CustomRoles.Slacker or
                CustomRoles.Transparent or
                CustomRoles.Amnesia or
                CustomRoles.Clumsy or
                CustomRoles.SlowStarter or
                CustomRoles.InfoPoor
                ;
        }
        public static bool IsBuffAddon(this CustomRoles roles)
        {
            return roles is
                CustomRoles.Moon or
                CustomRoles.Guesser or
                CustomRoles.Speeding or
                CustomRoles.Guarding or
                CustomRoles.watching or
                CustomRoles.Lighting or
                CustomRoles.Management or
                CustomRoles.Connecting or
                CustomRoles.Serial or
                CustomRoles.PlusVote or
                CustomRoles.Opener or
                CustomRoles.seeing or
                CustomRoles.Revenger or
                CustomRoles.Autopsy or
                CustomRoles.Tiebreaker
                ;
        }
        public static bool IsDebuffAddon(this CustomRoles roles)
        {
            return roles is
                CustomRoles.NonReport or
                CustomRoles.Notvoter or
                CustomRoles.Elector or
                CustomRoles.Water or
                CustomRoles.Slacker or
                CustomRoles.Transparent or
                CustomRoles.Amnesia or
                CustomRoles.Clumsy or
                CustomRoles.SlowStarter or
                CustomRoles.InfoPoor
                ;
        }
        public static bool IsRiaju(this CustomRoles roles)
        {
            return roles is
            CustomRoles.ALovers or
            CustomRoles.BLovers or
            CustomRoles.CLovers or
            CustomRoles.DLovers or
            CustomRoles.ELovers or
            CustomRoles.FLovers or
            CustomRoles.GLovers or
            CustomRoles.MaLovers;
        }
        public static bool IsRiaju(this PlayerControl pc)
        {
            return pc.Is(CustomRoles.ALovers) || pc.Is(CustomRoles.BLovers) || pc.Is(CustomRoles.CLovers) || pc.Is(CustomRoles.DLovers) || pc.Is(CustomRoles.ELovers) || pc.Is(CustomRoles.FLovers) || pc.Is(CustomRoles.GLovers) || pc.Is(CustomRoles.MaLovers);
        }
        public static bool IsWhiteCrew(this CustomRoles roles)
        {
            return
                roles is
                CustomRoles.UltraStar or
                CustomRoles.TaskStar;
        }
        public static bool IsGorstRole(this PlayerControl pc) => PlayerState.GetByPlayerId(pc.PlayerId)?.GhostRole.IsGorstRole() ?? false;
        public static bool IsGorstRole(this CustomRoles role)
        {
            return role is CustomRoles.GuardianAngel
                        or CustomRoles.Ghostbuttoner
                        or CustomRoles.GhostNoiseSender
                        or CustomRoles.GhostReseter
                        or CustomRoles.DemonicTracker
                        or CustomRoles.DemonicVenter
                        or CustomRoles.DemonicCrusher
                        or CustomRoles.AsistingAngel
                        ;
        }
        public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
        {
            CustomRoleTypes type = CustomRoleTypes.Crewmate;

            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType;

            if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
            if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
            if (role.IsMadmate()) type = CustomRoleTypes.Madmate;
            return type;
        }
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => Options.GetRoleCount(role),
                    CustomRoles.Scientist => Options.GetRoleCount(role),
                    CustomRoles.Shapeshifter => Options.GetRoleCount(role),
                    CustomRoles.Phantom => Options.GetRoleCount(role),
                    CustomRoles.Tracker => Options.GetRoleCount(role),
                    CustomRoles.Noisemaker => Options.GetRoleCount(role),
                    CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                    _ => 0
                };
            }
            else
            {
                return Options.GetRoleCount(role);
            }
        }
        public static int GetChance(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => Options.GetRoleChance(role),
                    CustomRoles.Scientist => Options.GetRoleChance(role),
                    CustomRoles.Shapeshifter => Options.GetRoleChance(role),
                    CustomRoles.Tracker => Options.GetRoleChance(role),
                    CustomRoles.Noisemaker => Options.GetRoleChance(role),
                    CustomRoles.Phantom => Options.GetRoleChance(role),
                    CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                    _ => 0
                };
            }
            else
            {
                return Options.GetRoleChance(role);
            }
        }
        public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
        public static bool CanMakeMadmate(this CustomRoles role)
        {
            if (role.GetRoleInfo() is SimpleRoleInfo info)
            {
                return info.CanMakeMadmate;
            }

            return false;
        }
        public static RoleTypes GetRoleTypes(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.BaseRoleType.Invoke();
            return role switch
            {
                CustomRoles.GM => RoleTypes.GuardianAngel,

                _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            };
        }
    }
    public enum CountTypes
    {
        OutOfGame,
        None,
        Crew,
        Impostor,
        Jackal,
        Remotekiller,
        TaskPlayer,
        GrimReaper,
    }
}