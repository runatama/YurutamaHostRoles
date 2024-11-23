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

                var role = seer.GetCustomRole();
                var seerRole = seer.GetRoleClass();
                var seerRoleInfo = seer.GetCustomRole().GetRoleInfo();
                var seerisAlive = seer.IsAlive();
                var Amnesiacheck = Amnesia.CheckAbility(seer);
                var seercone = seer.Is(CustomRoles.Connecting);
                var jikaku = seerRole?.Jikaku() is CustomRoles.NotAssigned;
                RoleAddAddons.GetRoleAddon(role, out var data, seer);
                // 会議じゃなくて，キノコカオス中で，seerが生きていてdesyncインポスターの場合に自身の名前を消す
                if (!isForMeeting && isMushroomMixupActive && seerisAlive && !role.IsImpostor() && seerRoleInfo?.IsDesyncImpostor == true)
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
                    if (Amnesiacheck && jikaku)
                        SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: isForMeeting) ?? "");

                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: isForMeeting));

                    //ハートマークを付ける(自分に)
                    var lover = seer.GetRiaju();
                    if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));

                    if ((seercone && role is not CustomRoles.WolfBoy)
                    || (seercone && !seerisAlive)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

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
                    if (Amnesiacheck && jikaku)
                        SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: isForMeeting) ?? "");
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: isForMeeting));
                    //追放者
                    if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "" && !isForMeeting)
                    {
                        if (SelfSuffix.ToString() != "") SelfSuffix.Append('\n');
                        SelfSuffix.Append("<color=#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                    }
                    if ((seer.Is(CustomRoles.Guesser) ||
                    (LastNeutral.GiveGuesser.GetBool() && seer.Is(CustomRoles.LastNeutral)) ||
                    (LastImpostor.giveguesser && seer.Is(CustomRoles.LastImpostor)) ||
                    (data.GiveAddons.GetBool() && data.GiveGuesser.GetBool())
                    ) && isForMeeting
                    )
                    {
                        var gi = $" <line-height=10%>\n<color={GetRoleColorCode(CustomRoles.Guesser)}><size=50%>{GetString("GuessInfo")}</color></size></line-height>";
                        SelfSuffix.Append(gi);
                    }
                    //seer役職が対象のSuffix
                    if (Amnesiacheck)
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

                    if (!isForMeeting && MeetingStates.FirstMeeting && (Options.ChangeNameToRoleInfo.GetBool() || SuddenDeathMode.NowSuddenDeathMode) && Main.IntroHyoji)
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
                        var list = p.ToArray().AddRangeToArray(a.ToArray());

                        if (list[0] != null)
                            if (list[0] == seer)
                            {
                                var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + Info + SelfName + Info.RemoveText() + "\r\n<size=1.5> ";
                                SelfName = Name;
                            }
                            else
                            {
                                var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + SelfName;
                                SelfName = Name;
                            }

                        if (list.LastOrDefault() != null)
                            if (list.LastOrDefault() == seer)
                            {
                                var team = role.GetCustomRoleTypes();
                                if (Options.CanSeeTimeLimit.GetBool() && DisableDevice.optTimeLimitDevices)
                                {
                                    var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                                    if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                    || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seerisAlive)
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
                var rolech = seerRole?.NotifyRolesCheckOtherName ?? false;

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || role.IsImpostor() //seerがインポスター
                    || seer.IsNeutralKiller() //seerがキル出来るニュートラル
                    || PlayerState.GetByPlayerId(seer.PlayerId).TargetColorData.Count > 0 //seer視点用の名前色データが一つ以上ある
                    || Witch.IsSpelled()
                    || CustomRoles.TaskStar.IsEnable()
                    || seer.IsRiaju()
                    || seercone
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || isMushroomMixupActive
                    || rolech
                    || Options.CurrentGameMode == CustomGameMode.TaskBattle
                    || (seerRole is IUseTheShButton) //ﾜﾝｸﾘｯｸシェイプボタン持ち
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in PlayerCatch.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer) continue;
                        if (target == null) continue;
                        var targetisalive = target.IsAlive();
                        logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole().RemoveHtmlTags() + ":START");

                        // 会議じゃなくて，キノコカオス中で，targetが生きていてseerがdesyncインポスターの場合にtargetの名前を消す
                        if (!isForMeeting && isMushroomMixupActive && targetisalive && !role.IsImpostor() && seerRoleInfo?.IsDesyncImpostor == true)
                        {
                            target.RpcSetNamePrivate("<size=0>", true, seer, force: NoCache);
                        }
                        else
                        {
                            var targetrole = target.GetRoleClass();
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            /// targetの名前を一時的に上書きするかのチェック
                            string name = ""; bool nomarker = false;
                            var TemporaryName = targetrole?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, isForMeeting));

                            //ハートマークを付ける(相手に)
                            var seerri = seer.GetRiaju();
                            var tageri = target.GetRiaju();
                            var seerisone = seer.Is(CustomRoles.OneLove);
                            var seenIsOne = target.Is(CustomRoles.OneLove);
                            if (seerri == tageri && seer.IsRiaju() && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(seerri), "♥"));
                            else if (seer.Data.IsDead && !seer.Is(tageri) && tageri != CustomRoles.NotAssigned && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(tageri), "♥"));

                            if ((seerisone && seenIsOne)
                            || (seer.Data.IsDead && !seerisone && seenIsOne)
                            || (seerisone && target.PlayerId == Lovers.OneLovePlayer.Ltarget)
                            )
                                TargetMark.Append("<color=#ff7961>♡</color>");

                            if (seercone && target.Is(CustomRoles.Connecting) && (role is not CustomRoles.WolfBoy || !seerisAlive)
                            || (seer.Data.IsDead && !seercone && target.Is(CustomRoles.Connecting))
                            ) //狼少年じゃないか死亡なら処理
                                TargetMark.Append($"<color=#96514d>Ψ</color>");

                            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                                if (Options.TaskBattletaskc.GetBool())
                                    TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");

                            //インサイダーモードタスク表示
                            if (Options.Taskcheck.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (role.IsImpostor())
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

                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: isForMeeting));
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                                TargetSuffix.Insert(0, "\r\n");

                            if (Amnesiacheck)
                            {
                                //seer役職が対象のMark
                                TargetMark.Append(seerRole?.GetMark(seer, target, isForMeeting) ?? "");
                                TargetSuffix.Append(seerRole?.GetSuffix(seer, target, isForMeeting: isForMeeting) ?? "");

                                if (target.Is(CustomRoles.Workhorse))
                                {
                                    if (target.Is(CustomRoles.Workhorse))
                                    {
                                        if (((seerRole as Alien)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                                        || ((seerRole as JackalAlien)?.modeProgresskiller == true && JackalAlien.ProgressWorkhorseseen)
                                        || (seer.Is(CustomRoles.ProgressKiller) && ProgressKiller.ProgressWorkhorseseen))
                                        {
                                            TargetMark.Append($"<color=blue>♦</color>");
                                        }
                                    }
                                }
                            }

                            //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                            string TargetPlayerName = (seerRole is IUseTheShButton) ? Main.AllPlayerNames[target.PlayerId] : target.GetRealName(isForMeeting);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, isForMeeting);
                            if (isForMeeting)
                            {
                                if (seer.Is(CustomRoles.Guesser)
                                || (data.GiveAddons.GetBool() && data.GiveGuesser.GetBool())
                                || (seer.Is(CustomRoles.LastImpostor) && LastImpostor.giveguesser)
                                || (seer.Is(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()))
                                {
                                    if (seerisAlive && targetisalive && isForMeeting)
                                    {
                                        TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                    }
                                }
                            }

                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})";

                            if (((IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                            || (role is CustomRoles.Monochromer && seerisAlive)
                            || Camouflager.NowUse
                            || (Options.SuddenCannotSeeName.GetBool() && !TemporaryName))
                            && (!((targetrole as Jumper)?.ability == true)))
                            {
                                TargetPlayerName = $"<size=0>{TargetPlayerName}</size> ";
                                name = $"<size=0>{name}</color>";
                            }
                            //全てのテキストを合成します。
                            var g = string.Format("<line-height={0}%>", isForMeeting ? "90" : "85");
                            string TargetName = $"{g}{TargetRoleText}{(TemporaryName ? name : TargetPlayerName)}{((TemporaryName && nomarker) ? "" : TargetDeathReason + TargetMark + TargetSuffix)}</line-height>";
                            if (!isForMeeting && !seerisAlive && !((targetrole as Jumper)?.ability == true))
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

                                var list = p.ToArray().AddRangeToArray(a.ToArray());
                                if (list[0] != null)
                                    if (list[0] == target)
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
                                if (list.LastOrDefault() != null)
                                    if (list.LastOrDefault() == target)
                                    {
                                        var team = role.GetCustomRoleTypes();
                                        if (Options.CanSeeTimeLimit.GetBool() && DisableDevice.optTimeLimitDevices)
                                        {
                                            var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                                            if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                            || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seerisAlive)
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