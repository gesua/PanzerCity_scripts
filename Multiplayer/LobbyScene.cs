using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로비 씬 총괄 — 닉네임/로비/룸 패널 전환 관리
/// </summary>
public class LobbyScene : MonoBehaviour
{
    [Header("----- 씬 -----")]
    [SerializeField] AudioListener _audioListener;
    [SerializeField] EventSystem _eventSystem;

    [Header("----- 닉네임 패널 -----")]
    [SerializeField] GameObject _nicknamePanel;
    [SerializeField] TMP_InputField _nicknameInput;

    [Header("----- 로비 패널 -----")]
    [SerializeField] GameObject _lobbyPanel;
    [SerializeField] Button _refreshButton;
    [SerializeField] TMP_InputField _joinRoomNameInput;
    [SerializeField] TMP_InputField _joinRoomPasswordInput;
    [SerializeField] Transform _lobbyListParent;
    [SerializeField] GameObject _lobbyItemPrefab;
    [SerializeField] TMP_Text _statusText;
    [SerializeField] GameObject _createPanel;
    [SerializeField] TMP_InputField _createRoomNameInput;
    [SerializeField] TMP_InputField _createRoomPasswordInput;

    [Header("----- 룸 패널 -----")]
    [SerializeField] GameObject _roomPanel;
    [SerializeField] RoomUI _roomUI;

    bool _isRefreshing;      // 새로고침 연타 방지
    bool _isCreatingRoom;    // 방 만들기 중복 클릭 방지
    bool _isJoining;         // 방 참가 중복 클릭 방지
    bool _isFindingRoom;     // 방 이름 검색 중복 클릭 방지

    void Start()
    {
        LobbyManager.Instance.OnLobbyListUpdated += RefreshLobbyListUI;
        LobbyManager.Instance.OnStatusChanged += UpdateStatus;
        LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdated;
        LobbyManager.Instance.OnLeftLobby += HandleLeftLobby;
        LobbyManager.Instance.OnHostLeft += HandleHostLeft;
        LobbyManager.Instance.OnKicked += HandleKicked;
        LobbyManager.Instance.OnGameStart += HandleGameStart;

        LobbyManager.Instance.PendingLobbyAudioListener = _audioListener; // AudioListener 메시지 처리용

        ShowPanel(_nicknamePanel);
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance == null) return;

