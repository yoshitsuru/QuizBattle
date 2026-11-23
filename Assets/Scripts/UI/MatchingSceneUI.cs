using UnityEngine;

[DisallowMultipleComponent]
public sealed class MatchingSceneUI : MonoBehaviour
{
    [Header("接続管理")]
    [SerializeField] private PhotonConnectionManager _connection;
    [Header("入力と操作")]
    [SerializeField] private TMPro.TMP_InputField _sessionInput;
    [SerializeField] private UnityEngine.UI.Button _createButton;
    [SerializeField] private UnityEngine.UI.Button _joinButton;
    [SerializeField] private UnityEngine.UI.Button _exitButton;
    [Header("状態表示")]
    [SerializeField] private TMPro.TMP_Text _sessionText;
    [SerializeField] private TMPro.TMP_Text _statusText;
    [SerializeField] private TMPro.TMP_Text _playerCountText;
    [SerializeField] private TMPro.TMP_Text _errorText;
    [Header("参加者と開始予定")]
    [SerializeField] private TMPro.TMP_InputField _displayNameInput;
    [SerializeField] private TMPro.TMP_Text _hostPlayerText;
    [SerializeField] private TMPro.TMP_Text _clientPlayerText;
    [SerializeField] private UnityEngine.UI.Button _startButton;
    [SerializeField] private TMPro.TMP_Text _startStatusText;

    private void Awake()
    {
        // シーンの参照不足を起動時にまとめて検出し、連続したNullReferenceを防ぐ。
        if (_connection == null || _sessionInput == null || _createButton == null || _joinButton == null || _exitButton == null ||
            _sessionText == null || _statusText == null || _playerCountText == null || _errorText == null ||
            _displayNameInput == null || _hostPlayerText == null || _clientPlayerText == null || _startButton == null || _startStatusText == null)
        {
            Debug.LogError("MatchingSceneUIのInspector参照が不足しています。", this);
            enabled = false;
            return;
        }
        _displayNameInput.characterLimit = MultiplayerRoomState.MaxDisplayNameLength;
        _displayNameInput.lineType = TMPro.TMP_InputField.LineType.SingleLine;
        _hostPlayerText.richText = false;
        _clientPlayerText.richText = false;
        _sessionInput.characterLimit = 32;
        _sessionInput.lineType = TMPro.TMP_InputField.LineType.SingleLine;
        // 共有セッション名をリッチテキストとして解釈しない。
        _sessionText.richText = false;
        _errorText.richText = false;
    }

    private void OnEnable()
    {
        if (_connection == null || _createButton == null || _joinButton == null || _exitButton == null || _startButton == null) return;
        // 登録と解除を対にし、シーン再入場でもクリックが二重実行されないようにする。
        _connection.Changed += Refresh;
        _createButton.onClick.AddListener(CreateRoom);
        _joinButton.onClick.AddListener(JoinRoom);
        _exitButton.onClick.AddListener(ExitToTitle);
        _startButton.onClick.AddListener(RequestMatchStart);
        Refresh();
    }
    private void OnDisable()
    {
        if (_connection != null) _connection.Changed -= Refresh;
        if (_createButton != null) _createButton.onClick.RemoveListener(CreateRoom);
        if (_joinButton != null) _joinButton.onClick.RemoveListener(JoinRoom);
        if (_exitButton != null) _exitButton.onClick.RemoveListener(ExitToTitle);
        if (_startButton != null) _startButton.onClick.RemoveListener(RequestMatchStart);
    }
    private void CreateRoom()
    {
        if (_connection.SetDisplayName(_displayNameInput.text)) _connection.CreateRoom(_sessionInput.text);
    }
    private void JoinRoom()
    {
        if (_connection.SetDisplayName(_displayNameInput.text)) _connection.JoinRoom(_sessionInput.text);
    }
    private void ExitToTitle() { _connection.ExitToTitle(); }
    private void RequestMatchStart() { _connection.RequestMatchStart(); }

    private void Refresh()
    {
        _sessionInput.interactable = _connection.CanStart;
        _createButton.interactable = _connection.CanStart;
        _joinButton.interactable = _connection.CanStart;
        // 接続中も退出を許可し、起動取消からタイトルへ戻れるようにする。
        _exitButton.interactable = _connection.State != PhotonConnectionManager.ConnectionState.ShuttingDown;
        _sessionText.text = "現在のセッション：" + (string.IsNullOrEmpty(_connection.SessionName) ? "—" : _connection.SessionName);
        _playerCountText.text = "参加人数：" + _connection.PlayerCount + " / 2";
        _errorText.text = _connection.ErrorMessage;
        _displayNameInput.interactable = _connection.CanStart;
        _startButton.interactable = _connection.CanRequestMatchStart;
        var room = _connection.Room;
        _hostPlayerText.text = "Host：" + (room == null || room.HostPlayer == Fusion.PlayerRef.None ? "未参加" : room.HostDisplayName.ToString());
        _clientPlayerText.text = "Client：" + (room == null || room.ClientPlayer == Fusion.PlayerRef.None ? "参加待ち" : room.ClientDisplayName.ToString());
        var match = _connection.OnlineMatch;
        if (match == null) _startStatusText.text = "接続後に問題セットを照合します。";
        else if (match.Phase == OnlineQuizMatchController.MatchPhase.Ready) _startStatusText.text = "問題データ一致：Hostが対戦を開始できます。";
        else if (match.Phase == OnlineQuizMatchController.MatchPhase.LoadingScene) _startStatusText.text = "オンライン対戦画面へ移動中…";
        else if (match.Phase == OnlineQuizMatchController.MatchPhase.DataMismatch) _startStatusText.text = "問題データ不一致：対戦開始できません。";
        else _startStatusText.text = "問題セット照合中：両者の準備完了を待っています。";
        if (match != null && string.IsNullOrEmpty(_connection.ErrorMessage)) _errorText.text = match.FailureMessage.ToString();
        string status;
        switch (_connection.State)
        {
            case PhotonConnectionManager.ConnectionState.Starting: status = "接続中…"; break;
            case PhotonConnectionManager.ConnectionState.Connected: status = "接続済み"; break;
            case PhotonConnectionManager.ConnectionState.ShuttingDown: status = "終了処理中…"; break;
            case PhotonConnectionManager.ConnectionState.Failed: status = "失敗"; break;
            case PhotonConnectionManager.ConnectionState.Disconnected: status = "切断済み"; break;
            default: status = "未接続"; break;
        }
        _statusText.text = "接続状態：" + status;
    }
}
