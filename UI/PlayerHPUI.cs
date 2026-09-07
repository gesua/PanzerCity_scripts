using UnityEngine;

/// <summary>
/// 왼쪽 아래에 플레이어 HP 표시하는 UI
/// </summary>
public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] HeartSlot[] _hpSlots;
    [SerializeField] PlayerTank _player;

    public void SetPlayer(PlayerTank player)
    {
        // 재호출(재도전 등) 시 이전 플레이어 구독이 남지 않도록 먼저 해제
        if (_player != null) _player.Model.OnHpChanged -= UpdateHP;

        _player = player;
        _player.Model.OnHpChanged += UpdateHP;
    }

    /// <summary>
    /// HP 갱신
    /// </summary>
    void UpdateHP(int current, int max)
    {
        for (int i = 0; i < _hpSlots.Length; i++)
        {
            _hpSlots[i].SetState(i < current);
        }
    }
}