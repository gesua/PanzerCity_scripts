using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로비 씬 총괄 — 닉네임/로비/룸 패널 전환 관리
/// </summary>
public class LobbyScene : MonoBehaviour
{
    [Header("----- 씬 -----")]
    [SerializeField] AudioListener _audioListener;
    [SerializeField] UnityEngine.EventSystems.EventSystem _eventSystem;

    [Header("----- 닉네임 패널 -----")]
    [SerializeField] GameObject _nicknamePanel;
    [SerializeField] TMP_InputField _nicknameInput;

    [Header("----- 로비 패널 -----")]
    [SerializeField] GameObject _lobbyPanel;
    [SerializeField] TMP_InputField _joinRoomNameInput;
    [SerializeField] Transform _lobbyListParent;
    [SerializeField] GameObject _lobbyItemPrefab;
    [SerializeField] TMP_Text _statusText;
    [SerializeField] GameObject _createPanel;
    [SerializeField] TMP_InputField _createRoomNameInput;

    [Header("----- 룸 패널 -----")]
    [SerializeField] GameObject _roomPanel;
    [SerializeField] RoomUI _roomUI;

    bool _isCreatingRoom; // 방 만들기 중복 클릭 방지

    void Start()
    {
        LobbyManager.Instance.OnLobbyListUpdated += RefreshLobbyListUI;
        LobbyManager.Instance.OnStatusChanged += UpdateStatus;
        LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdated;
        LobbyManager.Instance.OnLeftLobby += HandleLeftLobby;
        LobbyManager.Instance.OnHostLeft += HandleHostLeft;
        LobbyManager.Instance.OnKicked += HandleKicked;
        LobbyManager.Instance.OnGameStart += HandleGameStart;

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

        UpdateStatus($"{nickname} 로그인 성공");
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

        _createPanel.SetActive(false);

        try
        {
            string trimmed = _createRoomNameInput.text.Trim();
            string roomName = (trimmed == "") ?
                $"{LobbyManager.Instance.Nickname} Room" : trimmed;

            await LobbyManager.Instance.CreateLobbyAsync(roomName);
            ShowPanel(_roomPanel);
            _roomUI.Refresh(LobbyManager.Instance.CurrentLobby);
        }
        finally
        {
            _isCreatingRoom = false;
        }
    }

    /// <summary>
    /// 로비 목록 새로고침
    /// </summary>
    public async void OnRefreshClicked()
    {
        await LobbyManager.Instance.RefreshLobbyListAsync();
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
            bool canJoin = (isLocked == false);// && (isFull == false);

            // 방 이름 + 인원 + 상태 표시
            string statusTag = (isLocked) ? " [시작됨]" : "";
            string displayText = $"{lobby.Name} [{lobby.Players.Count}/{lobby.MaxPlayers}]{statusTag}";
            string lobbyName = lobby.Name;

            itemUI.Setup(displayText, canJoin, lobby.Id,
                onSelect: () => _joinRoomNameInput.text = lobbyName,
                onJoined: () =>
                {
                    ShowPanel(_roomPanel);
                    _roomUI.Refresh(LobbyManager.Instance.CurrentLobby);
                });
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
    /// 직접 나감 → 로비 패널로
    /// </summary>
    void HandleLeftLobby()
    {
        ShowPanel(_lobbyPanel);
    }

    /// <summary>
    /// 방장이 연결 끊음 → 로비 패널로
    /// </summary>
    void HandleHostLeft()
    {
        ShowPanel(_lobbyPanel);
    }

    /// <summary>
    /// 강퇴당함 → 로비 패널로
    /// </summary>
    void HandleKicked()
    {
        ShowPanel(_lobbyPanel);
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

        // Game 씬 로드
        AsyncOperation gameSceneLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        yield return gameSceneLoad;

        // Stage 씬 로드 (로딩 완료까지 대기)
        string stageName = LobbyManager.Instance.FirstStageName;
        AsyncOperation stageLoad = SceneManager.LoadSceneAsync(stageName, LoadSceneMode.Additive);
        stageLoad.allowSceneActivation = false;

        StartCoroutine(loadingUI.UpdateProgress(stageLoad));
        yield return new WaitUntil(() => stageLoad.progress >= 0.9f);

        stageLoad.allowSceneActivation = true;
        yield return stageLoad;

        yield return new WaitForSeconds(0.1f);

        loadingUI.Hide();
        SceneManager.UnloadSceneAsync("Lobby");
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