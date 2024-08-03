using System;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using TownOfHost.Roles.AddOns.Common;
using Rewired;
//using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    class SetUpRoleTextPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            if (!GameStates.IsModHost) return;
            _ = new LateTask(() =>
            {
                CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
                if (PlayerControl.LocalPlayer.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && PlayerControl.LocalPlayer.GetRoleClass() != null) role = PlayerControl.LocalPlayer.GetRoleClass().Jikaku();

                if (!role.IsVanilla() && !PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
                {
                    __instance.YouAreText.color = Utils.GetRoleColor(role);
                    __instance.RoleText.text = Utils.GetRoleName(role);
                    __instance.RoleText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.color = Utils.GetRoleColor(role);

                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
                }

                foreach (var subRole in PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SubRoles)
                {
                    if (subRole == CustomRoles.Amnesia) continue;
                    __instance.RoleBlurbText.text += "<size=75%>\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));
                }
                __instance.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId, amkesu: true);

            }, 0.01f, "Override Role Text");

        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    class CoBeginPatch
    {
        public static void Prefix()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.Amnesia))
                {
                    if (pc.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false && pc.Is(CustomRoleTypes.Crewmate))
                    {
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                            continue;
                        }
                        pc.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                        continue;
                    }
                    if (Amnesia.DontCanUseAbility.GetBool())
                    {
                        if (pc.Is(CustomRoleTypes.Impostor))
                        {
                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                            {
                                RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Impostor);
                                continue;
                            }
                            pc.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId());
                            continue;
                        }
                        else
                        {
                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                            {
                                RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                                continue;
                            }
                            pc.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                            continue;
                        }
                    }
                }
            }
            var logger = Logger.Handler("Info");
            logger.Info("------------名前表示------------");
            foreach (var pc in Main.AllPlayerControls)
            {
                logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
                pc.cosmetics.nameText.text = pc.name;
            }
            logger.Info("----------役職割り当て----------");
            foreach (var pc in Main.AllPlayerControls)
            {
                logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
            }
            logger.Info("--------------環境--------------");
            foreach (var pc in Main.AllPlayerControls)
            {
                try
                {
                    var text = pc.AmOwner ? "[*]" : "   ";
                    text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                    if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                        text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                    else text += ":Vanilla";
                    logger.Info(text);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Platform");
                }
            }
            logger.Info("------------基本設定------------");
            var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
            foreach (var t in tmp) logger.Info(t);
            logger.Info("------------詳細設定------------");
            foreach (var o in OptionItem.AllOptions)
                if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.GetBool()))
                    logger.Info($"{(o.Parent == null ? o.Name.PadRightV2(40) : $"┗ {o.Name}".PadRightV2(41))}:{o.GetString().RemoveHtmlTags()}");
            logger.Info("-------------その他-------------");
            logger.Info($"プレイヤー数: {Main.AllPlayerControls.Count()}人");
            Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
            GameData.Instance.RecomputeTaskCounts();
            TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

            Utils.NotifyRoles();
            GameStates.InGame = true;
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral))
            {
                //ぼっち役職
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                teamToDisplay = soloTeam;
            }
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            //チーム表示変更
            CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
            var pc = PlayerControl.LocalPlayer;
            var ch = pc.GetRoleClass();
            if (ch?.Jikaku() != CustomRoles.NotAssigned && pc.GetRoleClass() != null) role = ch.Jikaku();

            if (role.GetRoleInfo()?.IntroSound is AudioClip introSound)
            {
                PlayerControl.LocalPlayer.Data.Role.IntroSound = introSound;
            }

            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Neutral:
                    __instance.TeamTitle.text = GetString("Neutral");
                    __instance.TeamTitle.color = Palette.DisabledGrey;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("NeutralInfo");
                    if (!pc.Is(CustomRoles.Amnesia)) StartFadeIntro(__instance, Palette.DisabledGrey, Utils.GetRoleColor(role), 2000);
                    else __instance.BackgroundBar.material.color = Palette.DisabledGrey;
                    break;
                case CustomRoleTypes.Madmate:
                    __instance.TeamTitle.text = pc.Is(CustomRoles.Amnesia) ? GetString("Neutral") : GetString("Madmate");
                    __instance.TeamTitle.color = pc.Is(CustomRoles.Amnesia) ? Palette.DisabledGrey : Utils.GetRoleColor(CustomRoles.Madmate);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = pc.Is(CustomRoles.Amnesia) ? GetString("NeutralInfo") : GetString("MadmateInfo");
                    if (!pc.Is(CustomRoles.Amnesia)) StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed, 2000);
                    else __instance.BackgroundBar.material.color = Palette.DisabledGrey;
                    break;
            }
            switch (role)
            {
                case CustomRoles.Sheriff:
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    var numImpostors = Main.NormalOptions.NumImpostors;
                    var text = numImpostors == 1
                        ? GetString(StringNames.NumImpostorsS)
                        : string.Format(GetString(StringNames.NumImpostorsP), numImpostors);
                    __instance.ImpostorText.text = text.Replace("[FF1919FF]", "<color=#FF1919FF>").Replace("[]", "</color>");
                    break;
                case CustomRoles.WolfBoy:
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    var WnumImpostors = Main.NormalOptions.NumImpostors;
                    var Wtext = WnumImpostors == 1
                        ? GetString(StringNames.NumImpostorsS)
                        : string.Format(GetString(StringNames.NumImpostorsP), WnumImpostors);
                    __instance.ImpostorText.text = Wtext.Replace("[808080]", "<color=#808080>").Replace("[]", "</color>");
                    break;

                case CustomRoles.GM:
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(false);
                    break;

                case CustomRoles.TaskPlayerB:
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(false);
                    break;

            }

            /*if (Input.GetKey(KeyCode.RightShift))
            {
                __instance.TeamTitle.text = "<size=13>" + Main.ModName;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "";
                __instance.TeamTitle.color = Color.cyan;
                StartFadeIntro(__instance, Color.blue, Color.cyan);
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                __instance.TeamTitle.text = "Discord Server";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://discord.gg/v8SFfdebpz";
                __instance.TeamTitle.color = Color.magenta;
                StartFadeIntro(__instance, Color.magenta, Color.magenta);
            }*/

            if (pc.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && pc.GetRoleClass() != null)
            {
                role = pc.GetRoleClass().Jikaku();
                if (role.GetRoleInfo()?.IntroSound is AudioClip intro)
                {
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = intro;
                }
            }
            if (pc.Is(CustomRoles.Amnesia))
            {
                PlayerControl.LocalPlayer.Data.Role.IntroSound = Amnesia.DontCanUseAbility.GetBool() ?
                (role.IsImpostor() ? RoleBase.GetIntrosound(RoleTypes.Impostor) : RoleBase.GetIntrosound(RoleTypes.Crewmate)) : RoleBase.GetIntrosound(role.GetRoleInfo().BaseRoleType.Invoke());
            }
        }
        private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end, int t = 1000)
        {
            __instance.BackgroundBar.material.color = start;
            await Task.Delay(t);
            int milliseconds = 0;
            while (true)
            {
                await Task.Delay(20);
                milliseconds += 20;
                float time = (float)milliseconds / (float)500;
                Color LerpingColor = Color.Lerp(start, end, time);
                if (__instance == null || milliseconds > 500)
                {
                    Logger.Info("ループを終了します", "StartFadeIntro");
                    break;
                }
                __instance.BackgroundBar.material.color = LerpingColor;
            }
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    class BeginImpostorPatch
    {
        public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Sheriff) || PlayerControl.LocalPlayer.Is(CustomRoles.WolfBoy))
            {
                //シェリフの場合はキャンセルしてBeginCrewmateに繋ぐ
                yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                yourTeam.Add(PlayerControl.LocalPlayer);
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (!pc.AmOwner) yourTeam.Add(pc);
                }
                __instance.BeginCrewmate(yourTeam);
                __instance.overlayHandle.color = Palette.CrewmateBlue;
                return false;
            }
            BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
            return true;
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    class IntroCutsceneDestroyPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            if (!GameStates.IsInGame) return;

            Main.introDestroyed = true;
            var mapId = Main.NormalOptions.MapId;
            // エアシップではまだ湧かない
            if ((MapNames)mapId != MapNames.Airship)
            {
                foreach (var state in PlayerState.AllPlayerStates.Values)
                {
                    state.HasSpawned = true;
                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    _ = new LateTask(() =>
                    {
                        if (GameStates.InGame)
                            foreach (var pc in Main.AllPlayerControls)
                            {
                                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;

                                if (pc.Is(CustomRoles.Amnesia))//continueでいいかもだけど一応...
                                {
                                    if (pc.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false && pc.Is(CustomRoleTypes.Crewmate))
                                    {
                                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                                        {
                                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                                            continue;
                                        }
                                        pc.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                                        continue;
                                    }
                                    if (Amnesia.DontCanUseAbility.GetBool())
                                    {
                                        if (pc.Is(CustomRoleTypes.Impostor))
                                        {
                                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                                            {
                                                RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Impostor);
                                                continue;
                                            }
                                            pc.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId());
                                            continue;
                                        }
                                        else
                                        {
                                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                                            {
                                                RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                                                continue;
                                            }
                                            pc.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                                            continue;
                                        }
                                    }
                                }
                                if (pc == PlayerControl.LocalPlayer && (pc.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false)) continue;
                                pc.RpcSetRoleDesync(pc.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke(), pc.GetClientId());
                            }

                        _ = new LateTask(() =>
                        {
                            foreach (var pc in Main.AllPlayerControls)
                            {
                                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
                                (pc.GetRoleClass() as Roles.Core.Interfaces.IUseTheShButton)?.Shape(pc);
                            }
                            _ = new LateTask(() =>
                            {
                                foreach (var pc in Main.AllPlayerControls)
                                {
                                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
                                    if (pc == null) continue;
                                    var ri = pc.GetCustomRole().GetRoleInfo();
                                    if (ri?.BaseRoleType.Invoke() == RoleTypes.Shapeshifter || ri?.BaseRoleType.Invoke() == RoleTypes.Engineer)
                                        pc.RpcResetAbilityCooldown();
                                    Utils.NotifyRoles();
                                }
                            }, 0.2f, "ResetCool");
                        }, 0.2f, "Use On click Shepe");
                    }, 0.5f, "Set Rolet");
                }

                if (mapId != 4)
                {
                    if (Options.FixFirstKillCooldown.GetBool())
                        _ = new LateTask(() =>
                        {
                            Main.AllPlayerControls.Do(pc => pc.SetKillCooldown(Main.AllPlayerKillCooldown[pc.PlayerId] - 2f, delay: true));
                        }, 2f, "FixKillCooldownTask");
                    else _ = new LateTask(() =>
                        {
                            Main.AllPlayerControls.Do(pc => pc.SetKillCooldown(10f, kyousei: true, delay: true));
                        }, 0.7f, "FixKillCooldownTask");
                    GameStates.Intro = false;
                    GameStates.AfterIntro = true;
                }
                if (!(Options.CurrentGameMode is CustomGameMode.Standard && Main.SetRoleOverride))
                    _ = new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "SetImpostorForServer");
                if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
                {
                    PlayerControl.LocalPlayer.RpcExile();
                    PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetDead();
                }

                if (RandomSpawn.IsRandomSpawn())
                {
                    RandomSpawn.SpawnMap map;
                    switch (mapId)
                    {
                        case 0:
                            map = new RandomSpawn.SkeldSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 1:
                            map = new RandomSpawn.MiraHQSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 2:
                            map = new RandomSpawn.PolusSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 5:
                            map = new RandomSpawn.FungleSpawnMap();
                            Main.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                    }
                }

                foreach (var kvp in PlayerState.AllPlayerStates)
                {
                    kvp.Value.IsBlackOut = false;
                    if (Utils.GetPlayerById(kvp.Key) != null)
                        Utils.GetPlayerById(kvp.Key).MarkDirtySettings();
                }
                //役職選定後に処理する奴。
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Speeding)) Main.AllPlayerSpeed[pc.PlayerId] = Speeding.Speed;
                    //RoleAddons
                    if (RoleAddAddons.AllData.TryGetValue(pc.GetCustomRole(), out var d) && d.GiveAddons.GetBool())
                    {
                        if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[pc.PlayerId] = d.Speed.GetFloat();
                    }
                }

                // そのままだとホストのみDesyncImpostorの暗室内での視界がクルー仕様になってしまう
                var roleInfo = PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo();
                var amDesyncImpostor = roleInfo?.IsDesyncImpostor == true;
                if (amDesyncImpostor)
                {
                    PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
                }
                _ = new LateTask(() => Utils.DelTask(), 1.25f, "Fix all task");
                GameStates.task = true;

                //desyneインポかつ置き換えがimp以外ならそれにする。
                if (roleInfo?.IsDesyncImpostor ?? false && PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke() != RoleTypes.Impostor)
                    RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke());

                _ = new LateTask(() =>
                {
                    foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                    {
                        role.StartGameTasks();
                    }
                }, 0.3f, "");

                _ = new LateTask(() =>
                {
                    foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                        role.Colorchnge();
                }, 0.15f, "Color and Black");

                _ = new LateTask(() =>
                {
                    foreach (var s in PlayerState.AllPlayerStates)
                    {
                        if (s.Value == null) continue;
                        s.Value.IsBlackOut = false;
                        if (Utils.GetPlayerById(s.Key) == null) continue;
                        Utils.GetPlayerById(s.Key).SyncSettings();
                        Utils.NotifyRoles();
                    }
                    Utils.NotifyRoles();
                }, 1.2f, "");

                if (Options.Onlyseepet.GetBool()) Main.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
                if (AmongUsClient.Instance.AmHost) RemoveDisableDevicesPatch.UpdateDisableDevices();
            }
            Logger.Info("OnDestroy", "IntroCutscene");
        }
    }
}