using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Tuna : RoleBase, IAdditionalWinner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tuna),
                player => new Tuna(player),
                CustomRoles.Tuna,
                () => OptionUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
                CustomRoleTypes.Neutral,
                23400,
                SetupOptionItem,
                "マグロ",
                "#00bfff"
            );

        public Tuna(PlayerControl player)
        : base(RoleInfo, player)
        {
            waitTime = OptWaitTime.GetFloat();
            allowAddWin = OptAllowAdditionalWin.GetBool();

            afterFirstMeeting = false;
            stillCounter = 0f;
            lastPos = Vector3.zero;
            tick = 0f;
        }

        // ====== Options ======
        private static OptionItem OptWaitTime;
        private static OptionItem OptAllowAdditionalWin;
        private static OptionItem OptionUseVent;

        private float waitTime;
        private bool allowAddWin;

        // ランタイム
        private float tick;
        private float stillCounter;
        private Vector3 lastPos;
        private bool afterFirstMeeting;

        private static void SetupOptionItem()
        {
            // 立ち止まり秒（0～30, 1秒刻み, 既定=3）
            OptWaitTime = FloatOptionItem.Create(RoleInfo, 10, "TunaWaitTime", new(0f, 30f, 1f), 3f, false)
                .SetValueFormat(OptionFormat.Seconds);

            // 追加勝利ON/OFF
            OptAllowAdditionalWin = BooleanOptionItem.Create(RoleInfo, 11, "TunaAllowAdditionalWin", false, false);

            // ベント可否
            OptionUseVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
        }

        // ====== 追加勝利（IAdditionalWinner） ======
        public bool CheckWin(ref CustomRoles winnerRole)
        {
            // 生存＋追加勝利が許可されている時 → 他陣営勝利にも便乗可能
            if (Player.IsAlive() && allowAddWin)
            {
                winnerRole = CustomRoles.Tuna;
                return true;
            }
            return false;
        }

        // ====== 単独勝利 ======
        public static void CheckAliveWin(PlayerControl pc)
        {
            if (pc == null || !pc.Is(CustomRoles.Tuna)) return;

            if (pc.IsAlive())
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Tuna);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Tuna);
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            }
        }

        public override void OnStartMeeting()
        {
            afterFirstMeeting = true;
        }

        // ====== 自爆処理 ======
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ExileController.Instance) return;

            if (!GameStates.IsInTask) return;
            if (!afterFirstMeeting) return;
            if (!Player.IsAlive()) return;

            tick -= Time.fixedDeltaTime;
            if (tick <= 0f)
            {
                tick = 1f;
                if (player.CanMove) CheckStillAndMaybeSuicide();
            }
        }

        private void CheckStillAndMaybeSuicide()
        {
            if (lastPos == Player.transform.position)
            {
                stillCounter += 1f;
                if (stillCounter >= waitTime && waitTime > 0f && Player.IsAlive())
                {
                    var state = PlayerState.GetByPlayerId(Player.PlayerId);
                    if (state != null)
                        state.DeathReason = CustomDeathReason.Suicide;

                    Player.RpcMurderPlayer(Player);
                }
            }
            else
            {
                lastPos = Player.transform.position;
                stillCounter = 0f;
            }
        }
    }
}
