using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장비 슬롯 UI
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] Image _icon;
    [SerializeField] Image _emptyIcon;
    [SerializeField] EquipSlot _slotType;

    ItemModel _equippedItem;
    RectTransform _rectTransform;
    bool _isDragging; // 드래그 중인지(빈 슬롯에서 시작된 드래그와 구분하기 위한 플래그)

    public EquipSlot SlotType => _slotType;
    public ItemModel EquippedItem => _equippedItem; // 그리드 드롭 처리(InventoryPresenter)에서 어떤 아이템인지 조회용
    public event Action<EquipSlot> OnSlotClicked;
    public event Action<ItemModel, Vector3> OnHoverEnter; // 장착된 장비에 마우스 올림 <아이템, 슬롯 아래쪽 중앙 월드 위치>
    public event Action OnHoverExit;                      // 장착된 장비에서 마우스 나감
    public event Action<ItemModel, Vector2> OnDragBegin;  // 장착된 장비 드래그 시작 <아이템, 스크린 좌표> - 드래그 고스트 표시용
    public event Action<Vector2> OnDragging;              // 드래그 중 <스크린 좌표>
    public event Action OnDragEnded;                      // 드래그 종료(성공/실패 무관) - 드래그 고스트 숨김용
    public event Action<ItemModel> OnItemDropped;         // 인벤토리 아이템이 이 슬롯에 드롭됨(슬롯 타입 일치 시에만 발행)

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
        if (eventData.dragging) return; // 드래그 중엔 무시(ItemView와 동일)
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

    /// <summary>
    /// 드래그 시작 - 장착된 장비가 있을 때만 허용(빈 슬롯은 드래그 불가)
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_equippedItem == null) return;

        _isDragging = true;
        OnHoverExit?.Invoke(); // 드래그 시작 시 마우스 올림 상태 해제(툴팁 숨김)
        _icon.gameObject.SetActive(false);
        OnDragBegin?.Invoke(_equippedItem, eventData.position);
    }

    /// <summary>
    /// 드래그 중
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging == false) return;
        OnDragging?.Invoke(eventData.position);
    }

    /// <summary>
    /// 드래그 종료 - 유효한 곳에 드롭되면 OnDrop 처리(해제)가 이 시점보다 먼저 실행되어 _equippedItem이 비워짐.
    /// 이 시점에도 _equippedItem이 남아있다면 해제되지 않은 것이므로 아이콘을 복원함
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragging == false) return;
        _isDragging = false;

        OnDragEnded?.Invoke();

        if (_equippedItem != null)
        {
            _icon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 드롭 수신 - 인벤토리에서 드래그해온 아이템을 받음. 슬롯 타입이 일치할 때만 장착 이벤트 발행
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.TryGetComponent(out ItemView itemView) == false) return;
        if (itemView.Item.Config.EquipSlot != _slotType) return; // 슬롯 타입 불일치 시 무시

        OnItemDropped?.Invoke(itemView.Item);
    }
}