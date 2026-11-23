# Project Overview
- Game Title: QUIZ BATTLE
- High-Level Concept: ひとりプレイとオンライン2人対戦に対応した4択クイズゲーム。現在の作業対象は末尾の「STEP 9〜12：回答・結果・再戦・切断処理の一括実装と総合検証」で、既存SoloとSTEP8の問題・タイマー同期を保持する。

> 本ファイルは既存計画の継続更新です。下記STEP2〜4と当時の再開手順・未実装記述は履歴です。今回の実行対象・現状・検証方針は末尾のSTEP9〜12を参照してください。過去の範囲外指定は当時のSTEPにのみ適用します。
- Players: Single player、およびPhoton Fusion Host/Clientによるオンライン2人対戦。
- Inspiration / Reference Games: 指定なし。
- Tone / Art Direction: 青とオレンジを基調にしたシンプルで読みやすいゲームUI。
- Target Platform: Windows PC（StandaloneWindows64）。
- Screen Orientation / Resolution: 横画面、16:9、1920×1080基準。
- Render Pipeline: UniversalRP、Unity 6000.5.7f1、URP 17.6.0。

## 依頼・選択事項
- 原本: `Assets/AI/QuizBattle_STEP2_TitleScreen_UnityAI.md`。原本は変更しない。
- ユーザー選択: タイトル画面のみ実装。不存在のQuizScene／MatchingScene／ResultSceneは作成せず、遷移できない理由をログで示す。
- ユーザー選択: 外部フォントを追加せずWindows標準日本語フォントを参照する。
- 保存先: `_Project`を除いた既存構成として`Assets/Scripts/Core`および`Assets/Scripts/UI`を使用する。二重のScripts構成は作らない。
- 既存計画は調査時点でAssets/Plansに存在しなかったため、本ファイルを初回作成する。

# Game Mechanics
## Core Gameplay Loop
今回のループはタイトル表示→モード選択→利用可能なシーンへ遷移。現状は遷移先が存在しないため、ひとりで遊ぶ／オンライン対戦は明確なエラーログを出してタイトルに留まる。設定は「設定画面は今後実装します。」を出力する。未実装部分を動作済みと扱わない。

## Controls and Input Methods
- Input System 1.20.0が導入済みで、実際のProject SettingsのactiveInputHandler=1（新Input Systemのみ）を確認済み。
- uGUI 2.5.0のButtonとInputSystemUIInputModuleを利用する。旧StandaloneInputModuleやUnityEngine.Inputによるポーリングは使用しない。
- 既存`Assets/Settings/InputSystem_Actions.inputactions`のUIマップ（Navigate、Submit、Cancel、Point、Click、ScrollWheel等）を接続し、入力アセット自体は変更しない。
- マウスクリックで操作。既存Navigate／Submitバインドに従いキーボード・ゲームパッドの選択と決定も確認する。起動時の選択をSoloButtonにし、縦方向のナビゲーションを接続する。
- 大きなボタンと十分な余白、ホバー・押下・選択時の色変化で操作可能性を示す。

# UI
## ワイヤーフレーム（1920×1080基準）
```text
┌──────────────────────────────────────────┐
│                 青い背景                 │
│                                          │
│               QUIZ BATTLE                │
│              ━ オレンジ ━                │
│                                          │
│           [ ひとりで遊ぶ ]                │
│           [ オンライン対戦 ]             │
│           [      設定      ]             │
│                                          │
│               Version 0.1.0              │
└──────────────────────────────────────────┘
```
- Screen Space - Overlay、Canvas Scaler: Scale With Screen Size、1920×1080、Match Width Or Height、Match=0.5。
- Backgroundは画面全体にストレッチしRaycast Targetを無効化。装飾とラベルも不要なRaycastを無効化する。
- タイトルは中央上部、約100pxの太字英字。必要なら一部文字色をオレンジにする。標準UIのImageによる控えめなアクセントを使用し、外部画像は追加しない。
- 中央のボタンは約600×110、縦間隔約30。ひとりで遊ぶをオレンジ、他2つを青系の明るい色とし、白系ラベルとのコントラストを確保する。ラベルは約40px、折り返し／切れがないよう調整する。
- VersionTextは下中央アンカー、約24px、下余白40px程度。
- 既存Canvas／EventSystemはないことを確認済み。実装直前に再確認し、存在すれば再利用し重複させない。
- uGUIのTextを採用。日本語はOSフォントのMeiryo／Yu Gothic／MS Gothicを候補とし、外部フォントやTMP資産を追加しない。通常のEditor表示は組み込みフォントを代替として使えるが、日本語表示の合否はPlay Mode／Windowsビルドで確認する。

# Key Asset & Context
## 確認済みの現状
- `Assets/Scenes/TitleScene.unity`: 保存済み・未変更状態。Main Camera（Camera、AudioListener、UniversalAdditionalCameraData）とGlobal Light 2Dのみ。両方を維持する。
- `Assets/Scenes/QuizScene.unity`／`MatchingScene.unity`／`ResultScene.unity`: 存在しない。全AssetsのScene検索でも該当なし。
- `Assets/Scripts/Core`／`Assets/Scripts/UI`などのSTEP 1フォルダは存在するが、Assets内にC#ソースはない。SceneTransitionManagerの重複なし。
- `Assets/Fonts`: 存在するがフォント資産なし。開発PCにはMeiryo、Yu Gothic、MS Gothic等がインストール済み。
- `ProjectSettings/EditorBuildSettings.asset`: 有効シーンは`Assets/Scenes/SampleScene.unity`のみだが、そのファイルはAssetsに存在しない。実装ではこの古い登録を起動対象から外し、TitleSceneを最初の有効シーンにする。既存ファイルは削除しない。
- TitleSceneの依存はURPのCamera／Light2Dスクリプトと自身。Assets側の依存元は検出されなかった。UI入力アセットの依存も確認済み。
- 現在コンパイルエラー0件。BurstのWindowsコード整合性ポリシー警告とAIアカウントAPI警告がある。これらを本STEPで設定変更して回避しない。

## 作成・変更対象
1. 変更: `Assets/Scenes/TitleScene.unity`
   - Canvas、Background、TitleText、SoloButton、OnlineButton、SettingsButton、VersionText。
   - EventSystem + InputSystemUIInputModule。
   - CanvasにSceneTransitionManagerとTitleScreenFontInitializerを追加し、Inspector参照を保存する。
   - Button.OnClickを永続イベントとして明示的に接続する。
2. 新規: `Assets/Scripts/Core/SceneTransitionManager.cs`
   - MonoBehaviourクラス`SceneTransitionManager`。
   - `using UnityEngine;`、`using UnityEngine.SceneManagement;`。
   - public void StartSolo()／StartOnline()／OpenSettings()。
   - QuizScene／MatchingSceneを定数化。共通のロード処理で`Application.CanStreamedLevelBeLoaded`を確認してから`SceneManager.LoadScene`を呼ぶ。
   - 不可の場合は対象名、シーンファイル不存在または有効なBuild Profiles登録不足の可能性、対処方法を含むエラーログを出し、ロードは呼ばない。Editor専用APIはランタイムコードに混ぜない。
   - グローバルSingleton、DontDestroyOnLoad、通信処理は追加しない。