        LobbyManager.Instance.OnLobbyListUpdated -= RefreshLobbyListUI;
        LobbyManager.Instance.OnStatusChanged -= UpdateStatus;
        LobbyManager.Instance.OnLobbyUpdated -= HandleLobbyUpdated;
        LobbyManager.Instance.OnLeftLobby -= HandleLeftLobby;
        LobbyManager.Instance.OnHostLeft -= HandleHostLeft;
        LobbyManager.Instance.OnKicked -= HandleKicked;
        LobbyManager.Instance.OnGameStart -= HandleGameStart;
    }

    /// <summary>
    /// 패널 전환
    /// </summary>
    void ShowPanel(GameObject targetPanel)
    {
        _nicknamePanel.SetActive(_nicknamePanel == targetPanel);
        _lobbyPanel.SetActive(_lobbyPanel == targetPanel);
        _roomPanel.SetActive(_roomPanel == targetPanel);

        // 로비 진입 시 목록 자동 새로고침
        if (targetPanel == _lobbyPanel)
        {
            _ = LobbyManager.Instance.RefreshLobbyListAsync();
        }
    }

    /// <summary>
    /// 닉네임 확인 → 로비 패널로
    /// </summary>
    public void OnNicknameConfirmed()
    {
        string nickname = _nicknameInput.text.Trim();
        if (nickname == "") return;

        LobbyManager.Instance.SetNickname(nickname);
        ShowPanel(_lobbyPanel);

        UpdateStatus($"{nickname} Login successful");
    }

    /// <summary>
    /// 방 만들기 UI 띄우기
    /// </summary>
    public void CreateUIOpen()
    {
        _createPanel.SetActive(true);
    }

    /// <summary>
    /// 방 만들기 UI 닫기
    /// </summary>
    public void CreateUIClose()
    {
        _createPanel.SetActive(false);
    }

    /// <summary>
    /// 방 만들기 → 룸 패널로
    /// </summary>
    public async void OnCreateRoomClicked()
    {
        if (_isCreatingRoom) return;
        _isCreatingRoom = true;

        try
        {
            string trimmed = _createRoomNameInput.text.Trim();
            string roomName = (trimmed == "") ?
                $"{LobbyManager.Instance.Nickname} Room" : trimmed;
            string password = _createRoomPasswordInput.text.Trim();

            // 방 이름 중복 검사
            Lobby existingLobby = await LobbyManager.Instance.FindLobbyByNameAsync(roomName);

            if (existingLobby != null)
            {
                // 중복된 이름이 발견되면 상태 메시지를 띄움
                UpdateStatus("이미 존재하는 방 이름입니다. 다른 이름을 사용해주세요.");
                return;
            }

            _createPanel.SetActive(false);

            await LobbyManager.Instance.CreateLobbyAsync(roomName, password);

            // 생성 실패 시 방 패널로 넘어가지 않음
            if (LobbyManager.Instance.CurrentLobby == null)
            {
                return;
            }

            ShowPanel(_roomPanel);
            _roomUI.Refresh(LobbyManager.Instance.CurrentLobby);
        }
        finally
        {
            _isCreatingRoom = false;
        }
    }

    /// <summary>
    /// 입장 버튼 — 선택된 방으로 참가
    /// </summary>
    public async void OnJoinRoomClicked()
    {
        if (_isFindingRoom) return;
        _isFindingRoom = true;

        try
        {
            string roomName = _joinRoomNameInput.text.Trim();
            if (roomName == "") return;

            Lobby lobby = await LobbyManager.Instance.FindLobbyByNameAsync(roomName);

            if (lobby == null)
            {
                UpdateStatus("방을 찾을 수 없습니다.");
                return;
            }

            TryJoinLobby(lobby.Id);
        }
        finally
        {
            _isFindingRoom = false;
        }
    }

    /// <summary>
    /// 로비 참가 시도 (목록 참가 버튼 / 입장 버튼 공용)
    /// </summary>
    async void TryJoinLobby(string lobbyId)
    {
        if (_isJoining) return;
        _isJoining = true;

        try
        {
            string password = _joinRoomPasswordInput.text.Trim();
            await LobbyManager.Instance.JoinLobbyAsync(lobbyId, password);

            // 참가 실패 시 패널 전환 안 함
            if (LobbyManager.Instance.CurrentLobby == null) return;

            _joinRoomNameInput.text = LobbyManager.Instance.CurrentLobby.Name;
            ShowPanel(_roomPanel);
            _roomUI.Refresh(LobbyManager.Instance.CurrentLobby);
        }
        finally
        {
            _isJoining = false;
        }
    }

    /// <summary>
    /// 빠른 시작 버튼 (조건에 맞는 방 자동 검색 후 참가)
    /// </summary>
    public async void OnQuickJoinClicked()
    {
        // 일반 참가나 빠른 시작이 이미 진행 중이면 중복 클릭 방지
        if (_isJoining) return;
        _isJoining = true;

        try
        {
            await LobbyManager.Instance.QuickJoinLobbyAsync();

            // 참가 실패(또는 빈 방 없음) 시 패널 전환 안 함
            if (LobbyManager.Instance.CurrentLobby == null) return;

            // 방 UI로 이동 및 화면 갱신
            _joinRoomNameInput.text = LobbyManager.Instance.CurrentLobby.Name;
            ShowPanel(_roomPanel);
            _roomUI.Refresh(LobbyManager.Instance.CurrentLobby);
        }
        finally
        {
            _isJoining = false;
        }
    }

    /// <summary>
    /// 로비 목록 새로고침
    /// </summary>
    public async void OnRefreshClicked()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        _refreshButton.interactable = false;

        try
        {
            await LobbyManager.Instance.RefreshLobbyListAsync();

            UpdateStatus($"Room list refreshed");

            // 새로고침 최소 간격 2초
            await System.Threading.Tasks.Task.Delay(2000);
        }
        finally
        {
            _refreshButton.interactable = true;
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// 메인화면 버튼 → 타이틀 씬으로 복귀 (로비 목록 패널 전용)
    /// </summary>
    public void OnMainMenuClicked()
    {
        LobbyManager.Instance.StartSceneTransition(ReturnToTitleRoutine());
    }

    /// <summary>
    /// 로비 목록 UI 갱신
    /// </summary>
    void RefreshLobbyListUI(List<Lobby> lobbies)
    {
        foreach (Transform child in _lobbyListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbies)
        {
            GameObject item = Instantiate(_lobbyItemPrefab, _lobbyListParent);

            if (item.TryGetComponent(out LobbyItemUI itemUI) == false) continue;

            bool isLocked = lobby.IsLocked;
            bool isFull = lobby.AvailableSlots == 0;
            bool canJoin = (isLocked == false);

            // 방 이름 + 인원 + 상태 표시
            string statusTag = (isLocked) ? " [Started]" : "";
            string passwordTag = (lobby.HasPassword) ? " [Locked]" : "";
            string displayText = $"{lobby.Name} [{lobby.Players.Count}/{lobby.MaxPlayers}]{statusTag}{passwordTag}";

            string lobbyId = lobby.Id;
            string lobbyName = lobby.Name;

            itemUI.Setup(displayText, canJoin,
                onSelect: () =>
                {
                    _joinRoomNameInput.text = lobbyName;
                },
                onJoinRequested: () => TryJoinLobby(lobbyId));
        }
    }

    /// <summary>
    /// 룸 상태 갱신 (폴링)
    /// </summary>
    void HandleLobbyUpdated(Lobby lobby)
    {
        if (_roomPanel.activeSelf == false) return;
        _roomUI.Refresh(lobby);
    }

    /// <summary>
    /// 좀비 네트워크 연결을 확실하게 끊어주는 헬퍼 함수
    /// </summary>
    void ShutdownNetwork()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    /// <summary>
    /// 직접 나감 → 로비 패널로
    /// </summary>
    void HandleLeftLobby()
    {
        Debug.Log("로비로 직접 나감");
        ShutdownNetwork();
        ShowPanel(_lobbyPanel);
        UpdateStatus($"");
    }

    /// <summary>
    /// 방장이 연결 끊음 → 로비 패널로
    /// </summary>
    void HandleHostLeft()
    {
        Debug.Log("방장이 연결 끊음");
        ShutdownNetwork();
        ShowPanel(_lobbyPanel);
        UpdateStatus($"");
    }

    /// <summary>
    /// 강퇴당함 → 로비 패널로
    /// </summary>
    void HandleKicked()
    {
        Debug.Log("강퇴당함");
        ShutdownNetwork();
        ShowPanel(_lobbyPanel);
        UpdateStatus($"");
    }

    /// <summary>
    /// 게임 시작 신호 수신 → 씬 전환
    /// </summary>
    void HandleGameStart()
    {
        LobbyManager.Instance.StartSceneTransition(StartMultiplayerRoutine());
    }

    IEnumerator StartMultiplayerRoutine()
    {
        // Pool 미리 만들기
        GameManager.Instance.PoolManager.GetPool("DroppedItem");
        GameManager.Instance.PoolManager.GetPool("Shell");

        LoadingUI loadingUI = GameManager.Instance.LoadingUI;
        loadingUI.Show();

        if (_audioListener != null) _audioListener.enabled = false;
        if (_eventSystem != null) _eventSystem.gameObject.SetActive(false);

        // 멀티용 Game 씬 로드 (호스트만 요청, 클라이언트는 Netcode가 자동으로 밀어줌)
        yield return LoadNetworkedSceneRoutine("Game_Multi", 0f, 0.45f);

        // Stage 씬 로드
        string stageName = LobbyManager.Instance.FirstStageName;
        yield return LoadNetworkedSceneRoutine(stageName, 0.45f, 0.9f);

        // 로딩바 100% + 대기 문구 표시
        loadingUI.ShowWaitingForOthers();

        // 전원 씬 로드 완료 신호 대기(로딩창/적 스폰을 동시에 시작하기 위함)
        yield return WaitForAllClientsReadyRoutine();

        loadingUI.Hide();
        SceneManager.UnloadSceneAsync("Lobby");
    }

    /// <summary>
    /// NetworkGameManager가 전원 씬 로드 완료 신호를 보낼 때까지 대기
    /// </summary>
    IEnumerator WaitForAllClientsReadyRoutine()
    {
        bool isReady = false;
        void HandleAllClientsReady() => isReady = true;

        NetworkGameManager.Instance.OnAllClientsReady += HandleAllClientsReady;

        yield return new WaitUntil(() => isReady);

        NetworkGameManager.Instance.OnAllClientsReady -= HandleAllClientsReady;
    }

    /// <summary>
    /// 씬 로드 요청(호스트 전용) 및 완료 대기(호스트/클라이언트 공통)
    /// NetworkSceneManager.LoadScene은 호스트/서버만 호출 가능해서, 클라이언트는 요청 없이 대기만 함
    /// (호스트가 요청하면 Netcode가 연결된 클라이언트에도 같은 씬을 자동으로 밀어넣어줌)
    /// OnLoad로 내 로컬 AsyncOperation을 받아서 LoadingUI에 [rangeStart, rangeEnd] 구간으로 진행률을 반영함
    /// </summary>
    IEnumerator LoadNetworkedSceneRoutine(string sceneName, float rangeStart, float rangeEnd)
    {
        AsyncOperation localOp = null;

        // 서버(호스트) 입장에서는 OnLoad가 연결된 클라이언트 수만큼 반복 호출됨(전원의 로드 시작을 다 통지받음)
        // asyncOperation은 그 클라이언트 로컬의 값이라 내 것이 아니면 의미가 없으므로, 반드시 내 clientId만 필터링해야 함
        void HandleLoad(ulong clientId, string loadedSceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;
            if (loadedSceneName != sceneName) return;

            localOp = asyncOperation;
        }

        NetworkManager.Singleton.SceneManager.OnLoad += HandleLoad;

        if (NetworkManager.Singleton.IsHost)
        {
            SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"씬 로드 요청 실패: {sceneName} ({status})");
            }
        }

        // OnLoad 유실 등으로 localOp를 못 받는 경우에도 멈추지 않도록, 씬이 실제로 로드 완료됐는지도 같이 조건에 둠
        yield return new WaitUntil(() => localOp != null || SceneManager.GetSceneByName(sceneName).isLoaded);

        NetworkManager.Singleton.SceneManager.OnLoad -= HandleLoad;

        // localOp를 정상적으로 받았을 때만 진행률 애니메이션 재생(못 받았으면 진행률 표시 없이 다음 단계로)
        if (localOp != null)
        {
            yield return GameManager.Instance.LoadingUI.UpdateProgress(localOp, rangeStart, rangeEnd);
        }

        // 씬 로드 완료까지 대기 (호스트가 요청했든 클라이언트가 자동으로 받았든 로컬 씬 상태로 확인)
        yield return new WaitUntil(() => SceneManager.GetSceneByName(sceneName).isLoaded);
    }

    /// <summary>
    /// 메인화면 복귀 씬 전환
    /// </summary>
    IEnumerator ReturnToTitleRoutine()
    {
        if (_audioListener != null) _audioListener.enabled = false;
        if (_eventSystem != null) _eventSystem.gameObject.SetActive(false);

        yield return SceneManager.LoadSceneAsync("Title");
    }

    void UpdateStatus(string message)
    {
        if (_statusText == null) return;
        _statusText.text = message;
    }
}