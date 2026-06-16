using UnityEngine;

/// <summary>
/// 퀵슬롯 UI 관리
/// 슬롯 순서: 0=기지 무적(1002), 1=나 무적(1003), 2=적 멈춤(1004), 3=폭탄(1005)
/// </summary>
public class QuickSlotUI : MonoBehaviour
{
    static readonly int[] SlotItemIDs = { 1002, 1003, 1004, 1005 };

    [SerializeField] QuickSlot[] _slots; // Inspector에서 순서대로 연결

    InventoryPresenter _presenter;

    public void Initialize(InventoryPresenter presenter)
    {
        _presenter = presenter;
        _presenter.OnInventoryChanged += Refresh;
        Refresh();
    }

    /// <summary>
    /// 인벤토리 변경 시 슬롯 상태 갱신
    /// </summary>
    public void Refresh()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            int itemID = SlotItemIDs[i];
            ItemModel item = FindItemById(itemID);
            bool hasItem = (item != null);
            _slots[i].SetAvailable(hasItem);
        }
    }

    /// <summary>
    /// 키 입력으로 슬롯 사용
    /// </summary>
    public void UseSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotItemIDs.Length) return;
        _presenter.TryUseItemById(SlotItemIDs[slotIndex]);
    }

    ItemModel FindItemById(int itemID)
    {
        foreach (ItemModel item in _presenter.Items)
        {
            if (item.Config.Id == itemID) return item;
        }
        return null;
    }
}
