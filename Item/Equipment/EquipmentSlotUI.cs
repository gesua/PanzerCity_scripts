using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장비 슬롯 UI
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image _icon;
    [SerializeField] Image _emptyIcon;
    [SerializeField] EquipSlot _slotType;

    ItemModel _equippedItem;
    public EquipSlot SlotType => _slotType;
    public event Action<EquipSlot> OnSlotClicked;

    public void SetItem(ItemModel item)
    {
        _equippedItem = item;
        if (item != null)
        {
            _icon.sprite = item.Config.IconSprite;
            _icon.gameObject.SetActive(true);
            //_emptyIcon.gameObject.SetActive(false);
        }
        else
        {
            _icon.gameObject.SetActive(false);
            //_emptyIcon.gameObject.SetActive(true);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(_slotType);
    }
}