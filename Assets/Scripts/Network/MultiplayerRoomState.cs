using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MultiplayerRoomState : Fusion.NetworkBehaviour
{
    public const int MaxDisplayNameLength = 16;
    // Scene側の接続管理へSpawn/Despawnを通知するだけ。共有データ自体はNetworkedで同期する。
    public static event Action<MultiplayerRoomState> Appeared;
    public static event Action<MultiplayerRoomState> Disappeared;
    public event Action Changed;

    // このObjectはHostだけがSpawnするため、StateAuthorityも常にHostにある。
    [Fusion.Networked] public Fusion.PlayerRef HostPlayer { get; private set; }
    [Fusion.Networked] public Fusion.PlayerRef ClientPlayer { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._16> HostDisplayName { get; private set; }
    [Fusion.Networked] public Fusion.NetworkString<Fusion._16> ClientDisplayName { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool StartRequested { get; private set; }
    [Fusion.Networked] public Fusion.NetworkBool SessionLocked { get; private set; }

    public void LockForMatch()
    {
        if (!_spawned || !HasStateAuthority) return;
        SessionLocked = true;
        if (Runner.SessionInfo.IsValid) Runner.SessionInfo.IsOpen = false;
    }

    private bool _spawned;
    private bool _hasSnapshot;
    private Fusion.PlayerRef _shownHost, _shownClient;
    private Fusion.NetworkString<Fusion._16> _shownHostName, _shownClientName;
    private bool _shownStart;

    public bool IsSpawned => _spawned;
    public int PlayerCount => !_spawned ? 0 : (HostPlayer != Fusion.PlayerRef.None ? 1 : 0) + (ClientPlayer != Fusion.PlayerRef.None ? 1 : 0);
    public bool CanHostStart => _spawned && HasStateAuthority && PlayerCount == 2 && !StartRequested && !SessionLocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() { Appeared = null; Disappeared = null; }

    public override void Spawned()
    {
        Debug.Log($"[QBRoom] Spawned通知：name={name}, StateAuthority={HasStateAuthority}", this);
        _spawned = true;
        if (HasStateAuthority) SynchronizePlayers();
        Appeared?.Invoke(this);
        NotifyIfChanged();
    }
    public override void Despawned(Fusion.NetworkRunner runner, bool hasState)
    {
        Debug.Log($"[QBRoom] Despawned通知：name={name}, hasState={hasState}", this);
        _spawned = false;
        Disappeared?.Invoke(this);
        Changed = null;
    }
    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority) SynchronizePlayers();
    }
    public override void Render() { NotifyIfChanged(); }

    public void RefreshPlayers()
    {
        if (_spawned && HasStateAuthority) SynchronizePlayers();
    }
    private void SynchronizePlayers()
    {
        Fusion.PlayerRef host = Fusion.PlayerRef.None;
        Fusion.PlayerRef client = Fusion.PlayerRef.None;
        // PlayerRefは接続識別用。UIは必ずHost/Clientの固定スロットを使う。
        foreach (var player in Runner.CommittedPlayers)
        {
            if (player == Runner.LocalPlayer) host = player;
            else if (client == Fusion.PlayerRef.None) client = player;
        }
        if (host != HostPlayer) { HostPlayer = host; HostDisplayName = host == Fusion.PlayerRef.None ? string.Empty : "Host"; }
        if (client != ClientPlayer)
        {
            ClientPlayer = client;
            ClientDisplayName = client == Fusion.PlayerRef.None ? string.Empty : "Client";
            // 待機中だけ入れ替わりを許可する。試合中の退出では部屋を開き直さない。
            if (!SessionLocked)
            {
                StartRequested = false;
                if (Runner.SessionInfo.IsValid) Runner.SessionInfo.IsOpen = true;
            }
        }
        NotifyIfChanged();
    }

    public static bool TryValidateDisplayName(string value, out string name)
    {
        name = value == null ? string.Empty : value.Trim();
        if (name.Length == 0 || name.Length > MaxDisplayNameLength) return false;
        foreach (char c in name)
            if (char.IsControl(c) || char.IsSurrogate(c) || c == '<' || c == '>') return false;
        return true;
    }

    public void SubmitLocalDisplayName(string name)
    {
        if (_spawned && TryValidateDisplayName(name, out string validName)) RPC_SetDisplayName(validName);
    }
    // Sourceは実際の送信者をFusionから受け取り、他プレイヤーの名前変更は許可しない。
    [Fusion.Rpc(Fusion.RpcSources.All, Fusion.RpcTargets.StateAuthority, HostMode = Fusion.RpcHostMode.SourceIsHostPlayer)]
    private void RPC_SetDisplayName(string name, Fusion.RpcInfo info = default)
    {
        if (!HasStateAuthority || !TryValidateDisplayName(name, out string validName)) return;
        SynchronizePlayers();
        if (info.Source == HostPlayer && HostPlayer != Fusion.PlayerRef.None) HostDisplayName = validName;
        else if (info.Source == ClientPlayer && ClientPlayer != Fusion.PlayerRef.None) ClientDisplayName = validName;
        NotifyIfChanged();
    }

    public bool RequestMatchStart()
    {
        if (!_spawned || !HasStateAuthority) return false;
        SynchronizePlayers();
        if (!CanHostStart) return false;
        // このSTEPでは開始予定フラグだけを同期し、QuizSceneへは移動しない。
        StartRequested = true;
        if (Runner.SessionInfo.IsValid) Runner.SessionInfo.IsOpen = false;
        NotifyIfChanged();
        return true;
    }

    private void NotifyIfChanged()
    {
        if (!_spawned) return;
        bool requested = StartRequested;
        if (_hasSnapshot && _shownHost == HostPlayer && _shownClient == ClientPlayer && _shownHostName.Equals(HostDisplayName) && _shownClientName.Equals(ClientDisplayName) && _shownStart == requested) return;
        _hasSnapshot = true;
        _shownHost = HostPlayer; _shownClient = ClientPlayer;
        _shownHostName = HostDisplayName; _shownClientName = ClientDisplayName;
        _shownStart = requested;
        Changed?.Invoke();
    }
}
