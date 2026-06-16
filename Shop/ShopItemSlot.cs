using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 아이템 슬롯
/// </summary>
public class ShopItemSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _priceText;
    [SerializeField] GameObject _soldOutOverlay; // 매진 오버레이

    ItemConfig _itemConfig;
    bool _isSoldOut;

    public event Action<ItemConfig> OnClicked;

    public void Initialize(ItemConfig itemConfig)
    {
        _itemConfig = itemConfig;
        _icon.sprite = itemConfig.IconSprite;
        _priceText.text = itemConfig.BuyPrice.ToString() + "G";
        SetSoldOut(false); // 초기화할 때 매진 상태 리셋
    }

    /// <summary>
    /// 매진 상태 설정
    /// </summary>
    public void SetSoldOut(bool isSoldOut)
    {
        _isSoldOut = isSoldOut;
        if (_soldOutOverlay != null) // 일반 소모품은 매진 없음
            _soldOutOverlay.SetActive(isSoldOut);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isSoldOut) return;
        OnClicked?.Invoke(_itemConfig);
    }
}
