using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using static TownOfHost.Croissant;

namespace TownOfHost
{
    [Flags]
    public enum CustomGameMode
    {
        Standard, //= 0x01,
        HideAndSeek, //= 0x02,
        TaskBattle, //= 0x03,
        All = int.MaxValue
    }

    [HarmonyPatch]
    public static class Options
    {
        static Task taskOptionsLoad;
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
        public static void OptionsLoadStart()
        {
            Logger.Info("Options.Load Start", "Options");
            Main.UseYomiage.Value = false;
#if RELEASE
            Main.ViewPingDetails.Value = false; 
            Main.DebugSendAmout.Value = false;
            Main.ShowDistance.Value = false;
            Main.DebugChatopen.Value  =false;
#endif
            taskOptionsLoad = Task.Run(Load);
        }
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void WaitOptionsLoad()
        {
            taskOptionsLoad.Wait();
            Logger.Info("Options.Load End", "Options");
        }

        // プリセット
        private static readonly string[] presets =
        {
            Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
            Main.Preset4.Value, Main.Preset5.Value,Main.Preset6.Value,
            Main.Preset7.Value
        };

        // ゲームモード
        public static OptionItem GameMode;
        public static CustomGameMode CurrentGameMode => (CustomGameMode)GameMode.GetValue();

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek","TaskBattle",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;
        public static bool IsActiveFungle => AddedTheFungle.GetBool() || Main.NormalOptions.MapId == 5;

        // 役職数・確率
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, IntegerOptionItem> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
        public static OptionItem DoubleTriggerThreshold;
        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem CanMakeMadmateCount;
        public static OptionItem SkMadCanUseVent;
        public static OptionItem MadMateOption;
        public static OptionItem MadmateCanSeeKillFlash;
        public static OptionItem MadmateCanSeeDeathReason;
        public static OptionItem MadmateRevengeCrewmate;
        public static OptionItem MadNekomataCanImp;
        public static OptionItem MadNekomataCanNeu;
        public static OptionItem MadNekomataCanMad;
        public static OptionItem MadNekomataCanCrew;
        public static OptionItem MadCanSeeImpostor;
        public static OptionItem MadmateCanFixLightsOut;
        public static OptionItem MadmateCanFixComms;
        //public static OptionItem MadmateHasImpostorVision;
        public static OptionItem MadmateHasLighting;
        public static OptionItem MadmateHasMoon;
        public static OptionItem MadmateCanSeeOtherVotes;
        public static OptionItem MadmateTell;
        static string[] Tellopt =
        {"Sonomama","Crewmate","Madmate","Impostor"};
        public static CustomRoles MadTellOpt()
        {
            switch (Tellopt[MadmateTell.GetValue()])
            {
                case "Sonomama": return CustomRoles.NotAssigned;
                case "Crewmate": return CustomRoles.Crewmate;
                case "Madmate": return CustomRoles.Madmate;
                case "Impostor": return CustomRoles.Impostor;
            }
            return CustomRoles.NotAssigned;
        }
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;
        public static OptionItem MadmateCanMovedByVent;

        //試験的機能
        public static OptionItem ExperimentalMode;
        public static OptionItem ExAftermeetingflash;
        public static OptionItem ExHideChatCommand;
        public static OptionItem FixSpawnPacketSize;
        public static OptionItem BlackOutwokesitobasu;
        public static OptionItem ExRpcWeightR;

        //幽霊役職
        public static OptionItem GRRoleOp;
        public static OptionItem GRCanSeeOtherRoles;
        public static OptionItem GRCanSeeOtherTasks;
        public static OptionItem GRCanSeeOtherVotes;
        public static OptionItem GRCanSeeDeathReason;
        public static OptionItem GRCanSeeKillerColor;
        public static OptionItem GRCanSeeAllTasks;
        public static OptionItem GRCanSeeKillflash;
        public static OptionItem GRCanSeeNumberOfButtonsOnOthers;

        public static OptionItem KillFlashDuration;

        // HideAndSeek
        public static OptionItem AllowCloseDoors;
        public static OptionItem KillDelay;
        //public static OptionItem IgnoreCosmetics;
        public static OptionItem IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;
        //特殊モード
        public static OptionItem ONspecialMode;
        public static OptionItem InsiderMode;
        public static OptionItem Taskcheck;
        public static OptionItem RoleImpostor;
        public static OptionItem AllPlayerSkinShuffle;
        public static OptionItem SuddenDeathMode;
        public static OptionItem SuddenAllRoleonaji;
        public static OptionItem SuddenCannotSeeName;
        public static OptionItem SuddenDeathTimeLimit;
        public static OptionItem SuddenDeathReactortime;
        public static OptionItem SuddenItijohoSend;
        public static OptionItem SuddenItijohoSendstart;
        public static OptionItem SuddenItijohoSenddis;
        public static OptionItem SuddenNokoriPlayerCount;
        public static OptionItem SuddenCanSeeKillflash;
        public static OptionItem SuddenTeam;
        public static OptionItem SuddenTeamYellow;
        public static OptionItem SuddenTeamGreen;
        public static OptionItem SuddenTeamPurple;
        public static OptionItem SuddenTeamOption;
        public static OptionItem SuddenTeamMax;
        public static OptionItem SuddenTeamRole;
        public static OptionItem SuddenRedTeamRole;
        public static OptionItem SuddenBlueTeamRole;
        public static OptionItem SuddenYellowTeamRole;
        public static OptionItem SuddenGreenTeamRole;
        public static OptionItem SuddenPurpleTeamRole;
        public static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.Crewmate,
            CustomRoles.Emptiness,
            CustomRoles.Phantom,
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
        };
        //public static OptionItem CommRepo;

        public static OptionItem UploadDataIsLongTask;
        // タスク無効化
        public static OptionItem DisableTasks;
        public static OptionItem DisableSwipeCard;
        public static OptionItem DisableSubmitScan;
        public static OptionItem DisableUnlockSafe;
        public static OptionItem DisableUploadData;
        public static OptionItem DisableStartReactor;
        public static OptionItem DisableResetBreaker;
        public static OptionItem DisableCatchFish;
        public static OptionItem DisableDivertPower;
        public static OptionItem DisableFuelEngins;
        public static OptionItem DisableInspectSample;
        public static OptionItem DisableRebootWifi;
        //
        public static OptionItem DisableInseki;
        public static OptionItem disableCalibrateDistributor;
        public static OptionItem disableVentCleaning;
        public static OptionItem disableHelpCritter;
        public static OptionItem disablefixwiring;
        //デバイスブロック
        public static OptionItem DevicesOption;
        public static OptionItem DisableDevices;
        public static OptionItem DisableSkeldDevices;
        public static OptionItem DisableSkeldAdmin;
        public static OptionItem DisableSkeldCamera;
        public static OptionItem DisableMiraHQDevices;
        public static OptionItem DisableMiraHQAdmin;
        public static OptionItem DisableMiraHQDoorLog;
        public static OptionItem DisablePolusDevices;
        public static OptionItem DisablePolusAdmin;
        public static OptionItem DisablePolusCamera;
        public static OptionItem DisablePolusVital;
        public static OptionItem DisableAirshipDevices;
        public static OptionItem DisableAirshipCockpitAdmin;
        public static OptionItem DisableAirshipRecordsAdmin;
        public static OptionItem DisableAirshipCamera;
        public static OptionItem DisableAirshipVital;
        public static OptionItem DisableFungleDevices;
        public static OptionItem DisableFungleVital;
        public static OptionItem DisableDevicesIgnoreConditions;
        public static OptionItem DisableDevicesIgnoreImpostors;
        public static OptionItem DisableDevicesIgnoreMadmates;
        public static OptionItem DisableDevicesIgnoreNeutrals;
        public static OptionItem DisableDevicesIgnoreCrewmates;
        public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

