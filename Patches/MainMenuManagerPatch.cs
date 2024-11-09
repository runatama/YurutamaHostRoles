using System;
using HarmonyLib;
using TownOfHost.Templates;
using UnityEngine;
using Object = UnityEngine.Object;
using AmongUs.Data;
using Assets.InnerNet;
using AmongUs.Data.Player;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static SimpleButton discordButton;
        public static SimpleButton UpdateButton { get; private set; }
        public static SimpleButton UpdateButton2;
        private static SimpleButton gitHubButton;
        private static SimpleButton TwitterXButton;
        private static SimpleButton TOHkBOTButton;
        private static SimpleButton VersionChangeButton;
        private static SimpleButton betaversionchange;
        public static GameObject VersionMenu;
        public static GameObject betaVersionMenu;
        public static AnnouncementPopUp updatea;

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void StartPostfix(MainMenuManager __instance)
        {
            SimpleButton.SetBase(__instance.quitButton);
            //Discordボタンを生成
            if (SimpleButton.IsNullOrDestroyed(discordButton))
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(-2.5f, -1f, 1f),
                    new(88, 101, 242, byte.MaxValue),
                    new(148, 161, byte.MaxValue, byte.MaxValue),
                    () => Application.OpenURL(Main.DiscordInviteUrl),
                    "Discord",
                    isActive: Main.ShowDiscordButton);
            }

            // GitHubボタンを生成
            if (SimpleButton.IsNullOrDestroyed(gitHubButton))
            {
                gitHubButton = CreateButton(
                    "GitHubButton",
                    new(-0.8f, -1f, 1f),//-1f
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () => Application.OpenURL("https://github.com/KYMario/TownOfHost-K"),
                    "GitHub");
            }

            // TwitterXボタンを生成
            if (SimpleButton.IsNullOrDestroyed(TwitterXButton))
            {
                TwitterXButton = CreateButton(
                    "TwitterXButton",
                    new(0.9f, -1f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/Tohkserver_k"),
                    "Twitter(X)");
            }
            // TOHkBOTボタンを生成 BOTが完成次第アプデと同時に公開
            if (SimpleButton.IsNullOrDestroyed(TOHkBOTButton))
            {
                TOHkBOTButton = CreateButton(
                    "TOHkBOTButton",
                    new(2.6f, -1f, 1f),
                    new(0, 201, 87, byte.MaxValue),
                    new(60, 201, 87, byte.MaxValue),
                    () => Application.OpenURL("https://discord.com/api/oauth2/authorize?client_id=1198276538563567716&permissions=8&scope=bot"),
                    "TOHkBOT");
            }

            //Updateボタンを生成
            if (SimpleButton.IsNullOrDestroyed(UpdateButton))
            {
                UpdateButton = CreateButton(
                    "UpdateButton",
                    new(0f, -1.7f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () =>
                    {
                        //if (!Main.AllowPublicRoom)
                        //{
                        UpdateButton.Button.gameObject.SetActive(false);
                        ModUpdater.StartUpdate(ModUpdater.downloadUrl);
                        //}
                        /*else
                        {
                            UpdateButton.Button.gameObject.SetActive(false);
                            ModUpdater.GoGithub();
                        }*/
                    },
                    $"{Translator.GetString("updateButton")}\n{ModUpdater.latestTitle}",
                    new(2.5f, 1f),
                    isActive: false);
            }
            // アップデート(詳細)ボタンを生成
            if (SimpleButton.IsNullOrDestroyed(UpdateButton2))
            {
                UpdateButton2 = CreateButton(
                    "UpdateButton2",
                    new(1.3f, -1.9f, 1f),
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () =>
                    {
                        if (updatea == null)
                        {
                            updatea = Object.Instantiate(__instance.announcementPopUp);
                        }
                        updatea.name = "Update Detail";
                        updatea.gameObject.SetActive(true);
                        updatea.AnnouncementListSlider.SetActive(false);
                        updatea.Title.text = "TOH-K " + ModUpdater.latestTitle;
                        updatea.AnnouncementBodyText.text = Regex.Replace(ModUpdater.body.Replace("#", "").Replace("**", ""), @"\[(.*?)\]\(.*?\)", "$1");
                        updatea.DateString.text = "Latest Release";
                        updatea.SubTitle.text = "";
                        updatea.ListScroller.gameObject.SetActive(false);
                    },
                    "▽",
                    new(0.5f, 0.5f),
                    isActive: false);
            }
            //同じバージョンの 安定ver,デバッグバージョンの切り替えの奴
            if (SimpleButton.IsNullOrDestroyed(betaversionchange))
            {
                betaversionchange = CreateButton(
                    "betaversionchange",
                    new(-2.3f, -2.6963f, 1f),
                    new(0, 255, 183, byte.MaxValue),
                    new(60, 255, 183, byte.MaxValue),
                    () =>
                    {
                        CredentialsPatch.TohkLogo.gameObject.SetActive(false);
                        __instance.screenTint.enabled = true;
                        if (betaVersionMenu != null)
                        {
                            betaVersionMenu.SetActive(true);
                            return;
                        }
                        betaVersionMenu = new GameObject("verPanel");
                        betaVersionMenu.transform.parent = __instance.gameModeButtons.transform.parent;
                        betaVersionMenu.transform.localPosition = new(-0.0964f, 0.1378f, 1f);
                        betaVersionMenu.SetActive(true);
                        ModUpdater.CheckRelease(all: true, snap: true).GetAwaiter().GetResult();
                        int i = 0;
                        if (ModUpdater.snapshots.Count == 0) return;

                        foreach (var release in ModUpdater.snapshots)
                        {
                            int column = i % 4;
                            int row = i / 4;
                            // X 座標と Y 座標を計算
                            float x = -1.6891f + (1.6891f * column);
                            float y = 0.8709f - (0.3927f * row);
                            var button2 = new SimpleButton(
                            betaVersionMenu.transform,
                            release.TagName,
                            new(x, y, 1f),
                            release.TagName.Contains("S") ? new(0, 255, 183, byte.MaxValue) : new(0, 202, 255, byte.MaxValue),
                            release.TagName.Contains("S") ? new(60, 255, 183, byte.MaxValue) : new(60, 255, 255, byte.MaxValue),
                            () =>
                            {
                                if (release.DownloadUrl != null)
                                    ModUpdater.StartUpdate(release.DownloadUrl);
                            },
                            "v" + release.TagName.TrimStart('v') + (release.DownloadUrl == null ? "(ERROR)" : ""));
                            i++;
                        }
                    },
                    $"バージョン切り替え");
                betaversionchange.FontSize = 2;
            }

            // フリープレイの無効化
            var howToPlayButton = __instance.howToPlayButton;
            var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");
#if RELEASE
            if (freeplayButton != null)
            {
                var textm = freeplayButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
                textm.DestroyTranslator();
                textm.text = Translator.GetString("EditCSp");

                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Main.EditMode = true));
            }
            // フリープレイが消えるのでHowToPlayをセンタリング | 消えないのでしません☆
            //howToPlayButton.transform.SetLocalX(0);
