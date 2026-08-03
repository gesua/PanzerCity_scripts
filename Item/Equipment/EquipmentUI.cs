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

    public void Initialize(EquipmentManager equipmentManager, InventoryPresenter inventoryPresenter)
    {
        _equipmentManager = equipmentManager;
        _inventoryPresenter = inventoryPresenter;

        // 슬롯 클릭 이벤트 구독
        _mainGunSlotUI.OnSlotClicked += HandleSlotClicked;
        _turretSlotUI.OnSlotClicked += HandleSlotClicked;
        _hullSlotUI.OnSlotClicked += HandleSlotClicked;

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
}