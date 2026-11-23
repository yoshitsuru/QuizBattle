# QuizBattle — STEP6：Photon Fusion 2 ルーム接続（Unity AI実装指示書）

## 目的
既存のQuizBattleプロジェクトに、Photon Fusion 2.1.4を使った**2人用Host/Client接続**を実装する。STEP6の到達点は「Hostがセッションを作成し、Clientが同じセッションに参加し、双方で接続状態と参加人数を確認できる」こと。**問題・回答・スコアの同期は行わない。**

## 確認済みの環境（前回のUnity AI報告）
- Unity 6000.5.7f1
- Photon Fusion 2.1.4、主要アセンブリ読み込み済み
- Fusion用App ID設定済み（値をログや報告に表示しない）
- Network Project Config導入済み、コンパイルエラーなし
- 既存の1人用クイズは動作確認済み
- MatchingSceneは未作成

実装開始時に実プロジェクトを再確認すること。報告と異なる点があれば説明し、既存ファイルを不用意に上書きしない。

## 既存成果物の保護
以下を削除・全面変更しない：TitleScene、QuizScene、QuizGameController、QuizSceneUI、QuizQuestion、QuizQuestionSet、5問の問題アセット、BasicQuizQuestionSet、フォント・TMP設定。オフラインクイズの動作を維持する。

## 実装内容

### 1. MatchingSceneの作成
既存の `Assets/Scenes/` を確認し、`MatchingScene.unity` を新規作成する。既に存在する場合は再作成せず利用する。TitleSceneの「オンライン対戦」ボタンから遷移できるようにし、Build Profilesのシーンリストに追加する。既存のシーン登録は維持する。

### 2. UI
青×オレンジ基調で既存タイトル画面に合わせ、以下を表示する。
- 画面タイトル「オンライン対戦」
- セッション名入力欄（空なら生成するか、明確なエラーを表示）
- 「部屋を作る」ボタン
- 「部屋に参加」ボタン
- 現在のセッション名
- 接続状態（未接続／接続中／接続済み／失敗／切断済み）
- 参加人数（例：1 / 2）
- エラーメッセージ欄
- 「退出・タイトルへ戻る」ボタン

**注意：セッション名はPhotonのルーム識別子として扱う。Fusionの標準SessionNameを使う方式と、独自の短い招待コード生成は別物。今回はSessionNameの共有で参加できる仕様にする。** 自動マッチングや招待コード変換機構は作らない。

### 3. Photon Fusion接続
Fusion 2.1.4に対応する公式APIを使い、NetworkRunnerの生成・起動・終了を実装する。
- Host：「部屋を作る」で `GameMode.Host` と指定セッション名を使ってStartGameする
- Client：「部屋に参加」で `GameMode.Client` と同じセッション名を使ってStartGameする
- 2人までを想定し、セッションの人数制限を設定する（対応するStartGameArgs等を公式APIで確認）
- 必要なSceneManager（例：NetworkSceneManagerDefault）の設定をFusion 2.1.4の仕様に合わせる
- NetworkRunnerコールバック（INetworkRunnerCallbacks等）は導入版の正確なシグネチャで実装し、参加／退出／接続失敗／Shutdown等をUIに反映する
- Host側の参加人数は接続済みプレイヤー一覧など**実際の接続状態**から取得する。Client側で同じ人数が取得できない場合はHostから必要な状態を同期するなど、双方で正しく表示する仕組みを採用する
- 接続処理中は多重クリックを防止し、同時に複数のRunnerを起動しない
- StartGameの失敗理由をUIとConsoleに適切に表示する。App IDや秘密情報はログ出力しない
- 退出時はRunnerをShutdownし、コールバックや参照を整理してからTitleSceneへ戻る
- シーン再入場で古いRunnerが残らないようにする

### 4. クラス構成
既存の `Assets/Scripts/Network/` など実プロジェクトのフォルダ規則に従う。例：
- `PhotonConnectionManager.cs`：Runnerのライフサイクル、Host/Client起動、切断、接続状態
- `MatchingSceneUI.cs`：入力・ボタン・状態表示

必要に応じて責務を分けるが、無関係な機能を追加しない。Unity EditorのInspector参照とButtonイベントを設定する。直接設定できない場合は手動手順を具体的に報告する。

## エラー・境界条件
- App ID未設定／ネットワーク未接続／認証失敗／セッション名不正
- 同名セッション作成失敗、存在しないセッションへの参加
- 3人目の参加拒否
- 連打や二重StartGame
- Hostの切断・Clientの切断
- シーン移動中の切断やShutdown

接続失敗後も操作可能な状態に戻し、再試行できるようにする。**Hostが切断した場合のホスト移譲はSTEP6では実装しない**。Clientへ適切な切断表示を行う。

## 動作確認
実際に確認したものと未確認のものを分けて報告する。
1. MatchingSceneが開き、UIが表示される
2. TitleSceneからオンライン対戦ボタンで遷移できる
3. Hostでセッションを作成できる
4. 別プロセスのClientから同名セッションに参加できる
5. 両者の接続状態と参加人数が正しく表示される
6. 3人目が参加できない
7. Client退出でHostの人数が更新される
8. Host退出でClientに切断状態が表示される
9. 再接続でき、Runnerが重複しない
10. TitleSceneに戻れる
11. 既存の1人用クイズをプレイできる
12. Consoleにコンパイルエラーがない

**重要：2クライアント接続の検証には別々の実行環境（Editor＋Standalone Build、または2つのBuildなど）が必要。** Unity AIが単一Editorしか操作できない場合、オンライン接続済みと報告せず、ユーザーが再現できる手順を提示する。

## 今回実装しない
- クイズ問題・問題順・タイマーの同期
- 回答・スコア・勝敗の同期
- ランダムマッチ、公開ルーム一覧、独自招待コード
- ホスト移譲、途中参加での試合復元
- ランキング、アカウント、課金

## 完了報告に含めるもの
1. 作成・変更ファイルとシーン
2. 使用したFusion 2.1.4 APIとGameMode
3. UIとInspector参照の設定状況
4. Host/Clientの実接続テスト結果（実行環境を明記）
5. 接続失敗・退出・人数制限の確認結果
6. 既存オフラインクイズの維持確認
7. 未実施のテスト、手動操作、残課題

## Unity AIへの依頼
このSTEP6のみを既存Unityプロジェクトへ実装してください。最初にFusion 2.1.4と既存構成を確認し、古いFusion 1 APIを使わず、既存のSTEP1〜5成果物を保持してください。実際に検証していないオンライン接続を成功と報告しないでください。
