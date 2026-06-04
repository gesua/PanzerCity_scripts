using TMPro;
using UnityEngine;

/// <summary>
/// 상점 UI
/// </summary>
public class StoreUI : MonoBehaviour
{
    [SerializeField] Canvas _shopCanvas;
    [SerializeField] GameInfoUI _gameInfoUI;
    [SerializeField] StoreItemSlot[] _itemSlots; // 상점에서 파는 아이템
    InventoryUI _inventoryUI;

    public GameInfoUI GameInfoUI => _gameInfoUI;

    public void Initialize(InventoryUI inventoryUI)
    {
        _inventoryUI = inventoryUI;
    }

    void Start()
    {
        // 상점에 배치할 아이템 (항상 똑같음)
        int[] storeItemIDs = { 1001, 1002, 1003, 1004, 1005 };
        for (int i = 0; i < _itemSlots.Length; i++)
        {
            ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(storeItemIDs[i]);
            _itemSlots[i].Initialize(config);
            _itemSlots[i].OnClicked += HandleItemClicked;
        }
    }

    public void SetShopActive(bool active)
    {
        _shopCanvas.enabled = active;
    }

    void HandleItemClicked(ItemConfig itemConfig)
    {
        Debug.Log("구매 시도");

        // 골드 차감
        if (GameManager.Instance.PlayerData.SpendGold(itemConfig.BuyPrice) == false) return;

        Debug.Log("구매 성공");

        // 인벤토리에 추가
        ItemModel item = new ItemModel(itemConfig);
        _inventoryUI.Presenter.AddItem(item);
    }
}
