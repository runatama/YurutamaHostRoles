using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using Mathf = UnityEngine.Mathf;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Ghost;
using static TownOfHost.Options;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
        public static void SetDirty(byte playerId) =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .Where(sender => sender.player.PlayerId == playerId)
            .ToList().ForEach(sender => sender.SetDirty());
        public static void SetDirtyToAll() =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .ToList().ForEach(sender => sender.SetDirty());

        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV08(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            if (player.AmOwner)
            {
                var opt = BuildGameOptions();
                foreach (var com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast<LogicOptions>(out var lo))
                        lo.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else base.SendGameOptions();
        }

        public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, player.GetClientId());
                }
            }
        }
        public static void RemoveSender(PlayerControl player)
        {
            var sender = AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
            if (sender == null) return;
            sender.player = null;
            AllSenders.Remove(sender);
        }
        public override IGameOptions BuildGameOptions()
        {
            if (Main.RealOptionsData == null)
            {
                if (GameOptionsManager.Instance.CurrentGameOptions == null)
                {
                    Logger.Error($"CurrentGameOptionsがnullだ", "Pl.BuildGameOptions");
                    return BasedGameOptions;
                }
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;
            if (BasedGameOptions == null)
            {
                Logger.Error($"BasedGameOptionsがnullだ", "Pl.BuildGameOptions");
                return opt;
            }


            AURoleOptions.SetOpt(opt);

            AURoleOptions.NoisemakerImpostorAlert = true;
            AURoleOptions.NoisemakerAlertDuration = GhostNoiseSender.NoisTime.GetFloat();

            if (player == null)
            {
                Logger.Error($"playerがnullだ", "Pl.BuildGameOptions");
                return opt;
            }
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            if (state == null)
            {
                Logger.Error($"stateがnullやで", "Pl.BuildGameOptions");
                return opt;
            };
            opt.SetInt(Int32OptionNames.NumEmergencyMeetings, (int)state.NumberOfRemainingButtons);
            opt.BlackOut(state.IsBlackOut);

            var HasLithing = player.Is(CustomRoles.Lighting);
            var HasMoon = player.Is(CustomRoles.Moon);

            CustomRoles role = player.GetCustomRole();
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    AURoleOptions.ShapeshifterCooldown = DefaultShapeshiftCooldown.GetFloat();
                    break;
                case CustomRoleTypes.Madmate:
                    AURoleOptions.EngineerCooldown = MadmateVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = MadmateVentMaxTime.GetFloat();
                    HasLithing |= MadmateHasLighting.GetBool();
                    HasMoon |= MadmateHasMoon.GetBool();
                    if (MadmateCanSeeOtherVotes.GetBool())
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
            }

            var roleClass = player.GetRoleClass();
            if (roleClass == null)
            {
                Logger.Error($"roleClassがnullだ", "Pl.BuildGameOptions");
                //    return opt;
            }

            if (player.Is(CustomRoles.MagicHand))
                opt.SetInt(Int32OptionNames.KillDistance, MagicHand.KillDistance.GetInt());

            //キルレンジ
            if (OverrideKilldistance.AllData.TryGetValue(role, out var killdistance))
                opt.SetInt(Int32OptionNames.KillDistance, killdistance.Killdistance.GetInt());

            if (Amnesia.CheckAbility(player))
                roleClass?.ApplyGameOptions(opt);

            foreach (var subRole in player.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.LastImpostor:
                        if (LastImpostor.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        if (OverrideKilldistance.AllData.TryGetValue(CustomRoles.LastImpostor, out var kd))
                            opt.SetInt(Int32OptionNames.KillDistance, kd.Killdistance.GetInt());
                        break;
                    case CustomRoles.LastNeutral:
                        if (LastNeutral.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        if ((roleClass is ILNKiller || LastNeutral.ChKilldis.GetBool()) && OverrideKilldistance.AllData.TryGetValue(CustomRoles.LastNeutral, out var killd))
                            opt.SetInt(Int32OptionNames.KillDistance, killd.Killdistance.GetInt());

                        break;
                    case CustomRoles.watching:
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                }
            }

            //書く役職の処
            if (RoleAddAddons.GetRoleAddon(role, out var data, player, subrole: [CustomRoles.Lighting, CustomRoles.Moon, CustomRoles.watching]))
            {
                //Wac
                if (data.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                if (!data.IsImpostor)
                {
                    HasLithing |= data.GiveLighting.GetBool();
                    HasMoon |= data.GiveMoon.GetBool();
                }
            }

            var isElectrical = Utils.IsActive(SystemTypes.Electrical);

            //Moon
            if (HasMoon)
                if (isElectrical)
                {
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision * AURoleOptions.ElectricalCrewVision);
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                }

            //ホストだとまだﾋﾟｯｶｰﾝしちゃうのどうにかしたい。
            //Lighting
            if (HasLithing)
            {
                if (isElectrical && HasMoon)
                {
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision * (role.GetRoleTypes().IsCrewmate() ? AURoleOptions.ElectricalCrewVision : 5f));
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                }
                else//停電時はクルー視界
                if (isElectrical)
                {
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultCrewmateVision);
                }
                else
                {
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision);
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                };
            }

            //キルクール0に設定+修正する設定をONにしたと気だけ呼び出す。
            if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
            {
                //設定が有効で、キルボタンが使用可能の時は最小0.000...1　設定無効 or キルボタンが使用不可なら最小0
                AURoleOptions.KillCooldown = Mathf.Max(FixZeroKillCooldown.GetBool() && ((roleClass as IKiller)?.CanUseKillButton() == true) ? 0.00000000000000000000000000000000000000000001f : 0f, killCooldown);
            }
            if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
            {
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 10f);
            }

            state.taskState.hasTasks = UtilsTask.HasTasks(player.Data, false);
            if ((GhostCanSeeOtherVotes.GetBool() || !GhostOptions.GetBool()) && player.Data.IsDead && !player.Is(CustomRoles.AsistingAngel) && (!player.IsGorstRole() || GRCanSeeOtherVotes.GetBool()))
                opt.SetBool(BoolOptionNames.AnonymousVotes, false);
            if (AdditionalEmergencyCooldown.GetBool() && AdditionalEmergencyCooldownThreshold.GetInt() <= PlayerCatch.AllAlivePlayersCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown, AdditionalEmergencyCooldownTime.GetInt());
            }
            if (SyncButtonMode.GetBool() && SyncedButtonCount.GetValue() <= UsedButtonCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
                opt.SetInt(Int32OptionNames.NumEmergencyMeetings, 0);
            }
            if ((CurrentGameMode == CustomGameMode.HideAndSeek || IsStandardHAS) && HideAndSeekKillDelayTimer > 0)
            {
                if (!Main.HnSFlag)
                {
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
                    if (player.Is(CountTypes.Impostor))
                    {
                        AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
                    }
                }
            }
            if (CurrentGameMode == CustomGameMode.TaskBattle && TaskBattleCanVent.GetBool())
            {
                opt.SetFloat(FloatOptionNames.EngineerCooldown, TaskBattleVentCooldown.GetFloat());
                AURoleOptions.EngineerInVentMaxTime = 0;
            }
            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.PhantomCooldown = Mathf.Max(1f, AURoleOptions.PhantomCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;
            AURoleOptions.ImpostorsCanSeeProtect = false;
            AURoleOptions.ShapeshifterLeaveSkin = false;


            //幽霊役職用の奴
            if (player.IsGorstRole())
            {
                var gr = PlayerState.GetByPlayerId(player.PlayerId).GhostRole;
                switch (gr)
                {
                    case CustomRoles.Ghostbuttoner: AURoleOptions.GuardianAngelCooldown = CoolDown(Ghostbuttoner.CoolDown.GetFloat()); break;
                    case CustomRoles.GhostNoiseSender: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostNoiseSender.CoolDown.GetFloat()); break;
                    case CustomRoles.GhostReseter: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostReseter.CoolDown.GetFloat()); break;
                    case CustomRoles.GuardianAngel: AURoleOptions.GuardianAngelCooldown = CoolDown(GuardianAngel.CoolDown.GetFloat()); break;
                    case CustomRoles.DemonicTracker: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicTracker.CoolDown.GetFloat()); break;
                    case CustomRoles.DemonicCrusher: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicCrusher.CoolDown.GetFloat()); break;
                    case CustomRoles.DemonicVenter: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicVenter.CoolDown.GetFloat()); break;
                    case CustomRoles.AsistingAngel: AURoleOptions.GuardianAngelCooldown = CoolDown(AsistingAngel.CoolDown.GetFloat()); break;
                }
            }
            return opt;

            float CoolDown(float cool) => Mathf.Max(1f, cool);
        }

        public override bool AmValid()
        {
            //キルクとか反映されないから～
            return base.AmValid() && player != null && (!player.Data.Disconnected || !SelectRolesPatch.Disconnected.Contains(player.PlayerId)) && Main.RealOptionsData != null;
        }
    }
}