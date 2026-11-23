# QuizBattle STEP5：調査結果・手動導入案内・STEP6設計メモ

## 対応範囲と現在の状態
- ユーザー選択：Photon Fusion SDKは手動導入の案内を行う。今回はSDKをインストールしていない。
- 提供済みファイル：`Assets/AI/Photon-Fusion-2.1.4-Stable-2500.unitypackage`。ファイル名上の版は2.1.4 Stable build 2500。SDK内部の版とUnity 6000.5.7f1対応は未確認。
- 再確認：Photon/Fusionアセンブリ0件、PhotonAppSettings資産0件。ファイルをAssetsに配置しただけではSDKのインポートは完了しない。
- Photon Dashboard：ユーザーからFusion用アプリ準備済みとの連絡あり。App IDはこのメモ・ログへ転記しない。Unity側の設定は未実施。
- Unity Editor：6000.5.7f1。
- 対象：Windows PC、StandaloneWindows64。レンダリングは既存UniversalRPのまま。
- Photon Fusion／PUN／Quantum／Chat：Assetsの名前検索とロード済みアセンブリ、登録済みパッケージの調査では導入を検出しなかった。SDKバージョンは未導入につき未確定。
- Unity Netcode for GameObjects／Lobby／Relay／Multiplayer Services／Transport：登録済みパッケージと通信アセンブリ調査では該当実装を検出しなかった。
- com.unity.multiplayer.center 1.0.1は導入済み。これは通信環境の選択支援ツールであり、既存のNGO／Lobby／Relay実装があるという意味ではない。削除・変更しない。
- `Assets/Scripts/Network/`は存在する。再作成しない。
- `MatchingScene`はAssets内のシーン検索で存在しない。今回新規作成しない。
- TitleSceneのOnlineButtonは永続OnClickでSceneTransitionManager.StartOnlineに接続され、ソース内の遷移先はMatchingScene。現時点では存在しないシーンをロードせず原因をログで示す。
- TitleSceneは起動シーンとして有効、QuizSceneも有効、古いSampleScene登録は無効。
- 調査時点でコンパイル処理中ではなく、EditorUtility.scriptCompilationFailed=False。SDK導入後のコンパイルはまだ未実施。
- ConsoleにはUnity AIアカウント／会話更新のエラーがある。これをPhoton・クイズのコンパイルエラーと混同しない。
- QuizSceneは未保存変更なし、Play Mode終了状態。
- App ID設定：Dashboardの準備はユーザーから報告済み。Fusion未導入のためUnityへの設定は未実施。提供されたIDは再掲・メモ保存しない。

## 公式確認先と未確認事項
- SDK配布：https://doc.photonengine.com/fusion/current/getting-started/sdk-download
- Fusion 2の概要：https://doc.photonengine.com/fusion/current/getting-started/fusion-intro
- Host Modeの導入チュートリアル：https://doc.photonengine.com/fusion/current/tutorials/host-mode-basics/1-getting-started
- Photon Dashboard：https://dashboard.photonengine.com/

公式ページへアクセスしたが、今回のWeb取得結果からは対応Unityバージョン・SDKの現行安定版の具体的な番号・導入画面の詳細を確認できなかった。したがって「最新安定版の番号」や「6000.5.7f1への対応確認済み」とは記載しない。手動ダウンロード時に配布ページのリリースノートと同版の導入要件を確認する。Fusion 2を第一候補とし、Fusion 1や別通信基盤に無断で変更しない。

## ユーザーが行う手動作業
1. プロジェクトを保存し、Gitコミットまたはバックアップを作成する。SDKのインポート前の状態へ戻せるようにする。
2. 上記の公式SDK配布ページからFusion 2の安定版を選択する。SDKの正確な版、対応Unityバージョン、Windows対応、必要な.NET／IL2CPP／依存条件、リリースノートを確認する。6000.5.7f1への対応が確認できなければインポートを進めず、その条件を共有して再判断する。互換性のためにUNITY_*定義や実験的Defineを追加したり、パッケージのガードを変更したりしない。
3. Photonアカウントのログイン・利用規約への同意が必要な場合は自分で行う。料金、無料枠、同時接続数はその時点の公式プラン情報を確認する。ここでは数値や料金を推測しない。
4. 公式配布のSDKファイルを取得する。`.unitypackage`形式の場合はUnityのAssets > Import Package > Custom Packageからインポートする。配布形式が変わっていれば同版の公式手順を使う。PUN 2、Quantum、Chat、別ゲームのサンプル一式は今回の依頼に含まない。
5. インポート対象を確認し、既存QuizBattleのシーンやスクリプトを上書きする同名資産があれば、そのままImportを確定しない。既存Unity通信パッケージを削除する案内が出ても、変更前に確認する。
6. インポートとコンパイルが完了するまで待つ。Fusion用のセットアップUI（導入版のPhoton Hub等）が表示されたら、その版の手順で設定資産の生成・既存資産の状態を確認する。このSTEPではNetworkRunnerの常駐やダミー接続を作成しない。
7. Photon Dashboardに自分でログインし、Fusion用のアプリケーションを用意する。既存のFusionアプリを利用する場合はプロジェクト用途に合っていることを確認する。PUN／Realtime／Quantum／Chat用のIDをFusion用として流用しない。選択肢にSDK世代の指定がある場合は導入版と揃える。
8. DashboardのFusion App IDを、インストールした版のセットアップUIまたはPhotonAppSettingsのFusion用欄（App ID Fusion／AppIdFusionなど、実際の版で確認）へ自分で入力する。設定資産の正確なパスとUI項目はSDK導入後に確認するため、未確認の固定パスは指定しない。
9. App IDはチャットやConsoleに貼らず、スクリーンショットを共有する場合は伏せる。APIキー・秘密鍵は共有しない。公開リポジトリへの設定ファイルの扱いも確認する。App IDを秘密鍵の代わりと考えたり、IDの秘匿だけを不正対策としたりしない。
10. 導入版と「App IDをEditorで設定したか」のみを連絡する。ID本体は連絡不要。コンパイルエラーがあれば、認証情報を除いたエラー文とファイル・行番号を共有する。

