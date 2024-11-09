using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.UtilsRoleText;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using TownOfHost.Modules;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using static TownOfHost.RandomSpawn;
using TownOfHost.Roles.Impostor;
using System.Data;
using UnityEngine;
using HarmonyLib;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    public static class UtilsNotifyRoles
    {
        private static StringBuilder SelfMark = new(20);
        private static StringBuilder SelfSuffix = new(20);
        private static StringBuilder TargetMark = new(20);
        private static StringBuilder TargetSuffix = new(20);
        public static void NotifyRoles(bool isForMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerCatch.AllPlayerControls == null) return;

            //ミーティング中の呼び出しは不正
            if (GameStates.IsMeeting) return;

            if (GameStates.IsLobby) return;

            if (Main.introDestroyed)
                foreach (var pp in PlayerCatch.AllPlayerControls)
                {
                    var str = GetProgressText(pp.PlayerId, Mane: false, gamelog: true);
                    str = Regex.Replace(str, "ffffff", "000000");
                    if (Main.LastLogPro.ContainsKey(pp.PlayerId))
                        Main.LastLogPro[pp.PlayerId] = str;
                    else Main.LastLogPro.Add(pp.PlayerId, str);

                    var mark = GetSubRolesText(pp.PlayerId, mark: true);
                    if (Main.LastLogSubRole.ContainsKey(pp.PlayerId))
                        Main.LastLogSubRole[pp.PlayerId] = mark;
                    else Main.LastLogSubRole.Add(pp.PlayerId, mark);
                }
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;
            var caller = new StackFrame(1, false);
            var callerMethod = caller?.GetMethod();
            string callerMethodName = callerMethod?.Name ?? "ぬーるっ!!";
            string callerClassName = callerMethod?.DeclaringType?.FullName ?? "null!!";
            var logger = Logger.Handler("NotifyRoles");
            logger.Info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;
            var Info = $" <color=#ffffff><size=1.5f>\n\n</size><line-height=0%><color={Main.ModColor}>TownOfHost-K\t\t  <size=60%>　</size>\n　　\t\t</color><size=70%>";
            Info += $"v{Main.PluginShowVersion}</size>\n　</line-height></color><line-height=50%>\n</line-height><line-height=95%>";
            Info += $"Day.{Main.day}".Color(Palette.Orange) + $"\n{MeetingMoji}<line-height=0%>\n</line-height></line-height><line-height=250%>\n</line-height></color>";

            var seerList = PlayerControl.AllPlayerControls;
            if (SpecifySeer != null)
            {
                seerList = new();
                seerList.Add(SpecifySeer);
            }
            var isMushroomMixupActive = IsActive(SystemTypes.MushroomMixupSabotage);
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                //seerが落ちているときに何もしない
                if (seer == null || seer.Data.Disconnected) continue;

                if (seer.IsModClient()) continue;
                string fontSize = isForMeeting ? "1.5" : Main.RoleTextSize.ToString();
                if (isForMeeting && seer.GetClient() != null)
                    if (seer.GetClient()?.PlatformData?.Platform is Platforms.Playstation or Platforms.Switch) fontSize = "70%";
                logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole().RemoveHtmlTags() + ":START");

                var seerRole = seer.GetRoleClass();
                // 会議じゃなくて，キノコカオス中で，seerが生きていてdesyncインポスターの場合に自身の名前を消す
                if (!isForMeeting && isMushroomMixupActive && seer.IsAlive() && !seer.Is(CustomRoleTypes.Impostor) && seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)
                {
                    seer.RpcSetNamePrivate("<size=0>", true, force: NoCache);
                }
                else
                {
                    //名前の後ろに付けるマーカー
                    SelfMark.Clear();

                    //seerの名前を一時的に上書きするかのチェック
                    string name = ""; bool nomarker = false;
                    var TemporaryName = seerRole?.GetTemporaryName(ref name, ref nomarker, seer);

                    //seer役職が対象のMark
                    if (!seer.Is(CustomRoles.Amnesia) && !(seerRole?.Jikaku() != CustomRoles.NotAssigned) && seer.GetRoleClass() != null)
                        SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: isForMeeting) ?? "");

                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: isForMeeting));

                    //ハートマークを付ける(自分に)
                    var lover = seer.GetRiaju();
                    if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));

                    if ((seer.Is(CustomRoles.Connecting) && !seer.Is(CustomRoles.WolfBoy))
                    || (seer.Is(CustomRoles.Connecting) && !seer.IsAlive())) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        if (Options.TaskBattletaska.GetBool())
                        {
                            var t1 = 0f;
                            var t2 = 0;
                            if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                            {
                                foreach (var pc in PlayerCatch.AllPlayerControls)
                                {
                                    t1 += pc.GetPlayerTaskState().AllTasksCount;
                                    t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                                }
                            }
                            else
                            {
                                foreach (var t in Main.TaskBattleTeams)
                                {
                                    if (!t.Contains(seer.PlayerId)) continue;
                                    t1 = Options.TaskBattleTeamWinTaskc.GetFloat();
                                    foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                        t2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                }
                            }
                            SelfMark.Append($"<color=yellow>({t2}/{t1})</color>");
                        }
                        if (Options.TaskBattletasko.GetBool())
                        {
                            var to = 0;
                            if (!Options.TaskBattleTeamMode.GetBool() && !Options.TaskBattleTeamWinType.GetBool())
                            {
                                foreach (var pc in PlayerCatch.AllPlayerControls)
                                    if (pc.GetPlayerTaskState().CompletedTasksCount > to) to = pc.GetPlayerTaskState().CompletedTasksCount;
                            }
                            else
                                foreach (var t in Main.TaskBattleTeams)
                                {
                                    var to2 = 0;
                                    foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                        to2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                                    if (to2 > to) to = to2;
                                }
                            SelfMark.Append($"<color=#00f7ff>({to})</color>");
                        }
                    }
                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    if (!seer.Is(CustomRoles.Amnesia) && !(seerRole?.Jikaku() != CustomRoles.NotAssigned) && seer.GetRoleClass() != null) SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: isForMeeting) ?? "");
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: isForMeeting));
                    //追放者
                    if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "" && !GameStates.Meeting)
                    {
                        if (SelfSuffix.ToString() != "") SelfSuffix.Append('\n');
                        SelfSuffix.Append("<color=#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                    }
                    if ((seer.Is(CustomRoles.Guesser) ||
                    (LastNeutral.GiveGuesser.GetBool() && seer.Is(CustomRoles.LastNeutral)) ||
                    (LastImpostor.GiveGuesser.GetBool() && seer.Is(CustomRoles.LastImpostor)) ||
                    (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer) && data.GiveAddons.GetBool() && data.GiveGuesser.GetBool())
                    ) && GameStates.Meeting
                    )
                    {
                        var gi = $" <line-height=10%>\n<color={GetRoleColorCode(CustomRoles.Guesser)}><size=50%>{GetString("GuessInfo")}</color></size></line-height>";
                        SelfSuffix.Append(gi);
                    }
                    //seer役職が対象のSuffix
                    if (Amnesia.CheckAbility(seer))
                        SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: isForMeeting) ?? "");
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: isForMeeting));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = (seerRole is IUseTheShButton) ? Main.AllPlayerNames[seer.PlayerId] : seer.GetRealName(isForMeeting);

                    if (Options.SuddenCannotSeeName.GetBool())
                    {
                        SeerRealName = "";
                    }

                    if (TemporaryName ?? false)
                        SeerRealName = name;

                    if (!isForMeeting && MeetingStates.FirstMeeting && (Options.ChangeNameToRoleInfo.GetBool() || Options.SuddenDeathMode.GetBool()) && Main.IntroHyoji)
                        SeerRealName = seer?.GetRoleInfo() ?? "";

                    var next = "";
                    if (isForMeeting && Options.CanSeeNextRandomSpawn.GetBool())
                    {
                        if (SpawnMap.NextSpornName.TryGetValue(seer.PlayerId, out var r))
                            next += $"<size=40%><color=#9ae3bd>〔{r}〕</size>";
                    }

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = ((TemporaryName ?? false) && nomarker) ? "" : seer.KnowDeathReason(seer) ? $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})" : "";
                    string SelfName = $"{ColorString(seer.GetRoleColor(), SeerRealName)}{SelfDeathReason}{(((TemporaryName ?? false) && nomarker) ? "" : SelfMark)}";
                    SelfName = SelfRoleName + "\r\n" + SelfName + next;
                    var g = "<line-height=85%>";
                    SelfName += SelfSuffix.ToString() == "" ? "" : (g + "\r\n " + SelfSuffix.ToString() + "</line-height>");
                    if (!isForMeeting) SelfName = "<line-height=85%>" + SelfName + "\r\n";

                    if (isForMeeting)
                    {
                        var p = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                        var a = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);

                        if (p.ToArray().AddRangeToArray(a.ToArray())[0] != null)
                            if (p.ToArray().AddRangeToArray(a.ToArray())[0] == seer)
                            {
                                var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + Info + SelfName + Info.RemoveText() + "\r\n<size=1.5> ";
                                SelfName = Name;
                            }
                            else
                            {
                                var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + SelfName;
                                SelfName = Name;
                            }

                        if (p.ToArray().AddRangeToArray(a.ToArray()).LastOrDefault() != null)
                            if (p.ToArray().AddRangeToArray(a.ToArray()).LastOrDefault() == seer)
                            {
                                var team = seer.GetCustomRole().GetCustomRoleTypes();
                                if (Options.CanSeeTimeLimit.GetBool() && Options.TimeLimitDevices.GetBool())
                                {
                                    var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                                    if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                    || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seer.IsAlive())
                                        if (info != "")
                                        {
                                            var Name = info.RemoveText() + "\n" + SelfName + "\n" + info;
                                            SelfName = Name;
                                        }
                                }
                            }
                    }
                    //適用
                    //Logger.Info(SelfName, "Name");
                    seer.RpcSetNamePrivate(SelfName, true, force: NoCache);
                }
                var rolech = seer.GetRoleClass()?.NotifyRolesCheckOtherName ?? false;

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || seer.GetCustomRole().IsImpostor() //seerがインポスター
                    || seer.IsNeutralKiller() //seerがキル出来るニュートラル
                    || PlayerState.GetByPlayerId(seer.PlayerId).TargetColorData.Count > 0 //seer視点用の名前色データが一つ以上ある
                    || Witch.IsSpelled()
                    || CustomRoles.TaskStar.IsEnable()
                    || seer.IsRiaju()
                    || seer.Is(CustomRoles.Management)
                    || seer.Is(CustomRoles.Connecting)
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || isMushroomMixupActive
                    || rolech
                    || Options.CurrentGameMode == CustomGameMode.TaskBattle
                    || (seer.GetRoleClass() is IUseTheShButton) //ﾜﾝｸﾘｯｸシェイプボタン持ち
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in PlayerCatch.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        if (target == null) continue;
                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole().RemoveHtmlTags() + ":START");

                        // 会議じゃなくて，キノコカオス中で，targetが生きていてseerがdesyncインポスターの場合にtargetの名前を消す
                        if (!isForMeeting && isMushroomMixupActive && target.IsAlive() && !seer.Is(CustomRoleTypes.Impostor) && seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)
                        {
                            target.RpcSetNamePrivate("<size=0>", true, seer, force: NoCache);
                        }
                        else
                        {
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            /// targetの名前を一時的に上書きするかのチェック
                            string name = ""; bool nomarker = false;
                            var TemporaryName = target.GetRoleClass()?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                            //seer役職が対象のMark
                            if (Amnesia.CheckAbility(seer))
                                TargetMark.Append(seerRole?.GetMark(seer, target, isForMeeting) ?? "");
                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, isForMeeting));

                            //ハートマークを付ける(相手に)
                            if (seer.GetRiaju() == target.GetRiaju() && seer.IsRiaju() && seer.GetRiaju() != CustomRoles.OneLove) TargetMark.Append(ColorString(GetRoleColor(seer.GetRiaju()), "♥"));
                            else if (seer.Data.IsDead && !seer.Is(target.GetRiaju()) && target.GetRiaju() != CustomRoles.NotAssigned && seer.GetRiaju() != CustomRoles.OneLove) TargetMark.Append(ColorString(GetRoleColor(target.GetRiaju()), "♥"));

                            if (seer.Is(CustomRoles.OneLove) && target.Is(CustomRoles.OneLove)) TargetMark.Append(ColorString(GetRoleColor(CustomRoles.OneLove), "♡"));
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.OneLove) && target.Is(CustomRoles.OneLove)) TargetMark.Append(ColorString(GetRoleColor(CustomRoles.OneLove), "♡"));
                            else if (seer.Is(CustomRoles.OneLove) && target.PlayerId == Lovers.OneLovePlayer.Ltarget) TargetMark.Append(ColorString(GetRoleColor(CustomRoles.OneLove), "♡"));

                            if (seer.Is(CustomRoles.Connecting) && target.Is(CustomRoles.Connecting) && (!seer.Is(CustomRoles.WolfBoy) || !seer.IsAlive()))
                            {//狼少年じゃないか死亡なら処理
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                            }
                            else if (seer.Data.IsDead && !seer.Is(CustomRoles.Connecting) && target.Is(CustomRoles.Connecting))
                            {
                                TargetMark.Append($"<color={GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                            }
                            //プログレスキラー
                            if (Amnesia.CheckAbility(seer))
                            {
                                if (seer.Is(CustomRoles.ProgressKiller) && target.Is(CustomRoles.Workhorse) && ProgressKiller.ProgressWorkhorseseen)
                                {
                                    TargetMark.Append($"<color=blue>♦</color>");
                                }
                                //エーリアン
                                if (seer.Is(CustomRoles.Alien))
                                {
                                    foreach (var al in Alien.Aliens)
                                    {
                                        if (al.Player == seer)
                                            if (target.Is(CustomRoles.Workhorse) && al.modeProgresskiller && Alien.ProgressWorkhorseseen)
                                            {
                                                TargetMark.Append($"<color=blue>♦</color>");
                                            }
                                    }
                                }
                                if (seer.Is(CustomRoles.JackalAlien))
                                {
                                    foreach (var al in JackalAlien.Aliens)
                                    {
                                        if (al.Player == seer)
                                            if (target.Is(CustomRoles.Workhorse) && al.modeProgresskiller && JackalAlien.ProgressWorkhorseseen)
                                            {
                                                TargetMark.Append($"<color=blue>♦</color>");
                                            }
                                    }
                                }
                            }

                            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                                if (Options.TaskBattletaskc.GetBool())
                                    TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");

                            //インサイダーモードタスク表示
                            if (Options.Taskcheck.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (seer.Is(CustomRoleTypes.Impostor))
                                    {
                                        TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().GetNeedCountOrAll()})</color>");
                                    }
                                }
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target, false);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : $"";

                            TargetSuffix.Clear();
                            //seerに関わらず発動するLowerText
                            TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: isForMeeting));

                            //seer役職が対象のSuffix
                            if (Amnesia.CheckAbility(seer))
                                TargetSuffix.Append(seerRole?.GetSuffix(seer, target, isForMeeting: isForMeeting) ?? "");
                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: isForMeeting));
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                            {
                                TargetSuffix.Insert(0, "\r\n");
                            }

                            //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                            string TargetPlayerName = (seer.GetRoleClass() is IUseTheShButton) ? Main.AllPlayerNames[target.PlayerId] : target.GetRealName(isForMeeting);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, isForMeeting);

                            if (Options.SuddenCannotSeeName.GetBool())
                            {
                                TargetPlayerName = "";
                            }

                            if (seer.Is(CustomRoles.Guesser))
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            else if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer) && data.GiveAddons.GetBool() && data.GiveGuesser.GetBool())
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            else
                            if (seer.Is(CustomRoles.LastImpostor) && LastImpostor.GiveGuesser.GetBool())
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            else
                            if (seer.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool())
                            {
                                if (seer.IsAlive() && target.IsAlive() && isForMeeting)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }
                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})";

                            if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isForMeeting)
                                TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";

                            if (Camouflager.NowUse)
                            {
                                target.RpcSetNamePrivate($"<size=0>{TargetPlayerName}</size>", true, seer, force: NoCache);
                                continue;
                            }

                            if (Amnesia.CheckAbility(seer))
                                if (seer.Is(CustomRoles.Monochromer) && !isForMeeting && seer.IsAlive())
                                {
                                    TargetPlayerName = $"<size=0%>{TargetPlayerName}</size>";
                                    name = $"<size=0%>{TargetPlayerName}</size>";
                                }
                            if (seer.Is(CustomRoles.Jackaldoll))
                            {
                                if (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.JackalMafia))
                                {
                                    TargetPlayerName = ColorString(GetRoleColor(CustomRoles.Jackal), TargetPlayerName);
                                }
                                else
                                    TargetPlayerName = "<color=#ffffff>" + TargetPlayerName + "</color>";
                            }
                            //全てのテキストを合成します。
                            var g = string.Format("<line-height={0}%>", isForMeeting ? "90" : "85");
                            string TargetName = $"{g}{TargetRoleText}{(TemporaryName ? name : TargetPlayerName)}{((TemporaryName && nomarker) ? "" : TargetDeathReason + TargetMark + TargetSuffix)}</line-height>";
                            if (!isForMeeting && !seer.IsAlive())
                                TargetName = $"<size=65%><line-height=85%><line-height=-18%>\n</line-height>{TargetRoleText.RemoveSizeTags()}</size><size=70%><line-height=-17%>\n</line-height>{(TemporaryName ? name.RemoveSizeTags() : TargetPlayerName.RemoveSizeTags())}{((TemporaryName && nomarker) ? "" : TargetDeathReason.RemoveSizeTags() + TargetMark.ToString().RemoveSizeTags() + TargetSuffix.ToString().RemoveSizeTags())}";

                            if (isForMeeting)
                            {
                                var TInfo = $" <color=#ffffff><size=1.5f>\n\n</size><line-height=0%><color={Main.ModColor}>TownOfHost-K\t\t  <size=60%>　</size>\n　　\t\t</color><size=70%>";

                                var IInfo = $"v{Main.PluginShowVersion}</size>\n　</line-height></color><line-height=50%>\n</line-height><line-height=95%>";
                                IInfo += $"Day.{Main.day}".Color(Palette.Orange) + $"\n{MeetingMoji}<line-height=0%>\n</line-height></line-height><line-height=330%>\n</line-height></color> ";

                                TInfo += IInfo;
                                var Finfo = $" <size=0.9f>\n</size><color=#ffffff><size=1.5f>\n\n</size><line-height=0%><color={Main.ModColor}>TownOfHost-K\t\t  <size=60%>　</size>\n　　\t\t</color><size=70%>";
                                Finfo += IInfo;

                                var p = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                                var a = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);

                                if (p.ToArray().AddRangeToArray(a.ToArray())[0] != null)
                                    if (p.ToArray().AddRangeToArray(a.ToArray())[0] == target)
                                    {
                                        if (targetRoleData.enabled)
                                        {
                                            var Name = (TargetSuffix.ToString() == "" ? "" : (TargetSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + Info + TargetName + Info.RemoveText() + "\r\n<size=1.5> ";
                                            TargetName = Name;
                                        }
                                        else
                                        {
                                            var Name = (TargetSuffix.ToString() == "" ? "" : (TargetSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + TInfo + TargetName + Finfo.RemoveText();
                                            TargetName = Name;
                                        }
                                    }
                                if (p.ToArray().AddRangeToArray(a.ToArray()).LastOrDefault() != null)
                                    if (p.ToArray().AddRangeToArray(a.ToArray()).LastOrDefault() == target)
                                    {
                                        var team = seer.GetCustomRole().GetCustomRoleTypes();
                                        if (Options.CanSeeTimeLimit.GetBool() && Options.TimeLimitDevices.GetBool())
                                        {
                                            var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                                            if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                            || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seer.IsAlive())
                                                if (info != "")
                                                {
                                                    var Name = info.RemoveText() + "\n" + TargetName + "\n" + info;
                                                    TargetName = Name;
                                                }
                                        }
                                    }
                            }
                            //適用
                            target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);
                        }
                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole().RemoveHtmlTags() + ":END");
                    }
                }
                logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole().RemoveHtmlTags() + ":END");
            }
        }
        public static string MeetingMoji;
    }
}