using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 UI를 표시하고 드래그앤드롭 입력 처리
/// </summary>
public class InventoryView : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] RectTransform _itemContainer;  // 아이템 뷰 배치할 레이어
    [SerializeField] GameObject _itemPrefab; // 아이템 프리팹
    [SerializeField] GridCell[] _cells;      // 미리 만들어둔 셀들
    [SerializeField] float _cellSize = 75f;  // 셀 크기

    ItemModel _draggingItem;

    Dictionary<ItemModel, ItemView> _itemViews = new();

    public Action<ItemModel, Vector2Int> OnItemMoved;    // 아이템 이동
    public Action<ItemModel, Vector2Int> OnItemDragging; // 아이템 드래그중
    public Action<ItemModel> OnItemClicked;              // 아이템 클릭
    public Func<ItemModel, Vector2Int, bool> OnCanPlace; // 놓을 수 있는 위치인지 체크

    public void Initialize(int width)
    {
        // Cell 위치 Index 계산
        for (int i = 0; i < _cells.Length; i++)
        {
            int x = i % width;
            int y = i / width;
            _cells[i].Initialize(new Vector2Int(x, y));
            _cells[i].OnDropped += HandleDrop;
            _cells[i].OnHoverEnter += HandleHoverEnter;
            _cells[i].OnHoverExit += HandleHoverExit;
        }
    }

    /// <summary>
    /// 드롭 처리
    /// </summary>
    void HandleDrop(ItemView itemView, Vector2Int gridPos)
    {
        ItemModel item = GetItemByView(itemView);
        if (item != null) OnItemMoved?.Invoke(item, gridPos);
    }

    void HandleHoverEnter(Vector2Int gridPos)
    {
        if (_draggingItem == null) return;
        bool isValid = OnCanPlace?.Invoke(_draggingItem, gridPos) ?? false;
        UpdateCellColors(_draggingItem, gridPos, isValid);
    }

    void HandleHoverExit()
    {
        ResetCellColors();
    }

    /// <summary>
    /// ItemView로 ItemModel 찾기
    /// </summary>
    ItemModel GetItemByView(ItemView itemView)
    {
        foreach (var pair in _itemViews)
        {
            if (pair.Value == itemView) return pair.Key;
        }
        return null;
    }

    /// <summary>
    /// 아이템 뷰 추가
    /// </summary>
    public void AddItemView(ItemModel item)
    {
        GameObject itemGo = Instantiate(_itemPrefab, _itemContainer);
        ItemView itemView = itemGo.GetComponent<ItemView>();
        itemView.Initialize(item, _cellSize);
        itemView.OnClicked += () => OnItemClicked?.Invoke(item);
        itemView.OnDragBegin += () => _draggingItem = item;
        itemView.OnDragEnded += () => _draggingItem = null;
        itemView.OnDragCanceled += ResetCellColors;
        _itemViews[item] = itemView;
        UpdateItemViewPosition(item);
    }

    /// <summary>
    /// 아이템 뷰 제거
    /// </summary>
    public void RemoveItemView(ItemModel item)
    {
        if (_itemViews.TryGetValue(item, out ItemView view))
        {
            Destroy(view.gameObject);
            _itemViews.Remove(item);
        }
    }

    /// <summary>
    /// 아이템 뷰 위치 갱신
    /// </summary>
    public void UpdateItemViewPosition(ItemModel item)
    {
        if (_itemViews.TryGetValue(item, out ItemView view))
        {
            Vector2Int pos = item.GridPosition;
            view.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x * _cellSize, -pos.y * _cellSize);
        }
    }

    /// <summary>
    /// 아이템 뷰 위치 리셋
    /// </summary>
    public void ResetItemViewPosition(ItemModel item)
    {
        if (_itemViews.TryGetValue(item, out ItemView view))
        {
            view.ResetPosition();
        }
    }

    /// <summary>
    /// 드래그 중 셀 색상 업데이트
    /// </summary>
    public void UpdateCellColors(ItemModel item, Vector2Int hoverPos, bool isValid)
    {
        // 모든 셀 초기화
        foreach (GridCell cell in _cells)
        {
            cell.SetNormal();
        }

        // 아이템이 차지할 셀 색상 변경
        Vector2Int[] cells = item.GetOccupiedCells();
        foreach (Vector2Int cell in cells)
        {
            Vector2Int worldCell = hoverPos + cell;
            GridCell gridCell = GetCell(worldCell);
            if (gridCell == null) continue;
            if (isValid) gridCell.SetValid();
            else gridCell.SetInvalid();
        }
    }

    /// <summary>
    /// 모든 셀 색상 초기화
    /// </summary>
    public void ResetCellColors()
    {
        foreach (GridCell cell in _cells)
        {
            cell.SetNormal();
        }
    }

    GridCell GetCell(Vector2Int pos)
    {
        foreach (GridCell cell in _cells)
        {
            if (cell.GridPos == pos) return cell;
        }
        return null;
    }
}
