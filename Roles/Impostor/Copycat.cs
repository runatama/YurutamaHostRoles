using System;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost;
using TownOfHost.Modules;
using TownOfHost.Patches;

using static NetworkedPlayerInfo;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Copycat : RoleBase, IImpostor, IKiller
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Copycat),
                player => new Copycat(player),
                CustomRoles.Copycat,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                123000,
                SetupOptionItem,
                "cc",
                OptionSort: (4, 3),
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
                from: From.YurutamaHostRoles
            );

        public Copycat(PlayerControl player)
        : base(RoleInfo, player)
        {
            ResetCopy();
        }

        // ====== Options ======
        private static OptionItem OptCopyDuration;
        private static OptionItem OptCopyCooldown;
        private static OptionItem OptShowMark;
        private static OptionItem OptKillCooldown;

        // ====== 状態管理 ======
        private static float CopyDuration;
        private static float CopyCooldown;
        private static float KillCooldown;

        private string originalName;
        private float timer;
        private bool isCopying = false;

        public static void SetupOptionItem()
        {
            OptCopyDuration = FloatOptionItem.Create(RoleInfo, 40, "CopyDuration", new(1f, 120f, 1f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);

            OptCopyCooldown = FloatOptionItem.Create(RoleInfo, 41, "CopyCooldown", new(1f, 60f, 1f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);

            OptShowMark = BooleanOptionItem.Create(RoleInfo, 42, "CopyShowMark", true, false);

            OptKillCooldown = FloatOptionItem.Create(RoleInfo, 43, "CopycatKillCooldown", new(2.5f, 60f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public override void ApplyGameOptions(IGameOptions opt)
        {
            CopyDuration = OptCopyDuration.GetFloat();
            CopyCooldown = OptCopyCooldown.GetFloat();
            KillCooldown = OptKillCooldown.GetFloat();
        }

        // ====== キル処理フック ======
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill || info.IsFakeSuicide || info.IsGuard) return;
            var (killer, target) = info.AttemptTuple;
            if (!Is(killer) || target == null) return;

            info.DoKill = true;

            // Copycat専用のキルクール適用
            Player.SetKillCooldown(KillCooldown);

            // コピー開始
            StartCopy(target);
        }

        private void StartCopy(PlayerControl target)
        {
            if (isCopying) return;

            // 元の姿を保存
            PlayerSkinPatch.Save(Player);
            originalName = Player.Data.PlayerName;

            // コピー開始
            var tgtData = target.Data;
            Player.RpcSetName(GetCopyName(tgtData.PlayerName));
            Player.SetOutfit(tgtData.DefaultOutfit, PlayerOutfitType.Default);

            // 全員に同期
            RPC.RpcSyncAllNetworkedPlayer();

            UtilsNotifyRoles.NotifyRoles();

            timer = CopyDuration;
            isCopying = true;
        }

        private string GetCopyName(string name)
        {
            if (OptShowMark.GetBool())
                return name + "★";
            return name;
        }

        private void ResetCopy()
        {
            if (!isCopying) return;

            // 保存しておいた外見データをロード
            var (name, color, hat, skin, visor, nameplate, level, pet) = PlayerSkinPatch.Load(Player);

            if (!string.IsNullOrEmpty(name))
                Player.RpcSetName(name);
            else if (!string.IsNullOrEmpty(originalName))
                Player.RpcSetName(originalName);

            // Outfitを元に戻す
            Player.SetColor(color);
            Player.SetHat(hat, color);
            Player.SetSkin(skin, color);
            Player.SetVisor(visor, color);
            Player.SetNamePlate(nameplate);
            Player.SetPet(pet);

            // 全員に同期
            RPC.RpcSyncAllNetworkedPlayer();

            // 保存データを削除
            PlayerSkinPatch.Remove(Player);

            UtilsNotifyRoles.NotifyRoles();

            isCopying = false;
            timer = 0f;
        }

        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            if (isCopying)
            {
                timer -= Time.fixedDeltaTime;
                if (timer <= 0f)
                {
                    ResetCopy();
                    Player.SetKillCooldown(CopyCooldown);
                }

                // コピー中も定期的に同期（役職名など）
                UtilsNotifyRoles.NotifyRoles();
            }
        }

        public override void OnStartMeeting()
        {
            // 会議開始時は強制解除
            ResetCopy();
        }

        // ====== 残り時間の表示 ======
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            if (seer.PlayerId != Player.PlayerId) return "";
            if (isForMeeting || !Player.IsAlive()) return "";
            if (!isCopying) return "";

            float remaining = Mathf.Max(0f, timer);
            if (remaining > 0f)
            {
                return Utils.ColorString(RoleInfo.RoleColor, $" ({remaining:F0}s)");
            }

            return "";
        }
    }
}
