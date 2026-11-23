using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneTransitionManager : MonoBehaviour
{
    // 遷移先の名前を一か所にまとめ、ボタンごとの文字列の書き間違いを防ぐ。
    private const string SoloSceneName = "QuizScene";
    private const string OnlineSceneName = "MatchingScene";

    // 「ひとりで遊ぶ」ボタンから、クイズ本編のシーンへの遷移を要求する。
    public void StartSolo()
    {
        LoadSceneIfAvailable(SoloSceneName);
    }

    // 「オンライン対戦」ボタンから、マッチングシーンへの遷移を要求する。
    public void StartOnline()
    {
        LoadSceneIfAvailable(OnlineSceneName);
    }

    // 設定画面は後続STEPで実装するため、今回は案内ログだけを出す。
    public void OpenSettings()
    {
        Debug.Log("設定画面は今後実装します。", this);
    }

    private void LoadSceneIfAvailable(string sceneName)
    {
        // ファイル不存在やBuild Profilesの有効シーンへの未登録を検出し、ロード例外を防ぐ。
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"シーン「{sceneName}」をロードできません。シーンファイルが存在しない、" +
                "またはBuild Profiles / Build Settingsの有効なシーンリストに登録されていない可能性があります。" +
                "後続STEPで対象シーンを用意し、シーン名と有効なビルド登録を確認してください。タイトル画面に留まります。",
                this);
            return;
        }

        // 読み込み可能な場合だけ現在のタイトルシーンを遷移先に切り替える。
        SceneManager.LoadScene(sceneName);
    }
}
