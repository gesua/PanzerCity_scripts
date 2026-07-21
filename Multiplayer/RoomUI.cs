using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 룸 내부 UI — 슬롯, 준비, 시작, 강퇴
/// </summary>
public class RoomUI : MonoBehaviour
{
    const string ReadyKey = "UI_MP_READY";     // 준비
    const string UnreadyKey = "UI_MP_UNREADY"; // 준비 취소

    [SerializeField] TMP_Text _roomName;
    [SerializeField] TMP_Text _roomPassword;

    [Header("----- 슬롯 -----")]
    [SerializeField] List<PlayerSlotUI> _playerSlots; // 인스펙터에서 4개 연결

    [Header("----- 버튼 -----")]
    [SerializeField] Button _readyButton;      // 클라이언트용
    [SerializeField] TMP_Text _readyButtonText;
    [SerializeField] Button _startButton;      // 호스트용

    bool _isReady;
    bool _isTogglingReady; // 준비 버튼 중복 클릭 방지
    bool _isStarting;      // 시작 버튼 중복 클릭 방지
    bool _isLeaving;       // 나가기 버튼 중복 클릭 방지

    /// <summary>
    /// UI가 켜질 때(방 진입 시) 내부 상태 초기화
    /// </summary>
    void OnEnable()
    {
        // 변수 리셋
        _isReady = false;
        _isTogglingReady = false;
        _isStarting = false;
        _isLeaving = false;

        // 버튼 텍스트 초기화
        if (_readyButtonText != null)
        {
            _readyButtonText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", ReadyKey);
        }
    }

    /// <summary>
    /// 로비 데이터로 UI 갱신
    /// </summary>
    public void Refresh(Lobby lobby)
    {
        if (lobby == null) return;

        // 방 이름 설정
        _roomName.text = lobby.Name;

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
    public async void OnReadyClicked()
    {
        if (_isTogglingReady) return;
        _isTogglingReady = true;

        try
        {
            _isReady = !_isReady;

            // 준비, 준비 취소 글자
            string key = (_isReady) ? UnreadyKey : ReadyKey;
            _readyButtonText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", key);

            await LobbyManager.Instance.UpdateReadyStatusAsync(_isReady);
        }
        finally
        {
            _isTogglingReady = false;
        }

    }

    /// <summary>
    /// 게임 시작 (호스트 전용)
    /// </summary>
    public async void OnStartClicked()
    {
        if (_isStarting) return;
        if (LobbyManager.Instance.IsAllPlayersReady() == false) return;

        _isStarting = true;

        try
        {
            await LobbyManager.Instance.StartGameAsync();
        }
        finally
        {
            _isStarting = false;
        }
    }

    /// <summary>
    /// 로비 나가기
    /// </summary>
    public async void OnLeaveClicked()
    {
        if (_isLeaving) return;
        _isLeaving = true;

        try
        {
            _isReady = false;
            _readyButtonText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", ReadyKey);
            await LobbyManager.Instance.LeaveLobbyAsync();
        }
        finally
        {
            _isLeaving = false;
        }
    }
}