using UnityEngine;

/// <summary>
/// 아이템 클릭, 드래그앤드롭 입력을 받아서 InventoryModel에 전달
/// InventoryModel 변경 사항을 InventoryView에 반영
/// </summary>
public class InventoryPresenter
{
    InventoryModel _model;
    InventoryView _view;
    DraggingItem _draggingItem;

    public bool IsDragging => _draggingItem != null;

    public InventoryPresenter(InventoryModel model, InventoryView view, DraggingItem draggingItem)
    {
        _model = model;
        _view = view;
        _draggingItem = draggingItem;

        _view.Initialize(_model.Width);
        _view.OnDragBegin += HandleDragBegin;
        _view.OnDragMove += HandleDragMove;
        _view.OnDragEnd += HandleDragEnd;
        _view.OnItemMoved += HandleItemMoved;
        _view.OnItemClicked += HandleItemClicked;
        _view.OnCanPlace = (item, pos) => _model.CanPlace(item, pos);
    }

    /// <summary>
    /// 드래그 시작
    /// </summary>
    void HandleDragBegin(ItemModel item, Vector2 screenPos)
    {
        _view.SetItemContainerRaycast(false);
        _draggingItem.Show(item.Config.IconSprite, screenPos, GetItemSize(item));
    }

    /// <summary>
    /// 드래그 중
    /// </summary>
    void HandleDragMove(Vector2 screenPos)
    {
        _draggingItem.Follow(screenPos);
    }

    /// <summary>
    /// 드래그 끝
    /// </summary>
    void HandleDragEnd()
    {
        _view.SetItemContainerRaycast(true);
        _draggingItem.Hide();
    }

    /// <summary>
    /// 드래그 끝(외부용)
    /// </summary>
    public void EndDrag()
    {
        HandleDragEnd();
    }

    /// <summary>
    /// 아이템 이동 처리
    /// </summary>
    void HandleItemMoved(ItemModel item, Vector2Int newPos)
    {
        if (_model.TryMoveItem(item, newPos))
        {
            _view.UpdateItemViewPosition(item);
        }
        else
        {
            // 이동 실패 시 원래 위치로 복구
            _view.ResetItemViewPosition(item);
        }
        _view.ResetCellColors();
    }

    /// <summary>
    /// 아이템 클릭 처리
    /// </summary>
    void HandleItemClicked(ItemModel item)
    {
        if (item.Config.ItemType == ItemType.Consumable)
        {
            UseItem(item);
        }
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(ItemModel item)
    {
        if (!_model.TryGetEmptyPosition(item, out Vector2Int pos)) return false;
        if (!_model.TryAddItem(item, pos)) return false;

        _view.AddItemView(item);
        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public void RemoveItem(ItemModel item)
    {
        _model.RemoveItem(item);
        _view.RemoveItemView(item);
    }

    /// <summary>
    /// 아이템 사용
    /// </summary>
    void UseItem(ItemModel item)
    {
        // HACK:아이템 효과 처리는 나중에 추가
        RemoveItem(item);
    }

    /// <summary>
    /// 아이템 Cell 크기 계산
    /// </summary>
    Vector2 GetItemSize(ItemModel item)
    {
        Vector2Int[] cells = item.Config.OccupiedCells;
        int maxX = 0, maxY = 0;
        foreach (Vector2Int cell in cells)
        {
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }
        return new Vector2(_view.CellSize * (maxX + 1), _view.CellSize * (maxY + 1));
    }
}
