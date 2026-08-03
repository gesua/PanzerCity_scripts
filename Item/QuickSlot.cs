using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀵슬롯 하나의 상태(아이콘, 어둡기) 담당
/// </summary>
public class QuickSlot : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] GameObject _CountImg; // 갯수 부모 오브젝트
    [SerializeField] TextMeshProUGUI _countText; // 아이템 갯수

    public void SetCount(int count)
    {
        bool available = (count > 0);

        if (available) // 아이콘 밝게
        {
            _icon.color = Color.white;
        }
        else // 아이콘 어둡게
        {
            _icon.color = Color.gray2;
        }

        _CountImg.SetActive(available);
        _countText.text = count.ToString();
    }
}