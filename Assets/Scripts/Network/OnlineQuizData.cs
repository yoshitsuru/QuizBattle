using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class OnlineQuizData
{
    public const int QuestionCount = 5;
    public const float QuestionSeconds = 15f;

    public static bool TryCreateSignature(QuizQuestionSet set, out string hash, out string error)
    {
        hash = string.Empty;
        error = string.Empty;
        if (set == null || string.IsNullOrWhiteSpace(set.SetId) || set.SetId.Length > 32 || set.QuestionCount < QuestionCount)
        {
            error = "問題セットのIDまたは問題数が不正です。IDは32文字以内、問題は5問以上必要です。";
            return false;
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            // スキーマ番号と文字列長を含めたバイナリ形式で、連結による曖昧な一致を防ぐ。
            writer.Write(1);
            writer.Write(set.SetId);
            writer.Write(set.QuestionCount);
            for (int i = 0; i < set.QuestionCount; i++)
            {
                var question = set.GetQuestion(i);
                if (question == null || string.IsNullOrWhiteSpace(question.QuestionId) || question.QuestionId.Length > 32 ||
                    !ids.Add(question.QuestionId) || string.IsNullOrWhiteSpace(question.QuestionText) || question.ChoiceCount != 4 ||
                    question.CorrectAnswerIndex < 0 || question.CorrectAnswerIndex > 3)
                {
                    error = "問題セットの要素" + i + "が不正です。ID・本文・4択・正解番号を確認してください。";
                    return false;
                }
                writer.Write(question.QuestionId);
                writer.Write(question.QuestionText);
                for (int choice = 0; choice < 4; choice++)
                {
                    string text = question.GetChoice(choice);
                    if (string.IsNullOrWhiteSpace(text)) { error = "空の選択肢があります。"; return false; }
                    writer.Write(text);
                }
                // 正解もローカルの照合対象にするが、Clientが正解番号を送信することはない。
                writer.Write(question.CorrectAnswerIndex);
                writer.Write(question.Category);
                writer.Write((int)question.Difficulty);
                writer.Write(question.Explanation);
            }
            writer.Flush();
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", string.Empty);
        }
        return true;
    }
}
