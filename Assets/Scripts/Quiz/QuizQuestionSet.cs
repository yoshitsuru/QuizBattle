using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuizQuestionSet", menuName = "QuizBattle/Quiz Question Set")]
public sealed class QuizQuestionSet : ScriptableObject
{
    [Header("問題セットの識別")]
    [Tooltip("問題セットを識別するID。例: SET001。")]
    [SerializeField] private string setId = string.Empty;

    [Tooltip("Inspectorや管理画面で使用するセット名です。")]
    [SerializeField] private string displayName = string.Empty;

    [Header("出題候補の一覧")]
    [Tooltip("問題アセットを登録します。空の要素や同一問題IDの重複は警告されます。")]
    [SerializeField] private QuizQuestion[] questions = Array.Empty<QuizQuestion>();

    // 未設定文字列は空文字で読み取り、保存値はそのまま保持する。
    public string SetId => setId ?? string.Empty;
    public string DisplayName => displayName ?? string.Empty;

    // 登録スロット数を返す。nullスロットも含め、Inspectorの配列順と対応させる。
    public int QuestionCount => questions == null ? 0 : questions.Length;

    // 元の参照配列を公開せず、null配列は空の読み取り専用一覧として渡す。
    public IReadOnlyList<QuizQuestion> Questions => Array.AsReadOnly(questions ?? Array.Empty<QuizQuestion>());

    // 範囲外と空スロットはnullで返す。削除や並べ替え、抽出はこのデータクラスでは行わない。
    public QuizQuestion GetQuestion(int index)
    {
        if (questions == null || index < 0 || index >= questions.Length)
        {
            return null;
        }

        return questions[index];
    }

    private void OnValidate()
    {
        // 未設定の配列でも例外を起こさず、作成直後の空セットを許容する。
        if (questions == null)
        {
            return;
        }

        // IDは大文字・小文字を区別して比較し、問題アセットの登録順は変更しない。
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < questions.Length; i++)
        {
            QuizQuestion question = questions[i];
            if (question == null)
            {
                Debug.LogWarning($"QuizQuestionSet: 問題一覧の要素{i}がnullです。問題アセットを登録してください。", this);
                continue;
            }

            // 空IDは問題側の検証に任せ、有効なIDだけを重複検査する。
            string id = question.QuestionId;
            if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
            {
                Debug.LogWarning($"QuizQuestionSet: 問題ID「{id}」が重複しています（要素{i}）。", this);
            }
        }
    }
}