3. 新規: `Assets/Scripts/UI/TitleScreenFontInitializer.cs`
   - uGUI Text参照と候補フォント名を保持する軽量な補助MonoBehaviour。
   - Awakeでインストール済みOSフォントを確認し、利用可能なWindows標準日本語フォントをFont.CreateDynamicFontFromOSFontで作成してTextに割り当てる。
   - 必要な日本語文字が利用できるか確認する。候補が使えなければ理由の分かる警告を出し、参照が欠けてもNullReferenceExceptionを起こさない。
   - 自身が生成したフォントのみOnDestroyで解放する。毎フレームの生成・検索は行わない。
   - 通常Edit ModeではOSフォントを一時生成したままシーンへ保存しない。日本語の正式な目視確認はPlay Modeで行う。
4. 必要最小限の変更: `ProjectSettings/EditorBuildSettings.asset`または実際に有効なBuild Profile。
   - アクティブプロファイルのシーン上書きを確認してから、実際に使用されるリストのTitleSceneを先頭・有効にする。
   - 不存在のSampleScene登録は無効化して保持。遷移先3シーンを架空のパスで登録しない。
5. Unityが生成する新規スクリプト等の.metaファイル。

## 日本語コメント方針
- 作成する全ソースに、変数／定数の用途を宣言付近、初期化・判定・シーンロード・ログ・解放の説明をそれぞれ処理付近に簡潔な日本語で記載する。
- publicメソッドにも用途を説明する日本語コメントを付ける。
- ファイル先頭に説明をまとめない。検証等で追加ソースが必要になった場合も同じ方針を適用する。

# Implementation Steps
1. **実装直前の保全・影響確認**
   - Dependencies: 計画の承認。
   - TitleSceneの保存状態、スクリプト重複、Canvas／EventSystem、フォルダ、アクティブBuild Profileを再確認する。未保存のユーザー変更があれば勝手に破棄しない。
   - 対象資産の依存／依存元を再取得する。専用の依存照会が利用できなければAssetDatabase.GetDependenciesとAssets内逆参照走査で確認する。
2. **遷移管理と日本語フォント補助の作成**
   - Dependencies: Step 1。
   - 上記2つのC#ファイルを既存フォルダに作成し、処理の流れに沿った日本語コメントを付ける。
   - コンパイル完了後にエラーと型の重複がないことを確認する。
3. **TitleSceneのUI構築**
   - Dependencies: Steps 1–2。
   - 既存Camera／Lightを維持し、必要なCanvas／UI／EventSystemを作成・配置する。
   - 指定スケーリング、色、サイズ、アンカー、ボタン色遷移とNavigationを設定する。
   - フォント初期化用Text参照をInspectorに接続する。TitleSceneを保存する。
4. **ボタンと新Input Systemの接続**
   - Dependencies: Steps 2–3。
   - SoloButton → StartSolo、OnlineButton → StartOnline、SettingsButton → OpenSettingsを永続OnClickに接続する。
   - 既存UI入力アセットをInputSystemUIInputModuleに接続し、必要なAction参照を設定する。SoloButtonを初期選択にする。
   - それぞれのイベントの対象、メソッド名、重複登録がないことを読み取り確認する。
5. **起動シーン登録の最小修正**
   - Dependencies: Steps 1、3。
   - TitleSceneを実効ビルドリストの最初の有効シーンにする。不在のSampleSceneは無効化して保持する。
   - 遷移先の不存在を再確認して記録し、3シーンは作成・登録しない。その他の設定、パッケージ、Define Symbolsは変更しない。
6. **Play Mode／画面サイズ／可能なWindowsビルドで検証**
   - Dependencies: Steps 2–5。
   - 下記のテストを実施し、実行結果と未実行項目を分けて記録する。
7. **完了報告**
   - Dependencies: Step 6。
   - 作成・変更ファイル、Editor設定、3ボタンの接続、実効シーンリスト、実測テスト結果、必要な手動対応を報告する。
   - 未作成の遷移先とOSフォント方式の制約を明示する。

# Verification & Testing
- コンパイル: 新規C#が正常にコンパイルし、Consoleに本作業由来のエラーがない。既存Burst／AI警告は別扱い。
- シーン構造: CanvasとEventSystemが各1つ。Main Camera／Global Light 2Dを保持。Missing Scriptと未設定参照がない。
- UI表示: QUIZ BATTLE、3つの日本語ラベル、Version 0.1.0が表示され、欠字・豆腐・切れ・重なりがない。1920×1080、1280×720、非16:9のウィンドウでも中央配置と下部表示を確認する。
- 入力: 実際のクリックで各OnClickが1回だけ発火する。Navigate／Submitによる縦選択・決定も確認し、背景や装飾がクリックを遮らない。
- SoloButton: QuizSceneが現在存在しないため、原因と対象名が分かるエラーログが出てTitleSceneに留まる。これは失敗時ガードの合格であり、実遷移成功ではない。
- OnlineButton: MatchingSceneについて同じ失敗時ガードを確認する。
- SettingsButton: 「設定画面は今後実装します。」のログが出てシーンが変わらない。
- 連打: 存在しないシーンへのクリックを繰り返しても未捕捉例外やNull参照が出ない。
- 再読込: 保存後のTitleScene再読込でUI、Inspector参照、永続OnClick、Canvas設定が保持される。
- ビルド登録: TitleSceneが最初の有効シーン。不在のSampleSceneは起動対象外。QuizScene／ResultScene／MatchingSceneは未登録として報告する。
- Windowsビルド: 実行環境で可能ならビルドして、タイトルから起動することとOS日本語フォント表示を確認する。できなければ未実行と記載し、別Windows PCでのフォント可用性も残課題として提示する。
- 実遷移の成功確認: 遷移先がないため今回未実施。後続STEPで3シーンを作成・登録した後にStartSolo／StartOnlineの実ロードを確認する。検証のためだけに仮シーンを追加しない。

## 範囲外・禁止事項
STEP 1フォルダの再構築、既存ファイル削除、Quiz／Matching／Resultシーンの新規作成、クイズ本編・問題データ・スコア・タイマー・設定画面・オンライン通信、追加パッケージ・外部素材・フォントの無断導入、パッケージソース変更、互換性ガードの回避は行わない。

# 接続切断後の再開状況と残作業
## 再確認済みの保存状態
- TitleSceneは保存済みでdirty=False。Main Camera／Global Light 2Dは保持され、CanvasとEventSystemは各1つ。Missing Scriptは検出されなかった。
- CanvasはScreen Space Overlay、Scale With Screen Size、1920×1080、Match=0.5、GraphicRaycasterあり。
- タイトル、3つの日本語ラベル、Version 0.1.0のTextと600×110の3つのボタンは存在する。
- 永続OnClickはSoloButton→StartSolo、OnlineButton→StartOnline、SettingsButton→OpenSettingsに各1件接続済み。
- InputSystemUIInputModuleは既存InputSystem_ActionsのUI/Point、UI/Click、UI/Navigate、UI/Submit等を参照し、初期選択はSoloButton。
- SceneTransitionManager.csとTitleScreenFontInitializer.csは作成済み。処理付近の日本語コメントを確認済み。フォント初期化のText参照5件は全て設定済み。
- 現在isCompiling=False、scriptCompilationFailed=False。Consoleには以前の作成操作時の組み込みフォント取得エラーが残っているが、保存済みTextは修正後のLegacyRuntimeを参照している。過去ログを現在のコンパイルエラーと混同しない。
- 有効な起動シーンはTitleScene。不存在の旧SampleScene登録は無効で保持されている。
- PlayModeTest.StateはIdle、結果は未記録、テスト用ソースと画像はAssetsに存在しない。切断されたテスト用ソース作成は完了していない。
- Enter Play Mode Optionsは有効、DisableDomainReloadのまま。テスト用の一時変更はまだ行われておらず、復元が必要な変更も現在はない。