## 手動導入後にこちらで確認すること（現在は未実施）
- インストールされたSDKの正確な版とFusionアセンブリのロード状態。
- 同版の公式対応条件と現在のUnity／プラットフォームの一致。
- 必要な設定資産とセットアップUIの状態。App IDは値をログに出さず設定済み／未設定だけを確認する。
- コンパイル処理完了とコンパイルエラーの有無。SDKコードや互換性ガードを改変して回避しない。
- STEP1〜4のシーン・データ・ソース・フォント・TMP設定への意図しない影響がないこと。
- ひとり用クイズの起動と進行の回帰確認。今回の調査だけで「SDK導入後も動作済み」とはしない。

## STEP6：Host／Client接続フローの設計メモ
これは将来の接続方針であり、STEP5では以下の通信コード・UI・Runnerは作成しない。

### 画面・ライフサイクル
- TitleSceneのオンライン対戦から、STEP6で作成するMatchingSceneへ移動する。
- 「部屋を作る」／「コードで参加」／「タイトルへ戻る」、接続状態、参加人数、エラー表示を用意する。
- 接続状態はIdle → Starting → Connected → ShuttingDown → Idleを基本とし、Starting／ShuttingDown中の連打を拒否する。
- Fusion 2の導入版が対応するHost／ClientのGameModeとStartGame手順を使用する。部屋作成側はHost、コード参加側はClientを明示する。
- Runnerは接続試行単位で新規作成し、失敗・取消・退出時に適切にShutdownして破棄する。Shutdown済みRunnerを再接続に使い回さない。無目的に常駐させない。
- 人数上限は2。双方が同一のアプリ設定・リージョン方針・セッション名の規則を使用する。入力された部屋コードの検証、作成／参加の競合と既存部屋の扱いを定義する。
- async処理の例外、StartGameの失敗結果、切断コールバックを導入版のAPIに合わせて扱う。画面を離れた後に完了した接続を放置しない。
- 切断・接続失敗時は原因を示し、再操作可能な状態に戻す。SDKの秘密情報やApp IDをエラー表示へ混ぜない。

### 次段階の試合権限（STEP6の問題同期にもまだ実装しない）
- Host：試合進行、問題順、開始時刻、回答締切、正誤、得点、勝敗を決める。
- Client：選択肢indexと対象問題識別情報を送信し、同期された結果を表示する。自己申告の得点を採用しない。
- 両者に同じQuizQuestionSetを保持し、将来は問題IDと問題順を同期する。問題データの版・ハッシュ一致を確認する設計が必要。
- タイマーはHost側のネットワークtickに基づく。オフラインQuizGameControllerの実時間タイマーをそのまま通信の締切に流用しない。
- オンライン管理クラスは既存QuizGameControllerとは分離する。オフライン動作と問題データのGetterを維持する。
- Hostを信頼する方式にはHost側不正の限界がある。競技用途が強くなる場合は専用サーバーを再検討する。

### STEP6の接続検証
- Unity EditorとWindowsビルド等の独立した2環境で、同じセッションにHost／Clientとして接続する。
- 2人参加表示、存在しないコード、満員、二重クリック、接続取消、Host退出、Client退出、通信断、再試行を確認する。
- STEP6では問題・回答・スコア同期はまだ行わない。

## 今回の変更と残課題
- 新規ファイル：この記録メモのみ。
- 既存ソース、シーン、問題5問、問題セット、フォント、TMP設定、Package manifest、Project Settingsは変更していない。
- Fusion SDK導入、正確な対応版確認、App ID設定、SDK導入後のコンパイル・回帰確認は手動作業後に残る。
- MatchingSceneとオンライン接続はSTEP6の対象。未作成のままSTEP5で正常接続したと報告しない。
