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
        _presenter.OnItemUsed += HandleItemUsed;
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
            int count = CountItemsById(itemID);
            _slots[i].SetCount(count);
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

    /// <summary>
    /// 아이템 사용 시 해당 슬롯에 펀치 스케일/남은 시간 링 연출 재생
    /// </summary>
    void HandleItemUsed(ItemConfig config)
    {
        PlayEffectFeedback(config.Id, config.Duration);
    }

    /// <summary>
    /// 해당 슬롯에 펀치 스케일/남은 시간 링 연출 재생
    /// 본인이 직접 사용했을 때(HandleItemUsed)뿐 아니라, 멀티에서 다른 사람이 발동한 공유 효과(기지 무적/EMP)를
    /// 통지받았을 때(GameScene이 NetworkGameManager 이벤트를 구독해서 호출)도 재사용됨
    /// </summary>
    public void PlayEffectFeedback(int itemID, float duration)
    {
        for (int i = 0; i < SlotItemIDs.Length; i++)
        {
            bool isMatchingSlot = (SlotItemIDs[i] == itemID);
            if (isMatchingSlot == false) continue;

            _slots[i].PlayUseFeedback(duration);
            break;
        }
    }

    int CountItemsById(int itemID)
    {
        int count = 0;
        foreach (ItemModel item in _presenter.Items)
        {
            if (item.Config.Id == itemID) count++;
        }
        return count;
    }
}