#endif
#if DEBUG
            if (freeplayButton != null)
            {
                var csbutton = GameObject.Instantiate(freeplayButton, freeplayButton.parent);
                var textm = csbutton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
                textm.DestroyTranslator();
                textm.text = Translator.GetString("EditCSp");

                csbutton.transform.localPosition = new Vector3(2.8704f, -1.9916f);
                csbutton.transform.localScale = new Vector3(0.6f, 0.6f);
                var pb = csbutton.GetComponent<PassiveButton>();
                pb.inactiveSprites.GetComponent<SpriteRenderer>().color = new(88, 101, 242, byte.MaxValue);
                pb.activeSprites.GetComponent<SpriteRenderer>().color = new(148, 161, byte.MaxValue, byte.MaxValue);
                pb.OnClick.AddListener((Action)(() => Main.EditMode = true));
                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Main.EditMode = false));//ボタンを生成
                if (SimpleButton.IsNullOrDestroyed(VersionChangeButton))
                {
                    VersionChangeButton = CreateButton(
                        "VersionChangeButton",
                        new(2.4036f, -2.6963f, 1f),
                        new(0, 202, 255, byte.MaxValue),
                        new(60, 255, 255, byte.MaxValue),
                        () =>
                        {
                            CredentialsPatch.TohkLogo.gameObject.SetActive(false);
                            __instance.screenTint.enabled = true;
                            if (VersionMenu != null)
                            {
                                VersionMenu.SetActive(true);
                                return;
                            }
                            VersionMenu = new GameObject("verPanel");
                            VersionMenu.transform.parent = __instance.gameModeButtons.transform.parent;
                            VersionMenu.transform.localPosition = new(-0.0964f, 0.1378f, 1f);
                            VersionMenu.SetActive(true);
                            ModUpdater.CheckRelease(all: true).GetAwaiter().GetResult();
                            int i = 0;
                            foreach (var release in ModUpdater.releases)
                            {
                                int column = i % 4;
                                int row = i / 4;
                                // X 座標と Y 座標を計算
                                float x = -1.6891f + (1.6891f * column);
                                float y = 0.8709f - (0.3927f * row);
                                var button2 = new SimpleButton(
                                VersionMenu.transform,
                                release.TagName,
                                new(x, y, 1f),
                                new(0, 202, 255, byte.MaxValue),
                                new(60, 255, 255, byte.MaxValue),
                                () =>
                                {
                                    if (release.DownloadUrl != null)
                                        ModUpdater.StartUpdate(release.DownloadUrl, release.OpenURL);
                                },
                                "v" + release.TagName.TrimStart('v').Trim('S').Trim('s') + (release.DownloadUrl == null ? "(ERROR)" : ""));
                                i++;
                            }
                        },
                        $"{Translator.GetString("verChangeButton")}");
                    VersionChangeButton.FontSize = 2;
                }
            }
