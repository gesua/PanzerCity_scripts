using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 씬 총괄 — 닉네임/로비/룸 패널 전환 관리
/// </summary>
public class LobbyScene : MonoBehaviour
{
    [Header("----- 닉네임 패널 -----")]
    [SerializeField] GameObject _nicknamePanel;
    [SerializeField] TMP_InputField _nicknameInput;
    [SerializeField] Button _nicknameConfirmButton;

    [Header("----- 로비 패널 -----")]
    [SerializeField] GameObject _lobbyPanel;
    [SerializeField] TMP_InputField _roomNameInput;
    [SerializeField] Button _createRoomButton;
    [SerializeField] Button _refreshButton;
    [SerializeField] Transform _lobbyListParent;
    [SerializeField] GameObject _lobbyItemPrefab;  // TMP_Text + Button 조합
    [SerializeField] TMP_Text _statusText;

    [Header("----- 룸 패널 -----")]
    [SerializeField] GameObject _roomPanel;
    [SerializeField] RoomUI _roomUI;

    void Start()
    {
        _nicknameConfirmButton.onClick.AddListener(OnNicknameConfirmed);
        _createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        _refreshButton.onClick.AddListener(OnRefreshClicked);

        LobbyManager.Instance.OnLobbyListUpdated += RefreshLobbyListUI;
        LobbyManager.Instance.OnStatusChanged += UpdateStatus;
        LobbyManager.Instance.OnLobbyUpdated += HandleLobbyUpdated;
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
    }

    /// <summary>
    /// 닉네임 확인 → 로비 패널로
    /// </summary>
    void OnNicknameConfirmed()
    {
        string nickname = _nicknameInput.text.Trim();
        if (nickname == "") return;

        LobbyManager.Instance.SetNickname(nickname);
        ShowPanel(_lobbyPanel);
    }

    /// <summary>
    /// 방 만들기 → 룸 패널로
    /// </summary>
    async void OnCreateRoomClicked()
    {
        string trimmed = _roomNameInput.text.Trim();
        string roomName = (trimmed == "") ?
            $"{LobbyManager.Instance.Nickname}의 방" : trimmed;

        await LobbyManager.Instance.CreateLobbyAsync(roomName);
        ShowPanel(_roomPanel);
        _roomUI.Refresh(LobbyManager.Instance.CurrentLobby);
    }

    /// <summary>
    /// 로비 목록 새로고침
    /// </summary>
    async void OnRefreshClicked()
    {
        await LobbyManager.Instance.RefreshLobbyListAsync();
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
            item.GetComponentInChildren<TMP_Text>().text =
                $"{lobby.Name}  [{lobby.Players.Count}/{lobby.MaxPlayers}]";

            string lobbyId = lobby.Id;
            item.GetComponentInChildren<Button>().onClick.AddListener(async () =>
            {
                await LobbyManager.Instance.JoinLobbyAsync(lobbyId);
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
    /// 강퇴당함 → 로비 패널로
    /// </summary>
    void HandleKicked()
    {
        ShowPanel(_lobbyPanel);
    }

    /// <summary>
    /// 게임 시작 신호 수신
    /// TODO: 3단계에서 씬 전환 구현
    /// </summary>
    void HandleGameStart()
    {
        Debug.Log("게임 시작 - 3단계에서 씬 전환 구현 예정");
    }

    void UpdateStatus(string message)
    {
        if (_statusText == null) return;
        _statusText.text = message;
    }
}