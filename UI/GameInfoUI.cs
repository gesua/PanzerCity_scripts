using TMPro;
using UnityEngine;

/// <summary>
/// 현재 게임 정보 UI
/// </summary>
public class GameInfoUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _stageText; // 스테이지
    [SerializeField] TextMeshProUGUI _lifeText; // 목숨
    [SerializeField] TextMeshProUGUI _goldText; // 골드

    /// <summary>
    /// 스테이지 UI 세팅
    /// </summary>
    public void UpdateStage(int stage)
    {
        _stageText.text = stage.ToString();
    }

    /// <summary>
    /// 목숨 UI 세팅
    /// </summary>
    public void UpdateLife(int life)
    {
        _lifeText.text = life.ToString();
    }


    /// <summary>
    /// 골드 UI 세팅
    /// </summary>
    public void UpdateGold(int gold)
    {
        _goldText.text = gold.ToString();
    }
}
