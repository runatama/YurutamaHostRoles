using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Options;

namespace TownOfHost;

class Lovers
{
    public static List<byte> HaveLoverDontTaskPlayers = new();
    public static List<PlayerControl> LoversPlayers = new();
    public static bool isLoversDead = true;
    public static List<PlayerControl> RedLoversPlayers = new();
    public static bool isRedLoversDead = true;
    public static List<PlayerControl> YellowLoversPlayers = new();
    public static bool isYellowLoversDead = true;
    public static List<PlayerControl> BlueLoversPlayers = new();
    public static bool isBlueLoversDead = true;
    public static List<PlayerControl> GreenLoversPlayers = new();
    public static bool isGreenLoversDead = true;
    public static List<PlayerControl> WhiteLoversPlayers = new();
    public static bool isWhiteLoversDead = true;
    public static List<PlayerControl> PurpleLoversPlayers = new();
    public static bool isPurpleLoversDead = true;
    public static List<PlayerControl> MaMadonnaLoversPlayers = new();
    public static bool isMadonnaLoversDead = true;
    public static (byte OneLove, byte Ltarget, bool doublelove) OneLovePlayer = new();
    public static bool isOneLoveDead = true;
    public static OptionItem LoversRole;
    public static OptionItem RedLoversRole;
    public static OptionItem YellowLoversRole;
    public static OptionItem BlueLoversRole;
    public static OptionItem GreenLoversRole;
    public static OptionItem WhiteLoversRole;
    public static OptionItem PurpleLoversRole;
    public static OptionItem LoversRoleAddwin;
    public static OptionItem RedLoversRoleAddwin;
    public static OptionItem YellowLoversRoleAddwin;
    public static OptionItem BlueLoversRoleAddwin;
    public static OptionItem GreenLoversRoleAddwin;
    public static OptionItem WhiteLoversRoleAddwin;
    public static OptionItem PurpleLoversRoleAddwin;
    public static OptionItem LoversSolowin3players;
    public static OptionItem RedLoversSolowin3players;
    public static OptionItem YellowLoversSolowin3players;
    public static OptionItem BlueLoversSolowin3players;
    public static OptionItem GreenLoversSolowin3players;
    public static OptionItem WhiteLoversSolowin3players;
    public static OptionItem PurpleLoversSolowin3players;
    public static OptionItem OneLoveSolowin3players;
    public static OptionItem OneLoveRoleAddwin;
    public static OptionItem OneLoveLoversrect;
    public static void SetLoversOptions()
    {
        SetupRoleOptions(50370, TabGroup.Combinations, CustomRoles.OneLove, new(1, 1, 1));
        OneLoveRoleAddwin = BooleanOptionItem.Create(73081, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.OneLove]);
        SoloWinOption.Create(73084, TabGroup.Combinations, CustomRoles.OneLove, () => !OneLoveRoleAddwin.GetBool(), defo: 5);
        OneLoveLoversrect = IntegerOptionItem.Create(73082, "OneLoverLovers", new(0, 100, 2), 20, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.OneLove]).SetValueFormat(OptionFormat.Percent);
        OneLoveSolowin3players = BooleanOptionItem.Create(73083, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.OneLove]);

        SetupRoleOptions(50300, TabGroup.Combinations, CustomRoles.Lovers, assignCountRule: new(2, 2, 2), fromtext: "<color=#000000>From:</color><color=#ff6be4>Love Couple Mod</color></size>");
        LoversRole = BooleanOptionItem.Create(73010, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lovers]);
        LoversRoleAddwin = BooleanOptionItem.Create(73011, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lovers]);
        SoloWinOption.Create(73013, TabGroup.Combinations, CustomRoles.Lovers, () => !LoversRoleAddwin.GetBool(), defo: 6);
        LoversSolowin3players = BooleanOptionItem.Create(73012, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lovers]);

        SetupRoleOptions(50310, TabGroup.Combinations, CustomRoles.RedLovers, assignCountRule: new(2, 2, 2));
        RedLoversRole = BooleanOptionItem.Create(73020, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.RedLovers]);
        RedLoversRoleAddwin = BooleanOptionItem.Create(73021, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.RedLovers]);
        SoloWinOption.Create(73023, TabGroup.Combinations, CustomRoles.RedLovers, () => !RedLoversRoleAddwin.GetBool(), defo: 7);
        RedLoversSolowin3players = BooleanOptionItem.Create(73022, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.RedLovers]);

        SetupRoleOptions(50320, TabGroup.Combinations, CustomRoles.YellowLovers, assignCountRule: new(2, 2, 2));
        YellowLoversRole = BooleanOptionItem.Create(73030, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.YellowLovers]);
        YellowLoversRoleAddwin = BooleanOptionItem.Create(73031, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.YellowLovers]);
        SoloWinOption.Create(73033, TabGroup.Combinations, CustomRoles.YellowLovers, () => !YellowLoversRoleAddwin.GetBool(), defo: 8);
        YellowLoversSolowin3players = BooleanOptionItem.Create(73032, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.YellowLovers]);

        SetupRoleOptions(50330, TabGroup.Combinations, CustomRoles.BlueLovers, assignCountRule: new(2, 2, 2));
        BlueLoversRole = BooleanOptionItem.Create(73040, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.BlueLovers]);
        BlueLoversRoleAddwin = BooleanOptionItem.Create(73041, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.BlueLovers]);
        SoloWinOption.Create(73043, TabGroup.Combinations, CustomRoles.BlueLovers, () => !BlueLoversRoleAddwin.GetBool(), defo: 9);
        BlueLoversSolowin3players = BooleanOptionItem.Create(73042, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.BlueLovers]);

        SetupRoleOptions(50340, TabGroup.Combinations, CustomRoles.GreenLovers, assignCountRule: new(2, 2, 2));
        GreenLoversRole = BooleanOptionItem.Create(73050, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.GreenLovers]);
        GreenLoversRoleAddwin = BooleanOptionItem.Create(73051, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.GreenLovers]);
        SoloWinOption.Create(73053, TabGroup.Combinations, CustomRoles.GreenLovers, () => !GreenLoversRoleAddwin.GetBool(), defo: 10);
        GreenLoversSolowin3players = BooleanOptionItem.Create(73052, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.GreenLovers]);

        SetupRoleOptions(50350, TabGroup.Combinations, CustomRoles.WhiteLovers, assignCountRule: new(2, 2, 2));
        WhiteLoversRole = BooleanOptionItem.Create(73060, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.WhiteLovers]);
        WhiteLoversRoleAddwin = BooleanOptionItem.Create(73061, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.WhiteLovers]);
        SoloWinOption.Create(73063, TabGroup.Combinations, CustomRoles.WhiteLovers, () => !WhiteLoversRoleAddwin.GetBool(), defo: 11);
        WhiteLoversSolowin3players = BooleanOptionItem.Create(73062, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.WhiteLovers]);

        SetupRoleOptions(50360, TabGroup.Combinations, CustomRoles.PurpleLovers, assignCountRule: new(2, 2, 2));
        PurpleLoversRole = BooleanOptionItem.Create(73070, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.PurpleLovers]);
        PurpleLoversRoleAddwin = BooleanOptionItem.Create(73071, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.PurpleLovers]);
        SoloWinOption.Create(73073, TabGroup.Combinations, CustomRoles.PurpleLovers, () => !PurpleLoversRoleAddwin.GetBool(), defo: 12);
        PurpleLoversSolowin3players = BooleanOptionItem.Create(73072, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.PurpleLovers]);
    }
    [GameModuleInitializer, PluginModuleInitializer]
    public static void Reset()
    {
        HaveLoverDontTaskPlayers.Clear();
        LoversPlayers.Clear();
        isLoversDead = false;
        RedLoversPlayers.Clear();
        isRedLoversDead = false;
        YellowLoversPlayers.Clear();
        isYellowLoversDead = false;
        BlueLoversPlayers.Clear();
        isBlueLoversDead = false;
        GreenLoversPlayers.Clear();
        isGreenLoversDead = false;
        WhiteLoversPlayers.Clear();
        isWhiteLoversDead = false;
        PurpleLoversPlayers.Clear();
        isPurpleLoversDead = false;
        OneLovePlayer = (byte.MaxValue, byte.MaxValue, false);
        isOneLoveDead = false;
    }
    public static void RPCSetLovers(MessageReader reader)
    {
        CustomRoles role = (CustomRoles)reader.ReadInt32();
        switch (role)
        {
            case CustomRoles.Lovers:
                int Acount = reader.ReadInt32();
                for (int i = 0; i < Acount; i++)
                    LoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRoles.RedLovers:
                int Bcount = reader.ReadInt32();
                for (int i = 0; i < Bcount; i++)
                    RedLoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRoles.YellowLovers:
                int Ccount = reader.ReadInt32();
                for (int i = 0; i < Ccount; i++)
                    YellowLoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRoles.BlueLovers:
                int Dcount = reader.ReadInt32();
                for (int i = 0; i < Dcount; i++)
                    BlueLoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRoles.GreenLovers:
                int Ecount = reader.ReadInt32();
                for (int i = 0; i < Ecount; i++)
                    GreenLoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRoles.WhiteLovers:
                int Fcount = reader.ReadInt32();
                for (int i = 0; i < Fcount; i++)
                    WhiteLoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRoles.PurpleLovers:
                int Gcount = reader.ReadInt32();
                for (int i = 0; i < Gcount; i++)
                    PurpleLoversPlayers.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
                break;
        }
    }
    public static void AssignLoversRoles(int RawCount = -1)
    {
        //全部初期化
        LoversPlayers.Clear();
        isLoversDead = false;
        RedLoversPlayers.Clear();
        isRedLoversDead = false;
        YellowLoversPlayers.Clear();
        isYellowLoversDead = false;
        BlueLoversPlayers.Clear();
        isBlueLoversDead = false;
        GreenLoversPlayers.Clear();
        isGreenLoversDead = false;
        WhiteLoversPlayers.Clear();
        isWhiteLoversDead = false;
        PurpleLoversPlayers.Clear();
        isPurpleLoversDead = false;
        OneLovePlayer = (byte.MaxValue, byte.MaxValue, false);
        isOneLoveDead = false;

        var allPlayers = new List<PlayerControl>();
        var rand = IRandom.Instance;

        foreach (var player in PlayerCatch.AllPlayerControls)
        {
            if (player.Is(CustomRoles.GM)) continue;
            if (player.Is(CustomRoles.Madonna)) continue;
            if (player.Is(CustomRoles.Limiter)) continue;
            if (player.Is(CustomRoles.King)) continue;
            allPlayers.Add(player);
        }
        if (CustomRoles.Lovers.IsPresent())
        {
            var loversRole = CustomRoles.Lovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;

            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                LoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }

            RPC.SyncLoversPlayers(CustomRoles.Lovers);
        }
        if (CustomRoles.RedLovers.IsPresent())
        {
            var loversRole = CustomRoles.RedLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                RedLoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers(CustomRoles.RedLovers);
        }
        if (CustomRoles.YellowLovers.IsPresent())
        {
            var loversRole = CustomRoles.YellowLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                YellowLoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers(CustomRoles.YellowLovers);
        }
        if (CustomRoles.BlueLovers.IsPresent())
        {
            var loversRole = CustomRoles.BlueLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                BlueLoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers(CustomRoles.BlueLovers);
        }
        if (CustomRoles.GreenLovers.IsPresent())
        {
            var loversRole = CustomRoles.GreenLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                GreenLoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers(CustomRoles.GreenLovers);
        }
        if (CustomRoles.WhiteLovers.IsPresent())
        {
            var loversRole = CustomRoles.WhiteLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                WhiteLoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }

            RPC.SyncLoversPlayers(CustomRoles.WhiteLovers);
        }
        if (CustomRoles.PurpleLovers.IsPresent())
        {
            var loversRole = CustomRoles.PurpleLovers;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetRealCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            if (count != 2) count = 2;
            if (allPlayers.Count < 2) return;
            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                PurpleLoversPlayers.Add(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                allPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }

            RPC.SyncLoversPlayers(CustomRoles.PurpleLovers);
        }
        if (CustomRoles.OneLove.IsPresent())
        {
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(CustomRoles.OneLove.GetRealCount(), 0, allPlayers.Count);
            var assind = false;
            if (allPlayers.Count < 2) return;//2人居ない時は返す。
            if (count <= 0) return;
            if (allPlayers.Count < 2) return;
            var player = allPlayers[rand.Next(0, allPlayers.Count)];//片思いしてる人
            for (var i = 0; i < 2; i++)
            {
                if (assind)
                {
                    var d = false;
                    var p = rand.Next(0, 100);
                    var target = allPlayers[rand.Next(0, allPlayers.Count)];//片思いされてる人
                    if (p <= OneLoveLoversrect.GetInt())
                    {
                        HaveLoverDontTaskPlayers.Add(target.PlayerId);
                        d = true;
                        allPlayers.Remove(target);
                        PlayerState.GetByPlayerId(target.PlayerId).SetSubRole(CustomRoles.OneLove);
                        Logger.Info("両想いだったって！" + target?.Data?.GetLogPlayerName() + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.OneLove.ToString(), "AssignLovers");
                    }

                    Logger.Info($"{player.Data.GetLogPlayerName()} => {target.Data.GetLogPlayerName()} {d}", "OneLover");
                    OneLovePlayer = (player.PlayerId, target.PlayerId, d);
                    break;
                }
                assind = true;
                allPlayers.Remove(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(CustomRoles.OneLove);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + CustomRoles.OneLove.ToString(), "AssignLovers");
            }
        }
    }

    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.Lovers.IsPresent() && isLoversDead == false)
        {
            foreach (var loversPlayer in LoversPlayers)
            {
                //生きていて死ぬ予定でなければスキップ
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isLoversDead = true;
                foreach (var partnerPlayer in LoversPlayers)
                {
                    //本人ならスキップ
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    //残った恋人を全て殺す(2人以上可)
                    //生きていて死ぬ予定もない場合は心中
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void RedLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.RedLovers.IsPresent() && isRedLoversDead == false)
        {
            foreach (var loversPlayer in RedLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isRedLoversDead = true;
                foreach (var partnerPlayer in RedLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void YellowLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.YellowLovers.IsPresent() && isYellowLoversDead == false)
        {
            foreach (var loversPlayer in YellowLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isYellowLoversDead = true;
                foreach (var partnerPlayer in YellowLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void BlueLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.BlueLovers.IsPresent() && isBlueLoversDead == false)
        {
            foreach (var loversPlayer in BlueLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isBlueLoversDead = true;
                foreach (var partnerPlayer in BlueLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void GreenLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.GreenLovers.IsPresent() && isGreenLoversDead == false)
        {
            foreach (var loversPlayer in GreenLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isGreenLoversDead = true;
                foreach (var partnerPlayer in GreenLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void WhiteLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.WhiteLovers.IsPresent() && isWhiteLoversDead == false)
        {
            foreach (var loversPlayer in WhiteLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isWhiteLoversDead = true;
                foreach (var partnerPlayer in WhiteLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void PurpleLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.PurpleLovers.IsPresent() && isPurpleLoversDead == false)
        {
            foreach (var loversPlayer in PurpleLoversPlayers)
            {
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                isPurpleLoversDead = true;
                foreach (var partnerPlayer in PurpleLoversPlayers)
                {
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
    public static void OneLoveSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.OneLove.IsPresent() && isOneLoveDead == false)
        {
            var (Love, target, d) = OneLovePlayer;
            if (Love == byte.MaxValue || target == byte.MaxValue) return;

            if (d)//両片思い
            {
                foreach (var loversPlayer in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.OneLove)))
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    isOneLoveDead = true;
                    foreach (var partnerPlayer in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.OneLove)))
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
            else//片思い
            {
                var pc = PlayerCatch.GetPlayerById(target);
                var my = PlayerCatch.GetPlayerById(Love);
                if (!pc.Data.IsDead && pc.PlayerId != deathId) return;

                isOneLoveDead = true;
                if (my.PlayerId != deathId && !my.Data.IsDead)
                {
                    PlayerState.GetByPlayerId(my.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                    if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
                    {
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, my.PlayerId);
                        ReportDeadBodyPatch.Musisuruoniku[my.PlayerId] = false;
                    }
                    else
                        my.RpcMurderPlayer(my, true);
                }
            }
        }
    }
    public static void MadonnLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.Madonna.IsPresent() && isMadonnaLoversDead == false)
        {
            foreach (var MadonnaLoversPlayer in MaMadonnaLoversPlayers)
            {
                if (!MadonnaLoversPlayer.Data.IsDead && MadonnaLoversPlayer.PlayerId != deathId) continue;

                isMadonnaLoversDead = true;
                foreach (var partnerPlayer in MaMadonnaLoversPlayers)
                {
                    if (MadonnaLoversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[MadonnaLoversPlayer.PlayerId] = false;
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
        if (player.Is(CustomRoles.Lovers) && !player.Data.IsDead)
            foreach (var lovers in LoversPlayers.ToArray())
            {
                isLoversDead = true;
                LoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.Lovers);
            }
        if (player.Is(CustomRoles.RedLovers) && !player.Data.IsDead)
            foreach (var lovers in RedLoversPlayers.ToArray())
            {
                isRedLoversDead = true;
                RedLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.RedLovers);
            }
        if (player.Is(CustomRoles.YellowLovers) && !player.Data.IsDead)
            foreach (var lovers in YellowLoversPlayers.ToArray())
            {
                isYellowLoversDead = true;
                YellowLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.YellowLovers);
            }
        if (player.Is(CustomRoles.BlueLovers) && !player.Data.IsDead)
            foreach (var lovers in BlueLoversPlayers.ToArray())
            {
                isBlueLoversDead = true;
                BlueLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.BlueLovers);
            }
        if (player.Is(CustomRoles.GreenLovers) && !player.Data.IsDead)
            foreach (var lovers in GreenLoversPlayers.ToArray())
            {
                isGreenLoversDead = true;
                GreenLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.GreenLovers);
            }
        if (player.Is(CustomRoles.WhiteLovers) && !player.Data.IsDead)
            foreach (var lovers in WhiteLoversPlayers.ToArray())
            {
                isWhiteLoversDead = true;
                WhiteLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.WhiteLovers);
            }
        if (player.Is(CustomRoles.PurpleLovers) && !player.Data.IsDead)
            foreach (var lovers in PurpleLoversPlayers.ToArray())
            {
                isPurpleLoversDead = true;
                PurpleLoversPlayers.Remove(lovers);
                PlayerState.GetByPlayerId(lovers.PlayerId).RemoveSubRole(CustomRoles.PurpleLovers);
            }
        if (player.Is(CustomRoles.MadonnaLovers) && !player.Data.IsDead)
            foreach (var MadonnaLovers in MaMadonnaLoversPlayers.ToArray())
            {
                isMadonnaLoversDead = true;
                MaMadonnaLoversPlayers.Remove(MadonnaLovers);
                PlayerState.GetByPlayerId(MadonnaLovers.PlayerId).RemoveSubRole(CustomRoles.MadonnaLovers);
            }
        var one = PlayerCatch.AllPlayerControls.Where(x => x.Is(CustomRoles.OneLove));
        if (CustomRoles.OneLove.IsPresent() && one.Any())
            if (player.PlayerId == OneLovePlayer.OneLove || player.PlayerId == OneLovePlayer.Ltarget)
            {
                foreach (var pc in one)
                {
                    isOneLoveDead = true;
                    OneLovePlayer = (byte.MaxValue, byte.MaxValue, false);
                    PlayerState.GetByPlayerId(pc.PlayerId).RemoveSubRole(CustomRoles.OneLove);
                }
            }
    }
    public static void LoversSoloWin(ref GameOverReason reason)
    {
        if ((!LoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.Lovers) && LoversPlayers.Count > 0 && LoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Lovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.Lovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!RedLoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.RedLovers) && RedLoversPlayers.Count > 0 && RedLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.RedLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.RedLovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!YellowLoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.YellowLovers) && YellowLoversPlayers.Count > 0 && YellowLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.YellowLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.YellowLovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!BlueLoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.BlueLovers) && BlueLoversPlayers.Count > 0 && BlueLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.BlueLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.BlueLovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!GreenLoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.GreenLovers) && GreenLoversPlayers.Count > 0 && GreenLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GreenLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.GreenLovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!WhiteLoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.WhiteLovers) && WhiteLoversPlayers.Count > 0 && WhiteLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.WhiteLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.WhiteLovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!PurpleLoversRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.PurpleLovers) && PurpleLoversPlayers.Count > 0 && PurpleLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.PurpleLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.PurpleLovers) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if ((!Madonna.MadonnaLoverAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.MadonnaLovers) && MaMadonnaLoversPlayers.Count > 0 && MaMadonnaLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadonnaLovers, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.MadonnaLovers) && p.IsAlive())
                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if (CustomRoles.OneLove.IsPresent())
            if ((!OneLoveRoleAddwin.GetBool() || CustomWinnerHolder.WinnerTeam is CustomWinner.OneLove) && PlayerCatch.GetPlayerById(OneLovePlayer.OneLove).IsAlive() && PlayerCatch.GetPlayerById(OneLovePlayer.Ltarget).IsAlive())
            {
                if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.OneLove, byte.MaxValue))
                {
                    PlayerCatch.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.OneLove) && p.IsAlive())
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    if (!OneLovePlayer.doublelove) CustomWinnerHolder.WinnerIds.Add(OneLovePlayer.Ltarget);//両片思いじゃなかったら追加
                    reason = GameOverReason.ImpostorsByKill;
                }
            }
    }
    public static void LoversAddWin()
    {
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Lovers && LoversRoleAddwin.GetBool() && LoversPlayers.Count > 0 && LoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.Lovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.RedLovers && RedLoversRoleAddwin.GetBool() && RedLoversPlayers.Count > 0 && RedLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.RedLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.RedLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.YellowLovers && YellowLoversRoleAddwin.GetBool() && YellowLoversPlayers.Count > 0 && YellowLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.YellowLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.YellowLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.BlueLovers && BlueLoversRoleAddwin.GetBool() && BlueLoversPlayers.Count > 0 && BlueLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.BlueLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.BlueLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.GreenLovers && GreenLoversRoleAddwin.GetBool() && GreenLoversPlayers.Count > 0 && GreenLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.GreenLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.GreenLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.WhiteLovers && WhiteLoversRoleAddwin.GetBool() && WhiteLoversPlayers.Count > 0 && WhiteLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.WhiteLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.WhiteLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.PurpleLovers && PurpleLoversRoleAddwin.GetBool() && PurpleLoversPlayers.Count > 0 && PurpleLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.PurpleLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.PurpleLovers) && p.IsAlive())
                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
        }
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.MadonnaLovers && Madonna.MadonnaLoverAddwin.GetBool() && MaMadonnaLoversPlayers.Count > 0 && MaMadonnaLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.MadonnaLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.MadonnaLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomRoles.OneLove.IsPresent())
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.OneLove && OneLoveRoleAddwin.GetBool() && PlayerCatch.GetPlayerById(OneLovePlayer.OneLove).IsAlive() && PlayerCatch.GetPlayerById(OneLovePlayer.Ltarget).IsAlive())
            {
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.OneLove);
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.OneLove) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                if (!OneLovePlayer.doublelove)
                {
                    CustomWinnerHolder.WinnerIds.Add(OneLovePlayer.Ltarget);
                    CustomWinnerHolder.IdRemoveLovers.Remove(OneLovePlayer.Ltarget);
                }
            }
    }
    //これに関してはゲーム終了勝利開始だから複数同時発生する訳がない...ハズ。
    public static bool CheckPlayercountWin()
    {
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers)) ||
         (Lovers.LoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.LoversPlayers.Count != 0 && Lovers.LoversPlayers.All(pc => pc.IsAlive()))) //ラバーズ勝利
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Lovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.RedLovers)) ||
        (Lovers.RedLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.RedLoversPlayers.Count != 0 && Lovers.RedLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.RedLovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.YellowLovers)) ||
        (Lovers.YellowLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.YellowLoversPlayers.Count != 0 && Lovers.YellowLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.YellowLovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.BlueLovers)) ||
        (Lovers.BlueLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.BlueLoversPlayers.Count != 0 && Lovers.BlueLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.BlueLovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.GreenLovers)) ||
        (Lovers.GreenLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.GreenLoversPlayers.Count != 0 && Lovers.GreenLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GreenLovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.WhiteLovers)) ||
        (Lovers.WhiteLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.WhiteLoversPlayers.Count != 0 && Lovers.WhiteLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.WhiteLovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PurpleLovers)) ||
        (Lovers.PurpleLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.PurpleLoversPlayers.Count != 0 && Lovers.PurpleLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.PurpleLovers, byte.MaxValue);
            return true;
        }
        if (PlayerCatch.AllAlivePlayersCount <= 2 && PlayerCatch.AllAlivePlayerControls.All(pc => pc.PlayerId == Lovers.OneLovePlayer.Ltarget || pc.PlayerId == Lovers.OneLovePlayer.OneLove)
        || (Lovers.OneLoveSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.OneLove)?.IsAlive() == true && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.Ltarget)?.IsAlive() == true))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.OneLove, byte.MaxValue);
            if (!Lovers.OneLovePlayer.doublelove) CustomWinnerHolder.WinnerIds.Add(Lovers.OneLovePlayer.Ltarget);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.MadonnaLovers)) ||
        (Madonna.MaLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.MaMadonnaLoversPlayers.Count != 0 && Lovers.MaMadonnaLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadonnaLovers, byte.MaxValue);
            return true;
        }
        return false;
    }
}
