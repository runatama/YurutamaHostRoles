using Hazel;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Neutral
{
    public sealed class BakeCat : RoleBase, IAdditionalWinner, IKiller
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(BakeCat),
                player => new BakeCat(player),
                CustomRoles.BakeCat,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Neutral,
                38100,
                SetupOptionItem,
                "bk",
                "#ededc7",
                true,
                countType: CountTypes.Crew,
                assignInfo: new RoleAssignInfo(CustomRoles.BakeCat, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public BakeCat(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.ForRecompute
        )
        {
            CanKill = false;
            Killer = null;
        }
        public static bool CanKill;
        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        public static OptionItem OptionHasImpostorVision;
        public static OptionItem OptionDieKiller;
        public static OptionItem OptionDieKillerTIme;
        static OptionItem OptionCountChenge;
        PlayerControl Killer;
        /// <summary>
        /// 自分をキルしてきた人のロール
        /// </summary>
        private ISchrodingerCatOwner owner = null;
        private TeamType _team = TeamType.None;
        /// <summary>
        /// 現在の所属陣営<br/>
        /// 変更する際は特段の事情がない限り<see cref="RpcSetTeam"/>を使ってください
        /// </summary>
        public TeamType Team
        {
            get => _team;
            private set
            {
                logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
                _team = value;
            }
        }
        public Color DisplayRoleColor => GetCatColor(Team);
        private static LogHandler logger = Logger.Handler(nameof(BakeCat));
        enum Op
        {
            BakeCatDieKiller, BakeCatDieKillerTime, BakeCatCountChenge
        }
        public static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            OptionDieKiller = BooleanOptionItem.Create(RoleInfo, 14, Op.BakeCatDieKiller, true, false);
            OptionDieKillerTIme = FloatOptionItem.Create(RoleInfo, 15, Op.BakeCatDieKillerTime, new(0, 180, 1), 1, false, OptionDieKiller).SetValueFormat(OptionFormat.Seconds);
            OptionCountChenge = BooleanOptionItem.Create(RoleInfo, 16, Op.BakeCatCountChenge, false, false);
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetVision(OptionHasImpostorVision.GetBool() && Team != TeamType.None);
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            var killer = info.AttemptKiller;

            //自殺ならスルー
            if (info.IsSuicide) return true;

            if (killer.Is(CustomRoles.GrimReaper) || killer.Is(CustomRoles.BakeCat))
                return true;
            else
            if (Team == TeamType.None)
            {
                info.CanKill = false;
                ChangeTeamOnKill(killer);

                return false;
            }
            return true;
        }

        /// <summary>
        /// キルしてきた人に応じて陣営の状態を変える
        /// </summary>
        public void ChangeTeamOnKill(PlayerControl killer)
        {
            killer.RpcProtectedMurderPlayer(Player);
            Killer = killer;
            if (killer.GetRoleClass() is ISchrodingerCatOwner catOwner)
            {
                catOwner.OnBakeCatKill(this);
                RpcSetTeam((TeamType)catOwner.SchrodingerCatChangeTo);
                owner = catOwner;

                if (AmongUsClient.Instance.AmHost)
                {
                    Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == PlayerControl.LocalPlayer)
                        {
                            Player.StartCoroutine(Player.CoSetRole(RoleTypes.Crewmate, Main.SetRoleOverride));
                            if (Player != pc) pc.RpcSetRoleDesync(RoleTypes.Scientist, Player.GetClientId());
                        }
                        else
                        {
                            Player.RpcSetRoleDesync(pc == Player ? RoleTypes.Impostor : RoleTypes.Crewmate, pc.GetClientId());
                            if (Player != pc) pc.RpcSetRoleDesync(RoleTypes.Scientist, Player.GetClientId());
                        }
                    }
                }
                _ = new LateTask(() =>
                {
                    Player.SetKillCooldown(OptionKillCooldown.GetFloat(), kyousei: true);
                    CanKill = true;
                    if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);

                    if (OptionCountChenge.GetBool())
                    {
                        MyState.SetCountType(killer.GetCustomRole().GetRoleInfo()?.CountType ?? CountTypes.Crew);
                        if (OptionDieKiller.GetBool())//死ぬならカウントが増えないようにキラーのカウントをクルーにしてやる
                            PlayerState.GetByPlayerId(killer.PlayerId).SetCountType(CountTypes.Crew);
                    }
                }, 0.3f, "ResetKillCooldown");
                if (OptionDieKiller.GetBool())
                    _ = new LateTask(() =>
                    {
                        if (!killer.IsAlive() || GameStates.Meeting) return;
                        killer.RpcMurderPlayerV2(killer);
                    }, OptionDieKillerTIme.GetFloat(), "BakeCatKillerDie");
            }
            else
            {
                logger.Warn($"未知のキル役職からのキル: {killer.GetNameWithRole().RemoveHtmlTags()}");
            }

            RevealNameColors(killer);

            UtilsNotifyRoles.NotifyRoles();
            UtilsOption.MarkEveryoneDirtySettings();
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo sitai)
        {
            if (OptionDieKiller.GetBool())
            {
                if (!Killer.IsAlive()) return;
                Killer.RpcMurderPlayerV2(Killer);
            }
        }
        public override void AfterMeetingTasks()
        {
            if (!CanKill) return;

            _ = new LateTask(() =>
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == PlayerControl.LocalPlayer)
                        {
                            Player.StartCoroutine(Player.CoSetRole(RoleTypes.Crewmate, Main.SetRoleOverride));
                            if (Player != pc) pc.RpcSetRoleDesync(RoleTypes.Scientist, Player.GetClientId());
                        }
                        else
                        {
                            Player.RpcSetRoleDesync(pc == Player ? RoleTypes.Impostor : RoleTypes.Crewmate, pc.GetClientId());
                            if (Player != pc) pc.RpcSetRoleDesync(RoleTypes.Scientist, Player.GetClientId());
                        }
                    }
                }

                _ = new LateTask(() =>
                {
                    Player.SetKillCooldown(OptionKillCooldown.GetFloat(), kyousei: true);
                    CanKill = true;
                }, 0.3f, "ResetKillCooldown", true);
            }, 5f, "Bakecatkillbutton");
        }
        private void RevealNameColors(PlayerControl killer)
        {
            var c = RoleInfo.RoleColorCode;
            if (killer.Is(CustomRoles.WolfBoy))
                c = WolfBoy.Shurenekodotti.GetBool() ? UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : "#ffffff";

            NameColorManager.Add(killer.PlayerId, Player.PlayerId, c);
            NameColorManager.Add(Player.PlayerId, killer.PlayerId);

            UtilsGameLog.AddGameLog($"BakeNeko", Utils.GetPlayerColor(Player) + ":  " + string.Format(GetString("SchrodingerCat.Ch"), Utils.GetPlayerColor(killer, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(killer.PlayerId, false)}</b>)"));
            UtilsGameLog.LastLogRole[Player.PlayerId] = UtilsGameLog.LastLogRole[Player.PlayerId].RemoveColorTags().Color(DisplayRoleColor);
        }
        public override CustomRoles Jikaku() => Team == TeamType.None ? CustomRoles.Crewmate : CustomRoles.NotAssigned;
        public override CustomRoles GetFtResults(PlayerControl player) => Team == TeamType.None ? CustomRoles.Crewmate : CustomRoles.NotAssigned;
        public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
        {
            // 陣営変化前なら上書き不要
            if (Team == TeamType.None)
            {
                return;
            }
            roleColor = DisplayRoleColor;
        }
        public bool CheckWin(ref CustomRoles winnerRole)
        {
            bool? won = Team switch
            {
                TeamType.None => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
                TeamType.Mad => CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor,
                TeamType.Crew => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
                TeamType.Jackal => CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal,
                TeamType.Egoist => CustomWinnerHolder.WinnerTeam == CustomWinner.Egoist,
                TeamType.CountKiller => CustomWinnerHolder.WinnerTeam == CustomWinner.CountKiller,
                TeamType.Remotekiller => CustomWinnerHolder.WinnerTeam == CustomWinner.Remotekiller,
                TeamType.DoppelGanger => CustomWinnerHolder.WinnerTeam == CustomWinner.DoppelGanger,
                _ => null,
            };
            if (!won.HasValue)
            {
                logger.Warn($"不明な猫の勝利チェック: {Team}");
                return false;
            }
            return won.Value;
        }
        public void RpcSetTeam(TeamType team)
        {
            Team = team;
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender();
                sender.Writer.Write((byte)team);
            }
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            Team = (TeamType)reader.ReadByte();
        }

        /// <summary>
        /// 陣営状態
        /// </summary>
        public enum TeamType : byte
        {
            /// <summary>
            /// どこの陣営にも属していない状態
            /// </summary>
            None = 0,

            // 10-49 シェリフキルオプションを作成しない変化先

            /// <summary>
            /// インポスター陣営に所属する状態
            /// </summary>
            Mad = 10,
            /// <summary>
            /// クルー陣営に所属する状態
            /// </summary>
            Crew,

            // 50- シェリフキルオプションを作成する変化先

            /// <summary>
            /// ジャッカル陣営に所属する状態
            /// </summary>
            Jackal = 50,
            /// <summary>
            /// エゴイスト陣営に所属する状態
            /// </summary>
            Egoist,
            /// <summary>
            /// カウントキラーに所属する状態
            /// </summary>
            CountKiller,
            /// <summary>
            /// リモートキラーに所属する状態
            /// </summary>
            Remotekiller,
            /// <summary>
            /// ドッペルゲンガーに所属する状態
            /// </summary>
            DoppelGanger
        }
        public static Color GetCatColor(TeamType catType)
        {
            Color? color = catType switch
            {
                TeamType.None => RoleInfo.RoleColor,
                TeamType.Mad => UtilsRoleText.GetRoleColor(CustomRoles.Madmate),
                TeamType.Crew => UtilsRoleText.GetRoleColor(CustomRoles.Crewmate),
                TeamType.Jackal => UtilsRoleText.GetRoleColor(CustomRoles.Jackal),
                TeamType.Egoist => UtilsRoleText.GetRoleColor(CustomRoles.Egoist),
                TeamType.Remotekiller => UtilsRoleText.GetRoleColor(CustomRoles.Remotekiller),
                TeamType.CountKiller => UtilsRoleText.GetRoleColor(CustomRoles.CountKiller),
                TeamType.DoppelGanger => UtilsRoleText.GetRoleColor(CustomRoles.DoppelGanger),
                _ => null,
            };
            if (!color.HasValue)
            {
                logger.Warn($"不明な猫に対する色の取得: {catType}");
                return UtilsRoleText.GetRoleColor(CustomRoles.Crewmate);
            }
            return color.Value;
        }

        public bool CanUseSabotageButton() => OptionCanUseSabotage.GetBool() && Team != TeamType.None;
        public bool CanUseImpostorVentButton() => OptionCanVent.GetBool() && Team != TeamType.None;
        public bool CanUseKillButton() => Team != TeamType.None && CanKill;
        public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    }
}