        public static OptionItem TimeLimitDevices;
        public static OptionItem TimeLimitAdmin;
        public static OptionItem TimeLimitCamAndLog;
        public static OptionItem TimeLimitVital;
        public static OptionItem CanSeeTimeLimit;
        public static OptionItem CanseeImpTimeLimit;
        public static OptionItem CanseeMadTimeLimit;
        public static OptionItem CanseeCrewTimeLimit;
        public static OptionItem CanseeNeuTimeLimit;

        public static OptionItem TurnTimeLimitDevice;
        public static OptionItem TurnTimeLimitAdmin;
        public static OptionItem TurnTimeLimitCamAndLog;
        public static OptionItem TurnTimeLimitVital;

        // ランダムマップ
        public static OptionItem RandomMapsMode;
        public static OptionItem AddedTheSkeld;
        public static OptionItem AddedMiraHQ;
        public static OptionItem AddedPolus;
        public static OptionItem AddedTheAirShip;
        public static OptionItem AddedTheFungle;
        // public static OptionItem AddedDleks;

        // ランダムスポーン
        public static OptionItem EnableRandomSpawn;
        public static OptionItem CanSeeNextRandomSpawn;
        //Skeld
        public static OptionItem RandomSpawnSkeld;
        public static OptionItem RandomSpawnSkeldCafeteria;
        public static OptionItem RandomSpawnSkeldWeapons;
        public static OptionItem RandomSpawnSkeldLifeSupp;
        public static OptionItem RandomSpawnSkeldNav;
        public static OptionItem RandomSpawnSkeldShields;
        public static OptionItem RandomSpawnSkeldComms;
        public static OptionItem RandomSpawnSkeldStorage;
        public static OptionItem RandomSpawnSkeldAdmin;
        public static OptionItem RandomSpawnSkeldElectrical;
        public static OptionItem RandomSpawnSkeldLowerEngine;
        public static OptionItem RandomSpawnSkeldUpperEngine;
        public static OptionItem RandomSpawnSkeldSecurity;
        public static OptionItem RandomSpawnSkeldReactor;
        public static OptionItem RandomSpawnSkeldMedBay;
        //Mira
        public static OptionItem RandomSpawnMira;
        public static OptionItem RandomSpawnMiraCafeteria;
        public static OptionItem RandomSpawnMiraBalcony;
        public static OptionItem RandomSpawnMiraStorage;
        public static OptionItem RandomSpawnMiraJunction;
        public static OptionItem RandomSpawnMiraComms;
        public static OptionItem RandomSpawnMiraMedBay;
        public static OptionItem RandomSpawnMiraLockerRoom;
        public static OptionItem RandomSpawnMiraDecontamination;
        public static OptionItem RandomSpawnMiraLaboratory;
        public static OptionItem RandomSpawnMiraReactor;
        public static OptionItem RandomSpawnMiraLaunchpad;
        public static OptionItem RandomSpawnMiraAdmin;
        public static OptionItem RandomSpawnMiraOffice;
        public static OptionItem RandomSpawnMiraGreenhouse;
        //Polus
        public static OptionItem RandomSpawnPolus;
        public static OptionItem RandomSpawnPolusOfficeLeft;
        public static OptionItem RandomSpawnPolusOfficeRight;
        public static OptionItem RandomSpawnPolusAdmin;
        public static OptionItem RandomSpawnPolusComms;
        public static OptionItem RandomSpawnPolusWeapons;
        public static OptionItem RandomSpawnPolusBoilerRoom;
        public static OptionItem RandomSpawnPolusLifeSupp;
        public static OptionItem RandomSpawnPolusElectrical;
        public static OptionItem RandomSpawnPolusSecurity;
        public static OptionItem RandomSpawnPolusDropship;
        public static OptionItem RandomSpawnPolusStorage;
        public static OptionItem RandomSpawnPolusRocket;
        public static OptionItem RandomSpawnPolusLaboratory;
        public static OptionItem RandomSpawnPolusToilet;
        public static OptionItem RandomSpawnPolusSpecimens;
        //AIrShip
        public static OptionItem RandomSpawnAirship;
        public static OptionItem RandomSpawnAirshipBrig;
        public static OptionItem RandomSpawnAirshipEngine;
        public static OptionItem RandomSpawnAirshipKitchen;
        public static OptionItem RandomSpawnAirshipCargoBay;
        public static OptionItem RandomSpawnAirshipRecords;
        public static OptionItem RandomSpawnAirshipMainHall;
        public static OptionItem RandomSpawnAirshipNapRoom;
        public static OptionItem RandomSpawnAirshipMeetingRoom;
        public static OptionItem RandomSpawnAirshipGapRoom;
        public static OptionItem RandomSpawnAirshipVaultRoom;
        public static OptionItem RandomSpawnAirshipComms;
        public static OptionItem RandomSpawnAirshipCockpit;
        public static OptionItem RandomSpawnAirshipArmory;
        public static OptionItem RandomSpawnAirshipViewingDeck;
        public static OptionItem RandomSpawnAirshipSecurity;
        public static OptionItem RandomSpawnAirshipElectrical;
        public static OptionItem RandomSpawnAirshipMedical;
        public static OptionItem RandomSpawnAirshipToilet;
        public static OptionItem RandomSpawnAirshipShowers;
        //Fungle
        public static OptionItem RandomSpawnFungle;
        public static OptionItem RandomSpawnFungleKitchen;
        public static OptionItem RandomSpawnFungleBeach;
        public static OptionItem RandomSpawnFungleCafeteria;
        public static OptionItem RandomSpawnFungleRecRoom;
        public static OptionItem RandomSpawnFungleBonfire;
        public static OptionItem RandomSpawnFungleDropship;
        public static OptionItem RandomSpawnFungleStorage;
        public static OptionItem RandomSpawnFungleMeetingRoom;
        public static OptionItem RandomSpawnFungleSleepingQuarters;
        public static OptionItem RandomSpawnFungleLaboratory;
        public static OptionItem RandomSpawnFungleGreenhouse;
        public static OptionItem RandomSpawnFungleReactor;
        public static OptionItem RandomSpawnFungleJungleTop;
        public static OptionItem RandomSpawnFungleJungleBottom;
        public static OptionItem RandomSpawnFungleLookout;
        public static OptionItem RandomSpawnFungleMiningPit;
        public static OptionItem RandomSpawnFungleHighlands;
        public static OptionItem RandomSpawnFungleUpperEngine;
        public static OptionItem RandomSpawnFunglePrecipice;
        public static OptionItem RandomSpawnFungleComms;

