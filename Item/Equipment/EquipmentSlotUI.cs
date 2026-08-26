using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장비 슬롯 UI
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] Image _emptyIcon;
    [SerializeField] EquipSlot _slotType;

    ItemModel _equippedItem;
    RectTransform _rectTransform;
    public EquipSlot SlotType => _slotType;
    public event Action<EquipSlot> OnSlotClicked;
    public event Action<ItemModel, Vector3> OnHoverEnter; // 장착된 장비에 마우스 올림 <아이템, 슬롯 아래쪽 중앙 월드 위치>
    public event Action OnHoverExit;                      // 장착된 장비에서 마우스 나감

    void Awake()
    {
        _rectTransform = (RectTransform)transform;
    }

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

    /// <summary>
    /// 마우스 올림 - 장착된 장비가 있을 때만 툴팁 표시용 이벤트 발행 (빈 슬롯은 무시)
    /// pivot이 (1,1)이라 transform.position은 슬롯 오른쪽 위 모서리를 가리키므로,
    /// GetWorldCorners()로 pivot과 무관하게 실제 슬롯 아래쪽 중앙 좌표를 직접 계산함
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_equippedItem == null) return;

        Vector3[] corners = new Vector3[4]; // 0=좌하단, 1=좌상단, 2=우상단, 3=우하단(GetWorldCorners 반환 순서)
        _rectTransform.GetWorldCorners(corners);
        Vector3 bottomCenter = (corners[0] + corners[3]) * 0.5f; // 아래쪽 두 모서리의 중점

        OnHoverEnter?.Invoke(_equippedItem, bottomCenter);
    }

    /// <summary>
    /// 마우스 나감
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit?.Invoke();
    }
}