## 再開時の実装手順
1. **作成済み成果物を保全する**（依存: 再開計画の承認）
   - 上記保存状態を再確認する。Steps 2–5の成果物は作り直さず、ユーザーの追加変更も破棄しない。
2. **テスト前提を一時設定する**（依存: 再開Step 1）
   - ユーザーは前回、テスト中だけReload Domainを有効化し、終了後に元へ戻すことを許可済み。
   - 現在のenterPlayModeOptionsEnabledとenterPlayModeOptionsを記録してからDisableDomainReloadのみ一時解除する。未保存シーン変更があれば勝手に再読込しない。
3. **Play Modeの検証を実施する**（依存: 再開Step 2）
   - 利用可能な実行手段に合わせて一時テストを作成する。テストソースにも処理付近の日本語コメントを入れる。既存テスト資産は現在ない。
   - 3ボタンのマウス入力、キーボードNavigate／Submit、各イベントの発火回数、設定ログ、不存在シーンのガード、再操作時の例外有無を検証する。
   - 日本語フォントの起動時割当てと欠字有無を確認し、可能ならPlay ModeのGame View画像を取得して目視確認する。Edit ModeのLegacyRuntime表示だけでは日本語の動作確認済みとしない。
   - 1920×1080、1280×720、非16:9で可能な範囲の実表示検証を行う。実行手段に制約があれば未検証のサイズを明示する。
   - テストが失敗した場合は実測結果から製品側／テスト側の原因を区別し、必要箇所だけを修正して再検証する。
4. **任意のWindowsビルド確認と後片付け**（依存: 再開Step 3）
   - 環境が対応する場合のみWindowsビルドと起動・日本語表示を確認する。未実行ならその旨を記録する。
   - テスト終了後は成功・失敗にかかわらずPlay Modeを退出し、再開Step 2で記録したEditor設定に戻す。
   - 今回新規生成した一時テストソース・画像・SessionStateのみを後片付けする。既存ファイルは削除しない。
   - TitleSceneの保存状態、OnClick、コンパイル状態を最終確認する。
5. **最終報告**（依存: 再開Step 4）
   - 作成・変更ファイル、Editor設定、接続状況、シーン登録、実測テスト結果、未検証項目と手動対応を報告する。
   - QuizScene／MatchingScene／ResultSceneは未作成であり、実遷移成功は今回確認できないことを明記する。

# STEP 3：問題データ設計・作成（今回の実行対象）
## Project Overview
- Game Title: QUIZ BATTLE。
- High-Level Concept: クイズ問題をゲームロジックから分離し、Unity Inspectorで編集できるScriptableObjectとして管理する。今回は問題単体と問題セットのデータ定義、およびサンプルデータだけを作成する。
- Players: 将来のひとりプレイ／オンライン対戦向けの共通データ。今回プレイ機能は追加しない。
- Inspiration / Reference Games: 指定なし。
- Tone / Art Direction: 既存の青・オレンジのタイトル画面を維持。データ作業のため新規の見た目は追加しない。
- Target Platform: Windows PC、StandaloneWindows64。
- Screen Orientation / Resolution: 既存の横画面1920×1080基準を維持。
- Render Pipeline: UniversalRP、Unity 6000.5.7f1。
- 指示書: `Assets/AI/QuizBattle_STEP3_QuizData_UnityAI.md`。内容を確認済み。原本は変更しない。

## Game Mechanics
### Core Gameplay Loop
後続工程の「問題セットから問題を読む→問題文・4択を表示→回答」を支えるデータを作成する。今工程では問題の抽出、順次表示、ランダム出題、正誤判定、タイマー、スコア、通信を実装しない。
### Controls and Input Methods
入力処理は追加しない。制作者がInspectorで問題文、4択、正解番号、ジャンル、難易度、解説とセットの問題参照を編集する。既存の新Input System設定や入力アセットは変更しない。

## UI
新しいゲームUIやCustom Editorは作らず、Unity標準Inspectorを使用する。
```text
QuizQuestion                         QuizQuestionSet
Question Id        Q001              Set Id          SET001
Question Text      [日本語問題文]    Display Name    基本問題セット
Choices            Size 4            Questions       Size 5
  Element 0        選択肢1             Element 0     Q001
  Element 1        選択肢2             Element 1     Q002
  Element 2        選択肢3             Element 2     Q003
  Element 3        選択肢4             Element 3     Q004
Correct Answer Index 1                 Element 4     Q005
Category           地理
Difficulty         Easy
Explanation        [日本語解説]
```
- TextArea、Header、Tooltipなど標準属性で読みやすくする。正解番号は0が選択肢1、3が選択肢4と明記する。
- 不正値を確認できるよう、正解番号の自動クランプや選択肢配列の自動リサイズはしない。

## Key Asset & Context
### 再調査結果
- `Assets/Scripts/Quiz`と`Assets/Data`は既存フォルダとして存在する。`_Project`を追加せずこの構成を利用する。
- `Assets/Data/QuizQuestions`と`Assets/Data/QuizSets`は未作成。ここだけ必要に応じて追加する。
- QuizQuestion／QuizQuestionSetの同名資産・クラスは見つからなかった。Assets内のC#はSTEP 2の2本のみで、asmdefも見つからなかった。
- TitleSceneはdirty=False、Main Camera／Global Light 2D／EventSystem／Canvasを保持し、コンパイル失敗フラグ=False。
- 起動リストはTitleSceneが有効、SampleSceneが無効のまま。STEP 3ではシーンやビルド設定を変更しない。
- 指示書・TitleScene・既存2ソースの依存関係を読み取り確認済み。実装前には新規作成パスの存在と依存元を再確認する。

### 作成対象とAPI
1. `Assets/Scripts/Quiz/QuizQuestion.cs`
   - `QuizQuestion : ScriptableObject`。CreateAssetMenu: `QuizBattle/Quiz Question`。
   - 同ファイル内に`QuizDifficulty` enum（Easy=0、Normal=1、Hard=2）。
   - private SerializeField: `questionId`、`questionText`、`choices`（初期値4枠）、`correctAnswerIndex`、`category`、`difficulty`、`explanation`。
   - 読み取りプロパティ: QuestionId、QuestionText、Choices、CorrectAnswerIndex、Category、Difficulty、Explanation、ChoiceCount。
   - Choicesは`IReadOnlyList<string>`として読み取り専用ラッパーを返し、内部string[]をそのまま公開しない。null配列は空リスト、null文字列は取得時に安全に扱う。setterは公開しない。
   - `GetChoice(int index)`は範囲外・null選択肢なら空文字を返す。内部データを補正・変更しない。
   - 正解番号のGetterは保存値を返す。不正値を0に置換して黙って隠さず、OnValidateで警告する。正誤判定メソッドは追加しない。
2. `Assets/Scripts/Quiz/QuizQuestionSet.cs`
   - `QuizQuestionSet : ScriptableObject`。CreateAssetMenu: `QuizBattle/Quiz Question Set`。
   - private SerializeField: `setId`、`displayName`、`questions`（QuizQuestion[]、空配列で初期化）。
   - 読み取りプロパティ: SetId、DisplayName、Questions、QuestionCount。
   - Questionsは読み取り専用ラッパー。null配列は空リストとする。QuestionCountは登録スロット数（nullスロットも含む）とし、意味をコメントで明示する。
   - `GetQuestion(int index)`は範囲外またはnull要素ならnull。無効要素を勝手に除外せず、元の並びを維持する。
3. `Assets/Data/QuizQuestions/Q001.asset`〜`Q005.asset`。
4. `Assets/Data/QuizSets/BasicQuizQuestionSet.asset`。
5. 上記の新規.metaファイルと必要なデータ用サブフォルダ。

