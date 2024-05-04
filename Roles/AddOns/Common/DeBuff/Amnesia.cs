using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    //いつかクソゲーにはなるけど全員の役職分からない状態で試合させたい。
    public static class Amnesia
    {
        private static readonly int Id = 71300;
        public static List<byte> playerIdList = new();
        public static OptionItem Modoru;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amnesia);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Amnesia, true, true, true, true);
            Modoru = IntegerOptionItem.Create(Id + 50, "Am.modru", new(1, 99, 1), 4, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]).SetValueFormat(OptionFormat.day);
        }

        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            if (Modoru.GetFloat() < Main.day)
                playerIdList.Add(playerId);
        }
        public static void Kesu(byte playerId)
        {
            playerIdList.Remove(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}