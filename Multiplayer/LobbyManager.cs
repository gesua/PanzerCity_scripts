using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 로비 생성/조회/참가, Relay 연결, 준비/강퇴/게임 시작 담당
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // 로비/플레이어 데이터 키
    const string KeyRelayJoinCode = "RelayJoinCode";
    const string KeyGameStarted = "GameStarted";
    const string KeyNickname = "Nickname";
    const string KeyIsReady = "IsReady";

    // 하트비트/폴링 주기
    const float HeartbeatInterval = 15f;
    const float LobbyPollInterval = 1.5f;

    Lobby _currentLobby;
    float _heartbeatTimer;
    float _lobbyPollTimer;
    string _nickname;

    public Lobby CurrentLobby => _currentLobby;
    public string Nickname => _nickname;
    public string FirstStageName { get; private set; } = "Stage01"; // 멀티 시작 스테이지
    public bool IsHost => _currentLobby != null &&
        _currentLobby.HostId == AuthenticationService.Instance.PlayerId;

    public event Action<List<Lobby>> OnLobbyListUpdated;
    public event Action<Lobby> OnLobbyUpdated; // 룸 상태 갱신
    public event Action<string> OnStatusChanged;
    public event Action OnKicked;    // 강퇴당함
    public event Action OnLeftLobby; // 직접 나감
    public event Action OnHostLeft;  // 방장이 연결 끊음
    public event Action OnGameStart; // 게임 시작 신호

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await InitializeAsync();
    }

    /// <summary>
    /// UGS 초기화 및 익명 로그인
    /// </summary>
    async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsSignedIn == false)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke($"Initialization failed:{e.Message}");
        }
    }

    void Update()
    {
        HandleHeartbeat();
        HandleLobbyPoll();
    }

    /// <summary>
    /// 호스트만 하트비트 전송 (로비 살아있게 유지)
    /// </summary>
    async void HandleHeartbeat()
    {
        if (_currentLobby == null) return;
        if (IsHost == false) return;

        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer < HeartbeatInterval) return;

        _heartbeatTimer = 0f;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"하트비트 실패:{e.Message}");
        }
    }

    /// <summary>
    /// 로비 상태 폴링 — 강퇴/게임 시작 감지 포함
    /// </summary>
    async void HandleLobbyPoll()
    {
        if (_currentLobby == null) return;

        _lobbyPollTimer += Time.deltaTime;
        if (_lobbyPollTimer < LobbyPollInterval) return;

        _lobbyPollTimer = 0f;

        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);

            // 강퇴 감지 (내 ID가 로비에 없으면 강퇴됨)
            bool isStillInLobby = false;
            foreach (Player player in lobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    isStillInLobby = true;
                    break;
                }
            }

            if (isStillInLobby == false)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
                _currentLobby = null;
                NetworkManager.Singleton.Shutdown();
                OnKicked?.Invoke();
                return;
            }

            // 게임 시작 감지
            if (lobby.Data != null &&
                lobby.Data.ContainsKey(KeyGameStarted) &&
                lobby.Data[KeyGameStarted].Value == "true")
            {
                _currentLobby = null;
                OnGameStart?.Invoke();
                return;
            }

            _currentLobby = lobby;
            OnLobbyUpdated?.Invoke(_currentLobby);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"로비 폴링 실패:{e.Message}");
        }
    }

    /// <summary>
    /// 닉네임 설정
    /// </summary>
    public void SetNickname(string nickname)
    {
        _nickname = nickname;
    }

    /// <summary>
    /// 공개 방 생성
    /// </summary>
    public async Task CreateLobbyAsync(string lobbyName, string password, int maxPlayers = 3)
    {
        try
        {
            OnStatusChanged?.Invoke("Creating room...");

            //OnStatusChanged?.Invoke("Allocating Relay...");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Password = (string.IsNullOrEmpty(password) == false) ? password : null,
                Data = new Dictionary<string, DataObject>
                {
                    { KeyRelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                },
                Player = MakePlayerData(isReady: false)
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            if (NetworkManager.Singleton.TryGetComponent(out UnityTransport transport))
            {
                transport.SetRelayServerData(relayServerData);
            }

            NetworkManager.Singleton.StartHost();

            // 연결 끊김 감지 구독 (중복 방지)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            OnStatusChanged?.Invoke($"Room created:{_currentLobby.Name}");
        }
        catch (Exception e)
        {
            // 생성 실패 시 현재 로비 초기화
            _currentLobby = null;

            OnStatusChanged?.Invoke($"Room creation failed:{e.Message}");
        }
    }

    /// <summary>
    /// 공개 로비 목록 조회
    /// </summary>
    public async Task RefreshLobbyListAsync()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 10
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            OnLobbyListUpdated?.Invoke(response.Results);
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke($"Lobby list fetch failed:{e.Message}");
        }
    }

    /// <summary>
    /// 로비 검색
    /// </summary>
    public async Task<Lobby> FindLobbyByNameAsync(string roomName)
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(
                        QueryFilter.FieldOptions.Name,
                        roomName,
                        QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (response.Results.Count == 0)
            {
                return null;
            }

            return response.Results[0];
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke($"Room search failed:{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 로비 참가
    /// </summary>
    public async Task JoinLobbyAsync(string lobbyId, string password)
    {
        try
        {
            OnStatusChanged?.Invoke("Joining room...");

            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Password = (string.IsNullOrEmpty(password) == false) ? password : null,
                Player = MakePlayerData(isReady: false)
            };

            _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

            string joinCode = _currentLobby.Data[KeyRelayJoinCode].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            if (NetworkManager.Singleton.TryGetComponent(out UnityTransport transport))
            {
                transport.SetRelayServerData(relayServerData);
            }

            NetworkManager.Singleton.StartClient();

            // 연결 끊김 감지 구독 (중복 방지)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            OnStatusChanged?.Invoke($"Join successful:{_currentLobby.Name}");
        }
        catch (Exception e)
        {
            // 참가 실패 시 _currentLobby 보장
            _currentLobby = null;
            OnStatusChanged?.Invoke($"Join failed:{e.Message}");
        }
    }

    /// <summary>
    /// 준비 상태 업데이트
    /// </summary>
    public async Task UpdateReadyStatusAsync(bool isReady)
    {
        if (_currentLobby == null) return;

        try
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {
                        KeyIsReady,
                        new PlayerDataObject(
                            PlayerDataObject.VisibilityOptions.Member,
                            (isReady) ? "true" : "false"
                        )
                    }
                }
            };

            _currentLobby = await LobbyService.Instance.UpdatePlayerAsync(
                _currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                options
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning($"준비 상태 업데이트 실패:{e.Message}");
        }
    }

    /// <summary>
    /// 플레이어 강퇴 (호스트 전용)
    /// </summary>
    public async Task KickPlayerAsync(string targetPlayerId)
    {
        if (_currentLobby == null) return;
        if (IsHost == false) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, targetPlayerId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"강퇴 실패:{e.Message}");
        }
    }

    /// <summary>
    /// 게임 시작 신호 전송 (호스트 전용)
    /// 로비 데이터에 기록 → 클라이언트가 폴링으로 감지
    /// </summary>
    public async Task StartGameAsync()
    {
        if (_currentLobby == null) return;
        if (IsHost == false) return;

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                // 게임 시작 후 신규 입장 차단
                IsLocked = true,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        KeyGameStarted,
                        new DataObject(DataObject.VisibilityOptions.Member, "true")
                    }
                }
            };

            await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, options);

            // 호스트는 폴링 대기 없이 즉시 처리
            _currentLobby = null;
            OnGameStart?.Invoke();
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke($"Game start failed:{e.Message}");
        }
    }

    /// <summary>
    /// 모든 플레이어 준비 완료 여부 (방장 제외, 2명 이상)
    /// </summary>
    public bool IsAllPlayersReady()
    {
        if (_currentLobby == null) return false;
        if (_currentLobby.Players.Count < 2) return false;

        foreach (Player player in _currentLobby.Players)
        {
            // 방장은 준비 체크 제외
            if (player.Id == _currentLobby.HostId) continue;

            if (player.Data == null) return false;
            if (player.Data.ContainsKey(KeyIsReady) == false) return false;
            if (player.Data[KeyIsReady].Value != "true") return false;
        }

        return true;
    }

    /// <summary>
    /// 로비 나가기
    /// </summary>
    public async Task LeaveLobbyAsync()
    {
        if (_currentLobby == null) return;

        try
        {
            // 구독 해제 — Shutdown 시 HandleClientDisconnect 방지
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

            // 호스트는 로비 자체를 삭제, 클라이언트는 본인만 나감
            if (IsHost)
            {
                await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    _currentLobby.Id,
                    AuthenticationService.Instance.PlayerId
                );
            }

            _currentLobby = null;
            NetworkManager.Singleton.Shutdown();
            OnLeftLobby?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"로비 나가기 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 씬 전환 코루틴 실행 위임 (DontDestroyOnLoad 오브젝트에서 실행)
    /// </summary>
    public void StartSceneTransition(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    /// <summary>
    /// NetworkManager 연결 끊김 콜백 — 방장 퇴장 감지용
    /// </summary>
    void HandleClientDisconnect(ulong clientId)
    {
        // 서버 역할이면 클라이언트 연결 끊김으로 무시
        if (NetworkManager.Singleton.IsServer) return;
        if (_currentLobby == null) return;

        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

        _ = HandleHostLeftAsync();
    }

    /// <summary>
    /// 방장 퇴장 처리 — 로비 정리 후 이벤트 발행
    /// </summary>
    async Task HandleHostLeftAsync()
    {
        // 폴링 즉시 중단을 위해 먼저 null 설정 후 ID 보존
        string lobbyId = _currentLobby.Id;
        _currentLobby = null;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(
                lobbyId,
                AuthenticationService.Instance.PlayerId
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning($"호스트 퇴장 후 로비 정리 실패:{e.Message}");
        }

        OnHostLeft?.Invoke();
    }

    /// <summary>
    /// 플레이어 데이터 생성 (닉네임 + 준비 상태)
    /// </summary>
    Player MakePlayerData(bool isReady)
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {
                    KeyNickname,
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _nickname)
                },
                {
                    KeyIsReady,
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member,
                        (isReady) ? "true" : "false"
                    )
                }
            }
        };
    }
}