### データ検証設計
- 各クラスのOnValidateは警告だけを出し、配列、ID、番号、参照を削除・上書きしない。
- QuizQuestion: ID／問題文がnull・空・空白だけ、choicesがnullまたは4枠以外、各選択肢がnull・空・空白だけ、正解番号が0〜3以外を警告。未定義の難易度値も可能なら警告する。
- QuizQuestionSet: questionsのnull要素をスロット番号付きで警告。非空IDの重複をHashSet<string>等で検出して警告し、参照や順序は変更しない。同一問題の二重登録も同じIDとして検出する。
- 警告はアセットを特定できる文面とオブジェクト参照を含める。空配列のセットは作成直後の状態として安全に扱う。
- 標準C#とUnityのみ使用。追加パッケージ、Resourcesフォルダ、ランタイム資産検索は追加しない。

### サンプルデータ（指示書に一致させる）
| ID | 問題文 | 選択肢0 / 1 / 2 / 3 | 正解index | ジャンル |
|---|---|---|---|---|
| Q001 | 日本の首都として一般に知られている都市はどこ？ | 大阪 / 東京 / 名古屋 / 福岡 | 1 | 地理 |
| Q002 | 水が標準的な大気圧で沸騰する温度は何℃？ | 50℃ / 75℃ / 100℃ / 150℃ | 2 | 科学 |
| Q003 | 1年が通常何か月で構成される？ | 10か月 / 11か月 / 12か月 / 13か月 | 2 | 一般常識 |
| Q004 | 太陽系で太陽に最も近い惑星はどれ？ | 金星 / 地球 / 火星 / 水星 | 3 | 天文 |
| Q005 | 日本の硬貨で、現在一般的に流通している額面として存在するのはどれ？ | 7円 / 25円 / 50円 / 200円 | 2 | 一般常識 |
- 全問Easy。explanationは指示書の日本語解説をそのまま使用する。
- セット: setId=`SET001`、displayName=`基本問題セット`、Q001〜Q005を順番に各1件登録。

### 日本語コメント
両ソースの変数・定数・enumの用途を宣言付近、Getterのnull／範囲外処理、OnValidateの条件判定・重複検出・警告の理由を各処理付近に記載する。ファイル先頭の一括説明だけにはしない。検証用ソースを追加する場合も同様。

## Implementation Steps
1. **対象パスの最終確認**（依存: STEP 3計画の承認）
   - 同名ソース／データ資産とフォルダの存在、参照元を再確認。既存資産が見つかった場合は内容を読んで必要変更だけに留める。
   - STEP 2の2ソース、TitleScene、入力資産、ビルド／Editor設定を維持する。
2. **QuizQuestionを実装**（依存: STEP 3 Step 1）
   - QuizQuestion.csにenum、SerializeField、CreateAssetMenu、安全な読み取りAPI、警告だけのOnValidate、日本語コメントを作成する。
   - コンパイルを確認し、null・範囲外の読み取りとデータ非改変を検証する。
3. **QuizQuestionSetを実装**（依存: STEP 3 Steps 1–2）
   - QuizQuestionSet.csに問題参照配列と読み取りAPI、null／重複IDの警告処理、日本語コメントを作成する。
   - コンパイルを確認。抽出・ランダム出題は実装しない。
4. **5問と基本セットを作成・保存**（依存: STEP 3 Steps 2–3）
   - 必要なデータ用サブフォルダを作成し、SerializedObject等のEditor機能で指定値を保存する。実行時の書き込み用setterは追加しない。
   - 途中の未入力状態の警告と最終データの検証結果を区別する。
   - 5アセットをセットへ順番に接続し、保存後に再読込して全フィールドと参照を確認する。
5. **不正入力・非改変・読み取りAPIを検証**（依存: STEP 3 Steps 2–4）
   - サンプルを壊さない隔離した一時テストデータで、各警告条件と安全なGetterを確認する。
   - 値の変更前後・検証前後を比較し、警告が選択肢の増減や番号補正、問題削除を起こさないことを確認する。
   - 今回作成した一時テストデータのみ後片付けする。STEP 2のPlay Modeテストは行わず、Reload Domain設定も変更しない。
6. **保存状態と結果を報告**（依存: STEP 3 Steps 4–5）
   - スクリプト2本、問題5アセット、セット1アセット、セット登録、検証ログ、コンパイル状態、既存成果物の保全を実測に基づいて報告する。
   - 未実施の確認や手動操作が残れば、具体的な内容を明示する。

## Verification & Testing
- C#が正常にコンパイルし、QuizQuestion／QuizQuestionSetの重複型やMissing Scriptがない。
- ProjectのCreateメニューに問題と問題セットの項目があり、作成した資産をInspectorで編集・保存できる。
- 正常5問: ID、問題文、4枠の選択肢、正解index=1/2/2/3/2、ジャンル、Easy、解説が指示書と一致する。
- SET001: displayNameが基本問題セット、QuestionCount=5、参照順がQ001〜Q005でありnull・重複IDなし。
- ID／問題文: null、空文字、空白のみで警告。
- choices: null、3枠、5枠で警告し、検証後も元の内容と長さを保持。4枠内のnull／空文字／空白でも警告。
- correctAnswerIndex: -1、4で警告し値は保持、0と3は有効。0始まりの説明が確認できる。
- 問題セット: null配列で安全に読める。nullスロットと同一IDの別問題／同一参照の二重登録で警告し、スロット数・順序を保持。
- 安全な読み取り: GetChoice(-1／ChoiceCount)は空文字、GetQuestion(-1／QuestionCount)はnull、null配列のCountは0、公開setterはない。
- 読み取り一覧経由で元の配列を変更できないことを確認する。
- 警告ごとに実際のログと対象の値を記録する。正常データの検証では不正入力警告がない。
- 保存後の再読込で日本語文字列、正解番号、セット参照が保持される。
- STEP 2のTitleScene・ソース・入力・ビルド設定は変更しない。既存の未完了Play Mode検証は未完了のまま記録し、STEP 3完了と混同しない。

# STEP 4：ひとり用クイズ進行（今回の実行対象）
## Project Overview
- Game Title: QUIZ BATTLE。
- High-Level Concept: 既存の問題セットを使い、1問15秒の4択クイズを登録順に解き、得点・正解数・正答率を見てタイトルに戻れるひとり用モードを作る。
- Players: Single player。通信・マッチングは対象外。
- Inspiration / Reference Games: 指定なし。
- Tone / Art Direction: 既存タイトルに合わせた青×オレンジ。正誤は文字と色の両方で伝える。
- Target Platform: Windows PC、StandaloneWindows64。
- Screen Orientation / Resolution: 横画面1920×1080基準、Canvasのサイズ追従を設定する。
- Render Pipeline: UniversalRP、Unity 6000.5.7f1。
- 原本: `Assets/AI/QuizBattle_STEP4_QuizGameplay_UnityAI.md`を確認済み。原本は変更しない。
- ユーザー選択: ControllerとUIを2本に分離する。TMP＋Windows標準OSフォントを採用する。

