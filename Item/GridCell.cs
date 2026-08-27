using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 그리드 1칸
/// 놓을 수 있는 위치인지 색 변경, 드롭 인식
/// </summary>
public class GridCell : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _background;

    static readonly Color _normalColor = new Color(1f, 1f, 1f);  // 기본(파랑)
    static readonly Color _validColor = new Color(0f, 1f, 0f);   // 초록
    static readonly Color _invalidColor = new Color(1f, 0f, 0f); // 빨강

    Vector2Int _gridPos;
    public Action<ItemView, Vector2Int> OnDropped;
    public Action<EquipmentSlotUI, Vector2Int> OnEquipmentDropped; // 장비 슬롯에서 드래그해온 아이템이 이 칸에 드롭됨
    public Action<Vector2Int> OnHoverEnter;
    public Action OnHoverExit;

    public Vector2Int GridPos => _gridPos;

    public void Initialize(Vector2Int gridPos)
    {
        _gridPos = gridPos;
    }

    /// <summary>
    /// 드롭
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        if (eventData.pointerDrag.TryGetComponent(out ItemView itemView))
        {
            OnDropped?.Invoke(itemView, _gridPos);
            return;
        }

        if (eventData.pointerDrag.TryGetComponent(out EquipmentSlotUI equipmentSlot))
        {
            OnEquipmentDropped?.Invoke(equipmentSlot, _gridPos);
        }
    }

    /// <summary>
    /// 드래그 중에 마우스 들어온거 체크
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging) OnHoverEnter?.Invoke(GridPos);
    }

    /// <summary>
    /// 드래그 중에 마우스 나간거 체크
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.dragging) OnHoverExit?.Invoke();
    }

    public void SetNormal()
    {
        _background.color = _normalColor;
    }

    public void SetValid()
    {
        _background.color = _validColor;
    }

    public void SetInvalid()
    {
        _background.color = _invalidColor;
    }
}