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
        public static GameObject VersionMenu;
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
                    new(60, 255, 255, byte.MaxValue),
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
                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Main.EditMode = false));
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
                        Text = "ハロウィンにリリースしたのだぁー\n\rってことで(?)TOH-Kを使ってくれてありがとおおお!\n\r\n\rあ、詳しくは<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link> 見てね～\n\r\n\rマジでここなに書いたらいいんやろな なにも思いつかないぜ(これただ独り言めっちゃ書いてるやばいやつだ)\n\rまぁTOH-Kのこと話します 初リリースってことで元々24役職(ﾈﾀ役職含め)あったのを12役職まで減らしたんだぜ！ 多分いつかアプデで一部は追加すると思う\n\rそ～し～て～実は隠し要素あります！ 1つはコマンド、もう一つは隠しコマンド(key)で使えるようになるよ！探してみてね\n\rあとYouTubeとかTwitter(X)でTOHkの動画とかじゃんじゃん投稿しちゃって！"
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
