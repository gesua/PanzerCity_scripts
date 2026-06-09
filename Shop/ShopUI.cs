using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 상점 UI
/// </summary>
public class ShopUI : MonoBehaviour
{
    [SerializeField] Transform _rightPanelTr;
    [SerializeField] ShopOwnerUI _shopOwnerUI; // 상점 주인 대화
    [SerializeField] ShopItemSlot[] _itemSlots; // 상점에서 파는 아이템
    InventoryUI _inventoryUI;
    EquipmentUI _equipmentUI;

    public Transform RightPanelTr => _rightPanelTr;

    public event Action OnExitClicked;

    public void Initialize(InventoryUI inventoryUI)
    {
        _inventoryUI = inventoryUI;
    }

    void Start()
    {
        EquipmentManager equipmentManager = GameManager.Instance.EquipmentManager;
        _equipmentUI.Initialize(equipmentManager, _inventoryUI.Presenter);

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
        gameObject.SetActive(active);

        if (active)
        {
            _shopOwnerUI.ShowWelcome(); // 인사
        }
    }

    void HandleItemClicked(ItemConfig itemConfig)
    {
        // 골드 차감
        if (GameManager.Instance.PlayerData.SpendGold(itemConfig.BuyPrice) == false)
        {
            // 골드 부족
            _shopOwnerUI.ShowBuyFailGold();
            return;
        }

        // 인벤토리에 추가
        ItemModel item = new ItemModel(itemConfig);
        if (_inventoryUI.Presenter.AddItem(item) == false)
        {
            // 공간 부족
            GameManager.Instance.PlayerData.AddGold(itemConfig.BuyPrice); // 골드 환불
            _shopOwnerUI.ShowBuyFailSpace();
            return;
        }

        _shopOwnerUI.ShowBuySuccess(); // 구입 성공 대사
    }

    /// <summary>
    /// 나가기 버튼
    /// </summary>
    public void OnClickExit()
    {
        _shopOwnerUI.ShowExit(); // 나가기 인사
        // 잠깐 대기 후 나가기
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
        OnExitClicked?.Invoke();
    }
}
