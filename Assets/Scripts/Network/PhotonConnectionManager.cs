using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PhotonConnectionManager : MonoBehaviour, Fusion.INetworkRunnerCallbacks
{
    public enum ConnectionState { Idle, Starting, Connected, ShuttingDown, Failed, Disconnected }
    private const int MaxPlayers = 2;
    private const string TitleSceneName = "TitleScene";
    // 両PCが同じ地域を使うため、グローバル設定を変更せず接続ごとのコピーへ指定する。
    [Header("接続設定")]
    [Tooltip("参加する全PCで同じPhotonリージョンを使います。")]
    [SerializeField] private string _region = "jp";
    [Header("対戦ルーム")]
    [Tooltip("HostだけがSpawnする、NetworkObject付きのルーム状態Prefabです。")]
    [SerializeField] private MultiplayerRoomState _roomPrefab;

    private MultiplayerRoomState _room;
    private OnlineQuizMatchController _onlineMatch;
    public static PhotonConnectionManager Active { get; private set; }
    public OnlineQuizMatchController OnlineMatch => _onlineMatch != null && _onlineMatch.IsSpawned ? _onlineMatch : null;
    private string _displayName = string.Empty;
    private string _localDisplayName;
    public MultiplayerRoomState Room => _room != null && _room.IsSpawned ? _room : null;
    public bool IsLocalHost => State == ConnectionState.Connected && _runner != null && _runner.IsServer;
    public bool CanRequestMatchStart => !IsBusy && IsLocalHost && Room != null && OnlineMatch != null && OnlineMatch.CanHostBegin;
    public bool IsMatchStartRequested => Room != null && Room.StartRequested;

    private Fusion.NetworkRunner _runner;
    private CancellationTokenSource _startCancellation;
    private TaskCompletionSource<bool> _initialSceneLoaded;
    // 接続成立とシーン初期化完了は別。初期化失敗時に無限待機しないための上限。
    private const int InitialSceneTimeoutMilliseconds = 15000;
    private Task _operation = Task.CompletedTask;
    private Task _shutdownTask = Task.CompletedTask;
    private bool _leaving;
    private bool _destroyed;
    private string _callbackFailure;

    public event Action Changed;
    public ConnectionState State { get; private set; }
    public string SessionName { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public int PlayerCount { get; private set; }
    public bool IsBusy => State == ConnectionState.Starting || State == ConnectionState.ShuttingDown || _leaving;
    public bool CanStart => !IsBusy && State != ConnectionState.Connected && _runner == null;

    public void CreateRoom(string sessionName) { BeginConnection(sessionName, Fusion.GameMode.Host); }
    public void JoinRoom(string sessionName) { BeginConnection(sessionName, Fusion.GameMode.Client); }

    public bool SetDisplayName(string name)
    {
        if (!CanStart) return false;
        if (string.IsNullOrWhiteSpace(name)) { _displayName = string.Empty; return true; }
        if (!MultiplayerRoomState.TryValidateDisplayName(name, out string validName))
        {
            SetState(ConnectionState.Failed, "表示名は16文字以内で入力してください。制御文字・絵文字・< >は使えません。");
            return false;
        }
        _displayName = validName;
        return true;
    }
    public void RequestMatchStart()
    {
        if (CanRequestMatchStart) _onlineMatch.BeginMatch();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActive() { Active = null; }
    private void Awake()
    {
        if (Active != null && Active != this)
        {
            Debug.LogError("既に接続管理が存在します。セッションごとに一つだけ保持してください。", this);
            Destroy(gameObject);
            return;
        }
        Active = this;
    }
    private void OnEnable()
    {
        MultiplayerRoomState.Appeared += AttachRoom;
        MultiplayerRoomState.Disappeared += OnRoomDisappeared;
    }
    private void OnDisable()
    {
        MultiplayerRoomState.Appeared -= AttachRoom;
        MultiplayerRoomState.Disappeared -= OnRoomDisappeared;
    }
    private void AttachRoom(MultiplayerRoomState room)
    {
        if (_destroyed || room.Runner != _runner || _room == room) return;
        ClearRoom();
        _room = room;
        _room.Changed += RefreshRoomDisplay;
        // 同じNetworkObject内の別NetworkBehaviourで問題・タイマーの責務を分ける。
        _onlineMatch = room.GetComponent<OnlineQuizMatchController>();
        if (_onlineMatch != null) _onlineMatch.Changed += RefreshRoomDisplay;
        _room.SubmitLocalDisplayName(_localDisplayName);
        RefreshRoomDisplay();
    }
    private void OnRoomDisappeared(MultiplayerRoomState room)
    {
        if (_room == room) ClearRoom();
    }
    private void ClearRoom()
    {
        if (_room != null) _room.Changed -= RefreshRoomDisplay;
        if (_onlineMatch != null) _onlineMatch.Changed -= RefreshRoomDisplay;
        _onlineMatch = null;
        _room = null;
        PlayerCount = 0;
        if (!_destroyed) Changed?.Invoke();
    }
    private void RefreshRoomDisplay()
    {
        PlayerCount = Room == null ? 0 : Room.PlayerCount;
        if (!_destroyed) Changed?.Invoke();
    }

    private void BeginConnection(string sessionName, Fusion.GameMode mode)
    {
        // 状態は最初のawaitより前に変更するため、連打でもRunnerは一つだけになる。
        if (_destroyed || !CanStart) return;
        string name = sessionName == null ? string.Empty : sessionName.Trim();
        if (!IsValidSessionName(name))
        {
            SetState(ConnectionState.Failed, "セッション名は1〜32文字の半角英数字・ハイフン・アンダースコアで入力してください。");
            return;
        }
        var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global;
        if (settings == null || string.IsNullOrWhiteSpace(settings.AppSettings.AppIdFusion))
        {
            SetState(ConnectionState.Failed, "Fusion用App IDが設定されていません。");
            return;
        }
        if (_roomPrefab == null)
        {
            SetState(ConnectionState.Failed, "対戦ルームのPrefabがInspectorに設定されていません。");
            return;
        }
        _localDisplayName = string.IsNullOrEmpty(_displayName) ? (mode == Fusion.GameMode.Host ? "Host" : "Client") : _displayName;
        SessionName = name;
        PlayerCount = 0;
        _callbackFailure = string.Empty;
        SetState(ConnectionState.Starting);
        _operation = StartConnectionAsync(name, mode);
    }

    public static bool IsValidSessionName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 32) return false;
        foreach (char c in name)
            if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '-' && c != '_') return false;
        return true;
    }

    private async Task StartConnectionAsync(string sessionName, Fusion.GameMode mode)
    {
        Fusion.NetworkRunner runner = null;
        _startCancellation = new CancellationTokenSource();
        _initialSceneLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            // Shutdown済みRunnerは使い回さず、接続試行ごとに新規生成する。
            var runnerObject = new GameObject("QuizBattleNetworkRunner");
            runner = runnerObject.AddComponent<Fusion.NetworkRunner>();
            _runner = runner;
            runner.ProvideInput = false;
            var sceneManager = runnerObject.AddComponent<Fusion.NetworkSceneManagerDefault>();
            runner.AddCallbacks(this);
            var sceneInfo = new Fusion.NetworkSceneInfo();
            sceneInfo.AddSceneRef(Fusion.SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), LoadSceneMode.Single);
            var appSettings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();
            appSettings.FixedRegion = _region;
            var outcome = await runner.StartGame(new Fusion.StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName,
                PlayerCount = MaxPlayers,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                IsOpen = true,
                IsVisible = false,
                EnableClientSessionCreation = false,
                CustomPhotonAppSettings = appSettings,
                StartGameCancellationToken = _startCancellation.Token
            });
            if (_destroyed || _leaving || _startCancellation.IsCancellationRequested)
            {
                await StopRunnerAsync();
                return;
            }
            if (!outcome.Ok || runner == null || !runner.IsRunning)
            {
                string reason = !outcome.Ok ? outcome.ShutdownReason.ToString() : _callbackFailure;
                await StopRunnerAsync();
                SetState(ConnectionState.Failed, ConnectionFailureMessage(reason));
                Debug.LogWarning("[QBConnection] 接続失敗 reason=" + reason, this);
                return;
            }
            // StartGameはシーンの引継ぎ処理より先に完了する場合がある。
            // Singleロードの既存Runtime Object破棄が終わってからRoomを生成する。
            var sceneSignal = _initialSceneLoaded;
            using (_startCancellation.Token.Register(() => sceneSignal.TrySetCanceled()))
            {
                var timeout = Task.Delay(InitialSceneTimeoutMilliseconds, _startCancellation.Token);
                if (await Task.WhenAny(sceneSignal.Task, timeout) != sceneSignal.Task)
                {
                    _startCancellation.Token.ThrowIfCancellationRequested();
                    throw new TimeoutException("Initial Fusion scene load did not complete");
                }
                await sceneSignal.Task;
            }
            _startCancellation.Token.ThrowIfCancellationRequested();
            if (_destroyed || _leaving) { await StopRunnerAsync(); return; }
            // 接続管理とネットワーク状態はセッション終了までシーンを越えて保持する。
            DontDestroyOnLoad(gameObject);
            // Hostが一度だけ生成し、Clientにも同じ持続フラグで複製される。
            if (runner.IsServer)
            {
                Debug.Log("[QBRoom] HostのRoom Spawnを開始します。", this);
                var spawnedRoom = runner.Spawn(_roomPrefab, flags: Fusion.NetworkSpawnFlags.DontDestroyOnLoad);
                if (spawnedRoom == null) throw new InvalidOperationException("Room spawn failed");
                Debug.Log($"[QBRoom] Spawn戻り値：name={spawnedRoom.name}, scene={spawnedRoom.gameObject.scene.name}, active={spawnedRoom.gameObject.activeInHierarchy}, spawned={spawnedRoom.IsSpawned}, runner一致={spawnedRoom.Runner == runner}", this);
                AttachRoom(spawnedRoom);
                Debug.Log($"[QBRoom] 接続管理への登録：room参照あり={_room != null}, 表示用Roomあり={Room != null}", this);
            }
            SetState(ConnectionState.Connected);
            RefreshRoomDisplay();
        }
        catch (OperationCanceledException)
        {
            await StopRunnerAsync();
            if (!_leaving && !_destroyed)
                SetState(ConnectionState.Disconnected, string.IsNullOrEmpty(_callbackFailure) ? "接続を取り消しました。" : "接続が切れました。もう一度お試しください。");
        }
        catch (Exception exception)
        {
            await StopRunnerAsync();
            // SDK例外の本文には設定値が含まれる可能性があるため、型名のみ表示する。
            if (!_leaving && !_destroyed)
            {
                SetState(ConnectionState.Failed, "接続処理を完了できませんでした。もう一度お試しください。");
                Debug.LogWarning("[QBConnection] exception=" + exception.GetType().Name, this);
            }
        }
        finally
        {
            _initialSceneLoaded = null;
            _startCancellation?.Dispose();
            _startCancellation = null;
        }
    }

    public async void ExitToTitle()
    {
        if (_leaving || _destroyed) return;
        _leaving = true;
        SetState(ConnectionState.ShuttingDown);
        _startCancellation?.Cancel();
        try
        {
            // 起動の完了／取消を待ってから終了し、画面離脱後の接続完了を放置しない。
            await _operation;
            await StopRunnerAsync();
            if (_destroyed) return;
            if (!Application.CanStreamedLevelBeLoaded(TitleSceneName))
            {
                _leaving = false;
                SetState(ConnectionState.Failed, "TitleSceneがビルドシーンに登録されていません。");
                return;
            }
            SceneManager.LoadScene(TitleSceneName);
            // DontDestroyOnLoadで保持した管理Objectも、退出時には必ず解放する。
            Destroy(gameObject);
        }
        catch (Exception exception)
        {
            _leaving = false;
            SetState(ConnectionState.Failed, "退出処理を完了できませんでした。もう一度タイトルへ戻る操作をしてください。");
            Debug.LogWarning("[QBConnection] exit exception=" + exception.GetType().Name, this);
        }
    }

    private Task StopRunnerAsync()
    {
        if (!_shutdownTask.IsCompleted) return _shutdownTask;
        var runner = _runner;
        ClearRoom();
        if (runner == null) { _runner = null; return Task.CompletedTask; }
        // Runner.ShutdownがSpawn済みNetworkObjectを終了・破棄する。Hostだけが生成の責任を持つ。
        // 参照とコールバックを先に外し、Shutdown通知による再入を防ぐ。
        _runner = null;
        runner.RemoveCallbacks(this);
        _shutdownTask = ShutdownAndDestroyAsync(runner);
        return _shutdownTask;
    }

    private static async Task ShutdownAndDestroyAsync(Fusion.NetworkRunner runner)
    {
        try
        {
            if (runner != null && !runner.IsShutdown)
                await runner.Shutdown(destroyGameObject: false);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("QuizBattle Runner終了エラー：" + exception.GetType().Name);
        }
        finally
        {
            if (runner != null) Destroy(runner.gameObject);
        }
    }

    private async Task RecoverDisconnectedAsync(string reason)
    {
        SetState(ConnectionState.ShuttingDown, reason);
        await StopRunnerAsync();
        PlayerCount = 0;
        if (!_leaving && !_destroyed) SetState(ConnectionState.Disconnected, reason);
    }

    private void HandleDisconnect(Fusion.NetworkRunner runner, string reason)
    {
        if (_destroyed || runner != _runner) return;
        _callbackFailure = reason;
        if (State == ConnectionState.Starting) _initialSceneLoaded?.TrySetCanceled();
        if (State == ConnectionState.Connected && !_leaving)
        {
            Debug.LogWarning("[QBConnection] disconnected reason=" + reason, this);
            _operation = RecoverDisconnectedAsync("相手との接続が切れました。タイトルへ戻って再接続してください。");
        }
    }

    private static string ConnectionFailureMessage(string reason)
    {
        switch (reason)
        {
            case "GameNotFound": return "指定された部屋が見つかりません。セッション名を確認してください。";
            case "GameIsFull": return "部屋が満員です。別の部屋に参加してください。";
            case "GameClosed": return "この部屋では既に対戦が始まっています。";
            case "GameIdAlreadyExists": return "同じ名前の部屋が存在します。別の名前を入力してください。";
            case "InvalidAuthentication": return "接続認証に失敗しました。通信設定を確認してください。";
            default: return "接続できませんでした。通信状態を確認して、もう一度お試しください。";
        }
    }
    private void SetState(ConnectionState state, string error = "")
    {
        State = state;
        ErrorMessage = error;
        if (!_destroyed) Changed?.Invoke();
    }

    private void OnDestroy()
    {
        // 想定外のシーン移動でも、開始処理を取消してRunnerだけが残ることを防ぐ。
        _destroyed = true;
        if (Active == this) Active = null;
        _startCancellation?.Cancel();
        if (_runner != null) _runner.RemoveCallbacks(this);
        _ = CleanupAfterOperationAsync();
    }
    private async Task CleanupAfterOperationAsync()
    {
        try { await _operation; await StopRunnerAsync(); }
        catch (Exception exception) { Debug.LogWarning("QuizBattle離脱時の終了エラー：" + exception.GetType().Name); }
    }

    public void OnPlayerJoined(Fusion.NetworkRunner runner, Fusion.PlayerRef player)
    {
        if (runner == _runner && Room != null) Room.RefreshPlayers();
    }
    public void OnPlayerLeft(Fusion.NetworkRunner runner, Fusion.PlayerRef player)
    {
        if (runner == _runner && Room != null) Room.RefreshPlayers();
    }
    public void OnShutdown(Fusion.NetworkRunner runner, Fusion.ShutdownReason reason) { HandleDisconnect(runner, reason.ToString()); }
    public void OnDisconnectedFromServer(Fusion.NetworkRunner runner, Fusion.Sockets.NetDisconnectReason reason) { HandleDisconnect(runner, reason.ToString()); }
    public void OnConnectFailed(Fusion.NetworkRunner runner, Fusion.Sockets.NetAddress address, Fusion.Sockets.NetConnectFailedReason reason) { HandleDisconnect(runner, reason.ToString()); }
    public void OnConnectRequest(Fusion.NetworkRunner runner, Fusion.NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        int count = 0;
        foreach (var player in runner.ActivePlayers) count++;
        if (runner != _runner || _leaving || count >= MaxPlayers || (Room != null && Room.SessionLocked)) request.Refuse();
        else request.Accept();
    }
    public void OnReliableDataReceived(Fusion.NetworkRunner runner, Fusion.PlayerRef player, Fusion.Sockets.ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnHostMigration(Fusion.NetworkRunner runner, Fusion.HostMigrationToken token) { HandleDisconnect(runner, "Hostが退出しました。ホスト移譲は行いません。"); }
    public void OnConnectedToServer(Fusion.NetworkRunner runner) { }
    public void OnInput(Fusion.NetworkRunner runner, Fusion.NetworkInput input) { }
    public void OnInputMissing(Fusion.NetworkRunner runner, Fusion.PlayerRef player, Fusion.NetworkInput input) { }
    public void OnUserSimulationMessage(Fusion.NetworkRunner runner, Fusion.SimulationMessagePtr message) { }
    public void OnSessionListUpdated(Fusion.NetworkRunner runner, List<Fusion.SessionInfo> sessions) { }
    public void OnCustomAuthenticationResponse(Fusion.NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataProgress(Fusion.NetworkRunner runner, Fusion.PlayerRef player, Fusion.Sockets.ReliableKey key, float progress) { }
    public void OnSceneLoadDone(Fusion.NetworkRunner runner)
    {
        if (runner != _runner) return;
        Debug.Log("[QBRoom] 初期シーンのロード完了通知を受け取りました。", this);
        _initialSceneLoaded?.TrySetResult(true);
    }
    public void OnSceneLoadStart(Fusion.NetworkRunner runner) { }
    public void OnObjectEnterAOI(Fusion.NetworkRunner runner, Fusion.NetworkObject obj, Fusion.PlayerRef player) { }
    public void OnObjectExitAOI(Fusion.NetworkRunner runner, Fusion.NetworkObject obj, Fusion.PlayerRef player) { }
}