        // CustomSpawn
        public static OptionItem CustomSpawn;
        public static OptionItem RandomSpawnCustom1;
        public static OptionItem RandomSpawnCustom2;
        public static OptionItem RandomSpawnCustom3;
        public static OptionItem RandomSpawnCustom4;
        public static OptionItem RandomSpawnCustom5;
        public static OptionItem RandomSpawnCustom6;
        public static OptionItem RandomSpawnCustom7;
        public static OptionItem RandomSpawnCustom8;
        public static OptionItem MeetingAndVoteOpt;

        public static OptionItem ShowVoteResult;
        public static OptionItem ShowVoteJudgment;
        public static readonly string[] ShowVoteJudgments =
        {
            "Impostor","Neutral", "CrewMate(Mad)", "Crewmate","Role","ShowTeam"
        };
        // 投票モード
        public static OptionItem VoteMode;
        public static OptionItem WhenSkipVote;
        public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
        public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
        public static OptionItem WhenSkipVoteIgnoreEmergency;
        public static OptionItem WhenNonVote;
        public static OptionItem WhenTie;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static readonly string[] tieModes =
        {
            "TieMode.Default", "TieMode.All", "TieMode.Random"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

        // ボタン回数
        public static OptionItem SyncButtonMode;
        public static OptionItem SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // 全員生存時の会議時間
        public static OptionItem AllAliveMeeting;
        public static OptionItem AllAliveMeetingTime;

        // 追加の緊急ボタンクールダウン
        public static OptionItem AdditionalEmergencyCooldown;
        public static OptionItem AdditionalEmergencyCooldownThreshold;
        public static OptionItem AdditionalEmergencyCooldownTime;

        //会議時間
        public static OptionItem LowerLimitVotingTime;
        public static OptionItem MeetingTimeLimit;

        //転落死
        public static OptionItem LadderDeath;
        public static OptionItem LadderDeathChance;
        public static OptionItem LadderDeathNuuun;
        public static OptionItem LadderDeathZipline;

        // 通常モードでかくれんぼ
        public static bool IsStandardHAS => StandardHAS.GetBool() && CurrentGameMode == CustomGameMode.Standard;
        public static OptionItem StandardHAS;
        public static OptionItem StandardHASWaitingTime;

        // リアクターの時間制御
        public static OptionItem SabotageTimeControl;
        public static OptionItem SkeldReactor;
        public static OptionItem Skeldo2;
        public static OptionItem Mirare;
        public static OptionItem MiraO2;
        public static OptionItem PolusReactorTimeLimit;
        public static OptionItem AirshipReactorTimeLimit;
        public static OptionItem FungleReactorTimeLimit;
        public static OptionItem FungleMushroomMixupDuration;

        // サボタージュのクールダウン変更
        public static OptionItem ModifySabotageCooldown;
        public static OptionItem SabotageCooldown;

        // 停電の特殊設定
        public static OptionItem LightsOutSpecialSettings;
        public static OptionItem LightOutDonttouch;
        public static OptionItem LightOutDonttouchTime;
        public static OptionItem DisableAirshipViewingDeckLightsPanel;
        public static OptionItem DisableAirshipGapRoomLightsPanel;
        public static OptionItem DisableAirshipCargoLightsPanel;
        public static OptionItem BlockDisturbancesToSwitches;
        public static OptionItem CommsSpecialSettings;
        public static OptionItem CommsCamouflage;
        public static OptionItem CommsDonttouch;
        public static OptionItem CommsDonttouchTime;
        // 他サボ
        public static OptionItem Chcabowin;
        // マップ改造
        public static OptionItem Sabotage;
        public static OptionItem MapModification;
        public static OptionItem AirShipVariableElectrical;
        public static OptionItem DisableAirshipMovingPlatform;
        public static OptionItem CuseVent;
        public static OptionItem CuseVentCount;
        public static OptionItem MaxInVentMode;
        public static OptionItem MaxInVentTime;
        public static OptionItem ResetDoorsEveryTurns;
        public static OptionItem DoorsResetMode;
        public static OptionItem DisableFungleSporeTrigger;
        public static OptionItem CantUseZipLineTotop;
        public static OptionItem CantUseZipLineTodown;
        // その他
        public static OptionItem ConvenientOptions;
        public static OptionItem FirstTurnMeeting;
        public static bool firstturnmeeting;
        public static OptionItem FirstTurnMeetingCantability;
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem FixZeroKillCooldown;
        public static OptionItem CanseeVoteresult;
        public static OptionItem VRcanseemitidure;
        public static OptionItem Onlyseepet;
        public static OptionItem CommnTaskResetAssing;
        public static OptionItem OutroCrewWinreasonchenge;
        public static OptionItem TeamHideChat;
        public static OptionItem ImpostorHideChat;
        public static OptionItem LoversHideChat;
        public static OptionItem JackalHideChat;
        public static OptionItem TwinsHideChat;
        public static OptionItem ConnectingHideChat;

        public static OptionItem DisableTaskWin;

        public static OptionItem GhostOptions;
        public static OptionItem GhostCanSeeOtherRoles;
        public static OptionItem GhostCanSeeOtherTasks;
        public static OptionItem GhostCanSeeOtherVotes;
        public static OptionItem GhostCanSeeDeathReason;
        public static OptionItem GhostCanSeeKillerColor;
        public static OptionItem GhostIgnoreTasks;
        public static OptionItem GhostCanSeeAllTasks;
        public static OptionItem GhostCanSeeKillflash;
        public static OptionItem GhostCanSeeNumberOfButtonsOnOthers;

        // プリセット対象外
        public static OptionItem NoGameEnd;
        public static OptionItem AutoDisplayLastResult;
        public static OptionItem AutoDisplayKillLog;
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem HideSettingsDuringGame;
        public static OptionItem ColorNameMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;
        public static OptionItem sotodererukomando;
        public static OptionItem UseZoom;

        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        //public static OptionItem KickModClient;
        public static OptionItem ApplyBanList;
        public static OptionItem KiclHotNotFriend;

        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording",
            "SuffixMode.RoomHost",
            "SuffixMode.OriginalName",
            "SuffixMode.Timer"
        };
        public static readonly string[] RoleAssigningAlgorithms =
        {
            "RoleAssigningAlgorithm.Default",
            "RoleAssigningAlgorithm.NetRandom",
            "RoleAssigningAlgorithm.HashRandom",
            "RoleAssigningAlgorithm.Xorshift",
            "RoleAssigningAlgorithm.MersenneTwister",
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetValue();
        }

        public static int SnitchExposeTaskLeft = 1;

        public static bool IsLoaded = false;
        public static int GetRoleCount(CustomRoles role)
        {
            return GetRoleChance(role) == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }

