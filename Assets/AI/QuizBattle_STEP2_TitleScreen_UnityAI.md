# QuizBattle --- STEP 2：タイトル画面制作指示書（Unity AI向け）

## 目的

既にSTEP
1（Unityプロジェクト初期構築・フォルダ構成）が完了しているプロジェクトに、タイトル画面を実装する。

**STEP
1で作成済みのプロジェクト、フォルダ、設定を再作成・変更・削除しないこと。**
今回の作業対象はSTEP 2のみ。

## 作業対象

-   既存の `TitleScene`
-   タイトル画面UI
-   タイトル画面からのボタン操作
-   シーン遷移用スクリプト
-   必要に応じたBuild Profiles / Build Settingsの確認

## タイトル画面デザイン

ゲームタイトル：`QUIZ BATTLE`

ボタンを縦方向に配置: 1. 「ひとりで遊ぶ」 2. 「オンライン対戦」 3.
「設定」

画面下部に小さく `Version 0.1.0` を表示する。

### デザイン方針

-   青とオレンジを基調にする
-   シンプルで読みやすく、ゲームらしいデザイン
-   ボタンは押しやすい大きさにし、適切な余白を確保
-   画面中央を基準にレイアウトし、16:9を基準に整える
-   画像素材がない場合はUnity標準UIで作成する
-   外部素材や追加パッケージは勝手に導入しない

## Canvas設定

既存Canvasがある場合は設定を確認して必要な範囲だけ調整する。重複してCanvasを作らない。

推奨: - Render Mode：Screen Space - Overlay - Canvas Scaler：Scale With
Screen Size - Reference Resolution：1920 x 1080 - Screen Match
Mode：Match Width Or Height - Match：0.5

## Hierarchy

既存構成を尊重しつつ、TitleSceneに次のUI要素を用意する。

``` text
TitleScene
├── Main Camera（既存のものを利用）
├── EventSystem（存在しなければ作成）
└── Canvas
    ├── Background
    ├── TitleText
    ├── SoloButton
    ├── OnlineButton
    ├── SettingsButton
    └── VersionText
```

オブジェクト名は可能な限り上記に合わせる。既存オブジェクトを置き換える場合は、重複・破損を避けて慎重に対応する。

## ボタン動作

-   `SoloButton`（表示：ひとりで遊ぶ）→ `QuizScene` をロード
-   `OnlineButton`（表示：オンライン対戦）→ `MatchingScene` をロード
-   `SettingsButton`（表示：設定）→
    今回は設定画面未実装。押すとログに「設定画面は今後実装します。」等のメッセージを出す仮動作

遷移先のシーンがまだ仮状態でも、シーン名を確認し、遷移先が存在しない場合はエラー原因を明示する。遷移先シーンを勝手に新規作成する必要はない（既にSTEP
1で作成済みの想定）。

## スクリプト

`Assets/_Project/Scripts/Core/SceneTransitionManager.cs`

このファイルが既に存在する場合は内容を確認し、必要な変更だけを加える。重複クラスを作らない。

要件: - `using UnityEngine;` - `using UnityEngine.SceneManagement;` -
`SceneTransitionManager` クラス - ボタンから呼べるpublicメソッド: -
`StartSolo()`：`QuizScene` をロード - `StartOnline()`：`MatchingScene`
をロード - `OpenSettings()`：設定画面未実装の仮ログ -
シーン名は可能なら定数化し、文字列を複数箇所に散らさない - シーンがBuild
Profiles / Build
Settingsに登録されていない場合、原因が分かるログを出す -
Null参照や不要な複雑化を避ける - メソッドに簡潔な日本語コメントを付ける

## Button OnClick設定

各ButtonのOnClickイベントに以下を接続する。

-   SoloButton → `SceneTransitionManager.StartSolo()`
-   OnlineButton → `SceneTransitionManager.StartOnline()`
-   SettingsButton → `SceneTransitionManager.OpenSettings()`

SceneTransitionManagerを適切な既存GameObjectにアタッチする。適切なオブジェクトがなければCanvas内または専用管理オブジェクトに配置する。

Unity
AIがEditor上でOnClick設定を直接実施できない場合は、未設定のまま完了扱いにせず、手動設定手順を明確に記載する。

## シーン登録確認

Unityのバージョンに応じてBuild ProfilesまたはBuild
Settingsを確認し、次のシーンが登録されているか確認する。

1.  `TitleScene`
2.  `QuizScene`
3.  `ResultScene`
4.  `MatchingScene`

TitleSceneを起動シーンに設定する。既に正しく設定されている場合は変更不要。

## 作業時の制約

-   STEP 1で作成したフォルダ構成を再作成しない
-   既存のプロジェクト設定を不必要に変更しない
-   今回はクイズ本編、問題データ、タイマー、スコア計算を実装しない
-   通信ライブラリやオンライン対戦通信を導入しない
-   既存ファイルを削除しない
-   作業開始時に対象シーン・関連スクリプトの現状を確認する
-   変更範囲をSTEP 2に限定する

## 完了条件

-   [ ] TitleSceneにタイトル画面が作成されている
-   [ ] `QUIZ BATTLE` のタイトルが表示される
-   [ ] 「ひとりで遊ぶ」ボタンがある
-   [ ] 「オンライン対戦」ボタンがある
-   [ ] 「設定」ボタンがある
-   [ ] Version 0.1.0 が表示される
-   [ ] Canvasの画面サイズ対応設定が確認できる
-   [ ] SceneTransitionManagerが正しく作成または更新されている
-   [ ]
    3つのボタンにOnClickが接続されている（または未設定箇所の手動手順が明示されている）
-   [ ] シーン登録が確認されている
-   [ ] コンパイルエラーがない
-   [ ] ボタン動作をテストし、実際の結果が報告されている

## Unity AIへの依頼

この指示書に従い、既存のUnityプロジェクトにSTEP
2のタイトル画面のみ実装してください。作業前に現状を確認し、STEP
1の成果物を維持してください。

作業終了時には以下を報告してください。 1. 作成・変更したファイル一覧 2.
実施したUnity Editor上の設定 3. ボタンの接続状況 4. シーン登録状況 5.
動作確認の結果 6. 手動対応が必要な項目

実際に実行・確認できていない項目は完了として報告しないでください。
