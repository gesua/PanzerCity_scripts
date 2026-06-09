using System;
using UnityEngine;

/// <summary>
/// 아이템 클릭, 드래그앤드롭 입력을 받아서 InventoryModel에 전달
/// InventoryModel 변경 사항을 InventoryView에 반영
/// </summary>
public class InventoryPresenter
{
    InventoryModel _model;
    InventoryView _view;
    DraggingItemUI _draggingItemUI;
    ItemModel _draggingItemModel;
    EquipmentManager _equipmentManager;

    bool _isShop; // 상점일 때

    public bool IsDragging => _draggingItemModel != null;

    public event Action<int> OnItemUsed; // 아이템 사용(ID)

    public InventoryPresenter(InventoryModel model, InventoryView view, DraggingItemUI draggingItem, EquipmentManager equipmentManager)
    {
        _model = model;
        _view = view;
        _draggingItemUI = draggingItem;
        _equipmentManager = equipmentManager;

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
        _draggingItemModel = item;
        _view.SetItemContainerRaycast(false);
        _draggingItemUI.Show(item.Config.IconSprite, screenPos, GetItemSize(item));
    }

    /// <summary>
    /// 드래그 중
    /// </summary>
    void HandleDragMove(Vector2 screenPos)
    {
        _draggingItemUI.Follow(screenPos);
    }

    /// <summary>
    /// 드래그 끝
    /// </summary>
    void HandleDragEnd()
    {
        _draggingItemModel = null;
        _view.SetItemContainerRaycast(true);
        _draggingItemUI.Hide();
    }

    /// <summary>
    /// 드래그 강제 종료
    /// </summary>
    public void ForceDrop()
    {
        _view.ForceEndDrag();
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
        // 소모품
        if (item.Config.ItemType == ItemType.Consumable)
        {
            if (_isShop) return; // 상점에선 소모품 사용 막음
            UseItem(item);
        }
        // 장비
        else if (item.Config.ItemType == ItemType.Equipment)
        {
            if (_isShop == false) return; // 상점이 아니면 장착 막음

            // 장비 장착
            RemoveItem(item);
            ItemModel prevItem = _equipmentManager.Equip(item);
            // 기존 장착 아이템 인벤토리로 반환
            if (prevItem != null) AddItem(prevItem);
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
        OnItemUsed?.Invoke(item.Config.Id);
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

    /// <summary>
    /// 상점 입장 여부 세팅
    /// </summary>
    public void SetShopMode(bool enable)
    {
        _isShop = enable;
    }
}
