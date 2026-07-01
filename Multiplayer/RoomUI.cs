using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룸 내부 UI — 슬롯, 준비, 시작, 강퇴
/// </summary>
public class RoomUI : MonoBehaviour
{
    [Header("----- 슬롯 -----")]
    [SerializeField] List<PlayerSlotUI> _playerSlots; // 인스펙터에서 4개 연결

    [Header("----- 버튼 -----")]
    [SerializeField] Button _readyButton;      // 클라이언트용
    [SerializeField] TMP_Text _readyButtonText;
    [SerializeField] Button _startButton;      // 호스트용
    [SerializeField] Button _leaveButton;

    bool _isReady;

    void Start()
    {
        _readyButton.onClick.AddListener(OnReadyClicked);
        _startButton.onClick.AddListener(OnStartClicked);
        _leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    /// <summary>
    /// 로비 데이터로 UI 갱신
    /// </summary>
    public void Refresh(Lobby lobby)
    {
        if (lobby == null) return;

        // 슬롯 갱신
        for (int i = 0; i < _playerSlots.Count; i++)
        {
            if (i < lobby.Players.Count)
            {
                Player player = lobby.Players[i];
                bool isHost = player.Id == lobby.HostId;

                string nickname = "Unknown";
                bool isReady = isHost; // 방장은 항상 준비 상태로 표시

                if (player.Data != null)
                {
                    if (player.Data.ContainsKey("Nickname")) nickname = player.Data["Nickname"].Value;
                    if (isHost == false && player.Data.ContainsKey("IsReady"))
                    {
                        isReady = player.Data["IsReady"].Value == "true";
                    }
                }

                // 방장만 강퇴 권한 있고, 자신은 강퇴 불가
                bool canKick = LobbyManager.Instance.IsHost && isHost == false;
                _playerSlots[i].SetPlayer(nickname, isReady, isHost, canKick, player.Id);
            }
            else
            {
                _playerSlots[i].SetEmpty();
            }
        }

        // 호스트/클라이언트 버튼 분기
        bool iAmHost = LobbyManager.Instance.IsHost;
        _startButton.gameObject.SetActive(iAmHost);
        _startButton.interactable = LobbyManager.Instance.IsAllPlayersReady();
        _readyButton.gameObject.SetActive(iAmHost == false);
    }

    /// <summary>
    /// 준비/준비 취소 토글
    /// </summary>
    async void OnReadyClicked()
    {
        _isReady = !_isReady;
        _readyButtonText.text = (_isReady) ? "준비 취소" : "준비";
        await LobbyManager.Instance.UpdateReadyStatusAsync(_isReady);
    }

    /// <summary>
    /// 게임 시작 (호스트 전용)
    /// </summary>
    async void OnStartClicked()
    {
        if (LobbyManager.Instance.IsAllPlayersReady() == false) return;
        await LobbyManager.Instance.StartGameAsync();
    }

    /// <summary>
    /// 로비 나가기
    /// </summary>
    async void OnLeaveClicked()
    {
        _isReady = false;
        _readyButtonText.text = "준비";
        await LobbyManager.Instance.LeaveLobbyAsync();
    }
}