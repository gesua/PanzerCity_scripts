using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 개별 아이템
/// ItemModel을 받아서 아이콘을 UI로 보여줌
/// </summary>
public class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] Image _icon;
    [SerializeField] GameObject _dummy;

    public Action<Vector2> OnDragBegin; // 드래그 시작
    public Action<Vector2> OnDragging;  // 드래그 중
    public Action OnDragEnded;          // 드래그 끝
    public Action OnDragCanceled;       // 드래그 취소
    public Action OnClicked;            // 클릭

    ItemModel _item;
    float _cellSize;
    RectTransform _rectTransform;
    Vector2 _originalPos;


    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(ItemModel item, float cellSize)
    {
        _item = item;
        _cellSize = cellSize;

        // 아이콘 설정
        _icon.sprite = item.Config.IconSprite;

        // 아이템 크기 설정 (차지하는 셀 수에 맞게)
        Vector2Int[] cells = item.Config.OccupiedCells;
        int maxX = 0, maxY = 0;
        foreach (Vector2Int cell in cells)
        {
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }
        _rectTransform.sizeDelta = new Vector2((maxX + 1) * cellSize, (maxY + 1) * cellSize);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnDragBegin?.Invoke(eventData.position);
        _icon.enabled = false;

        _originalPos = _rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragging?.Invoke(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnded?.Invoke();
        _icon.enabled = true;

        // 드롭 대상이 없으면 취소
        if (eventData.pointerCurrentRaycast.gameObject == null)
        {
            OnDragCanceled?.Invoke();
            ResetPosition();
            return;
        }
    }

    /// <summary>
    /// 드래그 강제 종료
    /// </summary>
    public void ForceEndDrag()
    {
        OnDragEnded?.Invoke();
        _icon.enabled = true;

        OnDragCanceled?.Invoke();
        ResetPosition();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke();
    }

    /// <summary>
    /// 드래그 실패 시 원래 위치로 복구
    /// </summary>
    public void ResetPosition()
    {
        _rectTransform.anchoredPosition = _originalPos;
    }
}