using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 목록 아이템 하나 — 선택(이름 복사) / 참가
/// </summary>
public class LobbyItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text _nameText;
    [SerializeField] Button _selectButton; // 방 이름 복사
    [SerializeField] Button _joinButton;   // 방 참가

    bool _isJoining; // 이 버튼 인스턴스의 중복 클릭 방지

    /// <summary>
    /// 아이템 데이터 표시 및 버튼 연결
    /// </summary>
    public void Setup(string displayText, bool canJoin, string lobbyId, Action onSelect, Action onJoined)
    {
        _nameText.text = displayText;

        _selectButton.onClick.AddListener(() => onSelect());

        _joinButton.interactable = canJoin;
        if (canJoin == false) return;

        _joinButton.onClick.AddListener(async () =>
        {
            if (_isJoining) return;
            _isJoining = true;

            try
            {
                await LobbyManager.Instance.JoinLobbyAsync(lobbyId);

                // 참가 실패 시 패널 전환 안 함
                if (LobbyManager.Instance.CurrentLobby == null) return;

                onJoined();
            }
            finally
            {
                _isJoining = false;
            }
        });
    }
}