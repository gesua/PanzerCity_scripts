using System;
using UnityEngine;

/// <summary>
/// 장비 장착 관리
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    ItemModel _mainGunSlot;
    ItemModel _turretSlot;
    ItemModel _hullSlot;

    public ItemModel MainGunSlot => _mainGunSlot;
    public ItemModel TurretSlot => _turretSlot;
    public ItemModel HullSlot => _hullSlot;

    public event Action<EquipSlot, ItemModel> OnEquipped;   // 장착
    public event Action<EquipSlot, ItemModel> OnUnequipped; // 해제

    /// <summary>
    /// 장비 장착
    /// </summary>
    public ItemModel Equip(ItemModel item)
    {
        EquipSlot slot = item.Config.EquipSlot;
        ItemModel prevItem = GetSlot(slot);

        SetSlot(slot, item);
        OnEquipped?.Invoke(slot, item);

        return prevItem; // 기존 장착 아이템 반환 (인벤토리로 돌려줄 용도)
    }

    /// <summary>
    /// 장비 해제
    /// </summary>
    public ItemModel Unequip(EquipSlot slot)
    {
        ItemModel item = GetSlot(slot);
        if (item == null) return null;

        SetSlot(slot, null);
        OnUnequipped?.Invoke(slot, item);

        return item; // 해제된 아이템 반환
    }

    ItemModel GetSlot(EquipSlot slot)
    {
        return slot switch
        {
            EquipSlot.MainGun => _mainGunSlot,
            EquipSlot.Turret => _turretSlot,
            EquipSlot.Hull => _hullSlot,
            _ => null
        };
    }

    void SetSlot(EquipSlot slot, ItemModel item)
    {
        switch (slot)
        {
            case EquipSlot.MainGun: _mainGunSlot = item; break;
            case EquipSlot.Turret: _turretSlot = item; break;
            case EquipSlot.Hull: _hullSlot = item; break;
        }
    }
}