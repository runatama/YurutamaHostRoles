using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;

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
        public static List<Vector3> pos = new();
        public static void Reset()
        {
            SuddenDeathtime = 0;
            ItijohoSendTime = 0;
            sabotage = false;
            arrow = false;
            colorint = -1;
            color = Color.white;
            pos.Clear();
            nokori60s = false;
            nokori30s = false;
            nokori15s = false;
            nokori10s = false;

            var time = Options.SuddenDeathTimeLimit.GetFloat();
            if (time <= 60) nokori60s = null;
            if (time <= 30) nokori30s = null;
            if (time <= 15) nokori15s = null;
            if (time <= 10) nokori10s = null;
            CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
        }
        public static void SuddenDeathReactor()
        {
            var time = Options.SuddenDeathTimeLimit.GetFloat();
            if (sabotage) return;

            if (!GameStates.Intro) SuddenDeathtime += Time.fixedDeltaTime;

            if (SuddenDeathtime > time)
            {
                sabotage = true;

                var systemtypes = Utils.GetCriticalSabotageSystemType();
                ShipStatus.Instance.RpcUpdateSystem(systemtypes, 128);
                Logger.Info("ｷﾐﾊﾓｳｼﾞｷｼﾇ...!!", "SuddenDeath");
                UtilsNotifyRoles.NotifyRoles();
                return;
            }
            if (time - SuddenDeathtime < 10 && nokori10s != null)
            {
                nokori10s = true;
                UtilsNotifyRoles.NotifyRoles();
                return;
            }
            if (time - SuddenDeathtime < 15 && nokori15s != null)
            {
                nokori15s = true;
                UtilsNotifyRoles.NotifyRoles();
                return;
            }
            if (time - SuddenDeathtime < 30 && nokori30s != null)
            {
                nokori30s = true;
                UtilsNotifyRoles.NotifyRoles();
                return;
            }
            if (time - SuddenDeathtime < 60 && nokori60s != null)
            {
                nokori60s = true;
                UtilsNotifyRoles.NotifyRoles();
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
                ItijohoSendTime = 0;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls) pos.Do(pos => GetArrow.Remove(pc.PlayerId, pos));
                pos.Clear();
                foreach (var pc in PlayerCatch.AllAlivePlayerControls) pos.Add(pc.transform.position);
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    var p = pc.transform.position;
                    foreach (var po in pos) if (po != p) GetArrow.Add(pc.PlayerId, po);
                }
                if (Options.SuddenItijohoSenddis.GetFloat() != 0)
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
            var ar = "";
            if (Options.SuddenItijohoSend.GetBool())
            {
                foreach (var p in pos)
                {
                    ar += " " + GetArrow.GetArrows(seer, p);
                }
                ar = Utils.ColorString(color, ar);
            }
            return ar;
        }
    }
}