using UnityEngine;

/// <summary>
/// 왼쪽 아래에 플레이어 HP 표시하는 UI
/// </summary>
public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] HeartSlot[] _hpSlots;
    [SerializeField] PlayerTank _player;

    private void Awake()
    {
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
