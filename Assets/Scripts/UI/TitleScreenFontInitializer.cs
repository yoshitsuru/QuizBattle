using UnityEngine;

[DisallowMultipleComponent]
public sealed class TitleScreenFontInitializer : MonoBehaviour
{
    [Header("タイトル画面の文字")]
    [Tooltip("起動時に日本語フォントを割り当てるText。シーン内の参照を登録します。")]
    [SerializeField] private UnityEngine.UI.Text[] _texts;

    [Header("Windows標準フォントの候補")]
    [Tooltip("上から順にインストール状態と日本語の表示可否を調べます。")]
    [SerializeField] private string[] _fontCandidates = { "Meiryo", "Yu Gothic", "MS Gothic" };

    // 日本語ラベルに必要な文字を確認し、欠字のある候補を採用しない。
    private const string RequiredJapaneseCharacters = "ひとりで遊ぶオンライン対戦設定";
    // 自分で生成したフォントだけを終了時に破棄するため、参照を保持する。
    private Font _runtimeFont;

    private void Awake()
    {
        // Inspector参照が欠けている場合は、理由を示して安全に初期化を中止する。
        if (_texts == null || _texts.Length == 0 || _fontCandidates == null)
        {
            Debug.LogWarning("タイトル画面のText参照またはOSフォント候補が未設定です。", this);
            return;
        }

        // OS側のフォント一覧は起動時だけ取得し、毎フレームの検索を避ける。
        string[] installedFonts = Font.GetOSInstalledFontNames();
        foreach (string candidate in _fontCandidates)
        {
            if (string.IsNullOrEmpty(candidate) || System.Array.IndexOf(installedFonts, candidate) < 0)
            {
                continue;
            }

            // 外部フォントファイルを同梱せず、Windowsにある日本語フォントを生成する。
            Font font = Font.CreateDynamicFontFromOSFont(candidate, 40);
            if (font == null)
            {
                continue;
            }

            font.RequestCharactersInTexture(RequiredJapaneseCharacters, 40, FontStyle.Normal);
            bool hasAllCharacters = true;
            foreach (char character in RequiredJapaneseCharacters)
            {
                if (!font.HasCharacter(character))
                {
                    hasAllCharacters = false;
                    break;
                }
            }

            // 不適合な候補はこの場で解放し、次のフォントを試す。
            if (!hasAllCharacters)
            {
                Destroy(font);
                continue;
            }

            _runtimeFont = font;
            foreach (UnityEngine.UI.Text text in _texts)
            {
                // 個々の未設定参照はスキップし、他のラベルの初期化は続ける。
                if (text == null)
                {
                    Debug.LogWarning("タイトル画面に未設定のText参照があります。", this);
                    continue;
                }
                text.font = _runtimeFont;
            }
            return;
        }

        // 対象PCに候補がないと日本語が表示できないため、配布先での確認を促す。
        Debug.LogWarning(
            "日本語を表示できるWindows標準フォントが見つかりません。" +
            "Meiryo / Yu Gothic / MS Gothicのインストール状態を確認してください。",
            this);
    }

    private void OnDestroy()
    {
        // シーン終了時に自分が生成したフォントだけを解放し、アセットは破棄しない。
        if (_runtimeFont != null)
        {
            Destroy(_runtimeFont);
            _runtimeFont = null;
        }
    }
}