## Game Mechanics
### Core Gameplay Loop
TitleSceneの「ひとりで遊ぶ」→QuizSceneの問題表示→15秒以内に回答→正解100点／不正解・時間切れ0点→1.5秒のフィードバック→次問→最終結果→TitleSceneに戻る。
- 問題数は有効な問題の数から求め、5固定にしない。元セットの配列を並べ替えず、実行時の作業用リストに登録順で保持する。
- null、空ID／空問題文、4枠以外、空選択肢、正解番号範囲外の問題は理由とスロットを警告してスキップする。重複IDも実行時にスキップし、元アセットには触れない。
- 問題セット未設定／有効問題0件はエラー表示とログにし、回答UIを無効にしてタイトルへ戻れるようにする。
- 状態: Inactive／AwaitingAnswer／Feedback／Complete／Error。AwaitingAnswer以外の回答を拒否し、確定時に状態を先に変更して二重加点を防ぐ。
- 期限と回答が同フレームに競合した場合は、回答受付内でも時刻を確認し、期限以上なら時間切れを優先する。
- 制限時間は実時間基準の15秒。残り秒数は切り上げ表示、締切時0秒。毎フレーム減算誤差を積み重ねず、期限時刻から残り時間を計算する。
- フィードバックは「正解！」「不正解」「時間切れ」と正解選択肢を表示。全ボタンを無効化し、正解ボタンを識別色で強調する。
- 最終結果: 出題数、最終得点、正解数、正答率（正解数／有効出題数×100）。分母0では結果計算せずエラー表示する。
- OnDisable／OnDestroyで進行を停止し、シーン離脱後の処理を防ぐ。今回はCoroutineを使わず、Updateで状態と期限を処理する。

### Controls and Input Methods
- Project SettingsのactiveInputHandler=1を実測確認済み。Input System 1.20.0の既存UIマップをInputSystemUIInputModuleへ接続する。旧Input APIやStandaloneInputModuleを使わない。
- 4択はuGUI Button、文字はTextMeshProUGUI。マウスクリックと既存Navigate／Submitで操作する。
- 既存方式に合わせ永続Button.OnClickを設定する。AnswerButton0〜3が`QuizGameController.SubmitAnswer(int)`へ各index=0〜3を渡す。ReturnTitleButtonは`ReturnToTitle()`を呼ぶ。
- 問題更新時の初期選択はAnswerButton0、終了／エラー時はReturnTitleButtonにする。ボタンNavigationも明示する。

## UI
```text
┌────────────────────────────────────────────┐
│ 問題 1 / 5                      スコア：0 │
│ 残り時間：15秒       [タイマーバー]       │
│ ┌────────────────────────────────────────┐ │
│ │           日本語の問題文               │ │
│ └────────────────────────────────────────┘ │
│ [      選択肢1      ] [      選択肢2      ] │
│ [      選択肢3      ] [      選択肢4      ] │
│           正解／不正解／時間切れ           │
└────────────────────────────────────────────┘
終了時: 問題UIを隠して同じQuizScene内のCompletePanel
「クイズ終了！」／出題数／得点／正解数／正答率／タイトルへ戻る
エラー時: 同パネルで原因を案内し、結果数値を隠してタイトルへ戻る
```
- 指示書のHeader、TimerArea、QuestionPanel、AnswerPanel、FeedbackText、CompletePanelを作る。問題UIをまとめて非表示にできるGameplayRootを必要に応じて追加する。
- CanvasはScreen Space Overlay、Scale With Screen Size、Reference 1920×1080、Match=0.5。GraphicRaycasterとEventSystemは各シーンで必要数だけ作り、重複させない。
- 中央寄せの広い問題パネルと2×2の大きい回答ボタン、上下左右に余白を確保する。長いQ005は複数行で収める。カウンター・タイマーのTMP AutoSizeは無効とし、TimerTextは整数秒が変わった場合だけ更新する。
- BackgroundとラベルはRaycast Target=False、ButtonのImageはTrue。色だけに依存しないフィードバックを表示する。
- 終了パネルは初期非表示。FeedbackとCompletePanelを同時に不適切に表示しない。

## Key Asset & Context
### 確認済みの現状
- シーンはTitleSceneとURPテンプレートのみ。`Assets/Scenes/QuizScene.unity`は不存在なので今回新規作成する。
- 現在TitleSceneはdirty=False、コンパイル失敗なし。過去のText最大サイズの未保存差分は現在残っていない。TitleSceneを作り直し・自動再設定しない。
- 既存Core/SceneTransitionManager.StartSoloはQuizSceneをロードするため、QuizSceneの登録だけで利用できる。既存Coreソースの変更は不要。
- `QuizQuestion`: QuestionId／QuestionText／ChoiceCount／GetChoice(index)／CorrectAnswerIndex／Explanation等を利用する。
- `QuizQuestionSet`: QuestionCountはnullを含む登録スロット数、GetQuestion(index)で参照を取得できる。Controller側で有効問題数を算出する。
- `BasicQuizQuestionSet.asset`はQ001〜Q005の有効5問を保持し、正解indexは1／2／2／3／2。APIと依存資産を読み取り確認済み。
- BuildリストはTitleScene有効、SampleScene無効。TitleSceneを起動シーンに維持し、有効QuizSceneを追加する。MatchingScene／ResultSceneは追加しない。
- uGUI 2.5.0内にTMPが存在し、公開API `TMP_FontAsset.CreateFontAsset(string familyName, string styleName, int pointSize)`とDynamicOSモードが利用可能。
- 導入済みTMPのソースを読んで、上記APIがOSフォントを検索しDynamicOSの1024×1024 SDF資産を生成することを確認済み。パッケージソースは変更しない。
- Reload Domainは無効。前回の許可に基づき、必要なPlay Modeテスト中だけ一時有効にし、終了後に元の設定へ戻す。シーン離脱時にTitleSceneの未保存変更を勝手に破棄しない。

### 作成・変更するファイル
1. 新規 `Assets/Scripts/Quiz/QuizGameController.cs`
   - SerializeField: questionSet、sceneUI。基本セットをInspectorで接続する。
   - 定数: 制限15秒、正解100点、フィードバック1.5秒、TitleScene名。
   - 現在index、score、correctCount、状態、回答期限、次問時刻、有効問題リストを保持する。
   - public `SubmitAnswer(int choiceIndex)`／`ReturnToTitle()`。UIが不足すれば具体的なInspector項目をログに出し、進行を開始しない。
   - TitleSceneロード可否を確認してからロードする。既存SceneTransitionManagerを重複作成しない。
   - 必要なテスト観測用には状態・得点・正解数・有効問題数・現在index・残り時間の読み取りプロパティだけを用意する。テスト目的の加点・進行改変APIは作らない。
2. 新規 `Assets/Scripts/UI/QuizSceneUI.cs`
   - TMP_Text、Button[4]、ラベル[4]、TimerBar Image、GameplayRoot／CompletePanel、各結果表示、ReturnTitleButtonをInspector参照する。
   - UI参照の検証、問題表示、スコア／タイマー更新、フィードバック、終了／エラー表示、ボタン無効化を担当する。
   - AwakeのタイミングでControllerへのイベントを隠れて重複登録せず、接続は永続OnClickで統一する。
   - 入力と表示の責務を混ぜず、ゲームの得点や正解判定をUI側で行わない。
3. 新規 `Assets/Scenes/QuizScene.unity`
   - Main Camera、EventSystem、Canvas、上記UIとController/UIコンポーネント。
   - URPと既存入力設定に適合するCamera／InputSystemUIInputModuleを設定する。
