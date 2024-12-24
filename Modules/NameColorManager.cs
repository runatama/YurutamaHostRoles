using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

namespace TownOfHost
{
    public static class NameColorManager
    {
        public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            if (!AmongUsClient.Instance.IsGameStarted) return name;

            if (!seer || !target)
            {
                Logger.Error($"{seer?.Data?.PlayerName ?? "seer"} => {target?.Data?.PlayerName ?? "target"}がnull", "ApplyNameColorData");
                return name;
            }
            if (!TryGetData(seer, target, out var colorCode))
            {
                if (KnowTargetRoleColor(seer, target, isMeeting))
                    colorCode = target.GetRoleColorCode();
            }
            string openTag = "", closeTag = "";
            if (!SuddenDeathMode.NowSuddenDeathMode)
            {
                var roleClass = seer.GetRoleClass();
                if (seer.PlayerId == target.PlayerId && seer.Is(CustomRoles.Amnesia))
                {
                    colorCode = seer.Is(CustomRoleTypes.Crewmate) ? UtilsRoleText.GetRoleColorCode(CustomRoles.Crewmate) : (seer.Is(CustomRoleTypes.Impostor) ?
                    UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : UtilsRoleText.GetRoleColorCode(CustomRoles.SchrodingerCat));
                }
                if (seer.PlayerId == target.PlayerId && roleClass != null && roleClass?.Jikaku() != CustomRoles.NotAssigned)
                {
                    colorCode = UtilsRoleText.GetRoleColorCode(roleClass.Jikaku());
                }

                var seerRole = seer.GetCustomRole();
                var targetRole = target.GetCustomRole();
                if (seer != target && seerRole.IsImpostor() && targetRole.IsImpostor())
                {
                    if (targetRole.GetRoleInfo()?.IsCantSeeTeammates == true)
                        colorCode = Roles.Vanilla.Impostor.RoleInfo.RoleColorCode;
                    if (seerRole.GetRoleInfo()?.IsCantSeeTeammates == true && !(roleClass as Amnesiac).omoidasita)
                        colorCode = "#ffffff"; //white
                }
            }
            //会議中で決まってない場合は白
            if (isMeeting && colorCode == "") colorCode = "#ffffff";
            if (colorCode != "")
            {
                if (!colorCode.StartsWith('#'))
                    colorCode = "#" + colorCode;
                openTag = $"<color={colorCode}>";
                closeTag = "</color>";
            }
            return openTag + name + closeTag;
        }
        public static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            return seer == target
                || target.Is(CustomRoles.GM)
                || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)
                && (!seer.Is(CustomRoles.Amnesiac) || ((PlayerControl.LocalPlayer.GetRoleClass() as Amnesiac)?.omoidasita ?? false)))
                || Mare.KnowTargetRoleColor(target, isMeeting);
        }
        public static bool TryGetData(PlayerControl seer, PlayerControl target, out string colorCode)
        {
            colorCode = "";
            var state = PlayerState.GetByPlayerId(seer.PlayerId);
            if (!state.TargetColorData.TryGetValue(target.PlayerId, out var value)) return false;
            colorCode = value;
            return true;
        }

        public static void Add(byte seerId, byte targetId, string colorCode = "")
        {
            if (colorCode == "")
            {
                var target = PlayerCatch.GetPlayerById(targetId);
                if (target == null) return;
                colorCode = target.GetRoleColorCode();
            }

            var state = PlayerState.GetByPlayerId(seerId);
            if (state.TargetColorData.TryGetValue(targetId, out var value) && colorCode == value) return;
            if (!state.TargetColorData.TryAdd(targetId, colorCode))
            {
                state.TargetColorData[targetId] = colorCode;
            }

            SendRPC(seerId, targetId, colorCode);
        }
        public static void Remove(byte seerId, byte targetId)
        {
            var state = PlayerState.GetByPlayerId(seerId);
            if (!state.TargetColorData.ContainsKey(targetId)) return;
            state.TargetColorData.Remove(targetId);

            SendRPC(seerId, targetId);
        }
        public static void RemoveAll(byte seerId)
        {
            PlayerState.GetByPlayerId(seerId).TargetColorData.Clear();

            SendRPC(seerId);
        }
        private static void SendRPC(byte seerId, byte targetId = byte.MaxValue, string colorCode = "")
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            writer.Write(colorCode);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte seerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            string colorCode = reader.ReadString();

            if (targetId == byte.MaxValue)
                RemoveAll(seerId);
            else if (colorCode == "")
                Remove(seerId, targetId);
            else
                Add(seerId, targetId, colorCode);
        }
    }
}