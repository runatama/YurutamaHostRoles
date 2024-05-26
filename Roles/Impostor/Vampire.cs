using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Vampire : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Vampire),
                player => new Vampire(player),
                CustomRoles.Vampire,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                1300,
                SetupOptionItem,
                "va",
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
                from: From.TheOtherRoles
            );
        public Vampire(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillDelay = OptionKillDelay.GetFloat();

            BittenPlayers.Clear();
            Spped = SpeedDownCount.GetFloat();
            tmpSpeed = Main.NormalOptions.PlayerSpeedMod;
        }

        static OptionItem OptionKillDelay;
        static OptionItem SpeedDown;
        static OptionItem SpeedDownCount;
        enum OptionName
        {
            VampireKillDelay, VamSpeedDown, VamSpeedDownCount
        }

        static float KillDelay;
        static float Spped;
        static float tmpSpeed;
        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> BittenPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.VampireKillDelay, new(1f, 1000f, 1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            SpeedDown = BooleanOptionItem.Create(RoleInfo, 11, OptionName.VamSpeedDown, true, false);
            SpeedDownCount = FloatOptionItem.Create(RoleInfo, 12, OptionName.VamSpeedDownCount, new(0f, 1000f, 1f), 10f, false, SpeedDown)
            .SetValueFormat(OptionFormat.Seconds);
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait)) return;
            if (target.Is(CustomRoles.InSender)) return;
            if (info.IsFakeSuicide) return;

            //誰かに噛まれていなければ登録
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                killer.SetKillCooldown();
                BittenPlayers.Add(target.PlayerId, 0f);
            }
            info.DoKill = false;
        }
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BittenPlayers.ToArray())
            {
                if (timer >= KillDelay)
                {
                    var target = Utils.GetPlayerById(targetId);
                    KillBitten(target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    BittenPlayers[targetId] += Time.fixedDeltaTime;

                    if (SpeedDown.GetBool() && timer >= Spped)
                    {
                        var target = Utils.GetPlayerById(targetId);
                        if (target.IsAlive())
                        {
                            var x = KillDelay - Spped;
                            float Swariai = (KillDelay - Spped - (timer - Spped)) / x;
                            float Sp = tmpSpeed * Swariai;

                            if (KillDelay - timer <= 0.5f) Sp = Main.MinSpeed;//これは残り0,5sになったら静止させてｳｸﾞｯ...ｺｺﾏﾃﾞｶｯ...ってするやつ。

                            if (Sp >= Main.MinSpeed && Sp < tmpSpeed)
                            {
                                Main.AllPlayerSpeed[target.PlayerId] = Sp;
                                target.MarkDirtySettings();
                                //Logger.Info($"OK:{Sp}", "Vam");
                            }
                            //else
                            //Logger.Info($"Fall:{Sp}", "Vam");
                        }
                    }
                }
            }
        }
        public override void OnReportDeadBody(PlayerControl repo, GameData.PlayerInfo __)
        {
            foreach (var targetId in BittenPlayers.Keys)
            {
                var target = Utils.GetPlayerById(targetId);
                KillBitten(target, true);
                if (repo == target)
                {
                    ReportDeadBodyPatch.DieCheckReport(repo, __);
                }
            }
            BittenPlayers.Clear();
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("VampireBiteButtonText");
            return true;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Vampire_Kill";
            return true;
        }

        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            var vampire = Player;

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[target.PlayerId] = tmpSpeed;

                if (target.Is(CustomRoles.Speeding)) Main.AllPlayerSpeed[target.PlayerId] = AddOns.Common.Speeding.Speed;
                //RoleAddons
                if (RoleAddAddons.AllData.TryGetValue(target.GetCustomRole(), out var d) && d.GiveAddons.GetBool())
                {
                    if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[target.PlayerId] = d.Speed.GetFloat();
                }
                _ = new LateTask(() => target.MarkDirtySettings(), 0.9f, "Do-ki");
            }, 0.4f, "Modosu");

            if (target.IsAlive())
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bite;
                target.SetRealKiller(vampire);
                CustomRoleManager.OnCheckMurder(
                    vampire, target,
                    target, target
                );
                Logger.Info($"Vampireに噛まれている{target.name}を自爆させました。", "Vampire.KillBitten");
                if (!isButton && vampire.IsAlive())
                {
                    RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
                }
            }
            else
            {
                Logger.Info($"Vampireに噛まれている{target.name}はすでに死んでいました。", "Vampire.KillBitten");
            }

        }
    }
}
