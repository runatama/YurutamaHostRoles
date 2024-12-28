using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Bomber : RoleBase, IImpostor, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Bomber),
                player => new Bomber(player),
                CustomRoles.Bomber,
                () => RoleTypes.Phantom,
                CustomRoleTypes.Impostor,
                5100,
                SetupOptionItem,
                "bb"
            );
        public Bomber(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillDelay = OptionKillDelay.GetFloat();
            Blastrange = OptionBlastrange.GetFloat();
            BomberExplosionPlayers.Clear();
            BomberExplosion = OptionBomberExplosion.GetInt();
            Cooldown = OptionCooldown.GetFloat();
        }

        static OptionItem OptionKillDelay;
        static OptionItem OptionBlastrange;
        static OptionItem OptionBomberExplosion;
        static OptionItem OptionCooldown;
        enum OptionName
        {
            BomberKillDelay,
            blastrange,
            BomberExplosion
        }

        static float KillDelay;
        static float Blastrange = 1;
        int BomberExplosion;
        static float Cooldown;

        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> BomberExplosionPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.BomberKillDelay, new(1f, 1000f, 1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionBlastrange = FloatOptionItem.Create(RoleInfo, 11, OptionName.blastrange, new(1f, 30f, 0.5f), 1f, false).SetValueFormat(OptionFormat.Multiplier);
            OptionBomberExplosion = IntegerOptionItem.Create(RoleInfo, 12, OptionName.BomberExplosion, new(1, 99, 1), 2, false);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.Cooldown, new(0f, 999f, 0.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
        }

        private void SendRPC()
        {
            using var sender = CreateSender();
            sender.Writer.Write(BomberExplosion);
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            BomberExplosion = reader.ReadInt32();
        }
        public void OnClick(ref bool resetkillcooldown, ref bool? fall)
        {
            if (BomberExplosion <= 0) return;
            fall = true;
            var target = Player.GetKillTarget(true);
            Logger.Info($"{Player?.Data?.PlayerName ?? "???"} => {target?.Data?.PlayerName ?? "失敗"}", "Bomber");
            if (target == null || BomberExplosionPlayers.ContainsKey(target?.PlayerId ?? byte.MaxValue)) return;

            resetkillcooldown = true;
            if (target.Is(CustomRoles.King)) return;
            if (!BomberExplosionPlayers.TryAdd(target.PlayerId, 0f)) return;
            BomberExplosion--;
            SendRPC();
            Player.RpcResetAbilityCooldown(kousin: true);
            Player.SetKillCooldown(target: target);
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
        public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(0 < BomberExplosion ? Color.red : Color.gray, $"({BomberExplosion})");
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask || !Player.IsAlive()) return;

            foreach (var (targetId, timer) in BomberExplosionPlayers.ToArray())
            {
                if (timer >= KillDelay)
                {
                    var target = PlayerCatch.GetPlayerById(targetId);
                    if (target.IsAlive())
                    {
                        var pos = target.transform.position;
                        foreach (var target2 in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (target2.Is(CustomRoles.King)) continue;
                            var dis = Vector2.Distance(pos, target2.transform.position);
                            if (dis > Blastrange) continue;
                            if (target2.IsAlive())
                            {
                                PlayerState.GetByPlayerId(target2.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                target2.SetRealKiller(Player);
                                target2.RpcMurderPlayer(target2, true);
                                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                                Logger.Info($"{target.name}を爆発させました。", "bomber");
                            }
                        }
                    }

                    BomberExplosionPlayers.Remove(targetId);
                }
                else
                {
                    BomberExplosionPlayers[targetId] += Time.fixedDeltaTime;
                }
            }
        }

        public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
        {
            BomberExplosionPlayers.Clear();
        }
        public override bool OverrideAbilityButton(out string text)
        {
            text = "Bomber_Ability";
            return true;
        }
        public override string GetAbilityButtonText()
            => GetString("BomberAbilitytext");
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.PhantomCooldown = BomberExplosion <= 0 ? 200f : Cooldown;
        }
    }
}
