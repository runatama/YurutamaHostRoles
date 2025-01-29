using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using System.Linq;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public static class SuddenDeathMode
    {
        public static float SuddenDeathtime;
        public static float ItijohoSendTime;
        public static float gpsstarttime;
        public static bool sabotage;
        public static bool arrow;
        public static Color color;
        public static int colorint;
        //null→通知しない　false→未通知 true→通知済み
        public static bool? nokori60s;
        public static bool? nokori30s;
        public static bool? nokori15s;
        public static bool? nokori10s;
        public static Dictionary<byte, Vector3> pos = new();
        static float opttime;
        public static bool NowSuddenDeathMode;
        public static bool NowSuddenDeathTemeMode;
        public static bool SuddenCannotSeeName;
        public static List<byte> TeamRed = new();
        public static List<byte> TeamBlue = new();
        public static List<byte> TeamYellow = new();
        public static List<byte> TeamGreen = new();
        public static List<byte> TeamPurple = new();
        public static bool CheckTeam
            => PlayerCatch.AllPlayerControls.All(pc => TeamRed.Contains(pc.PlayerId) || TeamBlue.Contains(pc.PlayerId)
            || TeamYellow.Contains(pc.PlayerId) || TeamGreen.Contains(pc.PlayerId) || TeamPurple.Contains(pc.PlayerId));

        public static bool CheckTeamDoreka
            => PlayerCatch.AllPlayerControls.Any(pc => TeamRed.Contains(pc.PlayerId) || TeamBlue.Contains(pc.PlayerId)
            || TeamYellow.Contains(pc.PlayerId) || TeamGreen.Contains(pc.PlayerId) || TeamPurple.Contains(pc.PlayerId));
        public static void TeamReset()
        {
            TeamRed.Clear();
            TeamBlue.Clear();
            TeamYellow.Clear();
            TeamGreen.Clear();
            TeamPurple.Clear();
        }
        public static void Reset()
        {
            NowSuddenDeathTemeMode = Options.SuddenTeam.GetBool();
            NowSuddenDeathMode = Options.SuddenDeathMode.GetBool();
            SuddenCannotSeeName = Options.SuddenCannotSeeName.GetBool();
            SuddenDeathtime = 0;
            ItijohoSendTime = 0;
            gpsstarttime = 0;
            sabotage = false;
            arrow = false;
            colorint = -1;
            color = ModColors.MadMateOrenge;
            pos.Clear();
            nokori60s = false;
            nokori30s = false;
            nokori15s = false;
            nokori10s = false;

            opttime = Options.SuddenDeathTimeLimit.GetFloat();
            var time = Options.SuddenDeathTimeLimit.GetFloat();
            if (time <= 60) nokori60s = null;
            if (time <= 30) nokori30s = null;
            if (time <= 15) nokori15s = null;
            if (time <= 10) nokori10s = null;
            CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }
        public static void TeamSet()
        {
            if (!NowSuddenDeathTemeMode) return;

            if (!Options.SuddenTeamOption.GetBool())
            {
                TeamReset();
                var teammax = Options.SuddenTeamMax.GetInt();
                List<PlayerControl> Assing = new();
                PlayerCatch.AllAlivePlayerControls.DoIf(p => !p.Is(CustomRoles.GM), p => Assing.Add(p));

                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamRed.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.PlayerName ?? "?"} => red", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamBlue.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.PlayerName ?? "?"} => Blue", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0 || !Options.SuddenTeamYellow.GetBool()) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamYellow.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.PlayerName ?? "?"} => Yellow", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0 || !Options.SuddenTeamGreen.GetBool()) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamGreen.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.PlayerName ?? "?"} => Green", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0 || !Options.SuddenTeamPurple.GetBool()) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamPurple.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.PlayerName ?? "?"} => Purple", "SuddenDeathTeam");
                }
            }

            var list = CustomRolesHelper.AllRoles.Where(role => !Options.InvalidRoles.Contains(role)).ToArray();
            if (Options.SuddenTeamRole.GetBool())
            {
                foreach (var id in TeamRed.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[Options.SuddenRedTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamBlue.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[Options.SuddenBlueTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamYellow.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[Options.SuddenYellowTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamGreen.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[Options.SuddenGreenTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamPurple.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[Options.SuddenPurpleTeamRole.GetValue()], log: true);
                }
            }
            ColorSetAndRoleset();
        }
        public static void ColorSetAndRoleset()
        {
            if (TeamRed.Distinct().Count() > 0)
                foreach (var myid in TeamRed.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codered));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamRed.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId());
                    }
                }

            if (TeamBlue.Distinct().Count() > 0)
                foreach (var myid in TeamBlue.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codeblue));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamBlue.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId());
                    }
                }

            if (TeamYellow.Distinct().Count() > 0)
                foreach (var myid in TeamYellow.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codeyellow));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamYellow.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId());
                    }
                }

            if (TeamGreen.Distinct().Count() > 0)
                foreach (var myid in TeamGreen.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codegreen));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamGreen.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId());
                    }
                }

            if (TeamPurple.Distinct().Count() > 0)
                foreach (var myid in TeamPurple.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codepurple));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamPurple.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId());
                    }
                }
        }
        public static void NotTeamKill()
        {
            if (NowSuddenDeathTemeMode)
            {
                foreach (var p in PlayerCatch.AllAlivePlayerControls)
                {
                    if (p.Is(CustomRoles.GM) || TeamRed.Contains(p.PlayerId) || TeamBlue.Contains(p.PlayerId)
                    || TeamYellow.Contains(p.PlayerId) || TeamGreen.Contains(p.PlayerId) || TeamPurple.Contains(p.PlayerId)) continue;

                    p.RpcSetCustomRole(CustomRoles.Emptiness, true, true);
                    p.RpcMurderPlayerV2(p);
                    PlayerState.GetByPlayerId(p.PlayerId).DeathReason = CustomDeathReason.etc;
                    Logger.Warn($"{p?.Data?.name} => チームアサインで余ったゾ", "SuddenDesthMode");
                }
            }
        }
        public static void SuddenDeathReactor()
        {
            if (sabotage) return;

            if (!GameStates.Intro) SuddenDeathtime += Time.fixedDeltaTime;

            if (SuddenDeathtime > opttime)
            {
                sabotage = true;

                var systemtypes = Utils.GetCriticalSabotageSystemType();
                ShipStatus.Instance.RpcUpdateSystem(systemtypes, 128);
                Logger.Info("ｷﾐﾊﾓｳｼﾞｷｼﾇ...!!", "SuddenDeath");
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (opttime - SuddenDeathtime < 10 && nokori10s is false)
            {
                nokori10s = true;
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (opttime - SuddenDeathtime < 15 && nokori15s is false)
            {
                nokori15s = true;
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (opttime - SuddenDeathtime < 30 && nokori30s is false)
            {
                nokori30s = true;
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (opttime - SuddenDeathtime < 60 && nokori60s is false)
            {
                nokori60s = true;
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
        }
        public static void ItijohoSend()
        {
            if (!GameStates.Intro)
            {
                if (arrow) ItijohoSendTime += Time.fixedDeltaTime;
                else gpsstarttime += Time.fixedDeltaTime;
            }

            if (gpsstarttime > Options.SuddenItijohoSendstart.GetFloat()) arrow = true;

            if (ItijohoSendTime > Options.SuddenItijohoSenddis.GetFloat() && arrow)
            {
                if (Options.SuddenItijohoSenddis.GetFloat() is 0)
                {
                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                    {
                        foreach (var pl in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (pc.PlayerId == pl.PlayerId) continue;
                            if (TeamRed.Contains(pl.PlayerId) && TeamRed.Contains(pc.PlayerId)) continue;
                            if (TeamBlue.Contains(pl.PlayerId) && TeamBlue.Contains(pc.PlayerId)) continue;
                            if (TeamYellow.Contains(pl.PlayerId) && TeamYellow.Contains(pc.PlayerId)) continue;
                            if (TeamGreen.Contains(pl.PlayerId) && TeamGreen.Contains(pc.PlayerId)) continue;
                            if (TeamPurple.Contains(pl.PlayerId) && TeamPurple.Contains(pc.PlayerId)) continue;

                            TargetArrow.Add(pc.PlayerId, pl.PlayerId);
                        }
                    }
                    return;
                }
                ItijohoSendTime = 0;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls) pos.Do(pos => GetArrow.Remove(pc.PlayerId, pos.Value));
                pos.Clear();
                foreach (var pc in PlayerCatch.AllAlivePlayerControls) pos.Add(pc.PlayerId, pc.transform.position);
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    var p = pc.transform.position;
                    foreach (var po in pos)
                    {
                        //同チームなら消す
                        if (TeamRed.Contains(po.Key) && TeamRed.Contains(pc.PlayerId)) continue;
                        if (TeamBlue.Contains(po.Key) && TeamBlue.Contains(pc.PlayerId)) continue;
                        if (TeamYellow.Contains(po.Key) && TeamYellow.Contains(pc.PlayerId)) continue;
                        if (TeamGreen.Contains(po.Key) && TeamGreen.Contains(pc.PlayerId)) continue;
                        if (TeamPurple.Contains(po.Key) && TeamPurple.Contains(pc.PlayerId)) continue;

                        if (po.Value != p) GetArrow.Add(pc.PlayerId, po.Value);
                    }
                }
                if (Options.SuddenItijohoSenddis.GetFloat() > 0)
                    switch (colorint)
                    {
                        case -1:
                            color = Palette.Orange;
                            colorint = 1;
                            break;
                        case 1:
                            color = Palette.CrewmateBlue;
                            colorint = 2;
                            break;
                        case 2:
                            color = Palette.AcceptedGreen;
                            colorint = 3;
                            break;
                        case 3:
                            color = Color.yellow;
                            colorint = 1;
                            break;
                    }
            }
        }
        public static string SuddenDeathProgersstext(PlayerControl seer)
        {
            var nokori = "";
            if (!sabotage)
            {
                if (nokori60s ?? false) nokori = Utils.ColorString(Palette.AcceptedGreen, "60s");
                if (nokori30s ?? false) nokori = Utils.ColorString(Color.yellow, "30s");
                if (nokori15s ?? false) nokori = Utils.ColorString(Palette.Orange, "15s");
                if (nokori10s ?? false) nokori = Utils.ColorString(Color.red, "10s");
            }
            return nokori;
        }
        public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (seer != seen) return "";
            var ar = "";
            if (Options.SuddenItijohoSend.GetBool())
            {
                if (Options.SuddenItijohoSenddis.GetFloat() is 0)
                {
                    foreach (var sen in PlayerCatch.AllAlivePlayerControls)
                    {
                        ar += " " + TargetArrow.GetArrows(seer, sen.PlayerId);
                    }
                }
                else
                {
                    foreach (var p in pos)
                    {
                        ar += " " + GetArrow.GetArrows(seer, p.Value);
                    }
                }
                ar = Utils.ColorString(color, ar);
            }
            return ar;
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            var tex = "";
            seen ??= seer;
            if (!Options.SuddenNokoriPlayerCount.GetBool()) return "";
            if (NowSuddenDeathTemeMode && seen == seer)
            {
                var t1 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamRed.Contains(pc.PlayerId)).Count();
                var t2 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamBlue.Contains(pc.PlayerId)).Count();
                var t3 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamYellow.Contains(pc.PlayerId)).Count();
                var t4 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamGreen.Contains(pc.PlayerId)).Count();
                var t5 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamPurple.Contains(pc.PlayerId)).Count();
                if (t1 > 0) tex += $"<color={ModColors.codered}>({t1})</color>";
                if (t2 > 0) tex += $"<color={ModColors.codeblue}>({t2})</color>";
                if (t3 > 0) tex += $"<color={ModColors.codeyellow}>({t3})</color>";
                if (t4 > 0) tex += $"<color={ModColors.codegreen}>({t4})</color>";
                if (t5 > 0) tex += $"<color={ModColors.codepurple}>({t5})</color>";
                return $" <size=70%>{tex}</size>";
            }
            if (seen == seer) return $"<color=#03fcb6> ({PlayerCatch.AllAlivePlayersCount})</color>";
            return "";
        }

        public static bool IsOnajiteam(this byte pc, byte tage)
        {
            if (TeamRed.Contains(pc) && TeamRed.Contains(tage)) return true;
            if (TeamBlue.Contains(pc) && TeamBlue.Contains(tage)) return true;
            if (TeamYellow.Contains(pc) && TeamYellow.Contains(tage)) return true;
            if (TeamGreen.Contains(pc) && TeamGreen.Contains(tage)) return true;
            if (TeamPurple.Contains(pc) && TeamPurple.Contains(tage)) return true;

            return false;
        }
    }
}