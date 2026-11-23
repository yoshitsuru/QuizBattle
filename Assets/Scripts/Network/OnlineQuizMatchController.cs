using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OnlineQuizMatchController : Fusion.NetworkBehaviour
{
    public enum MatchPhase { WaitingForData, Ready, LoadingScene, Question, Complete, DataMismatch, Aborted, Feedback }
private const string OnlineScenePath = "Assets/Scenes/OnlineQuizScene.unity";
    [Header("両端末で共通の問題データ")]
    [SerializeField] private QuizQuestionSet _questionSet;

    // 書き込みはHostのStateAuthorityだけ。Clientは照合情報とシーン準備通知だけをRPCで送る。
    [Fusion.Networked] public MatchPhase Phase { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._32> SetId { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._64> ContentHash { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._32> MatchId { get; private set; }
    [Fusion.Networked] public int QuestionIndex { get; private set; }
    [Fusion.Networked] public int QuestionNumber { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._32> QuestionId { get; private set; }
    [Fusion.Networked] public int StartTick { get; private set; }
    [Fusion.Networked] public int EndTick { get; private set; }
    [Fusion.Networked] public Fusion.TickTimer Deadline { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool HostDataReady { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool ClientDataReady { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool HostSceneReady { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool ClientSceneReady { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._128> FailureMessage { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool HostAnswered { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool ClientAnswered { get; private set; }
    [Fusion.Networked] public OnlineAnswerResult HostAnswerResult { get; private set; }
    [Fusion.Networked] public OnlineAnswerResult ClientAnswerResult { get; private set; }
    [Fusion.Networked] public int HostScore { get; private set; }
    [Fusion.Networked] public int ClientScore { get; private set; }
    [Fusion.Networked] public int HostCorrectCount { get; private set; }
    [Fusion.Networked] public int ClientCorrectCount { get; private set; }
    [Fusion.Networked] public int CorrectChoice { get; private set; }
    [Fusion.Networked] public Fusion.TickTimer FeedbackDeadline { get; private set; }
    [Fusion.Networked] public OnlineMatchOutcome Outcome { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool HostRematchRequested { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool ClientRematchRequested { get; private set; }
    [Fusion.Networked] public Fusion.TickTimer PreparationDeadline { get; private set; }
    private string _pendingRematchId;
    public bool LocalRematchRequested => _spawned && ((Phase == MatchPhase.Complete && _pendingRematchId == MatchId.ToString()) || (LocalSlot == 0 ? (bool)HostRematchRequested : LocalSlot == 1 && ClientRematchRequested));
    public bool CanRequestRematch => _spawned && LocalSlot >= 0 && Phase == MatchPhase.Complete && _room.PlayerCount == 2 && !LocalRematchRequested;

    // 選択・正誤は確定までHostのローカルだけに保持し、回答中の相手への漏洩を防ぐ。
    private int _hostSelection = -1, _clientSelection = -1;
    private string _pendingMatch;
    private int _pendingQuestion;
    public int LocalSlot => !_spawned ? -1 : Runner.LocalPlayer == _room.HostPlayer ? 0 : Runner.LocalPlayer == _room.ClientPlayer ? 1 : -1;
    public bool LocalAnswered => LocalSlot == 0 ? HostAnswered : LocalSlot == 1 && ClientAnswered;
    public bool CanLocalAnswer => _spawned && LocalSlot >= 0 && Phase == MatchPhase.Question && Runner.Tick.Raw < EndTick && !LocalAnswered && !(_pendingMatch == MatchId.ToString() && _pendingQuestion == QuestionNumber);

    public event Action Changed;
    private MultiplayerRoomState _room;
    private bool _spawned, _verificationSent;
    private string _localHash, _localError;
    private string _reportedMatchId;
    private Fusion.PlayerRef _lastClient;
    private MatchPhase _shownPhase;
    private bool _shownHostReady, _shownClientReady;
    private int _shownQuestion = -1;
    private Fusion.NetworkString<Fusion._128> _shownFailure;
    private bool _hasSnapshot;
    private bool _shownHostAnswered, _shownClientAnswered, _shownHostRematch, _shownClientRematch;
    private int _shownHostScore, _shownClientScore;

    public bool IsSpawned => _spawned;
    public bool CanHostBegin => _spawned && HasStateAuthority && Phase == MatchPhase.Ready && HostDataReady && ClientDataReady && _room.CanHostStart;
    public QuizQuestion CurrentQuestion => !_spawned || _questionSet == null || QuestionIndex < 0 ? null : _questionSet.GetQuestion(QuestionIndex);
    public float RemainingSeconds => !_spawned ? 0 : Mathf.Max(0, Deadline.RemainingTime(Runner) ?? 0);

    public override void Spawned()
    {
        _room = GetComponent<MultiplayerRoomState>();
        _spawned = true;
        bool valid = OnlineQuizData.TryCreateSignature(_questionSet, out _localHash, out _localError);
        if (HasStateAuthority)
        {
            QuestionIndex = -1;
            HostDataReady = valid;
            ClientDataReady = false;
            SetId = valid ? _questionSet.SetId : string.Empty;
            ContentHash = valid ? _localHash : string.Empty;
            Phase = valid ? MatchPhase.WaitingForData : MatchPhase.DataMismatch;
            FailureMessage = valid ? string.Empty : _localError;
        }
        NotifyIfChanged();
    }
    public override void Despawned(Fusion.NetworkRunner runner, bool hasState) { _spawned = false; Changed = null; }
    public override void Render()
    {
        // Clientのローカルデータは、Hostのセット情報が届いてから一度だけ照合する。
        if (!HasStateAuthority && !_verificationSent && HostDataReady && _room.ClientPlayer == Runner.LocalPlayer)
        {
            _verificationSent = true;
            RPC_VerifyData(_questionSet == null ? string.Empty : _questionSet.SetId, _localHash);
        }
        NotifyIfChanged();
    }
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (Phase == MatchPhase.WaitingForData || Phase == MatchPhase.Ready || Phase == MatchPhase.DataMismatch)
            RefreshClientReadiness();
        if ((Phase == MatchPhase.LoadingScene || Phase == MatchPhase.Question || Phase == MatchPhase.Feedback) && _room.PlayerCount != 2)
        {
            Abort("相手が退出したため、試合を中止しました。");
            return;
        }
        if (Phase == MatchPhase.Complete)
        {
            if (_room.PlayerCount != 2) { Abort("相手が退出したため、再戦できません。"); return; }
            if (HostRematchRequested && ClientRematchRequested)
            {
                if (!OnlineQuizData.TryCreateSignature(_questionSet, out string currentHash, out _) || !string.Equals(currentHash, ContentHash.ToString(), StringComparison.Ordinal))
                { Abort("問題データが変更されたため、再戦できません。"); return; }
                PrepareMatch();
            }
        }
        if (Phase == MatchPhase.LoadingScene && PreparationDeadline.Expired(Runner)) { Abort("相手の画面準備を確認できませんでした。再接続してください。"); return; }
        if (Phase == MatchPhase.LoadingScene && HostSceneReady && ClientSceneReady && !Runner.SceneManager.IsBusy)
        {
            PreparationDeadline = Fusion.TickTimer.None;
            BeginQuestion(0);
        }
        if (Phase == MatchPhase.Question && Deadline.Expired(Runner)) FinalizeQuestion();
        if (Phase == MatchPhase.Feedback && FeedbackDeadline.Expired(Runner))
        {
            FeedbackDeadline = Fusion.TickTimer.None;
            if (QuestionIndex + 1 < OnlineQuizData.QuestionCount) BeginQuestion(QuestionIndex + 1);
            else { Outcome = OnlineQuizRules.Outcome(HostScore, ClientScore); Phase = MatchPhase.Complete; }
        }
        NotifyIfChanged();
    }
    private void RefreshClientReadiness()
    {
        if (_room.ClientPlayer == _lastClient) return;
        _lastClient = _room.ClientPlayer;
        ClientDataReady = false;
        if (HostDataReady) { Phase = MatchPhase.WaitingForData; FailureMessage = string.Empty; }
    }
    [Fusion.Rpc(Fusion.RpcSources.All, Fusion.RpcTargets.StateAuthority, HostMode = Fusion.RpcHostMode.SourceIsHostPlayer)]
    private void RPC_VerifyData(string setId, string hash, Fusion.RpcInfo info = default)
    {
        if (!HasStateAuthority || _room.SessionLocked || !HostDataReady || info.Source == Fusion.PlayerRef.None) return;
        _room.RefreshPlayers();
        RefreshClientReadiness();
        if (info.Source != _room.ClientPlayer) return;
        bool matches = string.Equals(setId, SetId.ToString(), StringComparison.Ordinal) && string.Equals(hash, ContentHash.ToString(), StringComparison.Ordinal);
        ClientDataReady = matches;
        Phase = matches ? MatchPhase.Ready : MatchPhase.DataMismatch;
        FailureMessage = matches ? string.Empty : "HostとClientの問題セットIDまたは内容ハッシュが一致しません。開始できません。";
        NotifyIfChanged();
    }

    public bool RequestRematch()
    {
        if (!CanRequestRematch) return false;
        bool valid = OnlineQuizData.TryCreateSignature(_questionSet, out string hash, out _);
        _pendingRematchId = MatchId.ToString();
        RPC_RequestRematch(_pendingRematchId, valid ? hash : string.Empty);
        return true;
    }
    [Fusion.Rpc(Fusion.RpcSources.All, Fusion.RpcTargets.StateAuthority, HostMode = Fusion.RpcHostMode.SourceIsHostPlayer)]
    private void RPC_RequestRematch(string matchId, string hash, Fusion.RpcInfo info = default)
    {
        if (!HasStateAuthority || !_spawned || Phase != MatchPhase.Complete || _room.PlayerCount != 2 || info.Source == Fusion.PlayerRef.None || !Runner.IsPlayerCommitted(info.Source)) return;
        if (!string.Equals(matchId, MatchId.ToString(), StringComparison.Ordinal)) { Debug.LogWarning("[QBRematch] 古い試合の要求を拒否しました。", this); return; }
        if (info.Source != _room.HostPlayer && info.Source != _room.ClientPlayer) return;
        if (!string.Equals(hash, ContentHash.ToString(), StringComparison.Ordinal)) { Abort("問題データが変更されたため、再戦できません。再接続してください。"); return; }
        if (info.Source == _room.HostPlayer) HostRematchRequested = true;
        else ClientRematchRequested = true;
        NotifyIfChanged();
    }
    private void PrepareMatch()
    {
        // 同じRunner/Roomを再利用するが、試合識別子と全問の状態は必ず新しくする。
        MatchId = Guid.NewGuid().ToString("N");
        HostScore = 0; ClientScore = 0; HostCorrectCount = 0; ClientCorrectCount = 0;
        HostAnswered = false; ClientAnswered = false;
        HostAnswerResult = OnlineAnswerResult.Pending; ClientAnswerResult = OnlineAnswerResult.Pending;
        HostRematchRequested = false; ClientRematchRequested = false;
        Outcome = OnlineMatchOutcome.None;
        QuestionIndex = -1; QuestionNumber = 0; QuestionId = string.Empty; CorrectChoice = -1;
        StartTick = 0; EndTick = 0;
        _hostSelection = -1; _clientSelection = -1;
        _pendingMatch = null; _pendingQuestion = 0;
        Deadline = Fusion.TickTimer.None; FeedbackDeadline = Fusion.TickTimer.None;
        HostSceneReady = false; ClientSceneReady = false;
        FailureMessage = string.Empty;
        PreparationDeadline = Fusion.TickTimer.CreateFromSeconds(Runner, 30f);
        Phase = MatchPhase.LoadingScene;
    }

    public bool BeginMatch()
    {
        if (!CanHostBegin) return false;
        // 指示の受付時に再照合し、待機後のデータ編集をそのまま開始に持ち込まない。
        if (!OnlineQuizData.TryCreateSignature(_questionSet, out string hash, out string error) || !string.Equals(hash, ContentHash.ToString(), StringComparison.Ordinal))
        { Phase = MatchPhase.DataMismatch; FailureMessage = string.IsNullOrEmpty(error) ? "開始前に問題データが変更されました。再接続してください。" : error; NotifyIfChanged(); return false; }
        if (!Application.CanStreamedLevelBeLoaded(OnlineScenePath)) { Abort("OnlineQuizSceneがビルドシーンに登録されていません。"); return false; }
        if (!_room.RequestMatchStart()) return false;
        _room.LockForMatch();
        PrepareMatch();
        try
        {
            // Runner、Room、接続管理はシーンを越えて保持し、両端末をFusion経由で移動する。
            var operation = Runner.LoadScene(OnlineScenePath, UnityEngine.SceneManagement.LoadSceneMode.Single);
            operation.AddOnCompleted(op => { if (_spawned && HasStateAuthority && op.Error != null) Abort("オンラインシーンの読込に失敗しました。"); });
        }
        catch (Exception) { Abort("オンラインシーンへの遷移に失敗しました。"); return false; }
        NotifyIfChanged();
        return true;
    }
    public void ReportLocalSceneReady()
    {
        if (!_spawned || Phase != MatchPhase.LoadingScene) return;
        string matchId = MatchId.ToString();
        if (string.IsNullOrEmpty(matchId) || _reportedMatchId == matchId) return;
        // UIの問題セットも照合してから準備完了を通知する。
        if (!OnlineQuizData.TryCreateSignature(_questionSet, out string hash, out _) || !string.Equals(hash, ContentHash.ToString(), StringComparison.Ordinal))
        { RPC_ReportSceneReady(matchId, false); _reportedMatchId = matchId; return; }
        _reportedMatchId = matchId;
        RPC_ReportSceneReady(matchId, true);
    }
    [Fusion.Rpc(Fusion.RpcSources.All, Fusion.RpcTargets.StateAuthority, HostMode = Fusion.RpcHostMode.SourceIsHostPlayer)]
    private void RPC_ReportSceneReady(string matchId, bool dataMatches, Fusion.RpcInfo info = default)
    {
        if (!HasStateAuthority || Phase != MatchPhase.LoadingScene || !string.Equals(matchId, MatchId.ToString(), StringComparison.Ordinal)) return;
        if (info.Source != _room.HostPlayer && info.Source != _room.ClientPlayer) return;
        if (!dataMatches) { Abort("シーン移動後に問題データの不一致を検出しました。"); return; }
        if (info.Source == _room.HostPlayer) HostSceneReady = true;
        if (info.Source == _room.ClientPlayer) ClientSceneReady = true;
    }
    public bool SubmitAnswer(int choice)
    {
        if (!CanLocalAnswer || choice < 0 || choice > 3) return false;
        _pendingMatch = MatchId.ToString(); _pendingQuestion = QuestionNumber;
        RPC_SubmitAnswer(_pendingMatch, _pendingQuestion, choice);
        return true;
    }
    [Fusion.Rpc(Fusion.RpcSources.All, Fusion.RpcTargets.StateAuthority, HostMode = Fusion.RpcHostMode.SourceIsHostPlayer)]
    private void RPC_SubmitAnswer(string matchId, int questionNumber, int choice, Fusion.RpcInfo info = default)
    {
        if (!HasStateAuthority || !_spawned) return;
        int slot = info.Source == _room.HostPlayer ? 0 : info.Source == _room.ClientPlayer ? 1 : -1;
        if (info.Source == Fusion.PlayerRef.None || !Runner.IsPlayerCommitted(info.Source)) slot = -1;
        bool answered = slot == 0 ? HostAnswered : slot == 1 && ClientAnswered;
        var rejected = OnlineQuizRules.Validate(matchId, MatchId.ToString(), questionNumber, QuestionNumber,
            Phase == MatchPhase.Question && _room.PlayerCount == 2, Runner.Tick.Raw, EndTick, slot, choice, answered);
        if (rejected != AnswerRejection.None)
        {
            Debug.LogWarning("[QBAnswer] rejected=" + rejected + ", slot=" + slot, this);
            if (rejected == AnswerRejection.Deadline) FinalizeQuestion();
            return;
        }
        if (slot == 0) { _hostSelection = choice; HostAnswered = true; }
        else { _clientSelection = choice; ClientAnswered = true; }
        if (HostAnswered && ClientAnswered) FinalizeQuestion();
        NotifyIfChanged();
    }
    private void FinalizeQuestion()
    {
        if (!HasStateAuthority || Phase != MatchPhase.Question) return;
        var question = CurrentQuestion;
        if (question == null || question.CorrectAnswerIndex < 0 || question.CorrectAnswerIndex > 3) { Abort("問題データを読み取れないため、試合を中止しました。"); return; }
        // 状態を先に閉じる。RPCと期限チェックが競合しても、この処理は一度だけ通る。
        Phase = MatchPhase.Feedback;
        Deadline = Fusion.TickTimer.None;
        int correct = question.CorrectAnswerIndex;
        CorrectChoice = correct;
        HostAnswerResult = !HostAnswered ? OnlineAnswerResult.Timeout : _hostSelection == correct ? OnlineAnswerResult.Correct : OnlineAnswerResult.Incorrect;
        ClientAnswerResult = !ClientAnswered ? OnlineAnswerResult.Timeout : _clientSelection == correct ? OnlineAnswerResult.Correct : OnlineAnswerResult.Incorrect;
        HostAnswered = true; ClientAnswered = true;
        HostScore += OnlineQuizRules.Points(HostAnswerResult); ClientScore += OnlineQuizRules.Points(ClientAnswerResult);
        if (HostAnswerResult == OnlineAnswerResult.Correct) HostCorrectCount++;
        if (ClientAnswerResult == OnlineAnswerResult.Correct) ClientCorrectCount++;
        FeedbackDeadline = Fusion.TickTimer.CreateFromSeconds(Runner, OnlineQuizRules.FeedbackSeconds);
        NotifyIfChanged();
    }

    private void BeginQuestion(int index)
    {
        var question = _questionSet.GetQuestion(index);
        if (question == null) { Abort("出題する問題が見つかりません。"); return; }
        QuestionIndex = index;
        QuestionNumber = index + 1;
        QuestionId = question.QuestionId;
        HostAnswered = false; ClientAnswered = false;
        HostAnswerResult = OnlineAnswerResult.Pending; ClientAnswerResult = OnlineAnswerResult.Pending;
        _hostSelection = -1; _clientSelection = -1; CorrectChoice = -1;
        FeedbackDeadline = Fusion.TickTimer.None;
        StartTick = Runner.Tick.Raw;
        Deadline = Fusion.TickTimer.CreateFromSeconds(Runner, OnlineQuizData.QuestionSeconds);
        EndTick = Deadline.TargetTick ?? StartTick;
        Phase = MatchPhase.Question;
    }
    private void Abort(string message)
    {
        if (!_spawned || !HasStateAuthority) return;
        Deadline = Fusion.TickTimer.None;
        FeedbackDeadline = Fusion.TickTimer.None;
        PreparationDeadline = Fusion.TickTimer.None;
        HostRematchRequested = false; ClientRematchRequested = false;
        Phase = MatchPhase.Aborted;
        FailureMessage = message;
        if (Runner.SessionInfo.IsValid) Runner.SessionInfo.IsOpen = false;
        NotifyIfChanged();
    }
    private void NotifyIfChanged()
    {
        if (!_spawned) return;
        bool hostReady = HostDataReady, clientReady = ClientDataReady;
        bool sameAnswers = _shownHostAnswered == (bool)HostAnswered && _shownClientAnswered == (bool)ClientAnswered && _shownHostScore == HostScore && _shownClientScore == ClientScore;
        bool sameRematch = _shownHostRematch == (bool)HostRematchRequested && _shownClientRematch == (bool)ClientRematchRequested;
        if (_hasSnapshot && sameAnswers && sameRematch && _shownPhase == Phase && _shownQuestion == QuestionIndex && _shownHostReady == hostReady && _shownClientReady == clientReady && _shownFailure.Equals(FailureMessage)) return;
        _shownHostAnswered = HostAnswered; _shownClientAnswered = ClientAnswered;
        _shownHostScore = HostScore; _shownClientScore = ClientScore;
        _shownHostRematch = HostRematchRequested; _shownClientRematch = ClientRematchRequested;
        _hasSnapshot = true; _shownPhase = Phase; _shownQuestion = QuestionIndex;
        _shownHostReady = hostReady; _shownClientReady = clientReady; _shownFailure = FailureMessage;
        Changed?.Invoke();
    }
}
