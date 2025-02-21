/*using System.Collections.Generic;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class AntiTeleporter
    {
        private static readonly int Id = 76200;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.AntiTeleporter);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "t");
        public static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AntiTeleporter);
            AddOnsAssignData.Create(Id + 10, CustomRoles.AntiTeleporter, true, true, true, true);
        }
        public static Dictionary<byte, Vector2> LastPlace = new();
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                Vector2 v = new(1, 1);
                LastPlace.Add(id, v);
                if (AmongUsClient.Instance.AmHost) SendRPC(id);
            }
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

        public static void SetLastPlace()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p.Is(CustomRoles.AntiTeleporter))
                {
                    Vector2 now = new(p.transform.position.x, p.transform.position.y);
                    // 位置の更新処理
                    LastPlace[p.PlayerId] = now;
                    SendRPC(p.PlayerId);
                }
            }
        }
        public static bool IsLadderOrNun(Vector2 position)
        {
            /*
 -------------昇降機かいだん---------------
上:(4.542149,14.2624)
下:(4.532301,9.709933)
//xほぼ変化なし
-------------------------------------------
-------------プルプル----------------
右:(9.810959,8.9246)
左:(5.59947,8.9246)
//y変化なし
-------------------------------------------
-----ーーしゃわーかいだん--------
上:(12.87232,-3.351996)
下:(12.87169,-5.755963)
//xほぼ変化なし
 */
/*bool IsSyo = 4.3f < position.x && position.x < 4.8f && 9.6f < position.y && position.y < 14.4f;
bool IsNun = 5.4f < position.x && position.x < 9.9f && 8.8f <= position.y && position.y <= 9.1f;
bool IsShawa = 12.7f <= position.x && position.x < 13f && -3.2f <= position.y && position.y <= -5.9f;
return IsSyo || IsNun || IsShawa;
}

public static void SendRPC(byte playerid)
{
MessageWriter writer;
writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAntiTeleporterPosition, SendOption.Reliable, -1);
writer.Write(playerid);
writer.Write(LastPlace[playerid].x);
writer.Write(LastPlace[playerid].y);
AmongUsClient.Instance.FinishRpcImmediately(writer);
}
public static void ReceiveRPC(MessageReader reader)
{
byte playerId = reader.ReadByte();
float x = reader.ReadByte();
float y = reader.ReadByte();
Vector2 vTwo = new(x, y);
LastPlace[playerId] = vTwo;
}
}
}*/
