using TMPro;
using UnityEngine;

/// <summary>
/// 현재 게임 정보 UI
/// </summary>
public class GameInfoUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _stageText; // 스테이지
    [SerializeField] TextMeshProUGUI _goldText; // 골드
    [SerializeField] PlayerLifeSlot[] _lifeSlots; // 1P ~ 4P 목숨 슬롯(인덱스 = OwnerClientId, 싱글은 0번만 사용)

    [System.Serializable]
    class PlayerLifeSlot
    {
        public GameObject Root;          // 슬롯 전체 켜고 끄기용
        public TextMeshProUGUI LifeText; // 목숨 숫자 표시
    }

    /// <summary>
    /// 스테이지 UI 세팅
    /// </summary>
    public void UpdateStage(int stage)
    {
        _stageText.text = stage.ToString();
    }

    /// <summary>
    /// 골드 UI 세팅
    /// </summary>
    public void UpdateGold(int gold)
    {
        _goldText.text = gold.ToString();
    }

    /// <summary>
    /// 목숨 UI 세팅
    /// </summary>
    public void UpdateLife(int playerIndex, int life)
    {
        if (playerIndex < 0 || playerIndex >= _lifeSlots.Length) return;

        _lifeSlots[playerIndex].LifeText.text = life.ToString();
    }

    /// <summary>
    /// 멀티플레이:접속 인원 수만큼만 목숨 슬롯 활성화(나머지는 비활성)
    /// </summary>
    public void SetActivePlayerCount(int count)
    {
        for (int i = 0; i < _lifeSlots.Length; i++)
        {
            _lifeSlots[i].Root.SetActive(i < count);
        }
    }

    /// <summary>
    /// 멀티플레이:특정 플레이어의 목숨 슬롯 비활성화 — 게임 도중 해당 클라이언트가 연결 종료했을 때 사용
    /// </summary>
    public void HideLifeSlot(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _lifeSlots.Length) return;

        _lifeSlots[playerIndex].Root.SetActive(false);
    }

    /// <summary>
    /// 싱글플레이:멀티용 슬롯 크기가 작아서 빈 공간 대비 글자가 작아 보이므로 확대
    /// </summary>
    public void SetSingleplayerScale()
    {
        _lifeSlots[0].Root.transform.localScale = Vector3.one * 1.2f;
    }
}