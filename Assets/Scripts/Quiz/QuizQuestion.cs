using System;
using System.Collections.Generic;
using UnityEngine;

// 問題の難易度。保存値を明示し、後続工程でも同じ値で扱えるようにする。
public enum QuizDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

[CreateAssetMenu(fileName = "QuizQuestion", menuName = "QuizBattle/Quiz Question")]
public sealed class QuizQuestion : ScriptableObject
{
    // 4択の枠数と正解番号の上限に同じ基準を使う。
    private const int RequiredChoiceCount = 4;

    [Header("問題の識別と本文")]
    [Tooltip("問題を識別するID。例: Q001。同じセット内では重複させません。")]
    [SerializeField] private string questionId = string.Empty;

    [Tooltip("プレイヤーに表示する問題文です。")]
    [TextArea(3, 6)]
    [SerializeField] private string questionText = string.Empty;

    [Header("4つの選択肢と正解")]
    [Tooltip("4個の選択肢。要素0が選択肢1、要素3が選択肢4です。")]
    [SerializeField] private string[] choices = new string[RequiredChoiceCount];

    [Tooltip("正解の選択肢番号（0〜3）。0が選択肢1、3が選択肢4です。")]
    [SerializeField] private int correctAnswerIndex;

    [Header("分類と解説")]
    [Tooltip("問題のジャンル名。例: 地理、科学、一般常識。")]
    [SerializeField] private string category = string.Empty;

    [Tooltip("問題の難易度です。")]
    [SerializeField] private QuizDifficulty difficulty = QuizDifficulty.Easy;

    [Tooltip("正解の解説。任意ですが、サンプル問題では必ず設定します。")]
    [TextArea(2, 5)]
    [SerializeField] private string explanation = string.Empty;

    // 文字列が未設定でも呼び出し側にnullを返さず、保存値は変更しない。
    public string QuestionId => questionId ?? string.Empty;
    public string QuestionText => questionText ?? string.Empty;
    public string Category => category ?? string.Empty;
    public string Explanation => explanation ?? string.Empty;
    public QuizDifficulty Difficulty => difficulty;

    // 不正な保存値を黙って補正しない。正解番号の範囲はOnValidateで警告する。
    public int CorrectAnswerIndex => correctAnswerIndex;
    public int ChoiceCount => choices == null ? 0 : choices.Length;

    // 配列そのものを公開せず、読み取り専用の一覧として渡す。個別のnull要素はGetChoiceで安全に読める。
    public IReadOnlyList<string> Choices => Array.AsReadOnly(choices ?? Array.Empty<string>());

    // 範囲外や未設定の選択肢は空文字で返し、外部から内容を書き換える経路は提供しない。
    public string GetChoice(int index)
    {
        if (choices == null || index < 0 || index >= choices.Length)
        {
            return string.Empty;
        }

        return choices[index] ?? string.Empty;
    }

    private void OnValidate()
    {
        // 編集途中の不足を警告するだけに留め、入力内容を削除・補正しない。
        if (string.IsNullOrWhiteSpace(questionId))
        {
            Debug.LogWarning("QuizQuestion: 問題IDが空です。", this);
        }

        if (string.IsNullOrWhiteSpace(questionText))
        {
            Debug.LogWarning("QuizQuestion: 問題文が空です。", this);
        }

        // 選択肢数が違っていても自動リサイズせず、制作者に修正を促す。
        if (choices == null || choices.Length != RequiredChoiceCount)
        {
            Debug.LogWarning($"QuizQuestion: 選択肢は4個にしてください（現在{ChoiceCount}個）。", this);
        }

        if (choices != null)
        {
            for (int i = 0; i < choices.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(choices[i]))
                {
                    Debug.LogWarning($"QuizQuestion: 選択肢{i + 1}（index {i}）が空です。", this);
                }
            }
        }

        // 番号は0始まり。範囲外でもClampせず、そのまま保持して問題を見える状態にする。
        if (correctAnswerIndex < 0 || correctAnswerIndex >= RequiredChoiceCount)
        {
            Debug.LogWarning($"QuizQuestion: 正解番号は0〜3で指定してください（現在{correctAnswerIndex}）。", this);
        }

        // 不正なenumの保存値も通知し、勝手にEasyへ変更しない。
        if (!Enum.IsDefined(typeof(QuizDifficulty), difficulty))
        {
            Debug.LogWarning($"QuizQuestion: 難易度の値が不正です（現在{(int)difficulty}）。", this);
        }
    }
}
