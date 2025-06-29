using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class NiceLogger : RoleBase, IKiller, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(NiceLogger),
                player => new NiceLogger(player),
                CustomRoles.NiceLogger,
                () => RoleTypes.Phantom,
                CustomRoleTypes.Crewmate,
                8700,
                (1, 2),
                SetupOptionItem,
                "NL",
                "#4a5c59",
                true
            );
        public NiceLogger(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
        static OptionItem OptionCoolTime;
        bool Taskmode;
        float Cooltime;
        string Room;
        Vector2 LogPos;
        Dictionary<int, PlayerControl> Log = new();
        static HashSet<NiceLogger> NiceLoggers = new();
        enum Option
        {
            NiceLoggerCoolTime
        }
        public override void Add()
        {
            Taskmode = false;
            LogPos = new(999f, 999f);
            Log.Clear();
            Cooltime = 0f;
            Room = "";

            NiceLoggers.Add(this);
        }
        public override void OnDestroy() => NiceLoggers.Clear();
        private static void SetupOptionItem()
        {
            OptionCoolTime = FloatOptionItem.Create(RoleInfo, 10, Option.NiceLoggerCoolTime, new(0f, 60f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        }
        public bool CanUseImpostorVentButton() => false;
        public bool CanUseSabotageButton() => false;
        public bool CanUseKillButton() => false;
        public override bool CanUseAbilityButton() => !Taskmode;
        public float CalculateKillCooldown() => 0f;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetVision(false);
            AURoleOptions.PhantomCooldown = 0.0001f;
        }
        public void OnClick(ref bool resetkillcooldown, ref bool? fall)
        {
            resetkillcooldown = true;
            fall = null;
            Dictionary<OpenableDoor, float> Distance = new();
            Vector2 position = Player.transform.position;
            foreach (var door in ShipStatus.Instance.AllDoors)
            {
                Distance.Add(door, Vector2.Distance(position, door.transform.position));
            }

            var logdoor = Distance.OrderByDescending(x => x.Value).LastOrDefault();

            LogPos = logdoor.Key.transform.position;
            Cooltime = 0;
            Room = GetString($"{logdoor.Key.Room}");

            var a = logdoor.Key.Room;
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        Player.StartCoroutine(Player.CoSetRole(RoleTypes.Crewmate, true));
                    if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                        Player.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                }
                Taskmode = true;
            }
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.2f, $"NiceLogger Set : {Room} ");
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;

            if (seen != seer) return "";
            if (isForMeeting) return "";

            if (Taskmode) return "";
            if (!Player.IsAlive()) return "";

            var s = "";
            if (!isForHud) s = "<size=50%>";
            return s + GetString("NiceLoggerLower") + (s == "" ? "" : "</size>");
        }
        public override void OnStartMeeting()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            if (Player.IsAlive() && LogPos != new Vector2(999f, 999f))
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                    Player.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());

                string Send = "<size=70%>";
                if (Log.Count != 0)
                    foreach (var log in Log.Values)
                    {
                        Send += string.Format(GetString("NiceLoggerAbility"), Utils.GetPlayerColor(log), Room);
                    }
                else Send += string.Format(GetString("NiceLoggerAbility2"), Room);

                _ = new LateTask(() => Utils.SendMessage(Send, Player.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.NiceLogger), GetString("NiceLoggerTitle"))), 4f, "NiceLoggerSned");
            }
        }
        public override void AfterMeetingTasks()
        {
            Log.Clear();
            LogPos = new(999f, 999f);
            Taskmode = false;
        }

        public override RoleTypes? AfterMeetingRole => RoleTypes.Phantom;
        public override bool CanTask() => Taskmode;
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!player.IsAlive()) return;
            Cooltime += Time.fixedDeltaTime;
        }
        public static void OnFixedUpdateOthers(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!player.IsAlive()) return;

            foreach (var logger in NiceLoggers)
            {
                if (!logger.Taskmode) continue;
                if (logger.LogPos.x == 999f) continue;

                var playerpos = player.transform.position;
                float targetDistance = Vector2.Distance(logger.LogPos, playerpos);
                if (targetDistance <= 0.5f && player.CanMove)
                {
                    if (logger.Cooltime >= OptionCoolTime.GetFloat())
                    {
                        logger.Log.Add(logger.Log.Count, player);
                        logger.Cooltime = 0f;
                    }
                }
            }
        }
        public override bool OverrideAbilityButton(out string text)
        {
            text = "NiceLogger_Ability";
            return true;
        }
        public override string GetAbilityButtonText() => GetString("NiceLogger_Ability");
    }
}