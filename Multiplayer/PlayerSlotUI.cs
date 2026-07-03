using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룸의 플레이어 슬롯 하나
/// </summary>
public class PlayerSlotUI : MonoBehaviour
{
    [SerializeField] TMP_Text _nicknameText;
    [SerializeField] TMP_Text _statusText;
    [SerializeField] Button _kickButton;

    string _playerId;

    /// <summary>
    /// 플레이어 정보 표시
    /// </summary>
    public void SetPlayer(string nickname, bool isReady, bool isHost, bool canKick, string playerId)
    {
        _playerId = playerId;
        _nicknameText.text = (isHost) ? $"[방장] {nickname}" : nickname;
        _statusText.text = (isReady) ? "준비" : "대기";
        _kickButton.gameObject.SetActive(canKick);
    }

    /// <summary>
    /// 빈 슬롯 표시
    /// </summary>
    public void SetEmpty()
    {
        _playerId = null;
        _nicknameText.text = "빈 슬롯";
        _statusText.text = "";
        _kickButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 강퇴 버튼
    /// </summary>
    public async void OnKickClicked()
    {
        if (_playerId == null) return;
        await LobbyManager.Instance.KickPlayerAsync(_playerId);
    }
}