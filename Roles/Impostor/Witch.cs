using System.Collections.Generic;
using System.Text;
using Hazel;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Witch : RoleBase, IImpostor, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Witch),
                player => new Witch(player),
                CustomRoles.Witch,
                () => ((SwitchTrigger)OptionModeSwitchAction.GetValue() is SwitchTrigger.OnPhantom or SwitchTrigger.WitchOcButton) ? RoleTypes.Phantom : RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                9300,
                SetupOptionItem,
                "wi",
                from: From.TheOtherRoles
            );
        public Witch(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            cool = OptionShcool.GetFloat();
            occool = cool;
        }
        public override void OnDestroy()
        {
            Witches.Clear();
            SpelledPlayer.Clear();
            CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
        }
        public static OptionItem OptionModeSwitchAction;
        public static OptionItem OptionShcool;
        enum OptionName
        {
            WitchModeSwitchAction,
        }
        public enum SwitchTrigger
        {
            TriggerKill,
            TriggerVent,
            TriggerDouble,
            OnPhantom,
            WitchOcButton,
        };

        public bool IsSpellMode;
        public float cool;
        private float occool;
        public List<byte> SpelledPlayer = new();
        public static SwitchTrigger NowSwitchTrigger;

        public static List<Witch> Witches = new();
        public static void SetupOptionItem()
        {
            OptionModeSwitchAction = StringOptionItem.Create(RoleInfo, 10, OptionName.WitchModeSwitchAction, EnumHelper.GetAllNames<SwitchTrigger>(), 0, false);
            OptionShcool = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.PhantomCooldown = NowSwitchTrigger is SwitchTrigger.WitchOcButton ? occool : 0;
        }
        public override void Add()
        {
            IsSpellMode = false;
            SpelledPlayer.Clear();
            NowSwitchTrigger = (SwitchTrigger)OptionModeSwitchAction.GetValue();
            Witches.Add(this);
            Player.AddDoubleTrigger();

        }
        private void SendRPC(bool doSpell, byte target = 255)
        {
            using var sender = CreateSender();
            sender.Writer.Write(doSpell);
            if (doSpell)
            {
                sender.Writer.Write(target);
            }
            else
            {
                sender.Writer.Write(IsSpellMode);
            }
        }

        public override void ReceiveRPC(MessageReader reader)
        {
            var doSpel = reader.ReadBoolean();
            if (doSpel)
            {
                var spelledId = reader.ReadByte();
                if (spelledId == 255)
                {
                    SpelledPlayer.Clear();
                }
                else
                {
                    SpelledPlayer.Add(spelledId);
                }
            }
            else
            {
                IsSpellMode = reader.ReadBoolean();
            }
        }
        public void SwitchSpellMode(bool kill)
        {
            bool needSwitch = false;
            switch (NowSwitchTrigger)
            {
                case SwitchTrigger.TriggerKill:
                    needSwitch = kill;
                    break;
                case SwitchTrigger.TriggerVent:
                    needSwitch = !kill;
                    break;
                case SwitchTrigger.OnPhantom:
                    needSwitch = !kill;
                    break;
            }
            if (needSwitch)
            {
                IsSpellMode = !IsSpellMode;
                SendRPC(false);
                UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
            }
        }
        public static bool IsSpelled(byte target = 255)
        {
            foreach (var witch in Witches)
            {
                if (target == 255 && witch.SpelledPlayer.Count != 0) return true;

                if (witch.SpelledPlayer.Contains(target))
                {
                    return true;
                }
            }
            return false;
        }
        public override string MeetingMeg()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return "";
            if (SpelledPlayer.Count == 0) return "";

            var r = GetString("Skill.Witchf").Color(Palette.ImpostorRed) + "\n";
            var tg = new List<byte>();

            foreach (var pc in SpelledPlayer)
            {
                if (pc == byte.MaxValue) continue;
                if (tg.Contains(pc)) continue;
                tg.Add(pc);
                r += (tg.Count == 0 ? "" : ",") + $"{Utils.GetPlayerColor(pc)}";
            }
            return r + GetString("Skill.WitchO");
        }
        public void SetSpelled(PlayerControl target)
        {
            if (target.Is(CustomRoles.King)) return;

            if (!IsSpelled(target.PlayerId))
            {
                SpelledPlayer.Add(target.PlayerId);
                SendRPC(true, target.PlayerId);
                //キルクールの適正化
                Player.SetKillCooldown();
            }
        }
        public bool UseOneclickButton => NowSwitchTrigger is SwitchTrigger.OnPhantom or SwitchTrigger.WitchOcButton;
        public void OnClick(ref bool resetkillcooldown, ref bool? fall)
        {
            if (NowSwitchTrigger is SwitchTrigger.WitchOcButton)
            {
                fall = false;
                var target = Player.GetKillTarget(true);
                if (target != null)
                {
                    var targetroleclass = target?.GetRoleClass();
                    if (targetroleclass is SchrodingerCat schrodingerCat)
                    {
                        if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
                        {
                            schrodingerCat.ChangeTeamOnKill(Player);
                            Player.SetKillCooldown(target: schrodingerCat.Player);
                            return;
                        }
                    }
                    if (targetroleclass is BakeCat bakeneko)
                    {
                        if (bakeneko.Team == BakeCat.TeamType.None)
                        {
                            bakeneko.ChangeTeamOnKill(Player);
                            Player.SetKillCooldown(target: bakeneko.Player);
                            return;
                        }
                    }

                    SetSpelled(target);
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                }
                occool = target is null ? 0 : cool;
                resetkillcooldown = target != null;
                Player.MarkDirtySettings();
                Player.RpcResetAbilityCooldown();
            }
            else
            if (NowSwitchTrigger is SwitchTrigger.OnPhantom)
            {
                fall = true;
                resetkillcooldown = false;
                SwitchSpellMode(false);
            }
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var (killer, target) = info.AttemptTuple;
            if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
            {
                info.DoKill = killer.CheckDoubleTrigger(target, () => { SetSpelled(target); });
            }
            else
            {
                if (IsSpellMode)
                {//呪いならキルしない
                    info.DoKill = false;
                    SetSpelled(target);
                }
                SwitchSpellMode(true);
            }
            //切れない相手ならキルキャンセル
            info.DoKill &= info.CanKill;
        }
        public override void AfterMeetingTasks()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            if (Player.IsAlive() && MyState.DeathReason != CustomDeathReason.Vote)
            {//吊られなかった時呪いキル発動
                var spelledIdList = new List<byte>();
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (SpelledPlayer.Contains(pc.PlayerId) && !Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                    {
                        pc.SetRealKiller(Player);
                        spelledIdList.Add(pc.PlayerId);
                    }
                }
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Spell, spelledIdList.ToArray());
            }
            //実行してもしなくても呪いはすべて解除
            SpelledPlayer.Clear();
            if (!AmongUsClient.Instance.AmHost) return;
            SendRPC(true);
            if (occool is 0)
            {
                occool = cool;
                Player.MarkDirtySettings();
            }
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting && IsSpelled(seen.PlayerId))
            {
                return Utils.ColorString(Palette.ImpostorRed, "†");
            }
            return "";
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (!Is(seen) || isForMeeting) return "";
            if (NowSwitchTrigger is SwitchTrigger.WitchOcButton) return "";

            var sb = new StringBuilder();
            sb.Append(isForHud ? GetString("WitchCurrentMode") : "Mode:");
            if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
            {
                sb.Append(GetString("WitchModeDouble"));
            }
            else
            {
                sb.Append(IsSpellMode ? GetString("WitchModeSpell") : GetString("WitchModeKill"));
            }
            return sb.ToString();
        }
        public bool OverrideKillButtonText(out string text)
        {
            if (NowSwitchTrigger != SwitchTrigger.TriggerDouble && IsSpellMode)
            {
                text = GetString("WitchSpellButtonText");
                return true;
            }
            text = default;
            return false;
        }
        public override string GetAbilityButtonText()
        {
            return GetString("WitchSpellButtonText");
        }
        public override bool OnEnterVent(PlayerPhysics physics, int ventId)
        {
            if (NowSwitchTrigger is SwitchTrigger.TriggerVent)
            {
                SwitchSpellMode(false);
            }
            return true;
        }
    }
}