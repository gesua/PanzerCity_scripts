using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 아이템을 화면에 표시
/// ItemModel을 받아서 아이콘을 UI로 보여줌
/// </summary>
public class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] Image _icon;

    public Action<Vector2> OnDragEnd;
    public Action<Vector2> OnDragging;  // 드래그 중 위치 전달
    public Action OnDragCanceled;       // 드래그 취소
    public Action OnClicked;

    ItemModel _item;
    float _cellSize;
    RectTransform _rectTransform;
    Canvas _canvas;
    Vector2 _originalPos;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
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
        // 이거 안 해놓으면 드래그가 아니라 OnPointerClick로 들어옴
        _icon.raycastTarget = false; // 본인 RayCast 막아놓음

        _originalPos = _rectTransform.anchoredPosition;

        // 드래그 중 최상위로 올리기
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        OnDragging?.Invoke(eventData.position); // 드래그 중 위치 전달
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _icon.raycastTarget = true;

        // 드롭 대상이 없으면 취소
        if (eventData.pointerCurrentRaycast.gameObject == null)
        {
            OnDragCanceled?.Invoke();
            ResetPosition();
            return;
        }

        transform.localScale = Vector3.one;
        OnDragEnd?.Invoke(eventData.position);
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