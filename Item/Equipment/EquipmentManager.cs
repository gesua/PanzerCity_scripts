using System;
using UnityEngine;

/// <summary>
/// 장비 장착 관리
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    TankModel _playerModel;

    ItemModel _mainGunSlot;
    ItemModel _turretSlot;
    ItemModel _hullSlot;

    public ItemModel MainGunSlot => _mainGunSlot;
    public ItemModel TurretSlot => _turretSlot;
    public ItemModel HullSlot => _hullSlot;

    public event Action<EquipSlot, ItemModel> OnEquipped;   // 장착
    public event Action<EquipSlot, ItemModel> OnUnequipped; // 해제

    public void Initialize(TankModel playerModel)
    {
        _playerModel = playerModel;
        _mainGunSlot = null;
        _turretSlot = null;
        _hullSlot = null;
    }

    /// <summary>
    /// 장비 장착
    /// </summary>
    public ItemModel Equip(ItemModel item)
    {
        // 기존 장착 해제
        ItemModel prevItem = Unequip(item.Config.EquipSlot);

        SetSlot(item.Config.EquipSlot, item);
        _playerModel.ApplyEquipment(item.Config, true); // 스탯 적용
        OnEquipped?.Invoke(item.Config.EquipSlot, item);

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
        _playerModel.ApplyEquipment(item.Config, false); // 스탯 해제
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

    /// <summary>
    /// 세이브 데이터로부터 장비 복원 (이어하기 시, 미장착이면 -1)
    /// </summary>
    public void LoadFromSaveData(int mainGunItemID, int turretItemID, int hullItemID)
    {
        UnequipAll(); // 장착 모두 해제

        EquipFromID(mainGunItemID);
        EquipFromID(turretItemID);
        EquipFromID(hullItemID);
    }

    /// <summary>
    /// ID로 ItemConfig를 찾아 장착(세이브 데이터 복원용)
    /// </summary>
    void EquipFromID(int itemID)
    {
        if (itemID == -1) return;

        ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(itemID);
        if (config == null) return;

        Equip(new ItemModel(config));
    }

    /// <summary>
    /// 장착된 모든 장비 해제 (새 게임 시작 시 초기화용)
    /// </summary>
    public void UnequipAll()
    {
        Unequip(EquipSlot.MainGun);
        Unequip(EquipSlot.Turret);
        Unequip(EquipSlot.Hull);
    }
}