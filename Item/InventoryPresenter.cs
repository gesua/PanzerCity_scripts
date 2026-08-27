using System;
using System.Collections.Generic;
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
    ItemTooltipUI _tooltip;

    bool _isShop; // 상점일 때

    public bool IsDragging => _draggingItemModel != null;
    public IReadOnlyList<ItemModel> Items => _model.Items;

    public event Action<ItemConfig> OnItemUsed; // 아이템 사용
    public event Action OnInventoryChanged; // 인벤토리 변경(퀵슬롯 갱신용)
    public event Action<ItemConfig> OnItemDropped; // 바닥에 아이템 버림

    public InventoryPresenter(InventoryModel model, InventoryView view, DraggingItemUI draggingItem, EquipmentManager equipmentManager, ItemTooltipUI tooltip)
    {
        _model = model;
        _view = view;
        _draggingItemUI = draggingItem;
        _equipmentManager = equipmentManager;
        _tooltip = tooltip;

        _view.Initialize(_model.Width);
        _view.OnDragBegin += HandleDragBegin;
        _view.OnDragMove += HandleDragMove;
        _view.OnDragEnd += HandleDragEnd;
        _view.OnItemMoved += HandleItemMoved;
        _view.OnItemClicked += HandleItemClicked;
        _view.OnCanPlace = (item, pos) => _model.CanPlace(item, pos);
        _view.OnItemHoverEnter += HandleItemHoverEnter;
        _view.OnItemHoverExit += HandleItemHoverExit;
        _view.OnEquipmentItemDropped += HandleEquipmentItemDropped;
    }

    /// <summary>
    /// 아이템이 들어갈 자리가 있는지만 확인(실제로 넣지는 않음, 멀티플레이 픽업 사전 확인용)
    /// </summary>
    public bool HasSpaceFor(ItemConfig config)
    {
        ItemModel tempItem = new ItemModel(config);
        return _model.TryGetEmptyPosition(tempItem, out _);
    }

    /// <summary>
    /// 드래그 시작
    /// </summary>
    void HandleDragBegin(ItemModel item, Vector2 screenPos)
    {
        _draggingItemModel = item;
        _tooltip.Hide(); // 드래그 시작 시 툴팁 숨김
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
    /// 장비 슬롯 드래그 시작 시 공용 드래그 고스트 표시 (인벤토리 드래그와 동일한 비주얼 재사용)
    /// </summary>
    public void ShowDraggingGhost(ItemModel item, Vector2 screenPos)
    {
        _view.SetItemContainerRaycast(false);
        _draggingItemUI.Show(item.Config.IconSprite, screenPos, GetItemSize(item));
    }

    /// <summary>
    /// 드래그 고스트 위치 갱신
    /// </summary>
    public void MoveDraggingGhost(Vector2 screenPos)
    {
        _draggingItemUI.Follow(screenPos);
    }

    /// <summary>
    /// 드래그 고스트 숨김
    /// </summary>
    public void HideDraggingGhost()
    {
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
    /// 장비 슬롯에서 드래그해온 아이템이 그리드 칸에 드롭됨 - 자리가 되면 해제 후 배치, 안 되면 무시(장착 상태 유지)
    /// </summary>
    void HandleEquipmentItemDropped(EquipmentSlotUI sourceSlot, Vector2Int gridPos)
    {
        ItemModel item = sourceSlot.EquippedItem;
        if (item == null) return;

        if (_model.CanPlace(item, gridPos) == false)
        {
            _view.ResetCellColors(); // 남아있을 수 있는 하이라이트 정리
            return; // 자리가 안 되면 장착 상태 그대로 유지
        }

        _equipmentManager.Unequip(sourceSlot.SlotType); // 슬롯 아이콘 갱신은 OnUnequipped 이벤트로 자동 처리
        if (_model.TryAddItem(item, gridPos) == false)
        {
            _equipmentManager.Equip(item); // 배치 실패 시 재장착(방어 코드, 이론상 CanPlace 통과 후엔 항상 성공)
            _view.ResetCellColors();
            return;
        }

        _view.AddItemView(item);
        _view.ResetCellColors();
        OnInventoryChanged?.Invoke();
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
            EquipFromInventory(item);
        }
    }

    /// <summary>
    /// 인벤토리 아이템을 장비로 장착 (클릭/드래그 공용) - 기존 장착 아이템이 있으면 인벤토리로 스왑
    /// </summary>
    public void EquipFromInventory(ItemModel item)
    {
        // 장비 장착
        RemoveItem(item);
        ItemModel prevItem = _equipmentManager.Equip(item);

        // 드래그로 장착한 경우 ItemView가 즉시 풀 반환되어 OnDragEnded 델리게이트가 비워지고,
        // 정상적인 드래그 종료 정리(드래그 상태 리셋, 고스트 숨김, 레이캐스트 복구)가 생략될 수 있음 → 방어적으로 직접 정리
        _draggingItemModel = null;
        ForceDrop();
        HideDraggingGhost();

        // 기존 장착 아이템 인벤토리로 반환
        if (prevItem == null) return;

        bool added = AddItem(prevItem);
        if (added == false)
        {
            // 자리가 없으면 장착을 되돌림
            _equipmentManager.Equip(prevItem);
            AddItem(item);
        }
    }

    /// <summary>
    /// 아이템에 마우스 올림
    /// </summary>
    void HandleItemHoverEnter(ItemModel item, Vector3 iconPos)
    {
        if (IsDragging) return; // 드래그 중엔 툴팁 표시 안 함
        _tooltip.Show(item.Config, iconPos);
    }

    /// <summary>
    /// 아이템에서 마우스 나감
    /// </summary>
    void HandleItemHoverExit()
    {
        _tooltip.Hide();
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(ItemModel item)
    {
        if (_model.TryGetEmptyPosition(item, out Vector2Int pos) == false) return false;
        if (_model.TryAddItem(item, pos) == false) return false;

        _view.AddItemView(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public void RemoveItem(ItemModel item)
    {
        _tooltip.Hide(); // 아이템 제거 시 툴팁 숨김
        _model.RemoveItem(item);
        _view.RemoveItemView(item);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 아이템 사용
    /// </summary>
    void UseItem(ItemModel item)
    {
        OnItemUsed?.Invoke(item.Config);
        GameManager.Instance.GameStatistics.AddItemUsed(); // 통계 기록
        RemoveItem(item); // RemoveItem 안에서 OnInventoryChanged 발행
    }

    /// <summary>
    /// 퀵슬롯용 — ID로 아이템 찾아서 사용
    /// </summary>
    public bool TryUseItemById(int itemID)
    {
        foreach (ItemModel item in _model.Items)
        {
            if (item.Config.Id == itemID)
            {
                UseItem(item);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 쓰레기통 드롭 — 아이템 버리기 (상점 전용)
    /// </summary>
    public void TrashItem(ItemModel item)
    {
        // 실제로는 OnDrop → OnEndDrag 순서로 실행되어 이 시점엔 드래그 상태가 아직 안 정리되어 있음
        if (_draggingItemModel != null)
        {
            _draggingItemModel = null;
            _view.SetItemContainerRaycast(true);
            _draggingItemUI.Hide();
        }
        RemoveItem(item);

        // RemoveItem이 ItemView를 즉시 풀에 반환해 OnDragEnded 델리게이트가 비워지므로,
        // InventoryView._draggingItem 리셋이 생략될 수 있음 → 방어적으로 직접 정리
        ForceDrop();
    }

    /// <summary>
    /// 드롭존 드롭 — 바닥에 아이템 버리기 (스테이지 전용)
    /// </summary>
    public void DropItemToGround(ItemModel item)
    {
        if (_draggingItemModel != null)
        {
            _draggingItemModel = null;
            _view.SetItemContainerRaycast(true);
            _draggingItemUI.Hide();
        }
        OnItemDropped?.Invoke(item.Config);
        RemoveItem(item);

        // RemoveItem이 ItemView를 즉시 풀에 반환해 OnDragEnded 델리게이트가 비워지므로,
        // InventoryView._draggingItem 리셋이 생략될 수 있음 → 방어적으로 직접 정리
        ForceDrop();
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

    /// <summary>
    /// 인벤토리 상태 저장 (스테이지 진입 시점)
    /// </summary>
    public void SaveSnapshot()
    {
        _model.SaveSnapshot();
    }

    /// <summary>
    /// 저장된 상태로 복구 (재시작 시)
    /// </summary>
    public void RestoreSnapshot()
    {
        _view.ClearAllViews();
        _model.RestoreSnapshot();

        foreach (ItemModel item in _model.Items)
        {
            _view.AddItemView(item);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 인벤토리 상태를 세이브 데이터로 변환
    /// </summary>
    public List<ItemSaveData> ToSaveData()
    {
        List<ItemSaveData> result = new List<ItemSaveData>();
        foreach (ItemModel item in _model.Items)
        {
            result.Add(new ItemSaveData
            {
                ItemID = item.Config.Id,
                PosX = item.GridPosition.x,
                PosY = item.GridPosition.y,
                IsRotated = item.IsRotated
            });
        }
        return result;
    }

    /// <summary>
    /// 세이브 데이터로부터 인벤토리 복원 (이어하기 시)
    /// </summary>
    public void LoadFromSaveData(List<ItemSaveData> itemSaveDataList)
    {
        _view.ClearAllViews();
        _model.Clear();

        foreach (ItemSaveData itemSaveData in itemSaveDataList)
        {
            ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(itemSaveData.ItemID);
            if (config == null) continue;

            ItemModel item = new ItemModel(config);
            item.SetRotated(itemSaveData.IsRotated);

            Vector2Int pos = new Vector2Int(itemSaveData.PosX, itemSaveData.PosY);
            if (_model.TryAddItem(item, pos) == false) continue;

            _view.AddItemView(item);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 인벤토리 전체 비우기
    /// </summary>
    public void Clear()
    {
        List<ItemModel> items = new List<ItemModel>(_model.Items);
        foreach (ItemModel item in items)
        {
            RemoveItem(item);
        }
    }
}