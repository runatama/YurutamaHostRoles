using System;
using System.Collections.Generic;
using Hazel;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;

namespace TownOfHost;
class Lovers
{
    public static List<PlayerControl> ALoversPlayers = new();
    public static bool isALoversDead = true;
    public static List<PlayerControl> BLoversPlayers = new();
    public static bool isBLoversDead = true;
    public static List<PlayerControl> CLoversPlayers = new();
    public static bool isCLoversDead = true;
    public static List<PlayerControl> DLoversPlayers = new();
    public static bool isDLoversDead = true;
    public static List<PlayerControl> ELoversPlayers = new();
    public static bool isELoversDead = true;
    public static List<PlayerControl> FLoversPlayers = new();
    public static bool isFLoversDead = true;
    public static List<PlayerControl> GLoversPlayers = new();
    public static bool isGLoversDead = true;
    public static List<PlayerControl> MaMaLoversPlayers = new();
    public static bool isMaLoversDead = true;
    public static void RPCSetLovers(MessageReader reader)
    {
        ALoversPlayers.Clear();
        BLoversPlayers.Clear();
        CLoversPlayers.Clear();
        DLoversPlayers.Clear();
        ELoversPlayers.Clear();
        FLoversPlayers.Clear();
        GLoversPlayers.Clear();
        int Acount = reader.ReadInt32();
        int Bcount = reader.ReadInt32();
        int Ccount = reader.ReadInt32();
        int Dcount = reader.ReadInt32();
        int Ecount = reader.ReadInt32();
        int Fcount = reader.ReadInt32();
        int Gcount = reader.ReadInt32();
        for (int i = 0; i < Acount; i++)
            ALoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
        for (int i = 0; i < Bcount; i++)
            BLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
        for (int i = 0; i < Ccount; i++)
            CLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
        for (int i = 0; i < Dcount; i++)
            DLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
        for (int i = 0; i < Ecount; i++)
            ELoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
        for (int i = 0; i < Fcount; i++)
            FLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
        for (int i = 0; i < Gcount; i++)
            GLoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
    }
    public static void AssignLoversRoles(int RawCount = -1)
    {
        //全部初期化
        ALoversPlayers.Clear();
        isALoversDead = false;
        BLoversPlayers.Clear();
        isBLoversDead = false;
        CLoversPlayers.Clear();
        isCLoversDead = false;
        DLoversPlayers.Clear();
        isDLoversDead = false;
        ELoversPlayers.Clear();
        isELoversDead = false;
        FLoversPlayers.Clear();
        isFLoversDead = false;
        GLoversPlayers.Clear();
        isGLoversDead = false;

        var allPlayers = new List<PlayerControl>();
        var rand = IRandom.Instance;

        foreach (var player in Main.AllPlayerControls)
        {
            if (player.Is(CustomRoles.GM)) continue;
            if (player.Is(CustomRoles.Madonna)) continue;
            if (player.Is(CustomRoles.Limiter)) continue;
            if (player.Is(CustomRoles.King)) continue;
            allPlayers.Add(player);
        }
        if (CustomRoles.ALovers.IsPresent())
        {
            var loversRole = CustomRoles.ALovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                ALoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        if (CustomRoles.BLovers.IsPresent())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.GM)) continue; if (player.Is(CustomRoles.Madonna)) continue;
                if (player.Is(CustomRoles.Limiter)) continue; if (player.Is(CustomRoles.ALovers)) continue;
                if (player.Is(CustomRoles.King)) continue;
                allPlayers.Add(player);
            }
            var loversRole = CustomRoles.BLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                BLoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        if (CustomRoles.CLovers.IsPresent())
        {
            var loversRole = CustomRoles.CLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                CLoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        if (CustomRoles.DLovers.IsPresent())
        {
            var loversRole = CustomRoles.DLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                DLoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        if (CustomRoles.ELovers.IsPresent())
        {
            var loversRole = CustomRoles.ELovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                ELoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        if (CustomRoles.FLovers.IsPresent())
        {
            var loversRole = CustomRoles.FLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                FLoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        if (CustomRoles.GLovers.IsPresent())
        {
            var loversRole = CustomRoles.GLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                GLoversPlayers.Add(player);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
        }
        RPC.SyncLoversPlayers();
    }

    public static void ALoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.ALovers.IsPresent() && isALoversDead == false)
        {
            foreach (var loversPlayer in ALoversPlayers)
            {
                //生きていて死ぬ予定でなければスキップ
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isALoversDead = true;
                foreach (var partnerPlayer in ALoversPlayers)
                {
                    //本人ならスキップ
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    //残った恋人を全て殺す(2人以上可)
                    //生きていて死ぬ予定もない場合は心中
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void BLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.BLovers.IsPresent() && isBLoversDead == false)
        {
            foreach (var loversPlayer in BLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isBLoversDead = true;
                foreach (var partnerPlayer in BLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void CLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.CLovers.IsPresent() && isCLoversDead == false)
        {
            foreach (var loversPlayer in CLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isCLoversDead = true;
                foreach (var partnerPlayer in CLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void DLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.DLovers.IsPresent() && isDLoversDead == false)
        {
            foreach (var loversPlayer in DLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isDLoversDead = true;
                foreach (var partnerPlayer in DLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void ELoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.ELovers.IsPresent() && isELoversDead == false)
        {
            foreach (var loversPlayer in ELoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isELoversDead = true;
                foreach (var partnerPlayer in ELoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void FLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.FLovers.IsPresent() && isFLoversDead == false)
        {
            foreach (var loversPlayer in FLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isFLoversDead = true;
                foreach (var partnerPlayer in FLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void GLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.GLovers.IsPresent() && isGLoversDead == false)
        {
            foreach (var loversPlayer in GLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isGLoversDead = true;
                foreach (var partnerPlayer in GLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void MadonnaLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.Madonna.IsPresent() && isMaLoversDead == false)
        {
            foreach (var MaloversPlayer in MaMaLoversPlayers)
            {
                if (!MaloversPlayer.Data.IsDead && MaloversPlayer.PlayerId != deathId) continue;

                isMaLoversDead = true;
                foreach (var partnerPlayer in MaMaLoversPlayers)
                {
                    if (MaloversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[MaloversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void LoverDisconnected(PlayerControl player)
    {
        if (player.Is(CustomRoles.ALovers) && !player.Data.IsDead)
            foreach (var lovers in ALoversPlayers.ToArray())
            {
                isALoversDead = true;
                ALoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.ALovers);
            }
        if (player.Is(CustomRoles.BLovers) && !player.Data.IsDead)
            foreach (var lovers in BLoversPlayers.ToArray())
            {
                isBLoversDead = true;
                BLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.BLovers);
            }
        if (player.Is(CustomRoles.CLovers) && !player.Data.IsDead)
            foreach (var lovers in CLoversPlayers.ToArray())
            {
                isCLoversDead = true;
                CLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.CLovers);
            }
        if (player.Is(CustomRoles.DLovers) && !player.Data.IsDead)
            foreach (var lovers in DLoversPlayers.ToArray())
            {
                isDLoversDead = true;
                DLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.DLovers);
            }
        if (player.Is(CustomRoles.ELovers) && !player.Data.IsDead)
            foreach (var lovers in ELoversPlayers.ToArray())
            {
                isELoversDead = true;
                ELoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.ELovers);
            }
        if (player.Is(CustomRoles.FLovers) && !player.Data.IsDead)
            foreach (var lovers in FLoversPlayers.ToArray())
            {
                isFLoversDead = true;
                FLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.FLovers);
            }
        if (player.Is(CustomRoles.GLovers) && !player.Data.IsDead)
            foreach (var lovers in GLoversPlayers.ToArray())
            {
                isGLoversDead = true;
                GLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.GLovers);
            }
        if (player.Is(CustomRoles.MaLovers) && !player.Data.IsDead)
            foreach (var Mlovers in MaMaLoversPlayers.ToArray())
            {
                isMaLoversDead = true;
                MaMaLoversPlayers.Remove(Mlovers);
                PlayerState.GetByPlayerId(Mlovers.PlayerId).RemoveSubRole(CustomRoles.MaLovers);
            }
    }
}
