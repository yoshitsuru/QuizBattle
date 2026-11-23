using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuizSceneUI : MonoBehaviour
{
    [Header("画面の切り替え")]
    [SerializeField] private GameObject _gameplayRoot;
    [SerializeField] private GameObject _completePanel;
    [SerializeField] private UnityEngine.EventSystems.EventSystem _eventSystem;

    [Header("問題とタイマー")]
    [SerializeField] private TMPro.TMP_Text _questionCountText;
    [SerializeField] private TMPro.TMP_Text _scoreText;
    [SerializeField] private TMPro.TMP_Text _questionText;
    [SerializeField] private TMPro.TMP_Text _timerText;
    [SerializeField] private TMPro.TMP_Text _feedbackText;
    [SerializeField] private UnityEngine.UI.Image _timerBar;

    [Header("4つの回答")]
    [SerializeField] private UnityEngine.UI.Button[] _answerButtons;
    [SerializeField] private TMPro.TMP_Text[] _answerLabels;

    [Header("終了とタイトル戻り")]
    [SerializeField] private TMPro.TMP_Text _completeTitleText;
    [SerializeField] private TMPro.TMP_Text _finalScoreText;
    [SerializeField] private TMPro.TMP_Text _correctCountText;
    [SerializeField] private TMPro.TMP_Text _accuracyText;
    [SerializeField] private UnityEngine.UI.Button _returnTitleButton;

    [Header("判定色（テキストも併用）")]
    [SerializeField] private Color _correctColor = new Color(0.3f, 0.95f, 0.7f);
    [SerializeField] private Color _incorrectColor = new Color(1f, 0.65f, 0.4f);
    // タイマー文字列は整数秒が変わった場合だけ更新する。
    private int _displayedSeconds = -1;

    public bool ValidateReferences(out string missing)
    {
        // Inspectorの未設定項目をまとめて提示し、問題表示を開始する前に検出する。
        var list = new StringBuilder();
        if (_gameplayRoot == null) list.AppendLine(nameof(_gameplayRoot));
        if (_completePanel == null) list.AppendLine(nameof(_completePanel));
        if (_eventSystem == null) list.AppendLine(nameof(_eventSystem));
        if (_questionCountText == null) list.AppendLine(nameof(_questionCountText));
        if (_scoreText == null) list.AppendLine(nameof(_scoreText));
        if (_questionText == null) list.AppendLine(nameof(_questionText));
        if (_timerText == null) list.AppendLine(nameof(_timerText));
        if (_feedbackText == null) list.AppendLine(nameof(_feedbackText));
        if (_completeTitleText == null) list.AppendLine(nameof(_completeTitleText));
        if (_finalScoreText == null) list.AppendLine(nameof(_finalScoreText));
        if (_correctCountText == null) list.AppendLine(nameof(_correctCountText));
        if (_accuracyText == null) list.AppendLine(nameof(_accuracyText));
        if (_returnTitleButton == null) list.AppendLine(nameof(_returnTitleButton));
        if (_answerButtons == null || _answerButtons.Length != 4) list.AppendLine("_answerButtons（4個）");
        else for (int i = 0; i < 4; i++) if (_answerButtons[i] == null) list.AppendLine($"_answerButtons[{i}]");
        if (_answerLabels == null || _answerLabels.Length != 4) list.AppendLine("_answerLabels（4個）");
        else for (int i = 0; i < 4; i++) if (_answerLabels[i] == null) list.AppendLine($"_answerLabels[{i}]");
        missing = list.ToString();
        return list.Length == 0;
    }

    public void ShowQuestion(QuizQuestion question, int number, int total, int score)
    {
        // 問題の順番とゲーム状態はControllerに任せ、ここでは表示だけを更新する。
        _completePanel.SetActive(false);
        _gameplayRoot.SetActive(true);
        _questionCountText.text = $"問題 {number} / {total}";
        _scoreText.text = $"スコア：{score}";
        _questionText.text = question.QuestionText;
        _feedbackText.text = string.Empty;
        _displayedSeconds = -1;
        for (int i = 0; i < 4; i++)
        {
            _answerLabels[i].text = $"{i + 1}. {question.GetChoice(i)}";
            _answerLabels[i].color = Color.white;
            _answerButtons[i].interactable = true;
        }
        _eventSystem.SetSelectedGameObject(_answerButtons[0].gameObject);
    }

    public void UpdateTimer(float remaining, float duration)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        if (_displayedSeconds != seconds)
        {
            _displayedSeconds = seconds;
            _timerText.text = $"残り時間：{seconds}秒";
        }
        // バーは任意参照。未設定でも文字のカウントダウンは継続できる。
        if (_timerBar != null) _timerBar.fillAmount = duration > 0 ? Mathf.Clamp01(remaining / duration) : 0;
    }

    public void SetAnswersEnabled(bool enabled)
    {
        // 異常データや未設定UIの場合も、存在する回答ボタンだけを安全にロックする。
        if (_answerButtons == null) return;
        foreach (UnityEngine.UI.Button button in _answerButtons)
            if (button != null) button.interactable = enabled;
    }

    public void ShowFeedback(QuizQuestion question, bool correct, bool timedOut, int score)
    {
        SetAnswersEnabled(false);
        _scoreText.text = $"スコア：{score}";
        _feedbackText.color = correct ? _correctColor : _incorrectColor;
        string status = timedOut ? "時間切れ" : correct ? "正解！" : "不正解";
        _feedbackText.text = $"{status}　正解：{question.GetChoice(question.CorrectAnswerIndex)}";
        // 無効ボタンの背景色だけに頼らず、正解ラベルを強調する。
        _answerLabels[question.CorrectAnswerIndex].color = _correctColor;
    }

    public void ShowComplete(int score, int correctCount, int total)
    {
        SetAnswersEnabled(false);
        _gameplayRoot.SetActive(false);
        _completePanel.SetActive(true);
        _completeTitleText.text = "クイズ終了！";
        _finalScoreText.gameObject.SetActive(true);
        _correctCountText.gameObject.SetActive(true);
        _accuracyText.gameObject.SetActive(true);
        _finalScoreText.text = $"最終得点：{score}点";
        _correctCountText.text = $"出題数：{total}問　正解数：{correctCount}問";
        _accuracyText.text = $"正答率：{(total > 0 ? 100f * correctCount / total : 0f):0.#}%";
        SelectReturnButton();
    }

    public void ShowError(string message)
    {
        // 必須参照不足でも残っているパネルで案内し、NullReferenceExceptionを避ける。
        SetAnswersEnabled(false);
        if (_gameplayRoot != null) _gameplayRoot.SetActive(false);
        if (_completePanel != null) _completePanel.SetActive(true);
        if (_completeTitleText != null) _completeTitleText.text = "クイズを開始できません\n" + message;
        if (_finalScoreText != null) _finalScoreText.gameObject.SetActive(false);
        if (_correctCountText != null) _correctCountText.gameObject.SetActive(false);
        if (_accuracyText != null) _accuracyText.gameObject.SetActive(false);
        SelectReturnButton();
    }

    private void SelectReturnButton()
    {
        if (_returnTitleButton == null) return;
        _returnTitleButton.interactable = true;
        if (_eventSystem != null) _eventSystem.SetSelectedGameObject(_returnTitleButton.gameObject);
    }
}
