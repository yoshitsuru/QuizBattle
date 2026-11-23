# QuizBattle --- STEP5：Photon Fusion導入・初期設定（Unity AI向け）

## 目的

既存のUnity製4択クイズゲームに **Photon Fusion**
を採用し、次工程で2人オンライン接続を実装できる開発環境を準備する。

**STEP5では通信ライブラリの導入・設定確認まで。**
ルーム作成・参加、問題同期、回答同期、スコア同期は実装しない。

## 最重要：既存成果物の保護

STEP1〜4は実装済み。特に次のファイル・アセットを保護する。 -
`Assets/Scripts/Quiz/QuizQuestion.cs` -
`Assets/Scripts/Quiz/QuizQuestionSet.cs` -
`Assets/Scripts/Quiz/QuizGameController.cs` -
`Assets/Scripts/UI/QuizSceneUI.cs` - `Assets/Scenes/QuizScene.unity` -
`Assets/Data/QuizQuestions/Q001.asset` ～ `Q005.asset` -
`Assets/Data/QuizSets/BasicQuizQuestionSet.asset` -
既存のTitleScene、MatchingScene、SceneTransitionManager、フォント、TMP設定

上記のパスは前回の実装報告に基づく。作業開始時に実際の配置を確認すること。既存ファイルの削除、全面書き換え、シーンの再作成は禁止。1人用クイズは引き続き動作させる。

## 1. 現状調査

実装前に以下を調査・報告する。 1. Unity Editorのバージョン 2.
プロジェクトの対象プラットフォーム 3.
インストール済みのPhoton関連パッケージとバージョン 4. Unity Netcode for
GameObjects、Lobby、Relayなど競合し得る通信実装の有無 5.
TitleSceneの「オンライン対戦」ボタンの遷移先 6.
MatchingSceneの有無と現在の構造 7. コンパイルエラーの有無

**既存のUnity公式通信パッケージがあっても勝手に削除しない。**
重複・競合がある場合は報告し、変更前にユーザーの確認を求める。

## 2. Photon Fusionを採用する

-   通信基盤は **Photon Fusion** に統一する。
-   PUN 2、Photon Quantum、Photon Chat、Unity
    Netcodeは今回の通信実装に使用しない。
-   利用中のUnityバージョンに対応したPhoton
    Fusionの安定版を公式配布元・公式ドキュメントで確認する。
-   FusionのバージョンによってAPIやセットアップが異なるため、インストールした版に対応する公式手順を使う。
-   **Fusion
    2を利用可能なら第一候補**とするが、Unityバージョンとの互換性を確認して決定する。
-   非公式サンプルや古いFusion 1のコードをそのまま混ぜない。

## 3. インストール

1.  公式のPhoton Fusion SDKを入手する手順を確認する。
2.  既に導入済みなら再インストールせず、バージョン・正常性を検証する。
3.  未導入なら、ユーザーの承認を得てから公式SDKを導入する。
4.  導入後、必要なセットアップウィザードや設定アセットの生成状況を確認する。
5.  コンパイルエラーがないか確認する。

Unity AIがAsset Store、Photon
Dashboard、ブラウザの操作を行えない場合は、**操作済みと偽らず、ユーザー向けに正確な手順を示す**。

## 4. Photon App ID設定

-   Photon
    DashboardでFusion用アプリケーションを作成する必要があるか、導入版の公式手順に沿って確認する。
-   App IDの設定先（Photon App Settings等）は導入版に従って確認する。
-   App IDはユーザーがPhoton
    Dashboardから取得・入力する。AIはIDを捏造しない。
-   App
    IDや認証情報をチャット、ログ、スクリーンショット、公開リポジトリに不用意に露出させない。
-   APIキー、秘密鍵、課金設定を勝手に作成・変更しない。
-   Photonの無料枠・同時接続上限・利用規約・料金はプランや時点で変わるため、最新の公式情報をユーザーに確認してもらう。具体的な枠や金額を推測しない。

## 5. 今後の通信アーキテクチャ

まずは **2人対戦、Host/Client型**
を設計候補とする（導入するFusionバージョンの対応するGameModeを確認する）。

### 役割

-   Host：試合進行、問題順、開始時刻、正誤判定、得点、勝敗を管理
-   Client：回答入力を送信し、同期された試合状態を表示
-   Photon
    Fusion：接続、プレイヤー参加・退出、必要なネットワーク状態同期

### 設計原則

-   オフライン用QuizGameControllerを壊さず、オンライン用の管理クラスを別に用意する。
-   問題を同期する際は、問題文の毎回送信ではなく、同じ問題セットを双方に保持したうえで
    **問題ID・問題順** を同期する方式を候補とする。
-   その場合、双方の問題データのバージョン・整合性を確認できる仕組みを将来追加する。
-   回答選択のみをClientから送信し、正誤・得点をClientの自己申告に任せない。
-   時間制限はHost基準の時刻・tickに基づいて公平に管理する。
-   切断、二重回答、遅延、タイムアウトを将来のテスト対象とする。
-   Host方式ではHost側に権限があるため、不正対策の限界がある。競技性が高くなる場合は専用サーバー方式を再検討する。

## 6. 今回作るもの

-   Photon Fusionの導入・初期設定（可能な範囲）
-   `Assets/Scripts/Network/` の存在確認。なければ追加
-   `MatchingScene` の現状調査
-   次のSTEP6で必要なHost/Client接続フローの設計メモ
-   Unity AIが直接設定できない箇所の手動作業手順

**STEP5では動作しないダミーの通信機能や、不要なNetworkRunner常駐オブジェクトを無理に作成しない。**
接続実装はSTEP6で行う。

## 7. STEP6で実装予定の機能（今回は対象外）

-   MatchingSceneに「部屋を作る」「コードで参加」「タイトルへ戻る」を配置
-   Fusion NetworkRunnerを適切なライフサイクルで作成・破棄
-   Host/Clientで同一セッションへ接続
-   接続状態・参加人数・エラーをUIに表示
-   2つのUnity実行環境で接続テスト
-   切断時の画面・状態の復旧
-   この段階ではまだ問題・回答・得点は同期しない

## 8. 完了チェック

-   [ ] Unityバージョン・通信関連パッケージを確認した
-   [ ] Fusion SDKの対応バージョンと導入方法を確認した
-   [ ] Fusionが導入済み、またはユーザーの手動導入手順が明示されている
-   [ ] Photon App IDの取得・設定方法を確認した
-   [ ] App ID設定の実施済み／未実施を明確にした
-   [ ] MatchingSceneとオンライン対戦ボタンの現状を確認した
-   [ ] Host/Client構成とSTEP6の接続方針を記録した
-   [ ] STEP1〜4の成果物が維持されている
-   [ ] コンパイル確認結果を報告した（未実施なら未実施と明記）

## Unity AIへの最終指示

現在のUnityプロジェクトを確認したうえで、**STEP5：Photon
Fusionの導入・初期設定だけ**を進めてください。Unity公式Netcode + Lobby +
Relayへの移行は行わず、Photon Fusionを採用してください。

外部アカウント、App
ID、SDKの入手、利用規約、課金などユーザー操作が必要な部分は明示し、未実施の作業を完了扱いにしないでください。

最後に以下を報告してください。 1. Unityバージョン 2. Photon Fusion
SDKのバージョンと導入状況 3. Photon App
IDの設定状況（IDの全文は表示しない） 4. 作成・変更したファイル 5.
既存シーン・スクリプトへの影響 6. コンパイル確認結果 7.
ユーザーが行う手動操作 8. STEP6に進むための残課題
