# QuizBattle STEP7 — Photon Fusion 対戦ルーム・プレイヤー管理

## 前提
- Unity 6000.5.7f1 / Photon Fusion 2.1.4
- STEP1〜5完了、STEP6の接続機能は実装状況を**必ず確認**する。未実装・接続未成功なら、その旨を報告しSTEP6の修正を優先する。
- 既存のSoloモード、QuizGameController、QuizSceneUI、QuizQuestion、QuizQuestionSetとアセットは保護する。

## 目標
同一ルームにHostとClientが最大2人参加し、両者の参加状況を同期表示する。2人揃ったときだけHostが対戦開始を指示できる状態を作る。**問題・タイマー・回答・得点の同期はSTEP8〜9で実装する。**

## 実装指示
1. STEP6で作成されたMatchingScene、NetworkRunner、接続コールバック、ルーム管理クラスを調査し、同じ責務のコンポーネントを重複作成しない。
2. Fusion 2.1.4の実際のAPIを確認して、Host/Clientモードを維持。NetworkRunnerは1セッションに1つを原則とし、作成・Shutdown・シーン遷移時の寿命を明示する。
3. Host権威で参加者スロット（Host / Client）とPlayerRefを管理。OnPlayerJoined/OnPlayerLeftなどのコールバックを適切に利用し、最大2人の制限を適用する。接続上限はセッション設定でも制御する。
4. 表示名は暫定の任意名とし、表示用文字列を検証・長さ制限する。PlayerRefをUIの固定識別子として扱わず、スロットと対応付ける。
5. MatchingSceneに参加人数（0〜2）、参加者名、Host/Clientの区別、接続状態、Host専用「対戦開始」、退出ボタンを表示する。Client側では開始操作を無効化する。
6. Hostは2人が揃ったことを確認してから開始操作を受け付ける。連打・重複開始を防ぐ。開始操作はこのSTEPでは開始予定状態を同期表示するだけとし、QuizSceneへのネットワークシーン遷移はSTEP8に委ねる。
7. Client退出・Host退出・接続失敗時にUIを安全に更新する。Hostが退出した場合のセッション終了とタイトル復帰の方針を定める。ホスト移行は今回対象外。
8. 必要なネットワーク状態はFusionの適切なNetworkObject / NetworkBehaviour等で管理する。Networkedプロパティの書き込み権限、生成と破棄の責任を明示し、UnityのMonoBehaviourだけでネットワーク状態を共有したつもりにならない。
9. SoloモードにPhoton接続を必須にしない。既存のクイズUIとスコア処理を変更しない。

## テスト
- Hostがルームを作成し、Clientが参加できる（STEP6機能の回帰確認）
- 参加人数が両端末で1→2と一致する
- HostとClientの表示が一致する
- 1人の状態では開始不可、2人揃うとHostだけ開始可能
- 3人目の参加は拒否される
- Client退出時に参加人数が更新される
- Host退出時に適切なエラー・退出処理が行われる
- 退出→再接続でRunner重複・イベント重複が起こらない
- Soloモードで5問を最後まで遊べる
- コンパイルエラーなし

## 完了報告
変更ファイル、Hierarchy、ネットワークオブジェクト構成、Runner寿命、StateAuthorityの所在、実機/Editorで確認した項目、未確認項目を報告する。オンライン実機接続を確認できなければ「未検証」と明記する。
