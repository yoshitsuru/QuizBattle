using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class QuizGameController : MonoBehaviour
{
    // 回答受付とフィードバックを分け、1問を二重に確定させない。
    public enum GameState { Inactive, AwaitingAnswer, Feedback, Complete, Error }
    private const float TimeLimit = 15f;
    private const float FeedbackDuration = 1.5f;
    private const int CorrectPoints = 100;
    private const string TitleSceneName = "TitleScene";

    [Header("問題データと表示")]
    [Tooltip("登録順で出題する問題セットです。")]
    [SerializeField] private QuizQuestionSet _questionSet;
    [Tooltip("同じシーンのQuizSceneUIを指定します。")]
    [SerializeField] private QuizSceneUI _sceneUI;

    // 元のセットは変更せず、有効な参照だけを作業用リストに保持する。
    private readonly List<QuizQuestion> _questions = new List<QuizQuestion>();
    private GameState _state;
    private int _currentIndex;
    private int _score;
    private int _correctCount;
    private double _deadline;
    private double _nextQuestionTime;
    private float _remainingTime;

    // 外部には観測用の読み取りだけを公開する。
    public GameState State => _state;
    public int CurrentIndex => _currentIndex;
    public int Score => _score;
    public int CorrectCount => _correctCount;
    public int TotalQuestions => _questions.Count;
    public float RemainingTime => _remainingTime;

    private void Start()
    {
        if (_sceneUI == null) { Fail("Inspectorの_sceneUIが未設定です。"); return; }
        if (!_sceneUI.ValidateReferences(out string missing))
        { Fail("QuizSceneUIの参照が不足しています：\n" + missing); return; }
        if (_questionSet == null) { Fail("Inspectorの_questionSetが未設定です。"); return; }

        // データの欠損はスロット番号と理由を通知し、元アセットを補正しない。
        var ids = new HashSet<string>();
        for (int i = 0; i < _questionSet.QuestionCount; i++)
        {
            QuizQuestion question = _questionSet.GetQuestion(i);
            string reason = GetInvalidReason(question);
            if (reason == null && !ids.Add(question.QuestionId)) reason = "問題IDが重複しています。";
            if (reason != null)
            {
                Debug.LogWarning($"QuizGameController: 問題スロット{i}をスキップ：{reason}", this);
                continue;
            }
            _questions.Add(question);
        }
        if (_questions.Count == 0) { Fail("問題セットに有効な問題がありません。"); return; }
        _score = 0;
        _correctCount = 0;
        _currentIndex = 0;
        ShowCurrentQuestion();
    }

    private void Update()
    {
        // 時刻差で残り時間を計算し、フレーム落ちやTime.timeScaleの影響を避ける。
        double now = Time.unscaledTimeAsDouble;
        if (_state == GameState.AwaitingAnswer)
        {
            _remainingTime = Mathf.Max(0, (float)(_deadline - now));
            _sceneUI.UpdateTimer(_remainingTime, TimeLimit);
            if (now >= _deadline) ResolveAnswer(-1, true);
        }
        else if (_state == GameState.Feedback && now >= _nextQuestionTime)
        {
            _currentIndex++;
            if (_currentIndex >= _questions.Count)
            {
                _state = GameState.Complete;
                _sceneUI.ShowComplete(_score, _correctCount, _questions.Count);
            }
            else ShowCurrentQuestion();
        }
    }

    // 各回答ボタンから0〜3の固定indexを受け取る。
    public void SubmitAnswer(int choiceIndex)
    {
        if (!isActiveAndEnabled || _state != GameState.AwaitingAnswer) return;
        // Updateより先にクリックが処理されても、期限以上なら時間切れを優先する。
        if (Time.unscaledTimeAsDouble >= _deadline) { ResolveAnswer(-1, true); return; }
        if (choiceIndex < 0 || choiceIndex >= 4) return;
        ResolveAnswer(choiceIndex, false);
    }

    private void ResolveAnswer(int choiceIndex, bool timedOut)
    {
        if (_state != GameState.AwaitingAnswer) return;
        // 加点より先に状態を確定し、連打・タイマーとの競合を遮断する。
        _state = GameState.Feedback;
        QuizQuestion question = _questions[_currentIndex];
        if (GetInvalidReason(question) != null) { Fail("出題中の問題データが無効になりました。"); return; }
        bool correct = !timedOut && choiceIndex == question.CorrectAnswerIndex;
        if (correct) { _score += CorrectPoints; _correctCount++; }
        if (timedOut) { _remainingTime = 0; _sceneUI.UpdateTimer(0, TimeLimit); }
        _sceneUI.ShowFeedback(question, correct, timedOut, _score);
        _nextQuestionTime = Time.unscaledTimeAsDouble + FeedbackDuration;
    }

    private void ShowCurrentQuestion()
    {
        QuizQuestion question = _questions[_currentIndex];
        string reason = GetInvalidReason(question);
        if (reason != null) { Fail("出題する問題が無効です：" + reason); return; }
        _state = GameState.AwaitingAnswer;
        _remainingTime = TimeLimit;
        _deadline = Time.unscaledTimeAsDouble + TimeLimit;
        _sceneUI.ShowQuestion(question, _currentIndex + 1, _questions.Count, _score);
        _sceneUI.UpdateTimer(TimeLimit, TimeLimit);
    }

    private static string GetInvalidReason(QuizQuestion question)
    {
        if (question == null) return "問題参照がnullです。";
        if (string.IsNullOrWhiteSpace(question.QuestionId)) return "問題IDが空です。";
        if (string.IsNullOrWhiteSpace(question.QuestionText)) return "問題文が空です。";
        if (question.ChoiceCount != 4) return "選択肢が4個ではありません。";
        if (question.CorrectAnswerIndex < 0 || question.CorrectAnswerIndex > 3) return "正解番号が0〜3の範囲外です。";
        for (int i = 0; i < 4; i++) if (string.IsNullOrWhiteSpace(question.GetChoice(i))) return $"選択肢{i}が空です。";
        return null;
    }

    private void Fail(string message)
    {
        _state = GameState.Error;
        Debug.LogError("QuizGameController: " + message, this);
        if (_sceneUI != null) _sceneUI.ShowError(message);
    }

    // 終了／エラー画面のボタンからタイトルへ戻る。
    public void ReturnToTitle()
    {
        if (!Application.CanStreamedLevelBeLoaded(TitleSceneName))
        { Fail("TitleSceneをロードできません。Build Profilesの有効な登録を確認してください。"); return; }
        _state = GameState.Inactive;
        if (_sceneUI != null) _sceneUI.SetAnswersEnabled(false);
        SceneManager.LoadScene(TitleSceneName);
    }

    private void OnDisable()
    {
        // シーン離脱や無効化後は回答も期限処理も行わない。
        _state = GameState.Inactive;
        if (_sceneUI != null) _sceneUI.SetAnswersEnabled(false);
    }
}
