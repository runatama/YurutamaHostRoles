using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Bomber : RoleBase, IImpostor, IUseTheShButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Bomber),
                player => new Bomber(player),
                CustomRoles.Bomber,
                () => RoleTypes.Shapeshifter,
                CustomRoleTypes.Impostor,
                1350,
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
            BomberExplosionMode = false;
        }

        static OptionItem OptionKillDelay;
        static OptionItem OptionBlastrange;
        static OptionItem OptionBomberExplosion;
        static OptionItem OptionCooldown;
        static OptionItem OptionOC;
        enum OptionName
        {
            BomberKillDelay,
            blastrange,
            BomberExplosion,
            Cooldown,
            UOcShButton
        }

        static float KillDelay;
        static float Blastrange = 1;
        int BomberExplosion;
        bool BomberExplosionMode;
        static float Cooldown;

        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> BomberExplosionPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.BomberKillDelay, new(1f, 1000f, 1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionBlastrange = FloatOptionItem.Create(RoleInfo, 11, OptionName.blastrange, new(1f, 30f, 0.5f), 1f, false);
            OptionBomberExplosion = IntegerOptionItem.Create(RoleInfo, 12, OptionName.BomberExplosion, new(1, 99, 1), 2, false);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 13, OptionName.Cooldown, new(0f, 999f, 1f), 0, false);
            OptionOC = BooleanOptionItem.Create(RoleInfo, 14, OptionName.UOcShButton, false, false);
        }

        private void SendRPC()
        {
            using var sender = CreateSender();
            sender.Writer.Write(BomberExplosion);
            sender.Writer.Write(BomberExplosionMode);
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            BomberExplosion = reader.ReadInt32();
            BomberExplosionMode = reader.ReadBoolean();
        }

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!BomberExplosionMode) return;
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait)) return;
            if (target.Is(CustomRoles.InSender)) return;
            if (info.IsFakeSuicide) return;

            //誰かに噛まれていなければ登録
            if (!BomberExplosionPlayers.ContainsKey(target.PlayerId))
            {
                BomberExplosion--;
                SendRPC();
                BomberExplosionMode = false;
                killer.RpcResetAbilityCooldown();
                killer.SetKillCooldown();
                BomberExplosionPlayers.Add(target.PlayerId, 0f);
                Utils.NotifyRoles(SpecifySeer: Player);
            }
            info.DoKill = false;
        }
        public override void OnShapeshift(PlayerControl target)
        {
            if (target.PlayerId != Player.PlayerId && 0 < BomberExplosion && !UseOCButton)
            {
                BomberExplosionMode = !BomberExplosionMode;
            }
        }
        public void OnClick()
        {
            var target = Player.GetKillTarget();
            if (0 >= BomberExplosion || target == null) return;
            BomberExplosion--;
            SendRPC();
            BomberExplosionMode = false;
            Player.RpcResetAbilityCooldown();
            Player.RpcProtectedMurderPlayer(target);
            BomberExplosionPlayers.Add(target.PlayerId, 0f);
            Utils.NotifyRoles(SpecifySeer: Player);
        }
        public override string GetProgressText(bool comms = false) => BomberExplosionMode ? "<color=red>◆" : "" + Utils.ColorString(0 < BomberExplosion ? Color.red : Color.gray, $"({BomberExplosion})");
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BomberExplosionPlayers.ToArray())
            {
                if (timer >= KillDelay)
                {
                    var target = Utils.GetPlayerById(targetId);
                    if (target.IsAlive())
                    {
                        var pos = target.transform.position;
                        foreach (var target2 in Main.AllAlivePlayerControls)
                        {
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

        public bool UseOCButton => OptionOC.GetBool();
        public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
        {
            BomberExplosionPlayers.Clear();
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("BomberButtonText");
            if (!BomberExplosionMode) return false;
            return true;
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = Cooldown;
            AURoleOptions.ShapeshifterLeaveSkin = false;
            AURoleOptions.ShapeshifterDuration = 1;
        }
    }
}
