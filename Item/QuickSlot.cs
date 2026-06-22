using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀵슬롯 하나의 상태(아이콘, 어둡기) 담당
/// </summary>
public class QuickSlot : MonoBehaviour
{
    [SerializeField] Image _icon;

    public void SetAvailable(bool available)
    {
        if (available) // 아이콘 밝게
        {
            _icon.color = Color.white;
        }
        else // 아이콘 어둡게
        {
            _icon.color = Color.gray2;
        }
    }
}
