using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Ghost;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            roleText.transform.localScale = new(1f, 1f, 1f);
            roleText.fontSize = Main.RoleTextSize;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;

            Croissant.BaketheDough(__instance);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
    class SetColorPatch
    {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor)
        {
            //色変更バグ対策
            if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if (AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance, true);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType, ref bool canOverrideRole)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole().RemoveHtmlTags();
            canOverrideRole = true;
            Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                roleType = RoleTypes.Shapeshifter;
                return true;
            }
            if (!(ShipStatus.Instance?.enabled == true)) return true;
            if (AntiBlackout.IsSet)
            {
                Logger.Info($"AntiBlackoutが動作ちゅうだからキャンセル！", "RpcSetRole");
                return false;
            }
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                var targetIsKiller = target.GetRoleClass() is IKiller;
                var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
                foreach (var seer in PlayerCatch.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.GetRoleClass() is IKiller;

                    {
                        ghostRoles[seer] = RoleTypes.CrewmateGhost;
                    }
                }
                if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
                {
                    roleType = RoleTypes.CrewmateGhost;
                }
                else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
                {
                    roleType = RoleTypes.ImpostorGhost;
                }
                else
                {
                    foreach ((var seer, var role) in ghostRoles)
                    {
                        Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole().RemoveHtmlTags()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(role, seer.GetClientId());
                    }
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    public static class PlayerControlDiePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                // 死者の最終位置にペットが残るバグ対応
                __instance.RpcSetPet("");

                if (__instance.Is(CustomRoles.Amnesia))//アムネシア削除
                {
                    Amnesia.Kesu(__instance.PlayerId);
                }

                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
                {
                    if (!SuddenDeathMode.NowSuddenDeathMode)
                        if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId)//サボ可能役職のみインポスターゴーストにする
                            if (__instance.GetCustomRole().IsImpostor() || ((__instance.GetRoleClass() as IKiller)?.CanUseSabotageButton() ?? false))
                                _ = new LateTask(() =>
                                {
                                    if (!GameStates.Meeting)
                                        foreach (var Player in PlayerCatch.AllPlayerControls)
                                        {
                                            if (Player.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                            __instance.RpcSetRoleDesync(RoleTypes.ImpostorGhost, Player.GetClientId());
                                        }
                                }, 1.4f, "Fix sabotage", true);

                    _ = new LateTask(() => GhostRoleAssingData.AssignAddOnsFromList(), 1.4f, "Fix sabotage", true);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MixUpOutfit))]
    public static class PlayerControlMixupOutfitPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!__instance.IsAlive())
            {
                return;
            }
            // 自分がDesyncインポスターで，バニラ判定ではインポスターの場合，バニラ処理で名前が非表示にならないため，相手の名前を非表示にする
            if (
                PlayerControl.LocalPlayer.Data.Role.IsImpostor &&  // バニラ判定でインポスター
                !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) &&  // Mod判定でインポスターではない
                PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)  // Desyncインポスター
            {
                // 名前を隠す
                __instance.cosmetics.ToggleNameVisible(false);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
    public static class PlayerControlCheckSporeTriggerPatch
    {
        public static bool Prefix()
        {
            if (Options.DisableFungleSporeTrigger.GetBool())
            {
                return false;
            }
            return true;
        }
    }

}