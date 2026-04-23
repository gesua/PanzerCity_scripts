using TMPro;
using UnityEngine;

/// <summary>
/// 현재 게임 정보 UI
/// </summary>
public class GameInfoUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _stageText; // 스테이지
    [SerializeField] TextMeshProUGUI _lifeText; // 남은 목숨

    /// <summary>
    /// 스테이지 UI 세팅
    /// </summary>
    public void UpdateStage(int stage)
    {
        _stageText.text = stage.ToString();
    }

    /// <summary>
    /// 남은 목숨 UI 세팅
    /// </summary>
    public void UpdateLife(int life)
    {
        _lifeText.text = life.ToString();
    }
}
