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

    /// <summary>
    /// 아이템 데이터 표시 및 버튼 연결
    /// </summary>
    public void Setup(string displayText, bool canJoin, Action onSelect, Action onJoinRequested)
    {
        _nameText.text = displayText;

        _selectButton.onClick.AddListener(() => onSelect());

        _joinButton.interactable = canJoin;
        if (canJoin == false) return;

        _joinButton.onClick.AddListener(() => onJoinRequested());
    }
}