4. 新規 `Assets/Fonts/QuizGameplayJapanese.asset`と関連material／atlas sub-assets。
   - Meiryo Regular等の利用可能なWindows標準日本語フォントから、確認済みの公開APIでDynamicOS資産を作る。
   - sourceフォントファイルをコピー・同梱しない。生成したSDFとmaterialをシリアライズして全TMPへ割り当てる。
   - 現在の問題5問とUIラベル・数字・エラーメッセージの文字が表示可能か確認する。アトラスは1024基準とし、無制限に増やさない。Clear Dynamic Data On Buildを設定できる場合は有効化する。
   - TMP標準リソースが必要なら導入済みパッケージ内の必要リソースだけを利用する。Examplesや外部素材、追加パッケージは導入しない。利用許諾が不明なフォントのコピーは禁止。
   - OSフォントが取得できなければ原因を報告し、日本語表示を未確認のまま成功扱いしない。別Windows PCのフォント可用性は検証範囲を明記する。
5. 必要最小限の変更 `ProjectSettings/EditorBuildSettings.asset`または実効Build Profile。
   - TitleSceneを先頭に維持、QuizSceneを有効登録、既存無効SampleScene登録は保持。
6. 必要なら一時Editorテストソース・画像。今回生成したものだけ後片付けする。既存データ5問・セット・STEP2ソース・TitleScene・入力アセット・packageは変更しない。

### コメント方針
新規ソース全てに、変数宣言、参照検証、状態変更、期限判定、正誤判定、加点、次問、終了、シーン離脱処理の近くへ簡潔な日本語コメントを記載する。ファイル先頭の説明だけにはしない。

## Implementation Steps
1. **保全・依存確認**（依存: STEP4計画の承認）
   - 対象パスの既存資産・参照元、シーン保存状態、実効Build Profile、OSフォントを最終確認する。
   - STEP1〜3の保存ファイルのハッシュと現在のEditor設定を記録する。変更があれば勝手に破棄しない。
2. **ControllerとUI実装**（依存: Step 1）
   - 指定パスへ2ソースを作成し、日本語コメントを付ける。状態によるガードと期限チェック、データフィルタ、エラー表示を実装する。
   - コンパイル完了を確認する。STEP3のGetterをそのまま利用しデータクラスは変更しない。
3. **日本語TMP資産の生成と確認**（依存: Step 1）
   - 現在の公開APIでOSフォントからDynamicOS資産を作り、必要文字の収録／追加可否とshader/materialを確認する。
   - 見えない・欠字がある場合は原因を特定し、無断で別方式や外部素材へ切り替えない。
4. **QuizSceneのUIと接続を作成・保存**（依存: Steps 2–3）
   - 不在のQuizSceneを新規作成し、Canvas/EventSystemを先に用意してUIを構築する。
   - Controller.questionSet=BasicQuizQuestionSetとUI全参照を設定。4回答indexとタイトル戻りの永続OnClick、Navigation、既存UI入力を接続する。
   - 全RectTransformが有効サイズで画面内にあること、CompletePanel初期非表示、背景が入力を遮らないことを確認する。
5. **実効シーン登録**（依存: Step 4）
   - QuizSceneを有効リストへ追加し、TitleScene起動を維持する。タイトル側のStartSolo→QuizSceneが利用可能か確認する。
6. **動作・競合・異常系テスト**（依存: Steps 2–5）
   - 下記テストをPlay Modeで行い、計測とスクリーンショットを照合する。必要なら一時テストを使用する。
   - 15秒タイマーと1.5秒フィードバックは本番値で実測する。高速化したテストだけで本番の時間を検証済みにしない。
   - 異常系は一時コピーを使い、正常サンプルを汚さない。
7. **後片付けと最終報告**（依存: Step 6）
   - Play Mode退出、一時テスト／画像の後片付け、Reload Domain等の一時設定復元。
   - 新規シーン・TMP資産の保存再読込、ボタン接続、コンパイル状態、保全ハッシュを確認する。自動検証がTitleSceneに未保存差分を作った場合は黙って保存・破棄せず報告する。
   - 作成変更一覧、Hierarchy、問題参照、仕様、実測結果と未実施テスト／手動対応を報告する。

## Verification & Testing
- QuizSceneが開き、Controller/UI/TMP/ボタン参照にMissing Script・不足がない。問題文と日本語4択が実際に見え、Q005も切れない。
- 問題番号1/5、スコア0、残り時間15秒を初期表示。カウントダウンが進み、実時間15秒で時間切れになり、残り表示は0。
- 正解クリックで100点と正解数1、不正解と時間切れでは0点。回答時に4ボタンが直ちに無効になる。
- 同フレームの連続Submit、連打、確定後のSubmitで二重加点しない。-1／4のindexは安全に拒否する。
- 期限と同時の回答は時間切れを優先し、1問で正解と時間切れを重複処理しない。
- 1.5秒程度のフィードバック後に次問へ進み、タイマー15秒と全ボタンの有効状態をリセット。元問題セットの登録順を維持。
- 5問の混合テスト（正解3、不正解1、時間切れ1）で300点、出題数5、正解数3、正答率60%を確認。
- 全問正解で500点／5問／100%。全問不正解または時間切れで0点／0正解／0%。最終問題後は問題UI非表示・終了パネル表示。
- 1問セット、5以外の件数、null混在セットを一時データで試し、有効問題数に一致する表示と結果を確認。元資産は改変しない。
- 未設定セット／空セット／全無効データは原因表示とログ、回答無効、戻りボタン有効。UI必須参照不足でもNullReferenceExceptionや暴走がない。
- タイトルの「ひとりで遊ぶ」からQuizSceneへ入り、終了／エラー時の戻りボタンでTitleSceneに戻れる。
- マウスの実入力と既存キーボードNavigate／Submitを確認。初期選択と終了時の選択が正しい。
- 1920×1080、1280×720、非16:9で画面の欠字・重なり・範囲外を確認。OSフォントの取得とDynamicOS資産のロードを実測する。
- シーン離脱／Controller無効化後にタイマーや回答処理が続かない。Domain Reload無効へ戻した状態で繰り返しプレイしても古い進行が残らない。
- 正常プレイのConsoleエラー0件・コンパイル失敗なし。異常系で意図して出した説明ログと予期しない例外を区別して報告する。
- 実行不能なテストや別PCのフォント表示は未確認と記載し、構成検査だけで動作済みとしない。

## STEP4の範囲外
オンライン通信、Photon、マッチング・ロビー、ランキング、アカウント、早押しボーナス、問題編集ツール、ランダム出題、別ResultScene、STEP2タイトルの作り直し、STEP3データクラス／5問／セットの変更、外部素材・パッケージ追加。

# STEP 9〜12：回答・結果・再戦・切断処理の一括実装と総合検証（今回の対象）

# Project Overview
- Game Title: QUIZ BATTLE。
- High-Level Concept: 同じ5問を2人で解き、Hostが正誤・得点・勝敗を確定するオンライン対戦を完成させる。既存の問題・タイマー同期に回答、結果パネル、双方同意による再戦、安全な中断・退出をまとめて追加し、最後に統合検証する。
- Players: Solo、オンラインHost/Client最大2人。ホスト移行は対象外。
- Inspiration / Reference Games: 指定なし。既存のクイズ対戦フローを維持。
- Tone / Art Direction: 既存の青・オレンジ、日本語TMP。色だけでなく文字で正誤・待機・切断を説明する。
- Target Platform: Windows PC、StandaloneWindows64。
- Screen Orientation / Resolution: 横画面1920×1080基準。1280×720と可能な別アスペクト比も確認。
- Render Pipeline: UniversalRP、Unity 6000.5.7f1。Photon Fusion 2.1.4を維持。

