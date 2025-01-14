using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public static class DemonicCrusher
    {
        private static readonly int Id = 60200;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem AbilityTime;
        public static bool DemUseAbility;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.DemonicCrusher);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.DemonicCrusher, CustomRoleTypes.Madmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.DemonicCrusher]);
            AbilityTime = FloatOptionItem.Create(Id + 3, "DemonicCrusherAbilityTime", new(1f, 30f, 1f), 10f, TabGroup.GhostRoles, false)
                    .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.DemonicCrusher]);
        }
        public static void Init()
        {
            playerIdList = new();
            DemUseAbility = false;
            CustomRoleManager.MarkOthers.Add(AbilityMark);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc)
        {
            if (pc.Is(CustomRoles.DemonicCrusher))
            {
                pc.RpcResetAbilityCooldown();
                if (DemUseAbility) return;//能力使用中に能力使えない。
                DemUseAbility = true;
                RemoveDisableDevicesPatch.UpdateDisableDevices();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                _ = new LateTask(() =>
                {
                    DemUseAbility = false;
                    RemoveDisableDevicesPatch.UpdateDisableDevices(true);
                    UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                    pc.RpcResetAbilityCooldown();
                }, AbilityTime.GetFloat(), "DemonicCrusher");
            }
        }
        public static string AbilityMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;

            if (seer == seen)
                if (DemUseAbility) return Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.DemonicCrusher), "？");

            return "";
        }
    }
}