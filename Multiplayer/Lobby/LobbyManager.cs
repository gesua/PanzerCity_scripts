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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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

    [SerializeField] NetworkObject _roomChatRelayPrefab; // 로비/룸 채팅 릴레이 — CreateLobbyAsync(StartHost 직후)에서 수동 스폰
    NetworkObject _spawnedRoomChatRelay; // 게임 시작 시 명시적으로 Despawn하기 위해 보관

    Lobby _currentLobby;
    float _heartbeatTimer;
    float _lobbyPollTimer;
    string _nickname;
    string _hostedLobbyId; // 로비 단계 정리용으로 유지
    string _joinedLobbyId; // 클라이언트용:_currentLobby와 달리 게임 시작 후에도 유지되어 방장 이탈 감지에 사용됨
    bool _hostLeftNotificationPending; // 타이틀 씬에서 "방장이 나가서 왔다" 팝업을 한 번만 표시하기 위한 플래그
    Dictionary<ulong, string> _clientIdToPlayerId = new Dictionary<ulong, string>(); // Netcode clientId ↔ Lobby Player.Id

    bool _isHeartbeating; // 하트비트 중복 방지용(응답이 주기보다 늦게 오면 재진입 가능)
    bool _isPolling; // 폴링 중복 방지용(GetLobbyAsync 응답이 폴링 주기보다 늦게 오면 재진입해서 두 번 실행될 수 있음)

    // 최근 검색된 로비 목록 캐싱 및 API Rate Limit 대응용
    List<Lobby> _cachedLobbies = new List<Lobby>();
    float _lastQueryTime;

    public Lobby CurrentLobby => _currentLobby;
    public string Nickname => _nickname;
    public string FirstStageName { get; private set; } = "Stage01"; // 멀티 시작 스테이지
    public bool IsHost => _currentLobby != null &&
        _currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    public AudioListener PendingLobbyAudioListener { get; set; } // AudioListener 메시지 처리용
    public EventSystem PendingLobbyEventSystem { get; set; } // EventSystem 중복 방지용

    public event Action<List<Lobby>> OnLobbyListUpdated;
    public event Action<Lobby> OnLobbyUpdated; // 룸 상태 갱신
    public event Action<string, object[]> OnStatusChanged; // 상태 메시지
    public event Action OnKicked;    // 강퇴당함
    public event Action OnLeftLobby; // 직접 나감
    public event Action OnHostLeft;  // 방장이 연결 끊음
    public event Action OnGameStart; // 게임 시작 신호
    public event Action<AsyncOperation> OnNetworkSceneLoadStarted; // 클라이언트가 Game_Multi 씬 로드 시작을 수신

    public int MyPlayerIndex { get; private set; } = -1; // 룸에서 배정받은 자리(Players 리스트 내 위치) — Game_Multi 씬 로드 시작 시 확정

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

            AuthenticationService.Instance.Expired += HandleAuthExpired;

            if (AuthenticationService.Instance.IsSignedIn == false)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke("UI_MP_ERR_INIT_FAIL", new object[] { e.Message });
        }
    }

    /// <summary>
    /// 액세스 토큰 만료 감지 — 장시간 유휴 후 자동 갱신 실패 시 발생
    /// (에디터를 오래 켜두고 자리를 비웠다가 돌아왔을 때 재현되는 문제 대응)
    /// </summary>
    void HandleAuthExpired()
    {
        _ = ReauthenticateAsync();
    }

    /// <summary>
    /// 만료된 인증 세션 재로그인
    /// </summary>
    async Task ReauthenticateAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke("UI_MP_ERR_AUTH_FAIL", new object[] { e.Message });
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
        if (_isHeartbeating) return; // 이전 하트비트가 아직 응답 대기 중이면 중복 실행 방지

        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer < HeartbeatInterval) return;

        _heartbeatTimer = 0f;
        _isHeartbeating = true;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"하트비트 실패:{e.Message}");
        }
        finally
        {
            _isHeartbeating = false;
        }
    }

    /// <summary>
    /// 로비 상태 폴링 — 강퇴/게임 시작 감지 포함
    /// </summary>
    async void HandleLobbyPoll()
    {
        if (_currentLobby == null) return;
        if (_isPolling) return; // 이전 폴링이 아직 응답 대기 중이면 중복 실행 방지

        _lobbyPollTimer += Time.deltaTime;
        if (_lobbyPollTimer < LobbyPollInterval) return;

        _lobbyPollTimer = 0f;
        _isPolling = true;

        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);

            // 대기하는 동안 로비를 이미 나갔거나 정리된 경우, 뒤늦게 온 응답은 무시
            if (_currentLobby == null) return;

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
                _joinedLobbyId = null;
                NetworkManager.Singleton.Shutdown();
                OnKicked?.Invoke();
                return;
            }

            _currentLobby = lobby;
            OnLobbyUpdated?.Invoke(_currentLobby);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"로비 폴링 실패:{e.Message}");
        }
        finally
        {
            _isPolling = false;
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
    /// UGS 로비 비밀번호 최소 8자리 제한 우회용 헬퍼 함수
    /// 유저가 1글자만 입력해도 강제로 8자리 이상으로 부풀려서 통과시킵니다.
    /// </summary>
    string GetUGSPassword(string rawPassword)
    {
        if (string.IsNullOrEmpty(rawPassword)) return null;

        // 뒤에 고정된 시크릿 문자열을 붙여 무조건 8자리 이상이 되게 만듭니다.
        return rawPassword + "_Secret";
    }

    /// <summary>
    /// 공개 방 생성
    /// </summary>
    public async Task CreateLobbyAsync(string lobbyName, string password, int maxPlayers = 4)
    {
        try
        {
            OnStatusChanged?.Invoke("UI_MP_MSG_CREATING_ROOM", null);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Password = GetUGSPassword(password),
                Data = new Dictionary<string, DataObject>
                {
                    { KeyRelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                },
                Player = MakePlayerData(isReady: false)
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            _hostedLobbyId = _currentLobby.Id;

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

            if (NetworkManager.Singleton.TryGetComponent(out UnityTransport transport))
            {
                transport.SetRelayServerData(relayServerData);
            }

            _clientIdToPlayerId.Clear();

            // 접속 승인 구독 (중복 방지) — clientId ↔ Player.Id 매핑용
            NetworkManager.Singleton.ConnectionApprovalCallback -= HandleConnectionApproval;
            NetworkManager.Singleton.ConnectionApprovalCallback += HandleConnectionApproval;

            NetworkManager.Singleton.StartHost();

            // 연결 끊김 감지 구독 (중복 방지)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            // 룸 채팅 릴레이 스폰 — Game_Multi 씬 로드 전까지는 NetworkGameManager가 없으므로 별도로 스폰
            _spawnedRoomChatRelay = Instantiate(_roomChatRelayPrefab);
            _spawnedRoomChatRelay.Spawn();

            OnStatusChanged?.Invoke("UI_MP_MSG_ROOM_CREATED", new object[] { _currentLobby.Name });
        }
        catch (Exception e)
        {
            // 생성 실패 시 현재 로비 초기화
            _currentLobby = null;
            OnStatusChanged?.Invoke("UI_MP_ERR_CREATE_FAIL", new object[] { e.Message });
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
                Count = 50
            };

            _lastQueryTime = Time.time;
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            _cachedLobbies = response.Results ?? new List<Lobby>();
            OnLobbyListUpdated?.Invoke(_cachedLobbies);
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke("UI_MP_ERR_LOBBY_FETCH_FAIL", new object[] { e.Message });
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
            OnStatusChanged?.Invoke("UI_MP_ERR_SEARCH_FAIL", new object[] { e.Message });
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
            OnStatusChanged?.Invoke("UI_MP_MSG_JOINING_ROOM", null);

            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Password = GetUGSPassword(password),
                Player = MakePlayerData(isReady: false)
            };

            _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            _joinedLobbyId = _currentLobby.Id; // _currentLobby와 달리 게임 시작 후에도 유지되어 방장 이탈 감지에 쓰임

            string joinCode = _currentLobby.Data[KeyRelayJoinCode].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            if (NetworkManager.Singleton.TryGetComponent(out UnityTransport transport))
            {
                transport.SetRelayServerData(relayServerData);
            }

            // 접속 승인 시 호스트가 clientId ↔ Player.Id를 매핑할 수 있도록 전달
            NetworkManager.Singleton.NetworkConfig.ConnectionData =
                System.Text.Encoding.UTF8.GetBytes(AuthenticationService.Instance.PlayerId);

            if (NetworkManager.Singleton.StartClient())
            {
                SubscribeToNetworkSceneLoad();
            }

            // 연결 끊김 감지 구독 (중복 방지)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            OnStatusChanged?.Invoke("UI_MP_MSG_JOIN_SUCCESS", new object[] { _currentLobby.Name });
        }
        catch (Exception e)
        {
            // 참가 실패 시 _currentLobby 보장
            _currentLobby = null;
            _joinedLobbyId = null;
            OnStatusChanged?.Invoke("UI_MP_ERR_JOIN_FAIL", new object[] { e.Message });
        }
    }

    /// <summary>
    /// 빠른 시작 (자동 매치메이킹:인원 많은 방 우선)
    /// </summary>
    public async Task QuickJoinLobbyAsync()
    {
        try
        {
            OnStatusChanged?.Invoke("UI_MP_MSG_QUICK_SEARCHING", null);

            // 이미 새로고침되어 갖고 있는 캐시 목록에서 조건에 맞는 방(비밀번호 없음, 미잠금, 빈자리 있음) 필터링
            List<Lobby> candidates = new List<Lobby>();
            if (_cachedLobbies != null && _cachedLobbies.Count > 0)
            {
                foreach (Lobby lobby in _cachedLobbies)
                {
                    if (lobby.HasPassword == false &&
                        lobby.IsLocked == false &&
                        lobby.AvailableSlots > 0)
                    {
                        candidates.Add(lobby);
                    }
                }
            }

            // 인원이 가장 많은 순서대로 정렬 (남은 자리가 적은 순)
            candidates.Sort((a, b) => a.AvailableSlots.CompareTo(b.AvailableSlots));

            // 캐시된 후보 방이 있다면 참가를 시도 (네트워크 Query 없이 즉시 실행)
            foreach (Lobby targetLobby in candidates)
            {
                try
                {
                    await JoinLobbyAsync(targetLobby.Id, null);
                    return; // 성공 시 즉시 종료
                }
                catch
                {
                    // 그 사이 다른 사람이 들어가서 실패한 경우 다음 후보 방 시도
                    continue;
                }
            }

            // 캐시 목록에 입장 가능한 방이 없는 경우 직접 UGS 서버에 검색 쿼리
            // 단, 마지막 검색 후 1.2초가 지나지 않았다면 Rate Limit 방지를 위해 대기 후 검색
            float timeSinceLastQuery = Time.time - _lastQueryTime;
            if (timeSinceLastQuery < 1.2f)
            {
                await Task.Delay((int)((1.2f - timeSinceLastQuery) * 1000));
            }

            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 20, // 최대 20개 탐색
                Filters = new List<QueryFilter>
                {
                    // 풀방 제외 (빈 자리가 0보다 큰 방만)
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    // 사람이 많은 순서로 오름차순 정렬
                    new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots)
                }
            };

            _lastQueryTime = Time.time;
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            // 목록 중 완벽한 조건의 방 하나 찾기
            Lobby fallbackLobby = null;
            foreach (Lobby lobby in response.Results)
            {
                // 비밀번호가 없고, 게임이 아직 시작되지 않은 방(잠기지 않은 방)
                if (lobby.HasPassword == false && lobby.IsLocked == false)
                {
                    fallbackLobby = lobby;
                    break; // 가장 먼저 찾은(가장 인원 많은) 방 채택
                }
            }

            if (fallbackLobby == null)
            {
                OnStatusChanged?.Invoke("UI_MP_ERR_QUICK_EMPTY", null);
                return;
            }

            // 찾은 방의 ID를 활용해 기존 참가 로직 그대로 실행
            await JoinLobbyAsync(fallbackLobby.Id, null);
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke("UI_MP_ERR_QUICK_FAIL", new object[] { e.Message });
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

            // 룸 채팅 릴레이 정리 — DontDestroyOnLoad가 아니라서 씬 전환 시 Unity가 그냥 파괴해버리면
            // NGO 입장에선 정상 Despawn 절차를 안 거친 게 되어 클라이언트에 지연된 삭제 메시지 경고가 남을 수 있음
            if (_spawnedRoomChatRelay != null)
            {
                _spawnedRoomChatRelay.Despawn();
                _spawnedRoomChatRelay = null;
            }

            // 호스트는 자신의 _currentLobby가 바로 아래서 null이 되므로, 그 전에 자리를 먼저 확정해둠
            // (HandleNetworkSceneLoad가 나중에 호스트 자신에 대해서도 호출되지만, 그때는 이미 null이라 스킵됨)
            ResolveMyPlayerIndex();

            // 호스트는 폴링 대기 없이 즉시 처리
            _currentLobby = null;
            OnGameStart?.Invoke();
        }
        catch (Exception e)
        {
            OnStatusChanged?.Invoke("UI_MP_ERR_START_FAIL", new object[] { e.Message });
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
        // _currentLobby는 게임 시작 신호와 함께 곧바로 null이 되므로, 역할에 맞게 유지되는 ID를 대신 사용
        // (RemoveDisconnectedPlayerAsync의 _hostedLobbyId와 같은 이유)
        // 주의:IsHost 프로퍼티 자체가 _currentLobby 기반이라 게임 중엔 항상 false가 되므로 여기선 쓸 수 없음
        bool isHost = _hostedLobbyId != null;
        string lobbyId = isHost ? _hostedLobbyId : _joinedLobbyId;
        if (lobbyId == null) return;

        try
        {
            // 구독 해제 — Shutdown 시 HandleClientDisconnect 방지
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

            // 호스트는 로비 자체를 삭제, 클라이언트는 본인만 나감
            if (isHost)
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    lobbyId,
                    AuthenticationService.Instance.PlayerId
                );
            }

            _currentLobby = null;
            _hostedLobbyId = null;
            _joinedLobbyId = null;
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
    /// NetworkManager 연결 끊김 콜백 — 클라이언트는 방장 퇴장 감지, 호스트는 끊긴 클라이언트 로비 정리
    /// </summary>
    void HandleClientDisconnect(ulong clientId)
    {
        // 서버 역할이면 끊긴 클라이언트 정리
        if (NetworkManager.Singleton.IsServer)
        {
            _ = RemoveDisconnectedPlayerAsync(clientId);
            return;
        }

        if (_joinedLobbyId == null) return;

        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

        _ = HandleHostLeftAsync();
    }

    /// <summary>
    /// 클라이언트 접속 승인 — clientId ↔ Lobby Player.Id 매핑 저장 (스폰은 NetworkGameManager가 별도 처리)
    /// </summary>
    void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string playerId = System.Text.Encoding.UTF8.GetString(request.Payload);
        if (string.IsNullOrEmpty(playerId) == false)
        {
            _clientIdToPlayerId[request.ClientNetworkId] = playerId;
        }

        response.Approved = true;
    }

    /// <summary>
    /// 강제 종료 등으로 연결이 끊긴 클라이언트를 로비에서 제거 (호스트 전용)
    /// _currentLobby는 게임 시작 신호와 함께 곧바로 null이 되므로, 로비 단계 동안 유지되는
    /// _hostedLobbyId를 대신 사용함
    /// </summary>
    async Task RemoveDisconnectedPlayerAsync(ulong clientId)
    {
        if (_hostedLobbyId == null) return;
        if (_clientIdToPlayerId.TryGetValue(clientId, out string playerId) == false) return;

        _clientIdToPlayerId.Remove(clientId);

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_hostedLobbyId, playerId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"연결 끊긴 플레이어 로비 정리 실패:{e.Message}");
        }
    }

    /// <summary>
    /// 방장 퇴장 처리 — 로비 정리 후 이벤트 발행
    /// </summary>
    async Task HandleHostLeftAsync()
    {
        // 폴링 즉시 중단을 위해 먼저 null 설정 후 ID 보존
        string lobbyId = _joinedLobbyId;
        _currentLobby = null;
        _joinedLobbyId = null;

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

        // 타이틀 씬에서 "방장이 나가서 왔다"는 팝업을 띄울 수 있도록 알림 플래그 세팅(ConsumeHostLeftNotification 참고)
        _hostLeftNotificationPending = true;

        OnHostLeft?.Invoke();
    }

    /// <summary>
    /// 방장 이탈로 메인메뉴에 왔다는 알림이 대기 중이면 true를 반환하고 플래그를 소비함(한 번 확인하면 다음부턴 false)
    /// 타이틀 씬이 Start()에서 호출해서 팝업 표시 여부를 판단하는 용도
    /// </summary>
    public bool ConsumeHostLeftNotification()
    {
        if (_hostLeftNotificationPending == false) return false;

        _hostLeftNotificationPending = false;
        return true;
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


    void SubscribeToNetworkSceneLoad()
    {
        NetworkManager.Singleton.SceneManager.OnLoad -= HandleNetworkSceneLoad;
        NetworkManager.Singleton.SceneManager.OnLoad += HandleNetworkSceneLoad;
    }

    void HandleNetworkSceneLoad(
        ulong clientId,
        string sceneName,
        LoadSceneMode loadSceneMode,
        AsyncOperation asyncOperation)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        if (sceneName != "Game_Multi") return;

        LoadingUI loadingUI = GameManager.Instance.LoadingUI;
        if (loadingUI.gameObject.activeSelf == false)
        {
            loadingUI.Show();
        }

        // 룸에서 배정받은 자리 확정(호스트는 StartGameAsync에서 이미 계산해뒀을 수 있음 — 그 경우 아래는 그냥 스킵됨)
        ResolveMyPlayerIndex();

        _currentLobby = null;
        OnNetworkSceneLoadStarted?.Invoke(asyncOperation);
    }

    /// <summary>
    /// 본인의 룸 자리 인덱스 확정 — _currentLobby가 유효한 동안 호출해야 함(null이면 조용히 스킵)
    /// 호스트는 자신의 _currentLobby가 StartGameAsync에서 곧바로 null이 되므로 거기서 먼저 계산해두고,
    /// 클라이언트는 HandleNetworkSceneLoad 시점까지 _currentLobby가 유효하게 남아있으므로 거기서 계산함
    /// </summary>
    void ResolveMyPlayerIndex()
    {
        if (_currentLobby == null) return;

        string myPlayerId = AuthenticationService.Instance.PlayerId;
        for (int i = 0; i < _currentLobby.Players.Count; i++)
        {
            if (_currentLobby.Players[i].Id != myPlayerId) continue;

            MyPlayerIndex = i;
            break;
        }
    }

    /// <summary>
    /// clientId로 로비 닉네임 조회(호스트 전용 — 서버에서만 호출) — 호스트 자기 자신은 ConnectionApproval의
    /// Payload가 비어있어 _clientIdToPlayerId에 매핑되지 않으므로 LocalClientId로 별도 분기
    /// 조회 실패 시 빈 문자열 반환(호출부에서 P# 등으로 대체 표시)
    /// </summary>
    public string GetNicknameByClientId(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return _nickname;
        if (_clientIdToPlayerId.TryGetValue(clientId, out string playerId) == false) return string.Empty;
        if (_currentLobby == null) return string.Empty;

        foreach (Player player in _currentLobby.Players)
        {
            if (player.Id != playerId) continue;
            if (player.Data == null) continue;
            if (player.Data.TryGetValue(KeyNickname, out PlayerDataObject data) == false) continue;

            return data.Value;
        }

        return string.Empty;
    }

    /// <summary>
    /// clientId로 룸에서 배정받은 자리(Players 리스트 내 위치) 조회(호스트 전용 — 서버에서만 호출)
    /// GetNicknameByClientId와 동일하게 호스트 자기 자신은 LocalClientId로 별도 분기
    /// 조회 실패 시 -1 반환(호출부에서 clientId 기반 등으로 대체 표시)
    /// </summary>
    public int GetPlayerIndexByClientId(ulong clientId)
    {
        string playerId;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            playerId = AuthenticationService.Instance.PlayerId;
        }
        else if (_clientIdToPlayerId.TryGetValue(clientId, out string mappedId))
        {
            playerId = mappedId;
        }
        else
        {
            return -1;
        }

        if (_currentLobby == null) return -1;

        for (int i = 0; i < _currentLobby.Players.Count; i++)
        {
            if (_currentLobby.Players[i].Id != playerId) continue;

            return i;
        }

        return -1;
    }
}