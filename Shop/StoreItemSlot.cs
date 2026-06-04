using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 아이템 슬롯
/// </summary>
public class StoreItemSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _priceText;

    ItemConfig _itemConfig;
    public event Action<ItemConfig> OnClicked;

    public void Initialize(ItemConfig itemConfig)
    {
        _itemConfig = itemConfig;
        _icon.sprite = itemConfig.IconSprite;
        _priceText.text = itemConfig.BuyPrice.ToString() + "G";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(_itemConfig);
    }
}
