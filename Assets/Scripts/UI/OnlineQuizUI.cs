using UnityEngine;

[DisallowMultipleComponent]
public sealed class OnlineQuizUI : MonoBehaviour
{
    [Header("問題表示")]
    [SerializeField] private TMPro.TMP_Text _questionText;
    [SerializeField] private TMPro.TMP_Text[] _choiceTexts;
    [SerializeField] private TMPro.TMP_Text _questionCountText;
    [SerializeField] private TMPro.TMP_Text _timerText;
    [SerializeField] private UnityEngine.UI.Image _timerFill;
    [Header("状態と退出")]
    [SerializeField] private TMPro.TMP_Text _stateText;
    [SerializeField] private UnityEngine.UI.Button _exitButton;
    [Header("回答と得点")]
    [SerializeField] private UnityEngine.UI.Button[] _choiceButtons;
    [SerializeField] private TMPro.TMP_Text _hostScoreText;
    [SerializeField] private TMPro.TMP_Text _clientScoreText;
    private readonly UnityEngine.Events.UnityAction[] _answerHandlers = new UnityEngine.Events.UnityAction[4];
    [Header("オンライン結果・中断")]
    [SerializeField] private GameObject _gameplayRoot;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TMPro.TMP_Text _resultTitle;
    [SerializeField] private TMPro.TMP_Text _hostResultText;
    [SerializeField] private TMPro.TMP_Text _clientResultText;
    [SerializeField] private TMPro.TMP_Text _rematchStatusText;
    [SerializeField] private UnityEngine.UI.Button _rematchButton;
    [SerializeField] private UnityEngine.UI.Button _resultExitButton;
    private string _shownMatchId;

    private PhotonConnectionManager _connection;
    private OnlineQuizMatchController _match;
    private int _shownQuestion = -2, _shownSeconds = -1;
    private OnlineQuizMatchController.MatchPhase _shownPhase;
    private bool _hasPhase;