#endif
        }

        /// <summary>TOHロゴの子としてボタンを生成</summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="normalColor">普段のボタンの色</param>
        /// <param name="hoverColor">マウスが乗っているときのボタンの色</param>
        /// <param name="action">押したときに発火するアクション</param>
        /// <param name="label">ボタンのテキスト</param>
        /// <param name="scale">ボタンのサイズ 変更しないなら不要</param>
        private static SimpleButton CreateButton(
            string name,
            Vector3 localPosition,
            Color32 normalColor,
            Color32 hoverColor,
            Action action,
            string label,
            Vector2? scale = null,
            bool isActive = true)
        {
            var button = new SimpleButton(CredentialsPatch.TohkLogo.transform, name, localPosition, normalColor, hoverColor, action, label, isActive);
            if (scale.HasValue)
            {
                button.Scale = scale.Value;
            }
            return button;
        }

        // プレイメニュー，アカウントメニュー，クレジット画面が開かれたらロゴとボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
        [HarmonyPostfix]
        public static void OpenMenuPostfix()
        {
            if (CredentialsPatch.TohkLogo != null)
            {
                CredentialsPatch.TohkLogo.gameObject.SetActive(false);
            }
            if (VersionMenu != null)
                VersionMenu.SetActive(false);
            if (betaVersionMenu != null)
                betaVersionMenu.SetActive(false);
        }
        [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
        public static void ResetScreenPostfix()
        {
            if (CredentialsPatch.TohkLogo != null)
            {
                CredentialsPatch.TohkLogo.gameObject.SetActive(true);
            }
            if (VersionMenu != null)
                VersionMenu.SetActive(false);
            if (betaVersionMenu != null)
                betaVersionMenu.SetActive(false);
        }
    }
    public class ModNews
    {
        public int Number;
        public int BeforeNumber;
        public string Title;
        public string SubTitle;
        public string ShortTitle;
        public string Text;
        public string Date;

        public Announcement ToAnnouncement()
        {
            var result = new Announcement
            {
                Number = Number,
                Title = Title,
                SubTitle = SubTitle,
                ShortTitle = ShortTitle,
                Text = Text,
                Language = (uint)DataManager.Settings.Language.CurrentLanguage,
                Date = Date,
                Id = "ModNews"
            };

            return result;
        }
    }

    //TOH_Yを参考にさせて貰いました ありがとうございます
    [HarmonyPatch]
    public class ModNewsHistory
    {
        public static List<ModNews> AllModNews = new();
        public static void Init()
        {
            {
                {
                    var news = new ModNews
                    {
                        Number = 100002,
                        //BeforeNumber = 0,
                        Title = "ハッピーハロウィンついにTOH-Kリリース！",
                        SubTitle = "やっとリリースしたよ！",
                        ShortTitle = "◆TOH-K v5.1.14",
                        Text = "ハロウィンにリリースしたのだぁー\n\rってことで(?)TOH-Kを使ってくれてありがとおおお!\n\r\n\rあ、詳しくは<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link> 見てね～\n\r\n\rマジでここなに書いたらいいんやろな なにも思いつかないぜ(これただ独り言めっちゃ書いてるやばいやつだ)\n\rまぁTOH-Kのこと話します 初リリースってことで元々24役職(ﾈﾀ役職含め)あったのを12役職まで減らしたんだぜ！ 多分いつかアプデで一部は追加すると思う\n\rそ～し～て～実は隠し要素あります！ 1つはコマンド、もう一つは隠しコマンド(key)で使えるようになるよ！<size=40%>\nやぁ。Yだ。1周年記念でニュースを閉じた画面で特定のキーを押す隠しコマンドを追加してみたのさ。\n探してみてネ。ハハハ</size>\n探してみてね\n\rあとYouTubeとかTwitter(X)でTOHkの動画とかじゃんじゃん投稿しちゃって！"
                        + " あ、でもちゃんとMODで本家じゃなくてTOHkってことわかるようにしてね それだけ守ってくれれば.. 配信とか動画で使ってくれるとめちゃ喜びます！\n\rそれじゃあこのぐらいでいいかな、じゃあkを楽しんできてね～\n\r\n\rTOH-K開発者: けーわい,タイガー,夜藍/中の人,ねむa,はろん\nサポーター:りぃりぃ",
                        Date = "2023-10-31T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100003,
                        Title = "もう12月！？ メリクリ～",
                        SubTitle = "Town Of Host-K v5.1.31",
                        ShortTitle = "◆TOH-K v5.1.31",
                        Text = "TOH v5.1.3への対応\nテレポートキラーに自爆設定が追加されたよ!\n (ターゲットが)ベントやぬーん、梯子を使っている時自爆する設定を追加したのだ！\n次はジャッカルマフィア!\n ジャッカルがｼﾞｬｯｶﾙﾏﾌｨｱを視認できるか(その逆も)\n ジャッカルがｼﾞｬｯｶﾙﾏﾌｨｱをキルできるかも設定できるようになったよぉ\nあと参加者がタイマーコマンド使うと霊界送りにされるﾔﾊﾞｲﾔﾂも直したのだ",
                        Date = "2023-12-23T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100004,
                        Title = "ハッピーニューイヤー",
                        SubTitle = "Town Of Host-K v5.1.45",
                        ShortTitle = "◆TOH-K v5.1.45",
                        Text = "TOH v5.1.4への対応\nメインメニューにX(Twitter)のボタンを追加したよ！\n是非フォローよろしくね^^(乞食)\nそして新役職！大狼の追加!!\nシェリフが大狼をキルとシェリフが誤爆する。\nまた、占い師が大狼を占ってもクルーメイトと表示される!\n新ゲームモード タスクバトル解放！\nタスクを誰よりも早く終わらせよう\nボタンが押せなく、インポスターがいないモードです\nタスクを全て終わらせると自動でゲームが終了します\n (実はv5.1.14から隠しコマンドとしてあったり..)\n\nそしてこれがほぼGithubのと同じになってる..\nってことでいつも通りここになにか書いときます(("
                        + "\n今回のアプデ、kyは..\n\n\n\n\n\nタスクバトルの解放以外なにもしていない!!(((殴\nﾀﾞｯﾃ!ﾀﾞﾚﾓkﾔｯﾃｸﾝﾅｲｼﾞｬﾝ!!(((\n\nてかﾊｯﾋﾟｰﾆｭｰｲﾔｰってもう遅かったりする..?",
                        Date = "2024-01-19T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100005,
                        Title = "鬼退治だ！！！",
                        SubTitle = "Town Of Host-K v5.1.46",
                        ShortTitle = "◆TOH-K v5.1.46",
                        Text = "新役職大量に追加したよ(ﾀｲﾘｮｳﾅﾉｶｼﾗﾝｹﾄﾞ)\n各役職紹介は<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link>をご覧ください！\n\n大狼とマッドジェスター、占い師に設定を追加！\n\n大狼はシェリフ誤爆時の死因を変えるかの設定追加\nマッドジェスターはベント使えるかの有無を追加\n占い師に能力を発揮するタスク数を追加!\n\n機能面の追加もあるよ！\nキルクール0sでやってみたいよね？そんな時は設定で出来るようになったよ！\n/sw 勝利させたい陣営 のコマンド追加！(ｱﾝﾏﾂｶﾜﾝｶﾓ)\n\n\n最後にK開発者2人増えた事をお知らせするね！\nみんなが沢山遊んでくれるMODにしていくのでみんなよろしくね^^",
                        Date = "2024-02-03T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100006,
                        Title = "ハッピーバレンタイン！",
                        SubTitle = "Town Of Host-K v5.1.47",
                        ShortTitle = "◆TOH-K v5.1.47",
                        Text = "今回はバグ修正や設定追加が基本だ！ﾀﾀﾞｼ新役職もあるぞい！\n\n新役職\n\nマジシャン！\n天秤！なんかこの役職聞いたことあるって？ｿﾝﾅﾉｷﾆｼﾀﾗﾏｹﾀﾞ!\n\nバグ修正\n\nウルトラスターが霊界でキルしてしまう問題\n非ホストModクライアントがいると発生する問題(一部)\n\n変更箇所\n\nぽんこつ占い師:\n占い失敗時今まではクルーメイト固定だったのが死亡者、自身、投票相手含むゲームにある役職が結果として表示されるように(更にぽんこつ度アップに)\n\n死神:\n死体通報出来ないように変更\n\n"
                        + "ウルトラスター:\nキル範囲をめっちゃ狭くしたぜ\n\n設定追加\n\n設定場所の移動、まとめたり、カラフルにしました。\n\nマッドメイト系役職の設定にレポート出来ない設定追加\n\nウォッチングをマッドメイトにアサインするように設定追加、それに伴いマッドメイト系役職の設定から他の人の投票先が見れる設定削除\n\nぽんこつ占い師、マッドテラーに陣営占い設定追加\n\n死神に緊急会議ボタンが使用可能かの設定追加\n\n自投票で能力発動する役職に1会議に使う能力を制限する設定追加\n\nクライアント設定に、「推奨設定を隠す」追加\n\nこの他にも変更あるから<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link>を見てね\n\nボイスコマンドにset追加。使い方は/voって打とう！",
                        Date = "2024-02-14T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100007,
                        Title = "ひな祭り",
                        SubTitle = "Town Of Host-K v5.1.48",
                        ShortTitle = "◆TOH-K v5.1.48",
                        Text = "バグ修正\n\n天秤の会議にて会議が終わらなくなる問題\n天秤会議にて、同数投票でホストが追放された時「どちらも追放された。」と表示されない問題\nGMが入ってる場合会議が終わらない問題\nマジシャンのマジックでキルした時のキルクールが元の半分になる問題\nマジシャンのハットの色がホストの色になってしまう問題\nホスト以外のmod導入済みプレイヤーがシャイボーイになったとき、mod導入者がBANされる問題\nメイヤーの覚醒OFF設定でも覚醒してしまう問題\nワークホースが配役されない問題",
                        Date = "2024-03-03T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100008,
                        Title = "ホワイトデー",
                        SubTitle = "Town Of Host-K v5.1.59",
                        ShortTitle = "◆TOH-K v5.1.59",
                        Text = "Among Us v2024.3.5sとTOH v5.1.5に対応\n\n属性二つ追加だ！\nスピーディング\nイレクター\n\n機能追加\nマッドメイトに停電を無効にするかの設定追加<size=50%>消すのめんどくさかった</size>\nフレンドコードがなくてもBANListを適用できるように。\nテンプレートに装飾できるようになったよ!\n\nバグ修正\nキルボタンを持っているmod導入済みプレイヤーに属性が\n 付くとキルボタンが消える問題\n天秤でホストが追放されている時の画面が終わる前に\n ゲームが終了するとホストの名前がバグる問題\n"
                        + "ホスト以外のmod導入済みプレイヤーがシャイボーイに\n なったとき、mod導入者がBANされる問題\n(↑今度こそ修正)\nミーティングシェリフのキルで会議が終わるはずなのに\n 終了しない問題\n\n仕様変更..?\n\n設定にある廃村ボタンをホストじゃない時は\n 表示しないように\n\nたぶん他にはなにも変わってないハズ...ﾊｽﾞ..",
                        Date = "2024-03-14T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100009,
                        Title = "エイプリルフール",
                        SubTitle = "Town Of Host-K v5.1.61.0",
                        ShortTitle = "◆TOH-K v5.1.61.0",
                        Text = "まっさかあのKがちゃんとリリースするなんて...!?(???)\n\nもう春ですかぁ\n...っということで春の大型アップデート！\n<nobr><link=\"https://youtu.be/P4IG7YluvoQ\">大雑把にまとめたYoutube</nobr></link>\n\n<size=125%>【新役職】</size>\n<b>Crewmate</b>\n巫女\nカムバッカー\n<b>Impostor</b>\nデクレッシェンド\nモグラ\nリミッター\nプログレスキラー\nエイリアン\n"
                        + "<b>Madmate</b>\nマッドリデュース\nマッドアベンジャー\n<b>Neutral</b>\nマドンナ\n<b>ユニット役職</b>\nドライバーとブレイド\n\n<size=120%>【新属性追加】</size>\nゲッサー\nムーン\nスピーディング\nライティング\nマネジメント\nシリアル\nコネクティング\nプラスポート\nオープナー\nノンレポート\nノットヴォウター\nイレクター\nウォーター\nクラムシー\nスラッカー\nトランスパレント\n"
                        + "ラストニュートラル\n\n<size=120%>【新機能】</size>\n★カスタムスポーン\n<size=50%>ホストが設定したスポーン位置にスポーンします。</size>\n★カスタムボタン\n<size=50%>今までMod導入者でもバニラボタンだったけど...\n今回から一部のボタンはカスタムボタンになるよ!\nMod設定(TOH-Kの設定)からボタンの見た目を変更するをONにすると変更できるよ!</size>"
                        + "\n☆設定画面でもルームタイマーを表示するように\n☆オブションの保存とリセットをボタンから操作できるように\n☆一部役職をワンクリックボタン対応\n☆ゲームマスターONの場合右上と開始ボタンの下に表記するように\n☆オプションの保存とリセットのボタン追加\n☆/kfコマンド追加\n☆インサイダーモード\n\n<size=120%>【新設定追加】</size>\n・マッドメイト系役職がベント移動できない\n"
                        + "・シーアに通信妨害中も効果が発揮されるか\n・テレポートキラーのテレポートキル時に死因を変える設定\n\n<size=120%>【タスクバトルに新機能追加】</size>\nチーム戦の設定を追加、チームでタスクを競い合おう！\nまた一人で開始するとタイマーが表示されるぞ!\n\n<size=120%>【バグ修正】</size>\n・ファングルのキノコカオスでマジシャンの挙動がおかしくなる問題\n・ホスト以外のMOD導入者が回数表示がおかしい問題\n"
                        + "・クルー以外のタスクでタスク勝利が出来ていた問題\n・エアシップのスポーンが正常に行われない時がある問題\n・ランダムスポーンが正常に動作しない問題\n・/nしても特殊モード設定が表示されなかった問題\n・天秤会議でウィッチの呪いなどが発動してしまう問題\n・一部ベント使用不可役職がベントに入ると動きがおかしくなる問題\n\n\n<size=120%>【DiscordにTOHkのBOTが！？】</size>"
                        + "\nTOHkの役職の説明などがコマンドで確認できたりします。\n詳しくは<nobr><link=\"https://discord.gg/5DPqH8seFq\">TOHk公式Discord鯖</nobr></link>まで～\n\n\n\n絶対他も追加したものなどあるのでGitHubをご覧ください！",
                        Date = "2024-04-01T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100010,
                        Title = "春のバグ修正祭り",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.61.1</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.61.1</color>",
                        Text = "バグ多くてすまんね！<size=25%>夜藍＜これでバグ無くなってマイクラしまくりt((((\n</size>\n<size=125%>【新コマンド追加】</size>\n・/meetinginfo(/mi)コマンド追加\n <size=70%>┗ 会議中に使えます。ミーティングインフォを自身に(ホストの場合は全員に)再表示します。</size>\n\n<size=125%>【バグ修正】</size>\n<size=80%>・イビルトラッカー等一部役職が条件下で行動不可になる問題\n"
                        + "・ルームタイマーが動かなくなる問題\n・マドンナの投票が他人でも検知してしまう問題\n・ホスト以外がカムバッカーを引くと正常に動作しない問題\n・タスクバトルでエアシップを選択した場合正常にスポーンしない問題\n・ゲッサーのID表示がホスト視点正常に動作してない問題\n・イビルトラッカーの追跡能力がホスト以外正常に動作しない問題\n・ブレイドがドライバーを視認をすることが出来ない問題(これに伴いドライバーとブレイド設定を別々に変更)\n\n"
                        + "<size=125%>【新設定】</size>\n<size=80%>・スナイパーの弾数を99まで増加。\n・スナイパーが弾を持っていない場合シェイプシフトできない設定追加\n・スナイパーの弾が残っていてもキル出来る設定追加。\n・ドライバーとブレイドにブレイドがベント使えるかの設定追加\n\n</size><size=125%>【仕様変更】</size>\n<size=80%>・ウィッチのモード変更がシェイプシフトの場合クールダウンを0sに\n・花火職人の設置をワンクリ固定に\n"
                        + "・花火職人の設置一回目をカウントに入れないように。\n・会議後死亡するプレイヤーの死体通報を無視するように\n・パン屋のメッセージをミーティングインフォで表示されるように\n・MeetingInfoにターン数と通報者/通報された死体の表示を追加。\n・KillLogを一新。\n・翻訳の幅広げました。\n・/feの表示を廃村表示に\n・霊界タスク数を試合結果に表示しないように\n・ゲッサーキルが行われた場合霊界に詳細を表示するように。\n・マッドアベンジャーの革命中に出る邪魔するなメッセージを投票者のみ表示に変更\n\n</size><size=125%>【クライアント対応について】</size>\n"
                        + "開発者がいうのもなんですが...\nTown Of Host-Kには\nホスト以外が導入すると面白くなる要素がたっぷり(?)あります。\nだからこそクライアント対応をしたいのですが...\nいかんせん想定外のバグが起こってしまっているのが現状です。(´・ω・｀)<size=20%>\n(夜藍はめんどくさいっていってるだけだけど!!)\nｹﾞﾌﾝｹﾞﾌﾝ</size>\n\n...\nこのverでは開発者同士でクライアントテストをしてない\nというのに加え、現状バグ報告に上がっておりなおかつ修正されていないバグが起こる可能性がある\nので参加者はModを無効化して参加することをお勧めいたします...\n_(:3 」∠)_ｺﾞﾒﾝﾈ...\nいつか対応する日まで。\n\n</size>永遠のぽんこつより。",
                        Date = "2024-04-15T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100011,
                        Title = "ゴールデンウィーク",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.61.2</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.61.2</color>",
                        Text =
                        "<size=80%><size=100%>なんか気づいたらそこそこ新要素多くなっちゃった</size>\n\n<size=125%>【☆新役職募集企画開始！☆】</size>\n<size=50%>ﾜｰﾊﾟﾁﾊﾟﾁ</size>\nということでこの度Town Of Host-Kの夏の大型アップデートに向けて...\n普段プレイしてくださっている方々から役職募集をやろうじゃないか!!\n<size=30%>(ほぼ虎さんのとっばつだったケド...)</size>\nｹﾞﾌﾝｹﾞﾌﾝ...\nということです...\n<size=125%>詳細はこちら⇒<nobr><link=\"https://youtu.be/iOkCr5Mcpxg\">【Youtube】</size>\n</nobr></link>\n皆様の素敵な案をお待ちしております!!\n\n"
                        + "<size=125%>【新役職】</size><b>\n~CrewMate Roles~</b>\n・狼少年\t(菅牧慧さんからの提案です!!有難うございます!!)\n・ナイスアドナー\n・ホワイトハッカー\n\n<b>~MadMate Roles~</b>\n・マッドワーカー\n\n<b>~Impostor Roles~</b>\n・イビルアドナー\n\n<b>~Neutral Roles~</b>\n・モノクラー\n・ワーカホリック\n※ジャッカルドール\n\n<b>~Addons~</b>\n・天邪鬼\t(pinaさんからの提案を改良したものです!!有難うございます!!)\n・タイブレーカー\n・アムネシア"
                        + "\n\n<size=125%>【設定追加】</size>\n・シェイプマスターにアニメーション再生設定追加\n・一部役職に属性を付与するかの設定追加\n・一部画面のフレンドコードを隠す設定追加\n・サボタージュ勝利の場合発生させた陣営が勝利する設定追加\n・他人のペットが見えない設定追加\n・メイン設定に会議時間の上限 / 加減設定移設\n・ジェスターにシェイプシフト,ベント使用設定追加"
                        + "\n\n<size=125%>【遂にジャッカルにアレが...!】</size>\nじゃっかるがよわーい!!\nリモキラとか死神とかいすぎてジャッカルなんてつかわなーい!!\nジャッカル版のマッドメイドが欲しいよ～( ﾉД`)ｼｸｼｸ…\n...ん?上のジャッカルドールってなんだよ!!\n\nええっと。遂にジャッカルにサイドキックが追加されました!!\nワンクリックボタンでサイドキックできるようになっております。(その時のキルターゲットをジャッカルドールにします。)\nジャッカルドールはサイドキックマッドメイトと一緒な感じです。\n夢に見たジャッカルのサイドキック...ご堪能あれ。"
                        + "\n\n<size=125%>【その他もろもろー】</size>\n☆属性名をTown Of Host_Y様と統一致しました\n<size=30%>〔かわったやーつ〕\nナース　　　　　　　　　→オートプシー\nバケネコ　　　　　　　　→リベンジャー\nディレクター　　　　　　→マネジメント\nサイキック　　　　　　　→シーイング\nアディショナルヴォウター→プラスポート\nノットコンヴィーナ　　　→ノンレポート\nサン　　　　　　　　　　→ライティング\n充電切れ　　　　　　　　→クラムシー\n怠け者　　　　　　　　　→スラッカー\n了承してくれてありがとうございます...</size>"
                        + "\n・読み上げ機能の改善\n・一部チャットコマンドの色の変更\n・サボ,キル位置などのログ追加\n・/h r,/hのヘルプメッセージを変更\n・バニラプレイヤーが展望最終の場合ちょっと上にtpするように。\n・/now set (/n s)コマンド追加"
                        + "\n\n<size=125%>【アンケートありがとねっ!】</size>\nちょっとこの前アンケートを実施させていただきました...\n人気投票や要望,良い部分など様々な意見が集められ開発者も嬉しかったり今後に活かしていったり...\n皆様からのご意見などあってのKですので...今後とも何卒宜しくお願い致します...!"
                        + "\n\nそれでは詳しくはGitHubをご覧ください...\n\n\n</size>キルログ好評で天狗になっている夜藍より。"
                        ,
                        Date = "2024-05-04T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100012,
                        Title = "梅雨がやってくるぞぉ!!",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.61.3</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.61.3</color>",
                        Text =
                        "<size=80%><size=100%>飴飴ふれふれ～</size>\n\n<size=125%>【☆新役職応募企画結果発表!☆】</size>\nまず一つ。発表遅くなり申し訳ございません!!!!!!!!!\n\n採用された役職は順次実装していきます。\n何が採用されたのか知りたいって?\n<size=125%><nobr><link=\"https://youtube.com/live/OKKaWXVSn0U\">ここを見よう!!</size>\n</nobr></link>\n\n"
                        + "<size=125%>【思いは引き継いだ。】</size>\n1周年と~2ヶ月前にリリース♪<size=50%>...伝わらないか。</size>(・ー＜)☆～\nかくかくしかじかありまして...Revolutionary Host Roles様にあった役職を\n(一部)TownOfHost-Kに移植しようかという話になりました。\n(´・ω・｀)\n\n今回のアップデートでは、インセンダー、スタッフを移植しております。\n\n"
                        + "<size=125%>【New Developer】</size>\nRHRは続く予定だったはずなのに急に終わりを告げられた...\n開発する場所が無くなった。\n\n...\n\n<size=30%>ﾅﾆｺﾚ(?)</size>\nということで...!\n<b>はろん</b>さんがTownOfHost-K開発者になりました～！\nどんどんぱふぱふ～\n\n"
                        + "<size=125%>【残して共有したいよね?】</size>\nKで結構好評なゲームログ...\n今まではスクショするくらいしか共有方法がありませんでしたが、このverから\n<b>TOHK_Data</b>というフォルダの中に<b>LastGameResult.txt</b>が登場しました～！\nAmongUsを再起動するまで各試合のゲームログが記録されます!\n※ゲーム終了後にその試合が記録されるから不正はできないゾ!!\n\n"
                        + "<size=125%>【バグ修正】</size>\n・一部役職がサイドキック作成後動けなくなる問題\n・ゲッサーの正誤判定が正しくない問題\n・アーソニストクールが0の場合反応しない問題\n・サイドキック系役職がシェリフにキルされない問題\n・巫女の判定が全て同陣営と表示される問題\n・プログレスキラーのマークが正しくない問題\n・サイドキック作成後憑依が出来ない問題\n・エイリアンが自身の能力を見れない問題\n\n"
                        + "<size=125%>【仕様変更】</size>\n・/h rを設定関わらず役職名,略称どちらでも対応\n・/swの役職名対応\n<size=30%>(/sw シャイボーイ)とするとシャイボーイ勝利になります</size>\n・パン屋にレアメッセージ追加\n・ヴァンブ等ラスポスが付与されない役職でもラスポス属性のみ付与するように\n・ヴァンブ,死神等の対象者がボタン,通報した場合会議がキャンセルされないように\n・イビルハッカーのアドミンを名前の下にも表示\n・バニラ視点の名前を調整\n・`R Shift`+`M`+`Enter`で会議を始めれるように\n\n"
                        + "<size=125%>【設定追加】</size>\n・タスク削除設定追加\n・共通タスクを共通にしない設定追加\n・占い等一部役職に一定のタスク/能力発動まで自覚できない設定追加\n・ヴァンパイアに移動速度低下設定追加\n・タスクバトルに全員のタスクを共通にする設定追加\n\n"
                        + "<size=125%>【新役職...?】</size>\n<b>CrewmateRole</b>\n・インセンダー\n・スタッフ\n<b>Neutral Roles</b>\n・ジャッカルドール(?)\n<b>Addons</b>\n・スロースターター\n・ガーディング"
                        + "\n\n多分次のアップデートは大型じゃなくて公式アプデ対応だと思います。知らんけど。\nK開発者さんしっかり働いてy(((殴\n\nby \"やかん\"でも\"よるあい\"でもないよ。夜藍(よらん)だよ。"
                        ,
                        Date = "2024-05-26T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100013,
                        Title = "バニラアプデが来たぞぉ!!",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v.5.1.7.12</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v.5.1.7.12</color>",
                        Text = "え!?もう一ヶ月以上も前の話だって?\nﾃﾍﾍﾟﾛ(・-＜)☆～\n\n"
                        + "<size=125%>【おしらせ】</size>\n<size=80%>ぜった～いにバグが発生すると思います。\nバグを発見した際はDiscordの方でバグ報告するようにお願いします。\nイントロで暗転してしまった場合はラグの影響もあるかもしれないので鯖変えたり立て直してみたりをお勧めします。\n\n"
                        + "<size=125%>【バグについて】</size>\n・エイリアンのモードが全員共通になるバグ...?を修正\n・/kfを使用した時に極々稀にリアクターが継続してしまう問題の抑制\n・一部役職が/h rした時に挙動がおかしくなる問題の修正\n・タスクバトルで共通にするがONでも共通にならない問題の修正\n・会議後暗転してしまう問題の抑制"
                        + "\n・imp視点マッドスニッチが見えない問題の修正\n・ゲッサーで試合終了した場合ゲームログに記載されない問題の修正\n・参加者が霊界でコマンドを使用しても一定条件下で反応が無かった問題\n・移動速度が5x以上ならなかった問題の修正\n・トイレファンが1ターン1回しかドアを開けれない問題の修正"

                        + "\n\n<size=125%>【待望のサイドキックが!?】</size>\nジャッカル君にジャッカルドール作成設定をこの前付けますた。\nこのバージョンからさらに新設定として、インポスターもサイドキックにできる設定とサイドキックが昇格する設定を付けました...\n混沌しちゃう！！\n\n"

                        + "\n\n<size=125%>【変更点諸々】</size>\n・追加勝利条件を満たしている時自身に星印が見えるように\n・スナイパーの撃った位置が見えるように\n・ホストが切断してもLastGameResult.txtに記録するように\n・シェリフのキルクール間隔を0.5s間隔に変更"
                        + "\n・シャイボーイがライティングの影響を受けるように\n・ポーラスのアドミン無効化の範囲調整\n・会議中の画面拡張(参考元:TownOfHost_Y)"

                        + "\n\n<size=125%>【☆新設定☆】</size>\n・死者視点死因の色がキラーの色に見える設定\n・ベイトに通信中無効化設定追加\n・スナイパーに新設定追加\n・メアーに設定追加\n・停電,通信妨害を一定時間修復不可にする設定追加\n・パペッティアに有効化までの時間設定追加\n・ランダムスポーン先が見える設定追加"
                        + "\n\n<size=125%>【Neeeeew Roooooooooles!!!!!!!!】</size>\n"
                        + "<b><u>Impostors</u></b>\n・リローダー(from:RHR)"
                        + "\n<b><u>Mad Mates</u></b>\n・マッドトラッカー\n・マッドチェンジャー"
                        + "\n<b><u>Crew Mates</u></b>\n・スイッチシェリフ\n・アンドロイド\n・エフィシェンシー\n・サイキック\n・ナイスロガー"
                        + "\n<b><u>Neutrals</u></b>\n・ドッペルゲンガー\n・マスメディア\n・バンカー\n・バケネコ"
                        + "\n<b><u>Addons</u></b>\n・インフォプアー(From:TownOfHost_Y)"
                        + "\n\n<size=125%>【New Ghost Roles】</size>\n"
                        + "・デーモンクラッシャー\n・デーモントラッカー\n・デーモンベンター\n・ゴーストボタナー\n・ゴーストノイズセンダー\n・ゴーストリセッター\n・アシスティングエンジェル"
                        + "\n\n<size=100%>【あとがき】</size>\nつかれた～～～～\n多分ド忘れしてる要素もある!!なんで毎回メモを取らないんだっ!!!\n計画性がないぞ!!\n\nよいこのみんなはちゃんと計画的にやろうね。\n<size=40%>(多分バグ修正で直ぐ会うだろう)</size>  みんなデバッグ版協力ありがとネ!! よらんより。"
                        ,
                        Date = "2024-08-3T03:34:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100014,
                        Title = "( ˘ω˘ )Zzz...Zzz...",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.7.13</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.7.13</color>",
                        Text = "今回はそこまで対応大変ではなかったです(感覚麻痺)\n"
                        + "対応だけかと思いきや新モードや便利な機能を追加してみたりしてるぜっ!!"
                        + "<size=80%>\n\n<size=125%>【追加設定】</size>\n"
                        + "・インポスターからエゴイストを視認できるかの設定追加\n<size=50%>ONで今まで通り、OFFでインポスター視点エゴイストとは分からず、インポスターだと思われます。</size>\n"
                        + "・/settask(/stt)からタスク数を変更できるように。\n<size=50%>(更新が反映されなかったりするので使用後なにかしら設定弄ってください)</size>\n"
                        + "\n<size=125%>【バグ修正/抑制】</size>\n"
                        + "・視界が霊界時の視界になる問題の抑制。\n"
                        + "・サイキックが自覚しない設定でも自覚してる問題の修正\n"
                        + "・ミーティングシェリフがほぼ全員撃ち殺せる問題の修正\n"
                        + "・タスクバトルでタイムアタック時の時、マップを開いてる間はカウントが進まない問題の修正\n"
                        + "\n<size=125%>【仕様変更】</size>\n"
                        + "・廃村が複数回コールされた場合、RPCを飛ばしての廃村(強制廃村)するように。\n"
                        + "・サウンド設定の一部名称をわかりやすく、ついでにちょっとテコ入れ。\n"
                        + "・ジャッカルドールとジャッカルマフィアのそれぞれの内通をちょっと変更\n"
                        + "・タスク上書き設定の通常タスクをOn/Offから個数設定に。\n<size=50%>ゲーム設定の通常タスクと同じにするとタスクが共通になるはず</size>\n"
                        + "\n<size=125%>【どんなところに迷い込んだんだっ...!?】</size>\n"
                        + "普通、ロビーでは勿論参加者視点チャットやホストの名前の下にTOH-K表示があったりなかったりするくらいでどこに迷い込んでしまったのか分からなかったりすることがたまにあります。\n(まぁKは他Modと結構区別化してますけど!!!(((((()\n"
                        + "そこで、参加者側でもロビーのみでTownOfHost-K v.(バージョン)とクライアントが普段右上で見てる情報が見れるようにしました～\nﾀｲﾍﾝﾀﾞｯﾀﾖ!!\n"
                        + "\n<size=125%>【サドンデスモード追加】</size>\n"
                        + "特殊モード設定の中に<b>サドンデスモード</b>が追加されました。\n有効にすると、勝利条件が\n<size=60%>1.自身以外全員全滅\n2.自身とラバー以外全滅\n3.時間切れ(リアクター)</size>\nになります。\n"
                        + "つまり...基本時間制限付きの殺し合いのゲームです。\n全員役職が一緒になって遊べたり...だから全員スナイパーとか...ウルトラスターとか()\n位置情報が送信されるオプションなども。詳細は色々試してみてね！"
                        + "\n\n\nモード増えてきたよね。プリセット5じゃ少なかったりします?\nまぁいいや。眠井中兎人でした。\n今日は速く寝ることにします。\n<size=40%>おうどんたべたい。</size>"
                        ,
                        Date = "2024-08-14T13:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100015,
                        Title = "夏休みももう終わり・・・",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.7.14</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.7.14</color>",
                        Text = "とりあえずバグ修正...\n"
                        + "<size=80%>\n\n<size=125%>【追加設定】</size>\n"
                        + "・各シェリフにラバーズを打ち抜ける設定追加\n"
                        + "・花火職人に爆破準備完了時のクールタイムを追加\n"
                        + "・イビルハッカーにシェイプ画面で疑似アドミンを見れる設定追加\n"
                        + "・メアーに停電強化中のキルディスタンス設定追加\n"
                        + "<size=80%>\n\n<size=125%>【バグ修正】</size>\n"
                        + "・一部処理でエラーになっていた問題の抑制\n"
                        + "・ネコカボチャがネコカボチャをキルした時クラッシュする問題の修正\n"
                        + "・サドンデスモード中にキルフラッシュが鳴らせる問題の修正\n"
                        + "・ベント使用不可能役職が強引にベント移動できる問題の修正。\n<size=50%>※まだ出来るっちゃできるんですがやるような人なんていないものとします()</size>\n"
                        + "・カスタムボタン適応中に、動作が不安定になる問題の修正。\n"
                        + "・天秤会議が始まった時、全員生存しているように見える問題の修正\n"
                        + "・天秤能力発動時、暗転対策がうまく動作していない問題の修正\n"
                        + "<size=80%>\n\n<size=125%>【もっとかわいく！】</size>\n"
                        + "やぁ。\nTOH-Kを入れているクライアントなら\nカスタムボタンを夜藍って奴が描いたものに差し替えれる機能があるんだ。\n"
                        + "前バージョンまではキルボタン/インポスターベントのみだったけど...\n今回からはワンクリックボタンなどにも\nカスタムボタンに差し替えられるようになったよ！\n"
                        + "かわいいね！(自画自賛)\n"
                        + "<size=80%>\n\n<size=125%>【あれ一個しかない...?】</size>\n"
                        + "さぁさぁ待望の新役職はっぴょーこーなー！\nといっても今回は一個しかないんだけどね。\n"
                        + "だって軽量化とバグ修正アップデートのつもりだもん！()\n\nそれはさておき。今回追加される役職は・・・\n\n<size=120%><b>Jumper/ジャンパー</size></b>\n\nだよ!\n"
                        + "/h rしたりGitHubに能力説明は乗ってるよ!()\nHostModには数少ない派手寄りの役職です...\nジャンパー君をよろしくねっ!!\n"
                        + "<size=80%>\n\n<size=125%>【設定が変わるよ!!】</size>\n"
                        + "といってもめっちゃ変わったりはしない。\n役職一覧に排出される可能性のある役職を陣営で分けて表示したり...\nあとプリセットを7つまで増やしました。\n\n今回のアップデートで設定があぼーんしてます！！！\n最初から設定してください！\nロビーで設定する奴だけね。"
                        + "\n\n\nはっぴーはろうぃん！\nおかしよりおすしが欲しい夜藍より。\nﾏｸﾞﾛﾀﾍﾞﾀｧｲ...\n<size=7%>そろそろ夜藍,りぃりぃ枠のあれを実装したいっ"
                        ,
                        Date = "2024-08-26T13:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100016,
                        Title = "はちがつさんじゅうににち！",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.7.15</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.7.15</color>",
                        Text = "<size=80%>"
                        + "(*<mark=#000000> ・</mark>_<mark=#000000>・</mark>)／●~\n"
                        + "い る み り う た と ｜ お な だ し き  ８\n"
                        + "し ス ん を は い あ っ わ つ っ い ょ  月\n"
                        + "か イ な し ス な そ と る や た い う  32\n"
                        + "っ カ で た イ ゜ ん み ま す ゜ ち も  日\n"
                        + "た は た よ カ き で ん で み 　 に た  (冥)\n"
                        + "な お べ ゜ わ ょ い な ず が 　 ち の  ☂\n"
                        + "\n...なにこれ。"
                        + "<size=80%>\n\n<size=125%>【虫捕り(BugFix)】</size>\n"
                        + "・ホスト視点バケネコであることが透けてしまう問題の修正\n"
                        + "・ジャンパー能力時、誰が使用してるか分かってしまう問題の修正\n"
                        + "・ジャンパー能力時、ベントに入れてしまう問題の修正\n"
                        + "・一部処理がnullとなっていた問題の抑制\n"
                        + "・通信の安定化をちょっと計ってみました\n"
                        + "・タスクが255以上配布された時にhacking判定食らう問題の対応\n"
                        + "<size=80%>\n\n<size=125%>【あたらしいおもちゃ!!(新役職)】</size>\n"
                        + "今回追加されるのは～\n"
                        + "<size=120%><b>King/君臨者</b></size>と<size=120%><b>EarnestWolf/一途な狼</b></size>だョ!\n"
                        + "君臨者は提案にあった奴です。一途な狼は使い方によっては強いです。\n詳しい役職説明はGitHubでご確認ください。\n"
                        + "<size=80%>\n\n<size=125%>【せんせーからのれんらく(変更点/追加要素)】</size>\n"
                        + "<size=40%>もうすこしどうにかならんかったかこれ。</size>\n"
                        + "・ベイト&インセンダーに通報までの遅延 , ランダムで追放遅延設定を追加\n"
                        + "・インセンダーにベイトの設定を入れ忘れてたのでそれも追加。\n"
                        + "(開発用のlog見やすいように変更...())\n\n"
                        + "もう8月終わっちゃう！。\n例の奴はりぃりぃが良いの思いついてくれるまで待機です。\nT年K組 よらん"
                        ,
                        Date = "2024-08-31T20:30:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100017,
                        Title = "何を言っている。9月だぞ。",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.8.16</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.8.16</color>",
                        Text = "<size=80%>"
                        + "よ～ら～ん～の～ぽんこつ～！m(__)m\n"
                        + "見直しって必要だね。\n"
                        + "\n<size=125%>【バグ修正】</size>\n"
                        + "・ホスト視点ニュートラルが正常なイントロにならない問題の修正\n"
                        + "・サボ可能役職で死亡した時、サボタージュが出来ない問題。\n"
                        + "\n<size=125%>【変更点】</size>\n"
                        + "・/n rの表示を変更\n\n"
                        + "気付かなくてごめんね!!\n"
                        + "<size=30%>最近一人なもんで・・・</size>\n"
                        + "ぽんこつより。"
                        ,
                        Date = "2024-09-02T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100018,
                        Title = "秋と言えば...月見...!!",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.8.17</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.8.17</color>",
                        Text = "<size=80%>"
                        + "月が綺麗ですね。"
                        + "\n<size=125%>【バグ修正】</size>\n"
                        + "・君臨者がサイドキックされていた問題の修正\n"
                        + "・ウィッチのワンクリック使用時、猫達が仲間にならない問題の修正\n"
                        + "・パペッティア/ウォーロックが遠隔で君臨者キル出来ていた問題を修正\n"
                        + "・バウンティハンターのキルが防がれた時変更されない問題の修正\n"
                        + "・バンカーのモードがターン明けおかしい問題の修正\n"
                        + "・試合結果の(;;)によくなっちゃう問題の修正\n"
                        + "・/rがロビーのチャットで反映されない問題の修正\n"
                        + "◇名前を表示しない設定ONでジャンパー等が機能しない問題の修正\n"

                        + "\n<size=125%>【変更点】</size>\n"
                        + "・バニラ視界設定の設定値を細かく\n"
                        + "・/kcでキルクールが設定可能に\n"
                        + "・リミッターに自爆モードになる制限時間設定を追加\n"
                        + "・ゲッサーが投票結果確定後推測できないように変更\n"
                        + "・属性ゲッサーの/mを自身の陣営設定のみに変更\n"
                        + "・ランダムスポーン通知機能をある程度の位置が分かるように変更\n"
                        + "・キルログのキル位置のテキストをある程度位置が分かるように変更\n"
                        + "・一部役職に実装していたワンクリックを廃止、\n　ワンクリック固定に変更\n"
                        + "・ロビーの現在の設定一覧のフォントを変更\n"
                        + "・/h r (i,m,c,g,a,g)で各種類の役職一覧を表示できるように変更\n"
                        + "・ベント使用不可役職がそもそもベントに入れないように変更\n"
                        + "◇クライアント視点、イントロ,アウトロを専用のものに変更\n"
                        + "▽RTAモードの時、イントロを地味に変更\n"
                        + "・ほーーーーーーんのちょっとだけ軽量化をやってみました!!\n軽くなってたらいいな程度に!!!"
                        + "　<size=50%>\n重かったら重いわぁって言ってネ!</size>"

                        + "\n\n某バーガーをめっちゃ食べ過ぎで死にかけてる夜藍より"
                        ,
                        Date = "2024-09-10T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100019,
                        Title = "はろうぃんぱーてぃーのじゅんびしないと！",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.8.18</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.8.18</color>",
                        Text = "<size=80%>"
                        + "<size=125%>【バグ修正】</size>\n"
                        + "・自動Modアップデートが動いてないかもしれない問題の修正\n・君臨者にアムネシアが付与されてしまう問題の修正\n・君臨者が追放中に抜けると処理が行われない問題の修正\n・バケネコだとキルボタンが生えないことがある問題の修正\n・ホストがナイスロガーだと設置後もタスクが出来ない問題の修正\n"
                        + "・エフィシェンシーが分岐タスクだとタスク完了しない問題の修正\n・カウントキラーの追加勝利が動いてない問題の修正\n・ホストの科学者バイタルが制限時間に影響を及ぼす問題の修正\n・マドンナ / ジャッカルドールが役職変更した時に暗転する問題の修正\n・ワンクリック使用役職がハットで透ける問題の修正"
                        + "\n・シャイボーイ / ウルトラスターが追放中にも加算されていた問題の修正\n・ペットを自視点のみ表示する設定が動いてなかった問題の修正\n・会議時の名前処理が次ターンも残ってた問題の抑制\n・モノクラーの色変更が少し不安定だった問題の修正"
                        + "\n\n<size=125%>【変更/追加設定】</size>\n"
                        + "・ロビーでの設定表示を一部変更\n・暗転対策の処理を一部変更\n・設定間隔を細かく変更\n・タスクバトルでGM + 1人の時もRTAモードにするように変更\n・タスクバトルのアウトロを変更\n・属性を付与する設定名をオプション画面でのみ変更\n"
                        + "・一部ニュートラルのインポスター視界を持つ設定を削除\n・アウトロ / 試合結果の表示を変更\n・チャット保存に投票履歴も保存するように\n・マッドガーディアン,マッドテラーに能力を発動するタスク数設定を追加\n"
                        + "・マッド系のタスク必要役職のタスク数表記を調整\n・天秤会議中対象者以外に能力を施行できない設定の追加\n"
                        + "\n<size=125%>【やっぱり見えないほうが良いよネ】</size>\n"
                        + "今回から試合中かつ生存してる場合のみ非クライアントもコマンドが隠れるようにしてみました。(参考元→SuperNewRoles様)\n"
                        + "好き勝手コマンドを使いましょう。\n\n実装の仕様上死亡判定を弄っているのでチャットでも安定しなかったり、\nオートミュート / ベタクルで動作がおかしい！という場合があるかもしれません。\n(オートミュート / ベタクルの仕様があまり分かってないので...ごめんなさい...)"
                        + "\n\n後設定画面ちょっと見やすくした。\n"
                        + "それじゃ一周年までおさらばだ。(アプデ来ない限り)\nこんだけバグを貯めてて放置するなんてそんなしたくなかったからな！\n3351 28000"
                        + "\n\n一ヶ月動きなしじゃなくて用意してますよ。by 夜藍"
                        ,
                        Date = "2024-10-10T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100020,
                        Title = "(^ ω ^ == ^ ω ^ )やぁ！またあったね！",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.9.18</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.9.18</color>",
                        Text = "<size=80%>"
                        + "<size=125%>【バグ修正】</size>\n"
                        + "・チャットコマンド秘匿がONでもラグで届かない問題"
                        + "\n↑ 現バージョンでも発生する場合はOFFにすると大丈夫だと思います。"
                        + "\n本家5.1.9に対応しました。"
                        + "\n\n話すことないです。\n"
                        + "他愛のない話はまぁ一周年ですると思うので大型アプデの内容ちょっとだけ\n"
                        + "進捗がそこまでよくないです！！！！！！！！！\n"
                        + "ジャンパーみたいなでHostModには少ない派手役職やりたいんですが思いつきません。\n思いついたら作りますし思いつけなかったら後回し。"
                        + "\n\nそれじゃ！本体アプデ来ない限り次は1周年で！\n"
                        + "じゃね!!!\n"
                        + "ガチャ運無かった夜藍より。"
                        ,
                        Date = "2024-10-12T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100021,
                        Title = "一周年だ～～～！！ﾄﾞﾝﾄﾞﾝﾊﾟﾌﾊﾟﾌ-!ﾄﾞﾝｶﾞﾗｶﾞｯｼｬｰﾝ!!",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v5.1.9.21</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v5.1.9.21</color>",
                        Text = "<size=80%>なんやかんやあって一周年。"
                        + "\nまず...全ての内容を知りたかったら<nobr><link=\"https://github.com/KYMario/TownOfHost-K/releases/tag/5.1.9.21\">GitHubのリリースノート</nobr></link>を見てね！\nめっちゃながい！\n"
                        + "\nざーっとおさらいします！\n"
                        + "\n<size=125%>【バグ修正】</size>\nいっぱい。(GitHubに記載してるよ!)\n"
                        + "\n<size=125%>【仕様変更】</size>\nたくさん！(GitHubに記載してるよ!)\n"
                        + "\n<size=125%>【新設定】</size>\nどっさり...(GitHubに記載してるよ!)\n"
                        + "\n<size=125%>【新役職】</size>\nそこそk(((殴  流石に新役職位は説明しまス...\n\n"
                        + "<b>Ⓘカモフラージャー</b>\n┗ ワンクリで一定時間全員カモフラージュさせます\n"
                        + "<b>Ⓘコネクトセーバー</b>\n┗ 半年前位の役職応募企画の夜藍枠です。\n"
                        + "<b>Ⓘ記憶喪失者</b>\n┗ 半年前位の役職応募企画のけーわい枠です。\n"
                        + "<b>Ⓒかけだし占い師</b>\n┗ 占いに1ターンかかってしまう占い師です。\n"
                        + "<b>Ⓝ怪盗</b>\n┗ 半年くらい前の役職応募企画のりぃりぃ枠です。\n"
                        + "<b>Ⓝカースメーカー</b>\n┗ 呪ってあぼーんしてゲーム終わったら勝ち！\n"
                        + "<b>Ⓛ片思い</b>\n┗ 片思いしているラバーズ系重複役職です。\n"
                        + "<b>Ⓐマジックハンド</b>\n┗ キルディスタンスが調整できる属性です。\n"
                        + "\n詳しくはGitHubのREADMEだったり/h r {役職名}で表示させてください。\n"
                        + "\n<size=125%>【1周年だョ！全員集合！】</size>\n"
                        + "...ここ書いてたやつほぼ消えました。よらんのぽんこつううううううううううううう！！！\n"
                        + "書き直し！ああもう！！\n"
                        + "...そういや全員からコメント貰ってましたっけ夜藍さん？\n"
                        + "ねむaさんは「開発期間が長すぎて実感ねぇ」って...\nはろんさんは「開発期間が短い俺がいちばん実感無い」「ということでほんとうに申し訳ない」って...\n"
                        + "けーわいさんは「僕からしたら実質二周年だしねぇ..」って言いながら虎さんとアンケート見て懐かしんでたり、何度K壊したんやってねむaさんと話してて...\n"
                        + "りぃりぃからは\n「サポーターになって9か月...時の流れは早い......\n　リリースから1年間頑張ったし、開発者様少し休暇ゲットしましょ。\n　休暇ゲット...キュウカゲット....キュウカゲツ.....ﾌﾌﾌ」\n...\n一人で長々喋りますかぁ！()\n"
                        + "<size=50%>というわけで、Kが初リリースしてから1周年となりました。\n皆様いつも使っていただいたりバグ報告だったり色々ありがとうございます...!\nKを遊ばない開発陣からするとすごく有難い限りです...\n"
                        + "\nTwitterで感想ポストが流れてきた時とか配信してる時とかそっこり見てたりします。!\n楽しい！とか笑ってくれてたりしたら凄いモチベーションにつながります!!!ｲﾂﾓｱﾘｶﾞﾄｳ!!!\n"
                        + "1年でこんな感じになってるとは思いませんでした！\nまぁ夜藍君は1月末辺りからK開発者活動始めたのでそこまで実感はないですけど。('ω')\n僕もそうですけどけーわいさん、虎さんもすごい成長したんじゃないかなぁって。"
                        + "1年でこんだけ役職数が増えたり要素が増えたり豊富になって行って凄いよなぁ...\nK追加役職数50は越えてます。凄い。属性抜きですよ!!( ﾟДﾟ)\nそういやKのコンセプトってなんでしょう?\nc#知識0の人が～とか他Modにある～とか書いてますけど...\n"
                        + "やりたいことをやるとかが結構大きいかなぁ。\n各々の開発者で作る役職の味が全然違いますし。\n夜藍君は多種多様だったりﾜｲﾜｲｶﾞﾔｶﾞﾔよりだったり、\nけーわいさんは何かすごい事だったり無かった部分だったり(殴)\n虎さんは真面目な時でも使えるような感じだったり。\n"
                        + "なんかKらしいな。\n1周年の後もよろしくお願いします...\nあっ、プルリクイツデモカンg(((ｹﾞﾌﾝｹﾞﾌﾝ..."
                        + "\n僕は開発者が協力していったらKは凄くなると思ってます。ホント。一人の限界ってありますし。\nもっと便利で使いやすくて面白いmodにけーわいさんが率先してやってくr(殴...\nKらしくのびのびゆったりたまに夜藍君が指揮とってせかしていきます。\n"
                        + "ちょっとしゃべり過ぎたかも。\nそうでもないか...?</size>\n\nいたずらっ子の\nYR",
                        Date = "2024-10-31T00:00:00Z"
                    };
                    AllModNews.Add(news);

                }
                {
                    var news = new ModNews
                    {
                        Number = 100023,
                        Title = "ほぼテストバージョン",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v519.22</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v519.22</color>",
                        Text = "<size=80%>"
                        + "バージョン表記がちょっと変わりました。\nいい感じに切り替えれる奴の実装で...\n"
                        + "<size=125%>【バグ修正】</size>\n"
                        + "・役職変更処理が行われるとホストのキルボタンが消える問題の修正"
                        + "\n・CustomRpcを使用しての試合終了処理がうまく動作しないので封印"
                        + "\n・秘匿チャットONの時ホスト死亡後、システムメッセージが届かない問題の修正"
                        + "\n・コネクトセーバーの能力発動メッセージがミーテの奴な問題の修正"
                        + "\n・一部翻訳が未翻訳になっている問題の修正"
                        + "\n・爆弾魔の爆弾がベイト、インセンダーに付与されない問題の修正"
                        + "\n・一部状況下で個人に贈る予定のチャットが全員に送られる問題の修正"
                        + "\n・エラーが起こっているかもしれない部分を抑制 / ログ仕込み"
                        + "\n・回線落ちがいる時、通報処理が正常に行われない問題の修正"
                        + "\n\n・ジャッカルマフィアがキル不可能になっている問題の修正"
                        + "\n・一部役職の役職設定表示がうまくいっていない問題の修正"
                        + "\n・タスクバトルでホストが正常にエンジニア置き換えになっていない問題の修正"
                        + "\n・バケネコのキルボタンが生えない問題の修正"
                        + "\n\n<size=125%>【変更点】</size>\n"
                        + "・<b>リセットカメラ式暗転対策</b>がOnの場合、プレイヤー数に関係なく追放を表示させるように"
                        + "\n・アップデートしたけどぽんこつしでかしてた時用にバージョンを弄ったりできる奴を追加"
                        + "\n・ハロウィン専用イントロ結構追加"
                        + "\n\n<size=125%>【新設定】</size>\n"
                        + "・追放を確認する設定"
                        + "\n┗ バニラのあれです。ModでOFFにしてるので次の会議で結果が表示されます。"
                        + "\n\n<size=125%>【ジャッカルとジャンパーでテスト!】</size>\n"
                        + "ジャッカルとジャンパーの判定を<b>亡r...ファントム</b>にしました。\nファントムの消えるボタンをクリックすることで能力が使えると思います"
                        + "何かしらバグが発生した場合はお知らせください。\n"
                        + "ファングルのキノコサボ中もワンクリ使えるぞ!\n\nそういえばアレ皆分かるのかな。Yr."
                        ,
                        Date = "2024-11-02T18:00:00Z"
                    };
                    AllModNews.Add(news);

                }
                {
                    var news = new ModNews
                    {
                        Number = 100024,
                        Title = "やっぱりテストバージョン",
                        SubTitle = "<color=#00c1ff>Town Of Host-K v519.22.01</color>",
                        ShortTitle = "<color=#00c1ff>◆TOH-K v519.22.01</color>",
                        Text = "<size=80%>"
                        + "<size=125%>【バグ修正】</size>\n"
                        + "・爆弾魔のクールダウンデフォルトが0だった問題の修正\n"
                        + "・記憶喪失者がチャットで透ける問題の対応\n"
                        + "・ベイト、インセンダーが自覚していない状態でも能力が発動される問題の修正\n"
                        + "\n<size=125%>【仕様変更】</size>\n"
                        + "・相手の名前の色が変わっている場合、会議中のチャットでも反映されるように\n"
                        + "・イントロ暗転をあんまり起こらないように\n"
                        + "・マッドメイトのイントロをクルーメイト→亡霊\n"
                        + "・ニュートラルのイントロをインポスター→クルーメイト　に。\n"
                        + "・/nで送られるメッセージをちょっと変更\n"
                        + "・怪盗が死亡した場合、下に出るメッセージを消すように\n"
                        + "\n<size=125%>【新機能】</size>\n"
                        + "・ロビーで/yamiと送信する事で全役職100%になるように\n"
                        + "・ロビーで/roleresetと送信することで全役職0%にするように\n"
                        + "・バケネコにキラーのカウントになる設定の追加\n"
                        + "・一途な狼にキルモーションを付けるかの設定追加\n"
                        + "・ラストインポス/ラストニュートラルにキルク適応設定追加"
                        + "\n\n"
                        + "あとワンクリックを一応全部ファントム置き換えでの実装にしてみました。"
                        + "\n\nウマクウゴケバイイナ。そこらのパンダ"
                        ,
                        Date = "2024-11-09T18:00:00Z"
                    };
                    AllModNews.Add(news);

                }
                AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
            }
        }

        [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
        public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
        {
            if (AllModNews.Count < 1)
            {
                Init();
                AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
            }

            List<Announcement> FinalAllNews = new();
            AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
            foreach (var news in aRange)
            {
                if (!AllModNews.Any(x => x.Number == news.Number))
                    FinalAllNews.Add(news);
            }
            FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

            aRange = new(FinalAllNews.Count);
            for (int i = 0; i < FinalAllNews.Count; i++)
                aRange[i] = FinalAllNews[i];

            return true;
        }
    }
}