## 依頼と実行方針
- 原本はAssets/AIのSTEP9_AnswerScoreSync、STEP10_ResultRematch、STEP11_DisconnectHandling、STEP12_IntegrationTestの4つのMarkdown。確認済みで、指示書を上書きしない。
- ユーザーはSTEP8の2端末での問題・タイマー・5問進行成功を報告済み。ただし、それをSTEP9以降の得点・再戦の検証結果に流用しない。
- ユーザーの時間削減を優先し、STEPごとのビルド・手動テスト依頼をやめ、9〜11を一括実装した後に12の統合検証と最終ビルドへまとめる。
- コンパイル、参照、権限、期限競合の内部確認は省略しない。バグ修正に必要な再検証は行うが、同じ不明状態でテストを繰り返さない。
- 結果UIはOnlineQuizScene内の専用パネルを推奨・採用予定。別OnlineResultSceneも可能だが、ロードと準備通知が増えるため、今回の短い対戦ループと検証時間削減には同一シーンのパネルが適する。分かりやすいフィードバックと再戦導線によりMDAの体験面と操作の明確さを重視する。
- この計画の承認後に実装する。今回は計画だけで、ゲーム資産の編集・ビルド・Play Modeテストはまだ実行しない。

# Game Mechanics
## Core Gameplay Loop
1. 作成・参加、問題セット照合、双方の画面準備完了までの既存フローを維持する。
2. 各問15秒。各プレイヤーは0〜3の選択インデックスを1回送信する。
3. Hostが試合ID、問題番号、受付フェーズ、期限tick、実際の送信元とスロット、範囲、回答済み状態を検証する。
4. Host側の問題データで正解を判定。正解100点、不正解・時間切れ0点。Clientから正解番号や点数は受け取らない。
5. 双方回答済み、または期限到達で一度だけ確定する。未回答を時間切れにし、約1.5秒のtick基準フィードバック後にHostが次問へ進める。
6. 最後のフィードバック後にHostが得点比較でHost勝利／Client勝利／引き分けを確定し、同じ同期値を両端末の結果パネルへ表示する。
7. 再戦は現在の試合IDに紐づく双方の同意が揃った時だけHostが実行する。新しい試合IDを作り、得点・正解数・回答済み・選択・結果・問題番号・タイマー・画面準備通知・ローカル送信待ち表示をリセットする。
8. 再戦は同じRunner、同じ2人、同じOnlineQuizSceneを再利用する。途中参加は引き続き不可。両端末の再戦準備通知を受けてから第1問の15秒を開始する。

## 競合・切断方針
- 締切tick以上の到着は時間切れを優先。RPC内の受付検証とHostのFixedUpdateNetworkの確定処理で同じ基準を使う。
- 1問の確定時に先にFeedbackへ遷移し、遅延RPCや同フレーム再入で追加加点しない。
- 試合中のClient退出は継続せず中断。Hostへ理由を表示し、再戦を無効化する。
- 結果・再戦待ち中の退出でも再戦を中止する。確定済み結果を後から別の勝敗へ変更しない。
- Host退出・回線断ではClientのゲーム操作を止め、理由とタイトルへ戻るボタンを表示する。ホスト移行・自動回線復旧は追加しない。
- 二重Shutdown、退出連打、遷移中の操作、古い再戦要求を拒否し、既存の一元化した接続管理から終了する。
- シーン準備・結果待ち等に無限待機が発生しないか確認し、必要な箇所に適切なタイムアウトと中断表示を追加する。

## Controls and Input Methods
- 実測activeInputHandler=1、新Input Systemのみ。既存InputSystemUIInputModuleを継続使用する。
- 既存4択Buttonを有効化し、各インデックスを送信。ローカル送信直後は連打を防ぐが、Host側でも独立に全条件を検証する。
- 受付終了、Feedback、結果、切断、再戦準備中は回答不可。
- 結果パネルの再戦・タイトル戻りはそれぞれ一度だけ登録し、OnDisable等で解除する。既存のマウス／Navigate／Submit操作を保持する。

# UI
## 試合中のワイヤーフレーム
```text
[問題 1 / 5]       オンラインクイズ        [残り15秒]
Host名：100点／1正解          Client名：0点／0正解
[                 タイマーバー                  ]
[                    問題文                      ]
[        選択肢1       ] [        選択肢2       ]
[        選択肢3       ] [        選択肢4       ]
あなた：回答済み／相手の回答を待っています
Feedback：Host 正解！／Client 不正解・時間切れ
[                 退出・タイトルへ戻る          ]
```
- 各問確定後に両者の結果を文字で表示する。正解表示はHost確定後の状態から表示し、回答中に相手の正解情報を漏らす表示を追加しない。
- 長い問題文と16文字の表示名で重なりを確認。既存サイズ・日本語フォントを基本とし、OnlineQuizSceneだけ必要な配置調整をする。

## 結果／再戦／エラーパネル
```text
┌──────────────────────────────────────┐
│       あなたの結果：勝利／敗北／引き分け       │
│ Host名    得点     正解数/5    正答率          │
│ Client名  得点     正解数/5    正答率          │
│ あなた：再戦希望   相手：同意待ち             │
│ [再戦する]       [タイトルへ戻る]            │
└──────────────────────────────────────┘
中断時：原因を表示し、再戦・回答を無効化。
        [安全に終了してタイトルへ戻る]
```
- 結果はHostの同期スコア・正解数・勝敗に基づく。SoloのCompletePanelは変更しない。
- 再戦の一方同意、両方準備中、相手退出を区別し、固まったように見せない。

# Key Asset & Context
## 今回読み取り確認した現状
- OnlineQuizMatchController.csにはWaitingForData、Ready、LoadingScene、Question、Complete、DataMismatch、Abortedがある。現在は15秒タイムアウトだけで次問へ進み、回答・得点・再戦はない。
- OnlineQuizUI.csは4択テキストとtickタイマーを表示するが、4択Buttonは未接続・無効で、結果パネルはない。
- PhotonConnectionManager.csは一つのRunnerを作り、初期OnSceneLoadDone後にRoomをSpawnする。ManagerとRoomをDontDestroyOnLoadで保持し、退出時にShutdown後にタイトルへ戻してManagerを破棄する。
- 既存Roomの同じNetworkObjectにMultiplayerRoomStateとOnlineQuizMatchControllerがあり、接続管理やネットワーク状態Objectを重複作成する必要はない。
- MatchingSceneは保存済み、Play Modeは停止、コンパイル失敗なし。有効ビルドシーンはTitleScene、QuizScene、MatchingScene、OnlineQuizScene。無効SampleSceneは保持されている。
- Domain Reload無効を確認。自動検証に必要な間だけ元の設定を記録して一時変更し、最後に復元する。

## 作成・変更候補
- 変更：Assets/Scripts/Network/OnlineQuizMatchController.cs
  - 回答RPC、Feedback、Host／Clientの回答状態・結果・得点・正解数、最終勝敗、再戦同意と新試合リセット、必要な待機timeout。
  - Networkedへの書き込みはStateAuthorityのHostに限定する。
- 変更：Assets/Scripts/UI/OnlineQuizUI.cs
  - 4択Button参照と入力、Host確定フィードバック、両者のスコア、結果・再戦・中断表示。
- 変更：Assets/Scripts/Network/PhotonConnectionManager.cs
  - 接続失敗・切断・Shutdownのユーザー向け文言、一度だけの終了、遷移中取消・再試行・購読解放の補強。
- 必要な時だけ変更：Assets/Scripts/Network/MultiplayerRoomState.cs、Assets/Scripts/UI/MatchingSceneUI.cs。
  - 再戦／結果中のセッションロックと相手退出通知を既存責務に統合する。
- 変更：Assets/Scenes/OnlineQuizScene.unity
  - 既存Canvas・EventSystemを再利用し、スコア、フィードバック、オンライン専用結果・エラーパネルと参照・ナビゲーションを保存する。