        public static int GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }
        public static void Load()
        {
            if (IsLoaded) return;
            OptionSaver.Initialize();
            // プリセット
            PresetOptionItem.Preset = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All)
                .SetColor(ModColors.bluegreen);

            #region 役職・詳細設定
            CustomRoleCounts = new();
            CustomRoleSpawnChances = new();

            var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);
            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.MainSettings, false)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.GM))
                .SetHeader(true);

            RoleAssignManager.SetupOptionItem();
            WinOption.SetupCustomOption();

            //タスクバトル
            TaskBattle.SetupOptionItem();

            //特殊モード
            ONspecialMode = BooleanOptionItem.Create(200000, "ONspecialMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#00c1ff");
            InsiderMode = BooleanOptionItem.Create(200001, "InsiderMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            Taskcheck = BooleanOptionItem.Create(200002, "Taskcheck", false, TabGroup.MainSettings, false).SetParent(InsiderMode);
            ColorNameMode = BooleanOptionItem.Create(200003, "ColorNameMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.All);
            RoleImpostor = BooleanOptionItem.Create(200006, "VRoleImpostor", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            AllPlayerSkinShuffle = BooleanOptionItem.Create(200100, "AllPlayerSkinShuffle", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetCansee(() => Event.April || Event.Special).SetInfo(Translator.GetString("AprilfoolOnly"));
            StandardHAS = BooleanOptionItem.Create(200004, "StandardHAS", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
            .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(200005, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            SuddenDeathMode = BooleanOptionItem.Create(200007, "SuddenDeathMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
            .SetGameMode(CustomGameMode.Standard);
            SuddenAllRoleonaji = BooleanOptionItem.Create(200008, "SuddenAllRoleonaji", false, TabGroup.MainSettings, false).SetParent(SuddenDeathMode)
            .SetGameMode(CustomGameMode.Standard);
            SuddenCannotSeeName = BooleanOptionItem.Create(200009, "SuddenCannotSeeName", false, TabGroup.MainSettings, false).SetParent(SuddenDeathMode)
            .SetGameMode(CustomGameMode.Standard);
            SuddenNokoriPlayerCount = BooleanOptionItem.Create(200015, "SuddenNokoriPlayerCount", true, TabGroup.MainSettings, false).SetParent(SuddenDeathMode);
            SuddenCanSeeKillflash = BooleanOptionItem.Create(200016, "SuddenCanSeeKillflash", true, TabGroup.MainSettings, false).SetParent(SuddenDeathMode);
            SuddenDeathTimeLimit = FloatOptionItem.Create(200010, "SuddenDeathTimeLimit", new(0, 300, 1f), 120f, TabGroup.MainSettings, false, true).SetParent(SuddenDeathMode).SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
            SuddenDeathReactortime = FloatOptionItem.Create(200011, "SuddenDeathReactortime", new(1, 300, 1f), 15f, TabGroup.MainSettings, false).SetParent(SuddenDeathMode).SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
            SuddenItijohoSend = BooleanOptionItem.Create(200012, "SuddenItijohoSend", true, TabGroup.MainSettings, false).SetParent(SuddenDeathMode)
            .SetGameMode(CustomGameMode.Standard);
            SuddenItijohoSendstart = FloatOptionItem.Create(200013, "SuddenItijohoSendstart", new(0, 300, 0.5f), 90f, TabGroup.MainSettings, false).SetParent(SuddenItijohoSend).SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
            SuddenItijohoSenddis = FloatOptionItem.Create(200014, "SuddenItijohoSenddis", new(0, 180, 0.5f), 5f, TabGroup.MainSettings, false).SetParent(SuddenItijohoSend).SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
            SuddenTeam = BooleanOptionItem.Create(200020, "SuddenTeam", false, TabGroup.MainSettings, false).SetParent(SuddenDeathMode);
            SuddenTeamYellow = BooleanOptionItem.Create(200030, "SuddenTeamYellow", true, TabGroup.MainSettings, false).SetParent(SuddenTeam).SetColor(ModColors.Yellow);
            SuddenTeamGreen = BooleanOptionItem.Create(200031, "SuddenTeamGreen", true, TabGroup.MainSettings, false).SetParent(SuddenTeam).SetColor(ModColors.Green);
            SuddenTeamPurple = BooleanOptionItem.Create(200032, "SuddenTeamPurple", true, TabGroup.MainSettings, false).SetParent(SuddenTeam).SetColor(ModColors.Purple);
            SuddenTeamOption = BooleanOptionItem.Create(200021, "SuddenTeamOption", false, TabGroup.MainSettings, false).SetParent(SuddenTeam);
            SuddenTeamMax = IntegerOptionItem.Create(200022, "SuddenTeamMax", new(1, 15, 1), 2, TabGroup.MainSettings, false).SetParent(SuddenTeam);
            SuddenTeamRole = BooleanOptionItem.Create(200023, "SuddenTeamRole", false, TabGroup.MainSettings, false).SetParent(SuddenTeam);

            var StringArray = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role)).Select(role => role.ToString()).ToArray();
            SuddenRedTeamRole = StringOptionItem.Create(200025, "SuddenRedTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Red).SetParent(SuddenTeamRole);
            SuddenBlueTeamRole = StringOptionItem.Create(200026, "SuddenBlueTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Blue).SetParent(SuddenTeamRole);
            SuddenYellowTeamRole = StringOptionItem.Create(200027, "SuddenYellowTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Yellow).SetParent(SuddenTeamRole);
            SuddenGreenTeamRole = StringOptionItem.Create(200028, "SuddenGreenTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Green).SetParent(SuddenTeamRole);
            SuddenPurpleTeamRole = StringOptionItem.Create(200029, "SuddenPurpleTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Purple).SetParent(SuddenTeamRole);

            // 試験的機能
            ExperimentalMode = BooleanOptionItem.Create(300000, "ExperimentalMode", false, TabGroup.MainSettings, false).SetColor(Palette.CrewmateSettingChangeText)
                .SetGameMode(CustomGameMode.Standard);
            ExAftermeetingflash = BooleanOptionItem.Create(300002, "ExAftermeetingflash", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode)
                            .SetGameMode(CustomGameMode.Standard);
            ExHideChatCommand = BooleanOptionItem.Create(300003, "ExHideChatCommand", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode)
                            .SetGameMode(CustomGameMode.Standard)
                            .SetInfo(Translator.GetString("ExHideChatCommandInfo"));
            TeamHideChat = BooleanOptionItem.Create(900_006, "TeamHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(ExHideChatCommand);
            ImpostorHideChat = BooleanOptionItem.Create(900_007, "ImpostorHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(ModColors.ImpostorRed).SetParent(TeamHideChat);
            JackalHideChat = BooleanOptionItem.Create(900_008, "JackalHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Jackal)).SetParent(TeamHideChat);
            LoversHideChat = BooleanOptionItem.Create(900_009, "LoversHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Lovers)).SetParent(TeamHideChat);
            TwinsHideChat = BooleanOptionItem.Create(76120, "TwinsCanUseHideChet", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Twins)).SetParent(TeamHideChat);
            ConnectingHideChat = BooleanOptionItem.Create(900_013, "ConnectingHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Connecting)).SetParent(TeamHideChat);
            /*
        BlackOutwokesitobasu = BooleanOptionItem.Create(1_000_009, "BlackOutwokesitobasu", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.All)
            .SetColorcode("#ff0000")
            .SetParent(ExperimentalMode)
            .SetInfo(Utils.ColorString(Color.red, "  " + Translator.GetString("BlackOutwokesitobasuInfo")));*/
            /*ExWeightReduction = BooleanOptionItem.Create(300007, "ExWeightReduction", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(ExperimentalMode)
                .SetInfo($"<color=red>{Translator.GetString("ExWeightReductionInfo")}</color>");*/
            ExRpcWeightR = BooleanOptionItem.Create(300008, "ExRpcWeightR", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode);

            //9人以上部屋で落ちる現象の対策
            FixSpawnPacketSize = BooleanOptionItem.Create(300004, "FixSpawnPacketSize", false, TabGroup.MainSettings, true)
                .SetColor(new Color32(255, 255, 0, 255))
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("FixSpawnPacketSizeInfo"));

            // Impostor
            sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Impostor).Do(info =>
            {
                if (info.RoleName is CustomRoles.AlienHijack) return;
                SetupRoleOptions(info);
                info.OptionCreator?.Invoke();
            });
            DoubleTriggerThreshold = FloatOptionItem.Create(1101, "DoubleTriggerThreashould", new(0.3f, 1f, 0.1f), 0.5f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            DefaultShapeshiftCooldown = FloatOptionItem.Create(1100, "DefaultShapeshiftCooldown", new(1f, 999f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);

            // Madmate, Crewmate, Neutral
            sortedRoleInfo.Where(role => role.CustomRoleType != CustomRoleTypes.Impostor).Do(info =>
            {
                //#if RELEASE
                if (info.RoleName == CustomRoles.Cakeshop && !(DateTime.Now.Month == 1 && DateTime.Now.Day <= 8)) return;
                //#endif
                SetupRoleOptions(info);
                info.OptionCreator?.Invoke();
            });
            SetupRoleOptions(1800, TabGroup.MainSettings, CustomRoles.NotAssigned, new(1, 1, 1));
            RoleAddAddons.Create(1805, TabGroup.MainSettings, CustomRoles.NotAssigned);
            // Madmate Common Options
            CanMakeMadmateCount = IntegerOptionItem.Create(101012, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
                .SetValueFormat(OptionFormat.Players)
                .SetHeader(true)
                .SetColor(Palette.ImpostorRed);
            SkMadCanUseVent = BooleanOptionItem.Create(1010019, "SkMadCanUseVent", false, TabGroup.MadmateRoles, false)
                .SetParent(CanMakeMadmateCount);
            MadMateOption = BooleanOptionItem.Create(1010013, "MadmateOption", false, TabGroup.MadmateRoles, false)
                .SetHeader(true)
                .SetColorcode("#ffa3a3");
            MadmateCanFixLightsOut = BooleanOptionItem.Create(101014, "MadmateCanFixLightsOut", false, TabGroup.MadmateRoles, false).SetColorcode("#ffcc66").SetParent(MadMateOption);
            MadmateCanFixComms = BooleanOptionItem.Create(101015, "MadmateCanFixComms", false, TabGroup.MadmateRoles, false).SetColorcode("#999999").SetParent(MadMateOption);
            //MadmateHasImpostorVision = BooleanOptionItem.Create(101004, "MadmateHasImpostorVision", false, TabGroup.MadmateRoles, false).SetParent(MadMateOption);
            MadmateHasLighting = BooleanOptionItem.Create(101004, "MadmateHasLighting", false, TabGroup.MadmateRoles, false).SetColorcode("#ec6800").SetParent(MadMateOption);
            MadmateHasMoon = BooleanOptionItem.Create(1010016, "MadmateHasMoon", false, TabGroup.MadmateRoles, false).SetColorcode("#ffff33").SetParent(MadMateOption);

            MadmateCanSeeKillFlash = BooleanOptionItem.Create(101005, "MadmateCanSeeKillFlash", false, TabGroup.MadmateRoles, false).SetColorcode("#61b26c").SetParent(MadMateOption);
            MadmateCanSeeOtherVotes = BooleanOptionItem.Create(101006, "MadmateCanSeeOtherVotes", false, TabGroup.MadmateRoles, false).SetColorcode("#800080").SetParent(MadMateOption);
            MadmateCanSeeDeathReason = BooleanOptionItem.Create(101007, "MadmateCanSeeDeathReason", false, TabGroup.MadmateRoles, false).SetColorcode("#80ffdd").SetParent(MadMateOption);
            MadmateRevengeCrewmate = BooleanOptionItem.Create(101008, "MadmateExileCrewmate", false, TabGroup.MadmateRoles, false).SetColorcode("#00fa9a").SetParent(MadMateOption);
            MadNekomataCanImp = BooleanOptionItem.Create(101017, "NekoKabochaImpostorsGetRevenged", false, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate);
            MadNekomataCanCrew = BooleanOptionItem.Create(101009, "NekomataCanCrew", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate);
            MadNekomataCanMad = BooleanOptionItem.Create(1010018, "NekoKabochaMadmatesGetRevenged", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate);
            MadNekomataCanNeu = BooleanOptionItem.Create(101010, "NekomataCanNeu", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengeCrewmate);
            MadCanSeeImpostor = BooleanOptionItem.Create(101019, "MadmateCanSeeImpostor", false, TabGroup.MadmateRoles, false).SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Snitch)).SetParent(MadMateOption);
            MadmateTell = StringOptionItem.Create(101020, "MadmateTellOption", Tellopt, 0, TabGroup.MadmateRoles, false).SetColor(UtilsRoleText.GetRoleColor(CustomRoles.FortuneTeller)).SetParent(MadMateOption);

            MadmateVentCooldown = FloatOptionItem.Create(101011, "MadmateVentCooldown", new(0f, 180f, 0.5f), 0f, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(101018, "MadmateVentMaxTime", new(0f, 180f, 0.5f), 0f, TabGroup.MadmateRoles, false, infinity: true).SetColorcode("#8cffff").SetParent(MadMateOption)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateCanMovedByVent = BooleanOptionItem.Create(101013, "MadmateCanMovedByVent", true, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption);

            //Com
            Twins.SetUpTwinsOptions();
            Lovers.SetLoversOptions();
            GhostRoleCore.SetupCustomOptionAddonAndIsGhostRole();

            //幽霊役職の設定
            GRRoleOp = BooleanOptionItem.Create(102001, "GRRoleOptions", false, TabGroup.GhostRoles, false)
                .SetHeader(true)
                .SetColorcode("#666699")
                .SetGameMode(CustomGameMode.All);
            GRCanSeeOtherRoles = BooleanOptionItem.Create(102002, "GRCanSeeOtherRoles", false, TabGroup.GhostRoles, false)
                .SetColorcode("#7474ab")
                .SetGameMode(CustomGameMode.All)
                .SetParent(GRRoleOp);
            GRCanSeeOtherTasks = BooleanOptionItem.Create(102003, "GRCanSeeOtherTasks", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.yellow)
                .SetParent(GRRoleOp);
            GRCanSeeOtherVotes = BooleanOptionItem.Create(102004, "GRCanSeeOtherVotes", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#800080")
                .SetParent(GRRoleOp);
            GRCanSeeDeathReason = BooleanOptionItem.Create(102005, "GRCanSeeDeathReason", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GRRoleOp);
            GRCanSeeKillerColor = BooleanOptionItem.Create(102006, "GRCanSeeKillerColor", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GRCanSeeDeathReason);
            GRCanSeeAllTasks = BooleanOptionItem.Create(102007, "GRCanSeeAllTasks", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cee4ae")
                .SetParent(GRRoleOp);
            GRCanSeeNumberOfButtonsOnOthers = BooleanOptionItem.Create(102008, "GRCanSeeNumberOfButtonsOnOthers", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#d7c447")
                .SetParent(GRRoleOp);
            GRCanSeeKillflash = BooleanOptionItem.Create(102009, "GRCanSeeKillflash", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#61b26c")
                .SetParent(GRRoleOp);
            #endregion

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#bf483f");

            // HideAndSeek
            SetupRoleOptions(100000, TabGroup.MainSettings, CustomRoles.HASFox, customGameMode: CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, TabGroup.MainSettings, CustomRoles.HASTroll, customGameMode: CustomGameMode.HideAndSeek);
            KillDelay = FloatOptionItem.Create(101001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = BooleanOptionItem.Create(101003, "IgnoreVent", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // マップ改造
            MapModification = BooleanOptionItem.Create(102000, "MapModification", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ccff66");
            AirShipVariableElectrical = BooleanOptionItem.Create(101600, "AirShipVariableElectrical", false, TabGroup.MainSettings, false).SetParent(MapModification);
            DisableAirshipMovingPlatform = BooleanOptionItem.Create(101700, "DisableAirshipMovingPlatform", false, TabGroup.MainSettings, false).SetParent(MapModification);
            DisableFungleSporeTrigger = BooleanOptionItem.Create(101900, "DisableFungleSporeTrigger", false, TabGroup.MainSettings, false).SetParent(MapModification);
            CantUseZipLineTotop = BooleanOptionItem.Create(101901, "CantUseZipLineTotop", false, TabGroup.MainSettings, false).SetParent(MapModification);
            CantUseZipLineTodown = BooleanOptionItem.Create(101902, "CantUseZipLineTodown", false, TabGroup.MainSettings, false).SetParent(MapModification);
            ResetDoorsEveryTurns = BooleanOptionItem.Create(101800, "ResetDoorsEveryTurns", false, TabGroup.MainSettings, false).SetParent(MapModification);
            DoorsResetMode = StringOptionItem.Create(101810, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.MainSettings, false).SetParent(ResetDoorsEveryTurns);
            CuseVent = BooleanOptionItem.Create(101701, "Can'tUseVent", false, TabGroup.MainSettings, false).SetParent(MapModification);
            CuseVentCount = FloatOptionItem.Create(101702, "CuseVentCount", new(1f, 15f, 1f), 5f, TabGroup.MainSettings, false).SetValueFormat(OptionFormat.Players).SetParent(CuseVent);
            MaxInVentMode = BooleanOptionItem.Create(101704, "MaxInVentMode", false, TabGroup.MainSettings, false).SetParent(MapModification);
            MaxInVentTime = FloatOptionItem.Create(101705, "MaxInVentTime", new(3f, 300, 0.5f), 30f, TabGroup.MainSettings, false).SetValueFormat(OptionFormat.Seconds).SetParent(MaxInVentMode);

            // タスク無効化
            UploadDataIsLongTask = BooleanOptionItem.Create(101703, "UploadDataIsLongTask", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(MapModification);
            DisableTasks = BooleanOptionItem.Create(100300, "DisableTasks", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#6b6b6b");
            DisableSwipeCard = BooleanOptionItem.Create(100301, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = BooleanOptionItem.Create(100302, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = BooleanOptionItem.Create(100303, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = BooleanOptionItem.Create(100304, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = BooleanOptionItem.Create(100305, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = BooleanOptionItem.Create(100306, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableCatchFish = BooleanOptionItem.Create(100307, "DisableCatchFish", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableDivertPower = BooleanOptionItem.Create(100308, "DisableDivertPower", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableFuelEngins = BooleanOptionItem.Create(100309, "DisableFuelEngins", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableInspectSample = BooleanOptionItem.Create(100310, "DisableInspectSample", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableRebootWifi = BooleanOptionItem.Create(100311, "DisableRebootWifi", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableInseki = BooleanOptionItem.Create(100312, "DisableInseki", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            disableCalibrateDistributor = BooleanOptionItem.Create(100313, "disableCalibrateDistributor", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            disableVentCleaning = BooleanOptionItem.Create(100314, "disableVentCleaning", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            disableHelpCritter = BooleanOptionItem.Create(100315, "disableHelpCritter", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            disablefixwiring = BooleanOptionItem.Create(100316, "disablefixwiring", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //デバイス設定
            DevicesOption = BooleanOptionItem.Create(101000, "DevicesOption", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#d860e0");
            DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.MainSettings, false).SetParent(DevicesOption)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");
            DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");
            DisableFungleDevices = BooleanOptionItem.Create(101250, "DisableFungleDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableFungleVital = BooleanOptionItem.Create(101251, "DisableFungleVital", false, TabGroup.MainSettings, false).SetParent(DisableFungleDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");

            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ff1919");
            DisableDevicesIgnoreMadmates = BooleanOptionItem.Create(101292, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ff1919");
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#808080");
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#8cffff");
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#666699");

            TimeLimitDevices = BooleanOptionItem.Create(109000, "TimeLimitDevices", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#948e50")
                .SetParent(DevicesOption);
            TimeLimitAdmin = FloatOptionItem.Create(109001, "TimeLimitAdmin", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false, true)
                .SetGameMode(CustomGameMode.Standard).SetColorcode("#00ff99").SetValueFormat(OptionFormat.Seconds).SetParent(TimeLimitDevices);
            TimeLimitCamAndLog = FloatOptionItem.Create(109002, "TimeLimitCamAndLog", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false, true)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#cccccc").SetValueFormat(OptionFormat.Seconds).SetParent(TimeLimitDevices);
            TimeLimitVital = FloatOptionItem.Create(109003, "TimeLimitVital", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false, true)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#33ccff").SetValueFormat(OptionFormat.Seconds).SetParent(TimeLimitDevices);
            CanSeeTimeLimit = BooleanOptionItem.Create(109050, "CanSeeTimeLimit", false, TabGroup.MainSettings, false)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#cc8b60").SetParent(TimeLimitDevices);
            CanseeImpTimeLimit = BooleanOptionItem.Create(109051, "CanseeImpTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(ModColors.ImpostorRed).SetParent(CanSeeTimeLimit);
            CanseeMadTimeLimit = BooleanOptionItem.Create(109052, "CanseeMadTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(ModColors.MadMateOrenge).SetParent(CanSeeTimeLimit);
            CanseeCrewTimeLimit = BooleanOptionItem.Create(109053, "CanseeCrewTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(ModColors.CrewMateBlue).SetParent(CanSeeTimeLimit);
            CanseeNeuTimeLimit = BooleanOptionItem.Create(109054, "CanseeNeuTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(Palette.DisabledGrey).SetParent(CanSeeTimeLimit);

            TurnTimeLimitDevice = BooleanOptionItem.Create(109100, "TurnTimeLimitDevice", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#b06927")
                .SetParent(DevicesOption);
            TurnTimeLimitAdmin = FloatOptionItem.Create(109101, "TimeLimitAdmin", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false, true)
            .SetGameMode(CustomGameMode.Standard).SetColorcode("#00ff99").SetValueFormat(OptionFormat.Seconds).SetParent(TurnTimeLimitDevice);
            TurnTimeLimitCamAndLog = FloatOptionItem.Create(109102, "TimeLimitCamAndLog", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false, true)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#cccccc").SetValueFormat(OptionFormat.Seconds).SetParent(TurnTimeLimitDevice);
            TurnTimeLimitVital = FloatOptionItem.Create(109103, "TimeLimitVital", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false, true)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#33ccff").SetValueFormat(OptionFormat.Seconds).SetParent(TurnTimeLimitDevice);

            //サボ
            Sabotage = BooleanOptionItem.Create(100800, "Sabotage", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#b71e1e")
                .SetGameMode(CustomGameMode.Standard);
            // リアクターの時間制御
            SabotageTimeControl = BooleanOptionItem.Create(100801, "SabotageTimeControl", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#f22c50");
            SkeldReactor = FloatOptionItem.Create(100802, "SkeldReactorTimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            Skeldo2 = FloatOptionItem.Create(100803, "SkeldO2TimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            Mirare = FloatOptionItem.Create(100804, "MiraReactorTimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            MiraO2 = FloatOptionItem.Create(100805, "MiraO2TimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = FloatOptionItem.Create(100806, "PolusReactorTimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100807, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            FungleReactorTimeLimit = FloatOptionItem.Create(100808, "FungleReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            FungleMushroomMixupDuration = FloatOptionItem.Create(100809, "FungleMushroomMixupDuration", new(1f, 20f, 1f), 10f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            // 他
            Chcabowin = BooleanOptionItem.Create(100813, "Chcabowin", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            // サボタージュのクールダウン変更
            ModifySabotageCooldown = BooleanOptionItem.Create(100810, "ModifySabotageCooldown", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            SabotageCooldown = FloatOptionItem.Create(100811, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(ModifySabotageCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            CommsSpecialSettings = BooleanOptionItem.Create(100812, "CommsSpecialSettings", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#999999");
            CommsDonttouch = BooleanOptionItem.Create(100814, "CommsDonttouch", false, TabGroup.MainSettings, false).SetParent(CommsSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            CommsDonttouchTime = FloatOptionItem.Create(100815, "CommsDonttouchTime", new(0f, 180f, 0.5f), 3.0f, TabGroup.MainSettings, false).SetParent(CommsDonttouch)
                .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            CommsCamouflage = BooleanOptionItem.Create(100816, "CommsCamouflage", false, TabGroup.MainSettings, false).SetParent(CommsSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            //CommRepo = BooleanOptionItem.Create(227, "CommRepo", false, TabGroup.MainSettings, false).SetParent(Sabotage)
            //    .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ffcc66");
            LightOutDonttouch = BooleanOptionItem.Create(1015015, "LightOutDonttouch", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            LightOutDonttouchTime = FloatOptionItem.Create(101510, "LightOutDonttouchTime", new(0f, 180f, 0.5f), 3.0f, TabGroup.MainSettings, false).SetParent(LightOutDonttouch)
            .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            BlockDisturbancesToSwitches = BooleanOptionItem.Create(101514, "BlockDisturbancesToSwitches", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            AllowCloseDoors = BooleanOptionItem.Create(101670, "AllowCloseDoors", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All).SetParent(Sabotage);
            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ffcc66")
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666666");
            AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff6633");
            AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#980098");
            AddedTheAirShip = BooleanOptionItem.Create(100404, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff3300");
            AddedTheFungle = BooleanOptionItem.Create(100406, "AddedTheFungle", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff9900");
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // ランダムスポーン
            EnableRandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ff99cc")
                .SetGameMode(CustomGameMode.All);
            CanSeeNextRandomSpawn = BooleanOptionItem.Create(101301, "CanSeeNextRandomSpawn", false, TabGroup.MainSettings, false).SetParent(EnableRandomSpawn)
                .SetGameMode(CustomGameMode.All);
            RandomSpawn.SetupCustomOption();

            //会議設定
            MeetingAndVoteOpt = BooleanOptionItem.Create(100539, "MeetingAndVoteOpt", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#64ff0a")
                .SetHeader(true);
            LowerLimitVotingTime = FloatOptionItem.Create(100950, "LowerLimitVotingTime", new(5f, 300f, 1f), 60f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            MeetingTimeLimit = FloatOptionItem.Create(100951, "LimitMeetingTime", new(5f, 300f, 1f), 300f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            // 投票モード
            VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.MainSettings, false)
                .SetColorcode("#33ff99")
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            SyncButtonMode = BooleanOptionItem.Create(100550, "SyncButtonMode", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt)
                .SetColorcode("#64ff0a");
            SyncedButtonCount = IntegerOptionItem.Create(100560, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times)
                .SetGameMode(CustomGameMode.Standard);
            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false).SetParent(MeetingAndVoteOpt);
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            ShowVoteResult = BooleanOptionItem.Create(100980, "ShowVoteResult", false, TabGroup.MainSettings, false).SetParent(MeetingAndVoteOpt);
            ShowVoteJudgment = StringOptionItem.Create(100981, "ShowVoteJudgment", ShowVoteJudgments, 0, TabGroup.MainSettings, false).SetParent(ShowVoteResult);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ffcc00");
            LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath);
            LadderDeathNuuun = BooleanOptionItem.Create(101111, "LadderDeathNuuun", false, TabGroup.MainSettings, false).SetParent(LadderDeath);
            LadderDeathZipline = BooleanOptionItem.Create(101112, "LadderDeathZipline", false, TabGroup.MainSettings, false).SetParent(LadderDeath);

            //幽霊設定
            GhostOptions = BooleanOptionItem.Create(901_000, "GhostOptions", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#6c6ce0")
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(901_001, "GhostCanSeeOtherRoles", true, TabGroup.MainSettings, false)
                .SetColorcode("#7474ab")
                .SetGameMode(CustomGameMode.All)
                .SetParent(GhostOptions);
            GhostCanSeeOtherTasks = BooleanOptionItem.Create(901_002, "GhostCanSeeOtherTasks", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.yellow)
                .SetParent(GhostOptions);
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(901_003, "GhostCanSeeOtherVotes", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#800080")
                .SetParent(GhostOptions);
            GhostCanSeeDeathReason = BooleanOptionItem.Create(901_004, "GhostCanSeeDeathReason", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GhostOptions);
            GhostCanSeeKillerColor = BooleanOptionItem.Create(901_005, "GhostCanSeeKillerColor", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GhostCanSeeDeathReason);
            GhostCanSeeAllTasks = BooleanOptionItem.Create(901_006, "GhostCanSeeAllTasks", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cee4ae")
                .SetParent(GhostOptions);
            GhostCanSeeNumberOfButtonsOnOthers = BooleanOptionItem.Create(901_007, "GhostCanSeeNumberOfButtonsOnOthers", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#d7c447")
                .SetParent(GhostOptions);
            GhostCanSeeKillflash = BooleanOptionItem.Create(901_008, "GhostCanSeeKillflash", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#61b26c")
                .SetParent(GhostOptions);
            GhostIgnoreTasks = BooleanOptionItem.Create(901_009, "GhostIgnoreTasks", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#bbbbdd")
                .SetParent(GhostOptions);

            // その他
            //初手強制会議
            ConvenientOptions = BooleanOptionItem.Create(900_999, "ConvenientOptions", true, TabGroup.MainSettings, false)
                .SetColorcode("#cc3366")
                .SetHeader(true);
            FirstTurnMeeting = BooleanOptionItem.Create(900_011, "FirstTurnMeeting", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#4fd6a7")
                .SetParent(ConvenientOptions);
            FirstTurnMeetingCantability = BooleanOptionItem.Create(900_012, "FirstTurnMeetingCantability", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.Standard).SetParent(FirstTurnMeeting);
            Onlyseepet = BooleanOptionItem.Create(900_004, "Onlyseepet", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#e6dfc1")
                .SetParent(ConvenientOptions);
            FixFirstKillCooldown = BooleanOptionItem.Create(900_000, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#fa7373")
                .SetParent(ConvenientOptions);
            FixZeroKillCooldown = BooleanOptionItem.Create(900_001, "FixZeroKillCooldown", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#e63054")
                .SetParent(ConvenientOptions);
            CommnTaskResetAssing = BooleanOptionItem.Create(900_005, "CommnTaskResetAssing", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.yellow)
                .SetParent(ConvenientOptions);
            CanseeVoteresult = BooleanOptionItem.Create(900_002, "CanseeVoteresult", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#64ff0a")
                .SetParent(ConvenientOptions);
            VRcanseemitidure = BooleanOptionItem.Create(900_003, "CanseeMeetingAfterMitidure", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(CanseeVoteresult);
            OutroCrewWinreasonchenge = BooleanOptionItem.Create(900_010, "OutroCrewWinreasonchenge", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#66ffff")
                .SetParent(ConvenientOptions);

            DisableTaskWin = BooleanOptionItem.Create(905_000, "DisableTaskWin", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ccff00")
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = BooleanOptionItem.Create(905_001, "NoGameEnd", false, TabGroup.MainSettings, false)
                .SetColorcode("#ff1919")
                .SetGameMode(CustomGameMode.All);
            // プリセット対象外
            AutoDisplayLastResult = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastResult", true, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#66ffff")
                .SetGameMode(CustomGameMode.All);
            AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#66ffff");
            HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            HideSettingsDuringGame = BooleanOptionItem.Create(1_000_003, "HideGameSettingsDuringGame", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff"); ;
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff")
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );
            sotodererukomando = BooleanOptionItem.Create(1_000_007, "sotodererukomando", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            UseZoom = BooleanOptionItem.Create(1_000_008, "UseZoom", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#9199a1");

            ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("KickBanOptionWhiteList"))
                .SetColor(Color.red);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("KickBanOptionWhiteList"))
                .SetColor(Color.red);
            //KickModClient = BooleanOptionItem.Create(1_000_102, "KickModClient", false, TabGroup.MainSettings, true)
            //.SetGameMode(CustomGameMode.All);
            ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.red);
            KiclHotNotFriend = BooleanOptionItem.Create(1_000_111, "KiclHotNotFriend", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("KickBanOptionWhiteList"))
                .SetColor(Color.red);

            jam = BooleanOptionItem.Create(1_000_112, "AntiCheat", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.gray)
                .SetInfo("...今は動かないよ...ごめんネ")
                .SetHidden(true);

            DebugModeManager.SetupCustomOption();

            OptionSaver.Load();

            Combinations = null; //使わないから消す

            IsLoaded = true;
        }
        private static List<CombinationRoles> Combinations = new();
        public static void SetupRoleOptions(SimpleRoleInfo info) => SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName, info.AssignInfo.AssignCountRule, fromtext: UtilsOption.GetFrom(info), combination: info.Combination);
        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, IntegerValueRule assignCountRule = null, CustomGameMode customGameMode = CustomGameMode.Standard, string fromtext = "", CombinationRoles combination = CombinationRoles.None)
        {
            if ((role is CustomRoles.Phantom) || (combination != CombinationRoles.None && Combinations.Contains(combination))) return;
            if (role.IsVanilla())
            {
                switch (role)
                {
                    case CustomRoles.Impostor: id = 10; break;
                    case CustomRoles.Shapeshifter: id = 30; break;
                    case CustomRoles.Phantom: id = 40; break;
                    case CustomRoles.Crewmate: id = 11; break;
                    case CustomRoles.Engineer: id = 200; break;
                    case CustomRoles.Scientist: id = 250; break;
                    case CustomRoles.Tracker: id = 300; break;
                    case CustomRoles.Noisemaker: id = 350; break;
                }
            }
            assignCountRule ??= new(1, 15, 1);
            var from = "<line-height=25%><size=25%>\n</size><size=60%><pos=50%></color> <b>" + fromtext + "</b></size>";

            var spawnOption = IntegerOptionItem.Create(id, combination == CombinationRoles.None ? role.ToString() : combination.ToString(), new(0, 100, 10), 0, tab, false, from)
                .SetColorcode(UtilsRoleText.GetRoleColorCode(role))
                .SetColor(UtilsRoleText.GetRoleColor(role, true))
                .SetCustomRole(role)
                .SetValueFormat(OptionFormat.Percent)
                .SetHeader(true)
                .SetCansee(() => role is not CustomRoles.Crewmate and not CustomRoles.Impostor || ShowFilter.NowSettingRole is not CustomRoles.NotAssigned)
                .SetHidden(role == CustomRoles.NotAssigned || (!DebugModeManager.AmDebugger && combination is CombinationRoles.AssassinandMerlin))
                .SetGameMode(customGameMode) as IntegerOptionItem;
            var hidevalue = role is CustomRoles.Driver || role.IsRiaju() || (assignCountRule.MaxValue == assignCountRule.MinValue);

            if (role is CustomRoles.Crewmate or CustomRoles.Impostor) return;

            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", assignCountRule, assignCountRule.Step, tab, false, HideValue: hidevalue)
                .SetParent(spawnOption)
                .SetValueFormat(assignCountRule.MaxValue is 7 ? OptionFormat.Set : OptionFormat.Players)
                .SetGameMode(customGameMode)
                .SetHidden(hidevalue);

            if (combination != CombinationRoles.None) Combinations.Add(combination);
            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
    }
}
