using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룸의 플레이어 슬롯 하나
/// </summary>
public class PlayerSlotUI : MonoBehaviour
{
    const string ReadyKey = "UI_MP_TAG_READY";     // 준비
    const string WaitingKey = "UI_MP_TAG_WAITING"; // 대기

    [SerializeField] TMP_Text _nicknameText;
    [SerializeField] TMP_Text _statusText;
    [SerializeField] Button _kickButton;

    string _playerId;
    bool _isKicking; // 강퇴 버튼 중복 클릭 방지

    /// <summary>
    /// 플레이어 정보 표시
    /// </summary>
    public void SetPlayer(string nickname, bool isReady, bool isHost, bool canKick, string playerId)
    {
        _playerId = playerId;
        _nicknameText.text = (isHost) ? $"[Host] {nickname}" : nickname;
        SetReadyStatus(isReady);
        _kickButton.gameObject.SetActive(canKick);
    }

    /// <summary>
    /// 준비상태 글자 변경
    /// </summary>
    public void SetReadyStatus(bool isReady)
    {
        string key = (isReady) ? ReadyKey : WaitingKey;
        _statusText.text = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("Localization", key);
    }

    /// <summary>
    /// 빈 슬롯 표시
    /// </summary>
    public void SetEmpty()
    {
        _playerId = null;
        _nicknameText.text = "Empty";
        _statusText.text = "";
        _kickButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 강퇴 버튼
    /// </summary>
    public async void OnKickClicked()
    {
        if (_isKicking) return;
        if (_playerId == null) return;

        _isKicking = true;

        try
        {
            await LobbyManager.Instance.KickPlayerAsync(_playerId);
        }
        finally
        {
            _isKicking = false;
        }
    }
}