- 変更：Assets/Prefabs/Network/MultiplayerRoomState.prefab
  - ネットワークプロパティ追加後に再ベイクし、既存登録と問題セット参照を維持する。
- 必要な時だけ新規：Assets/Scripts/Network/OnlineQuizRules.cs等の小さな共通ルール。
  - 本番Hostの判定と独立テストで同じ純粋ロジックを使える場合に限定する。テスト目的で加点・強制進行APIを本番へ追加しない。
- 一時：Editor Bootstrap、必要なら実Standalone検証用の隔離ドライバ・テストビルド・結果画像。最終配布ビルドにはテストドライバを含めない。
- 新規報告候補：Assets/AI/QuizBattle_STEP9_12_IntegrationResults.md。
  - 実際に行ったテスト、再現条件、期待／実測／修正／再テスト結果、未確認項目を記録する。チェックを架空の合格にしない。
- 新しい最終Windowsビルドは既存ConnectionTest、RoomTest、QuestionSyncTestを上書きしない専用フォルダへ出力する。

## 保護するもの
- QuizGameController、QuizSceneUI、QuizQuestion、QuizQuestionSet、問題5アセットと基本セット、TitleSceneの既存導線。
- 15秒・正解100点・不正解/時間切れ0点・約1.5秒という仕様。本番値をテスト高速化のために変更しない。
- 既存Unity／Fusion／Input System／URP。追加サービス・課金・ランキング・パッケージ・フォントは導入しない。
- パッケージソース、互換性ガード、Unity生成Define Symbolsは変更しない。App ID・トークンを報告や独自ログへ記載しない。

# Implementation Steps
1. **保全・影響とAPIの再確認**
   - Dependencies: この一括計画の承認。
   - 上記対象の依存元と保存状態、PrefabのBehaviour順・登録、入力バックエンド、Fusion RPC/HostMode/TickTimerの実APIを確認する。
   - 前STEPの一時テスト・SessionState残骸があれば、自分が生成したものだけ片付ける。既存Solo資産の保全を記録する。
2. **STEP9：回答・得点・問題確定を実装**
   - Dependencies: Step 1。
   - OnlineQuizMatchControllerのRPCとNetworked状態を追加。受付検証、Host判定、重複排除、締切競合、一度だけ確定、Feedback timer、最終勝敗を既存進行へ統合する。
   - 期限切れ回答を現在問の追加加点として扱わない。拒否理由を識別可能な開発ログにし、秘密・不要なクライアント文字列を出さない。
   - OnlineQuizUIへ回答操作・送信待ち・Host確定表示を接続する。コンパイルと純粋な検証を行うが、この段階でユーザーへ端末テストを依頼しない。
3. **STEP10：結果パネル・双方同意の再戦を実装**
   - Dependencies: Step 2。
   - OnlineQuizSceneの既存UIを再利用し、オンライン専用ResultPanelを追加。正答率とローカル視点の勝敗はHost確定値から表示する。
   - 再戦RPCに現試合IDを含め、本人スロットの同意だけを記録する。双方同意・同じ参加者・データ一致を確認し、Hostだけが新MatchIdと全状態を初期化する。
   - 同じシーン・Runnerを再利用し、両者の新試合準備が揃ってから出題する。各Buttonと購読の重複を防ぐ。
4. **STEP11：切断・待機・終了を統合**
   - Dependencies: Steps 2–3。
   - PhotonConnectionManagerとMatchControllerの既存コールバックから対戦中断・再戦中止・タイマー停止を一元化する。
   - Host/Client退出、開始/再戦準備中退出、接続失敗、回線断、退出連打を安全な状態へ収束させる。ユーザー向け文言と内部ログを分ける。
   - 解放後の参照読み取り、終了の再入、古い準備通知、非同期シーンロード後の接続完了残留を防ぐ。
5. **Prefab・UI最終接続と保存**
   - Dependencies: Steps 2–4。
   - Room prefabを再ベイクし、同じObject/Runner構成を維持する。OnlineQuizSceneの全参照・Button index・パネル初期表示・ナビゲーション・Raycastを確認して保存する。
   - コンパイル完了後に本番値、問題セット照合、シーン登録、Missing Script、必要な日本語表示を確認する。
6. **STEP12：最後にまとめて検証**
   - Dependencies: Steps 1–5。
   - まず高速な状態・判定・ID/期限・得点計算の検証をまとめて実施する。古い回答・重複・無効範囲・同点を再現可能なデータで検査する。
   - 一つの統合シナリオで接続→回答→結果→片側再戦待ち→双方再戦→退出／再接続を確認する。
   - Editor＋Standaloneの実2環境自動検証を利用可能な範囲で試す。テスト用ドライバを使う場合は隔離し、最終ビルドから除去する。実際に成立した2環境テストだけをオンライン成功として報告する。
   - 実2環境の自動操作が実行できなければ、最終ビルドでのユーザー向け統合チェックを一度だけ依頼する。全境界・障害ケースまで一度の正常系で合格扱いしない。
   - Play Modeの画像で1920×1080、1280×720、可能な別比率・長文・長い表示名を確認。実際のマウス／既存Navigate操作を可能な範囲で確認する。
   - Soloは全5問、得点、自然な15秒タイムアウト、タイトル復帰をまとめて回帰確認する。
7. **最終ビルド・後片付け・報告**
   - Dependencies: Step 6。
   - テストドライバ・一時ソース・一時設定を解放・復元し、最終製品コードを再コンパイルしてWindowsビルドを作る。
   - Editor設定と既存資産の保全、Runner/Active参照の残留、保存状態を最終確認する。
   - STEP12のチェック表に実測・未確認・既存警告を分けて記録し、残る手動チェックは一つの案内にまとめる。

# Verification & Testing
## 機能・境界
- 正解100点、不正解0点、未回答0点。正解数と正答率、同点の引き分け、両方の勝利パターンを確認。
- 同時回答と連打でも各人一度。無効インデックス、他プレイヤー扱い、古いMatchId、古い問題番号、受付外、期限tick以上の回答を拒否。
- 片方だけ回答、両方未回答、期限直前、期限同tickを確認。Feedbackへの一度だけの移行と1.5秒後のHost進行を検証。
- 結果パネルの双方数値・勝敗一致、片側再戦は待機、両方同意で初期値0・新ID・第1問。古い回答／再戦通知の混入なし。
- 実測15秒・1.5秒の確認と、tickを模した高速ロジック検証を区別する。テストだけ高速化した状態を本番時間の実測と呼ばない。

## 障害・寿命
- Host/Clientの試合中退出、結果・再戦待ち退出、接続失敗、回線断、遷移中取消、連続再接続。
- 3人目の拒否、問題セット不一致拒否、セッションロック保持。
- 一つのセッションに一つのRunner、同じネットワークRoom、購読・Buttonイベント多重なし。再戦で不要な追加ロード・Runner生成なし。
- 終了後に未観測Task例外や壊れたObject参照、Coroutine、Active singletonが残らない。

## 画面・回帰・完了基準
- 日本語の欠字・切れ・重なり、長い問題文と表示名、スコア・正答率・待機／中断表示の可読性。
- Solo全5問・得点・時間切れ・タイトル復帰が維持される。正常系のコンパイル／実行エラーなし。
- 実2環境での得点・結果・再戦が未確認なら、実装済みでもSTEP12の該当チェックは未完了にする。
- 既存のビルド警告（前回486件）やSDKが異常系で意図的に出すエラーを、正常系の新規不具合と混同しない。警告0や全テスト合格を実測なしで報告しない。

