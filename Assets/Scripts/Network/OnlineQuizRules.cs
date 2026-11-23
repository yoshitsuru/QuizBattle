using System;

public enum OnlineAnswerResult { Pending, Correct, Incorrect, Timeout }
public enum OnlineMatchOutcome { None, HostWin, ClientWin, Draw }
public enum AnswerRejection { None, InvalidSender, WrongMatch, WrongQuestion, Closed, Deadline, InvalidChoice, Duplicate }

public static class OnlineQuizRules
{
    public const int CorrectPoints = 100;
    public const float FeedbackSeconds = 1.5f;

    // Hostの実際の送信元スロットと受付tickを使い、Clientの点数・正解申告は入力にしない。
    public static AnswerRejection Validate(string sentMatch, string currentMatch, int sentQuestion, int currentQuestion,
        bool accepting, int receiveTick, int deadlineTick, int senderSlot, int choice, bool answered)
    {
        if (senderSlot < 0 || senderSlot > 1) return AnswerRejection.InvalidSender;
        if (string.IsNullOrEmpty(currentMatch) || !string.Equals(sentMatch, currentMatch, StringComparison.Ordinal)) return AnswerRejection.WrongMatch;
        if (sentQuestion != currentQuestion) return AnswerRejection.WrongQuestion;
        if (!accepting) return AnswerRejection.Closed;
        if (receiveTick >= deadlineTick) return AnswerRejection.Deadline;
        if (choice < 0 || choice > 3) return AnswerRejection.InvalidChoice;
        if (answered) return AnswerRejection.Duplicate;
        return AnswerRejection.None;
    }
    public static int Points(OnlineAnswerResult result) => result == OnlineAnswerResult.Correct ? CorrectPoints : 0;
    public static OnlineMatchOutcome Outcome(int hostScore, int clientScore) => hostScore == clientScore ? OnlineMatchOutcome.Draw : hostScore > clientScore ? OnlineMatchOutcome.HostWin : OnlineMatchOutcome.ClientWin;
}
