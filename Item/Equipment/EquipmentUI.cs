using UnityEngine;

/// <summary>
/// 장비 UI 전체 관리
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [SerializeField] EquipmentSlotUI _mainGunSlotUI;
    [SerializeField] EquipmentSlotUI _turretSlotUI;
    [SerializeField] EquipmentSlotUI _hullSlotUI;

    EquipmentManager _equipmentManager;
    InventoryPresenter _inventoryPresenter;
    ItemTooltipUI _tooltip;

    public void Initialize(EquipmentManager equipmentManager, InventoryPresenter inventoryPresenter, ItemTooltipUI tooltip)
    {
        _equipmentManager = equipmentManager;
        _inventoryPresenter = inventoryPresenter;
        _tooltip = tooltip;

        // 슬롯 클릭 이벤트 구독
        _mainGunSlotUI.OnSlotClicked += HandleSlotClicked;
        _turretSlotUI.OnSlotClicked += HandleSlotClicked;
        _hullSlotUI.OnSlotClicked += HandleSlotClicked;

        // 슬롯 호버 이벤트 구독(장착된 장비 툴팁)
        _mainGunSlotUI.OnHoverEnter += HandleHoverEnter;
        _turretSlotUI.OnHoverEnter += HandleHoverEnter;
        _hullSlotUI.OnHoverEnter += HandleHoverEnter;
        _mainGunSlotUI.OnHoverExit += HandleHoverExit;
        _turretSlotUI.OnHoverExit += HandleHoverExit;
        _hullSlotUI.OnHoverExit += HandleHoverExit;

        // 슬롯 드래그 이벤트 구독(장착 해제 - 슬롯에서 인벤토리로 드래그)
        _mainGunSlotUI.OnDragBegin += HandleDragBegin;
        _turretSlotUI.OnDragBegin += HandleDragBegin;
        _hullSlotUI.OnDragBegin += HandleDragBegin;
        _mainGunSlotUI.OnDragging += HandleDragMove;
        _turretSlotUI.OnDragging += HandleDragMove;
        _hullSlotUI.OnDragging += HandleDragMove;
        _mainGunSlotUI.OnDragEnded += HandleDragEnd;
        _turretSlotUI.OnDragEnded += HandleDragEnd;
        _hullSlotUI.OnDragEnded += HandleDragEnd;

        // 슬롯 드롭 이벤트 구독(장착 - 인벤토리에서 슬롯으로 드래그)
        _mainGunSlotUI.OnItemDropped += HandleItemDropped;
        _turretSlotUI.OnItemDropped += HandleItemDropped;
        _hullSlotUI.OnItemDropped += HandleItemDropped;

        // 장착/해제 이벤트 구독
        _equipmentManager.OnEquipped += HandleEquipped;
        _equipmentManager.OnUnequipped += HandleUnequipped;

        // LoadFromSaveData가 Initialize보다 먼저 실행돼 OnEquipped를 놓칠 수 있으므로
        // 현재 장착 상태를 직접 읽어와 초기 표시에 반영
        _mainGunSlotUI.SetItem(_equipmentManager.MainGunSlot);
        _turretSlotUI.SetItem(_equipmentManager.TurretSlot);
        _hullSlotUI.SetItem(_equipmentManager.HullSlot);
    }

    void OnDestroy()
    {
        if (_equipmentManager == null) return;

        _equipmentManager.OnEquipped -= HandleEquipped;
        _equipmentManager.OnUnequipped -= HandleUnequipped;

        _mainGunSlotUI.OnHoverEnter -= HandleHoverEnter;
        _turretSlotUI.OnHoverEnter -= HandleHoverEnter;
        _hullSlotUI.OnHoverEnter -= HandleHoverEnter;
        _mainGunSlotUI.OnHoverExit -= HandleHoverExit;
        _turretSlotUI.OnHoverExit -= HandleHoverExit;
        _hullSlotUI.OnHoverExit -= HandleHoverExit;

        _mainGunSlotUI.OnDragBegin -= HandleDragBegin;
        _turretSlotUI.OnDragBegin -= HandleDragBegin;
        _hullSlotUI.OnDragBegin -= HandleDragBegin;
        _mainGunSlotUI.OnDragging -= HandleDragMove;
        _turretSlotUI.OnDragging -= HandleDragMove;
        _hullSlotUI.OnDragging -= HandleDragMove;
        _mainGunSlotUI.OnDragEnded -= HandleDragEnd;
        _turretSlotUI.OnDragEnded -= HandleDragEnd;
        _hullSlotUI.OnDragEnded -= HandleDragEnd;

        _mainGunSlotUI.OnItemDropped -= HandleItemDropped;
        _turretSlotUI.OnItemDropped -= HandleItemDropped;
        _hullSlotUI.OnItemDropped -= HandleItemDropped;
    }

    /// <summary>
    /// 슬롯 클릭 시 장착 해제
    /// </summary>
    void HandleSlotClicked(EquipSlot slot)
    {
        ItemModel item = _equipmentManager.Unequip(slot);
        if (item == null) return;

        bool added = _inventoryPresenter.AddItem(item);
        if (added == false)
        {
            _equipmentManager.Equip(item); // 자리가 없으면 다시 장착
        }
    }

    /// <summary>
    /// 장착 시 슬롯 UI 갱신
    /// </summary>
    void HandleEquipped(EquipSlot slot, ItemModel item)
    {
        GetSlotUI(slot)?.SetItem(item);
    }

    /// <summary>
    /// 해제 시 슬롯 UI 갱신
    /// </summary>
    void HandleUnequipped(EquipSlot slot, ItemModel item)
    {
        GetSlotUI(slot)?.SetItem(null);
    }

    EquipmentSlotUI GetSlotUI(EquipSlot slot)
    {
        return slot switch
        {
            EquipSlot.MainGun => _mainGunSlotUI,
            EquipSlot.Turret => _turretSlotUI,
            EquipSlot.Hull => _hullSlotUI,
            _ => null
        };
    }

    /// <summary>
    /// 장착된 장비 호버 시작 (툴팁 켜기) - 아이템 크기(OccupiedCells)와 무관하게 슬롯 아래쪽 중앙에 고정 표시
    /// </summary>
    void HandleHoverEnter(ItemModel item, Vector3 slotBottomCenterPos)
    {
        _tooltip.ShowAtFixedAnchor(item.Config, slotBottomCenterPos);
    }

    /// <summary>
    /// 장착된 장비 호버 종료 (툴팁 끄기)
    /// </summary>
    void HandleHoverExit()
    {
        _tooltip.Hide();
    }

    /// <summary>
    /// 장비 슬롯에서 드래그 시작 - 공용 드래그 고스트 표시(인벤토리 드래그와 동일한 비주얼 재사용)
    /// </summary>
    void HandleDragBegin(ItemModel item, Vector2 screenPos)
    {
        _tooltip.Hide();
        _inventoryPresenter.ShowDraggingGhost(item, screenPos);
    }

    /// <summary>
    /// 드래그 중 - 고스트 위치 갱신
    /// </summary>
    void HandleDragMove(Vector2 screenPos)
    {
        _inventoryPresenter.MoveDraggingGhost(screenPos);
    }

    /// <summary>
    /// 드래그 종료 - 고스트 숨김(실제 해제/복구 처리는 드롭 시점에 InventoryPresenter 쪽에서 이미 완료됨)
    /// </summary>
    void HandleDragEnd()
    {
        _inventoryPresenter.HideDraggingGhost();
    }

    /// <summary>
    /// 인벤토리 아이템이 슬롯에 드롭됨 - 장착 시도(클릭과 동일한 스왑 로직 재사용)
    /// </summary>
    void HandleItemDropped(ItemModel item)
    {
        _inventoryPresenter.EquipFromInventory(item);
    }
}