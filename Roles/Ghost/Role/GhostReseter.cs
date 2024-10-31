using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public static class GhostReseter
    {
        private static readonly int Id = 60600;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem ResetAbilityCool;
        public static OptionItem Count;
        public static Dictionary<byte, int> Counts = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GhostReseter);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.GhostReseter, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "GhostButtonerCoolDown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]);
            ResetAbilityCool = BooleanOptionItem.Create(Id + 3, "GhostReseterResetAbilityCool", true, TabGroup.GhostRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]);
            Count = IntegerOptionItem.Create(Id + 4, "GhostReseterCount", new(1, 99, 1), 2, TabGroup.GhostRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]);
        }

        public static void Init()
        {
            playerIdList = new();
            Counts.Clear();

            CustomRoleManager.MarkOthers.Add(OtherMark);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GhostReseter))
            {
                if (!Counts.ContainsKey(pc.PlayerId))//登録前なら登録する
                    Counts.Add(pc.PlayerId, Count.GetInt());

                if (Counts[pc.PlayerId] <= 0) return;

                Counts[pc.PlayerId]--;

                target.SetKillCooldown(kyousei: true);
                if (ResetAbilityCool.GetBool()) target.RpcResetAbilityCooldown(kousin: true);

                UtilsNotifyRoles.NotifyRoles(SpecifySeer: pc);
                pc.RpcResetAbilityCooldown();
            }
        }
        public static string OtherMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;

            if (seer == seen && seer.Is(CustomRoles.GhostReseter))
            {
                var count = 0;
                if (Counts.ContainsKey(seer.PlayerId)) count = Counts[seer.PlayerId];
                return Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.GhostReseter).ShadeColor(-0.25f), $" ({count}/{Count.GetInt()})");
            }
            return "";
        }
    }
}