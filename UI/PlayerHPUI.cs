using UnityEngine;

/// <summary>
/// 왼쪽 아래에 플레이어 HP 표시하는 UI
/// </summary>
public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] GameObject[] _hpIcons;
    [SerializeField] PlayerTank _player;

    void Start()
    {
        _player.Model.OnHpChanged += UpdateHP;
    }

    /// <summary>
    /// HP 갱신
    /// </summary>
    void UpdateHP(int current, int max)
    {
        for (int i = 0; i < _hpIcons.Length; i++)
        {
            _hpIcons[i].SetActive(i < current);
        }
    }
}