    private void Awake()
    {
        _connection = PhotonConnectionManager.Active;
        if (_questionText == null || _choiceTexts == null || _choiceTexts.Length != 4 || _questionCountText == null || _timerText == null || _timerFill == null || _stateText == null || _exitButton == null ||
            _choiceButtons == null || _choiceButtons.Length != 4 || _hostScoreText == null || _clientScoreText == null || _gameplayRoot == null || _resultPanel == null ||
            _resultTitle == null || _hostResultText == null || _clientResultText == null || _rematchStatusText == null || _rematchButton == null || _resultExitButton == null)
        { Debug.LogError("OnlineQuizUIの参照が不足しています。", this); enabled = false; return; }
        foreach (var choice in _choiceTexts) if (choice == null) { Debug.LogError("オンラインの選択肢参照が不足しています。", this); enabled = false; return; }
        foreach (var button in _choiceButtons) if (button == null) { Debug.LogError("オンライン回答ボタンの参照が不足しています。", this); enabled = false; return; }
        _resultTitle.richText = false; _hostResultText.richText = false; _clientResultText.richText = false; _rematchStatusText.richText = false;
        _questionText.richText = false;
        foreach (var choice in _choiceTexts) choice.richText = false;
        _stateText.richText = false;
    }
    private void OnEnable()
    {
        if (_exitButton != null) _exitButton.onClick.AddListener(Exit);
        if (_resultExitButton != null) _resultExitButton.onClick.AddListener(Exit);
        if (_rematchButton != null) _rematchButton.onClick.AddListener(RequestRematch);
        if (_choiceButtons == null || _choiceButtons.Length != 4) return;
        for (int i = 0; i < 4; i++)
        {
            int choice = i;
            _answerHandlers[i] = () => { if (_match != null) _match.SubmitAnswer(choice); };
            if (_choiceButtons[i] != null) _choiceButtons[i].onClick.AddListener(_answerHandlers[i]);
        }
    }
    private void OnDisable()
    {
        if (_exitButton != null) _exitButton.onClick.RemoveListener(Exit);
        if (_resultExitButton != null) _resultExitButton.onClick.RemoveListener(Exit);
        if (_rematchButton != null) _rematchButton.onClick.RemoveListener(RequestRematch);
        if (_choiceButtons == null) return;
        for (int i = 0; i < Mathf.Min(4, _choiceButtons.Length); i++)
            if (_choiceButtons[i] != null && _answerHandlers[i] != null) _choiceButtons[i].onClick.RemoveListener(_answerHandlers[i]);
    }
    private void Exit()
    {
        if (_connection != null) _connection.ExitToTitle();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }
    private void RequestRematch() { if (_match != null) _match.RequestRematch(); }
    private void ShowResult(string error = null)
    {
        if (_gameplayRoot != null) _gameplayRoot.SetActive(false);
        if (_resultPanel != null) _resultPanel.SetActive(true);
        bool completed = error == null && _match != null && _match.Phase == OnlineQuizMatchController.MatchPhase.Complete;
        _rematchButton.interactable = completed && _match.CanRequestRematch;
        _resultExitButton.interactable = _connection == null || !_connection.IsBusy;
        if (!completed)
        {
            _resultTitle.text = "対戦中断";
            _hostResultText.text = string.Empty; _clientResultText.text = string.Empty;
            _rematchStatusText.text = string.IsNullOrEmpty(error) ? "接続状態を確認できません。タイトルへ戻って再接続してください。" : error;
            return;
        }
        bool isHost = _match.LocalSlot == 0;
        var outcome = _match.Outcome;
        _resultTitle.text = outcome == OnlineMatchOutcome.Draw ? "引き分け" : (isHost && outcome == OnlineMatchOutcome.HostWin) || (!isHost && outcome == OnlineMatchOutcome.ClientWin) ? "あなたの勝利！" : "あなたの敗北";
        var room = _connection.Room;
        _hostResultText.text = room.HostDisplayName + " (Host)\n" + _match.HostScore + "点\n正解 " + _match.HostCorrectCount + " / 5\n正答率 " + (_match.HostCorrectCount * 20) + "%";
        _clientResultText.text = room.ClientDisplayName + " (Client)\n" + _match.ClientScore + "点\n正解 " + _match.ClientCorrectCount + " / 5\n正答率 " + (_match.ClientCorrectCount * 20) + "%";
        _rematchStatusText.text = _match.LocalRematchRequested ? "あなたは再戦希望です。相手の同意を待っています…" : (_match.HostRematchRequested || _match.ClientRematchRequested) ? "相手は再戦を希望しています。" : "双方が再戦を希望すると、新しい試合を開始します。";
    }
    private static string AnswerLabel(OnlineAnswerResult result)
    {
        switch (result)
        {
            case OnlineAnswerResult.Correct: return "正解！";
            case OnlineAnswerResult.Incorrect: return "不正解";
            case OnlineAnswerResult.Timeout: return "時間切れ";
            default: return "未回答";
        }
    }
    private void Update()
    {
        if (_choiceButtons != null)foreach (var button in _choiceButtons) if (button != null) button.interactable = false;
        if (_connection == null || _connection.State != PhotonConnectionManager.ConnectionState.Connected)
        {
            _timerFill.fillAmount = 0;
            _timerText.text = "—";
            _stateText.text = _connection == null ? "オンライン接続がありません。タイトルから接続してください。" : _connection.ErrorMessage;
            ShowResult(_stateText.text);
            return;
        }
        _match = _connection.OnlineMatch;
        if (_match == null) { _stateText.text = "ネットワークの試合状態を待っています…"; return; }
        if (_match.Phase == OnlineQuizMatchController.MatchPhase.Complete) { ShowResult(); return; }
        if (_match.Phase == OnlineQuizMatchController.MatchPhase.Aborted || _match.Phase == OnlineQuizMatchController.MatchPhase.DataMismatch) { ShowResult(_match.FailureMessage.ToString()); return; }
        _resultPanel.SetActive(false); _gameplayRoot.SetActive(true);
        if (_shownMatchId != _match.MatchId.ToString())
        {
            _shownMatchId = _match.MatchId.ToString(); _shownQuestion = -2; _hasPhase = false;
            _questionText.text = "新しい試合の画面準備を待っています…";
            _questionCountText.text = "問題 — / 5";
        }
        // 両端末のUIが準備できるまで、Hostは15秒タイマーを開始しない。
        _match.ReportLocalSceneReady();
        var room = _connection.Room;
        if (_hostScoreText != null) _hostScoreText.text = room.HostDisplayName + "：" + _match.HostScore + "点 / " + _match.HostCorrectCount + "正解";
        if (_clientScoreText != null) _clientScoreText.text = room.ClientDisplayName + "：" + _match.ClientScore + "点 / " + _match.ClientCorrectCount + "正解";
        if (_choiceButtons != null) for (int i = 0; i < _choiceButtons.Length; i++)
        {
            if (_choiceButtons[i] == null) continue;
            _choiceButtons[i].interactable = _match.CanLocalAnswer;
            _choiceButtons[i].image.color = _match.Phase == OnlineQuizMatchController.MatchPhase.Feedback && i == _match.CorrectChoice ? new Color(.15f,.6f,.35f) : new Color(.19f,.3f,.46f);
        }
        if (_match.Phase == OnlineQuizMatchController.MatchPhase.Question)
            _stateText.text = _match.LocalAnswered ? "回答済み：相手の回答を待っています。" : _match.CanLocalAnswer ? "選択肢を一つ選んでください。" : "回答を送信中…";
        if (!_hasPhase || _shownPhase != _match.Phase)
        {
            _hasPhase = true; _shownPhase = _match.Phase;
            switch (_match.Phase)
            {
                case OnlineQuizMatchController.MatchPhase.Question: _stateText.text = "選択肢を一つ選んでください。"; break;
                case OnlineQuizMatchController.MatchPhase.Feedback:
                    _stateText.text = "Host：" + AnswerLabel(_match.HostAnswerResult) + " / Client：" + AnswerLabel(_match.ClientAnswerResult) + "\n正解：" + _match.CurrentQuestion.GetChoice(_match.CorrectChoice); break;
                case OnlineQuizMatchController.MatchPhase.Complete: _stateText.text = "対戦が終了しました。"; break;
                case OnlineQuizMatchController.MatchPhase.Aborted:
                case OnlineQuizMatchController.MatchPhase.DataMismatch: _stateText.text = _match.FailureMessage.ToString(); break;
                default: _stateText.text = "両プレイヤーの画面準備を待っています…"; break;
            }
        }
        if (_match.Phase == OnlineQuizMatchController.MatchPhase.Question && _shownQuestion != _match.QuestionIndex)
        {
            var question = _match.CurrentQuestion;
            if (question == null || !string.Equals(question.QuestionId, _match.QuestionId.ToString(), System.StringComparison.Ordinal))
            { _stateText.text = "問題IDの不一致を検出しました。対戦を中止して再接続してください。"; return; }
            _shownQuestion = _match.QuestionIndex;
            _questionText.text = question.QuestionText;
            for (int i = 0; i < 4; i++) _choiceTexts[i].text = question.GetChoice(i);
            _questionCountText.text = "問題 " + _match.QuestionNumber + " / " + OnlineQuizData.QuestionCount;
        }
        // 表示もTickTimerから読み取り、ローカルTime.timeを締切の根拠にしない。
        float remaining = _match.Phase == OnlineQuizMatchController.MatchPhase.Question ? _match.RemainingSeconds : 0;
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds != _shownSeconds) { _shownSeconds = seconds; _timerText.text = seconds + "秒"; }
        _timerFill.fillAmount = Mathf.Clamp01(remaining / OnlineQuizData.QuestionSeconds);
    }
}
