using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 UI
/// </summary>
public class ShopUI : MonoBehaviour
{
    [SerializeField] Transform _rightPanelTr;
    [SerializeField] ShopOwnerUI _shopOwnerUI; // 상점 주인 대화
    [SerializeField] ShopItemSlot[] _itemSlots; // 상점에서 파는 소모품들
    [SerializeField] ShopItemSlot _equipmentSlot;   // 장비 슬롯 (1개)
    [SerializeField] GameObject _equipmentSlotRoot; // 장비 슬롯 + 라벨 등 묶은 부모 오브젝트 (선택)
    [SerializeField] TrashCanUI _trashCanUI; // 쓰레기통

    InventoryUI _inventoryUI;
    EquipmentUI _equipmentUI;

    const int EquipDropGroupID = 8301; // 장비 확률

    public Transform RightPanelTr => _rightPanelTr;

    public event Action OnExitClicked;

    public void Initialize(InventoryUI inventoryUI, EquipmentUI equipmentUI)
    {
        _inventoryUI = inventoryUI;
        _equipmentUI = equipmentUI;
    }

    void Start()
    {
        EquipmentManager equipmentManager = GameManager.Instance.EquipmentManager;
        
        _equipmentUI.Initialize(equipmentManager, _inventoryUI.Presenter);
        _trashCanUI.Initialize(_inventoryUI.Presenter);

        // 상점에 배치할 아이템 (항상 똑같음)
        int[] storeItemIDs = { 1001, 1002, 1003, 1004, 1005 };
        for (int i = 0; i < _itemSlots.Length; i++)
        {
            ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(storeItemIDs[i]);
            _itemSlots[i].Initialize(config);
            _itemSlots[i].OnClicked += HandleItemClicked;
        }

        // 장비 슬롯 이벤트 구독
        _equipmentSlot.OnClicked += HandleEquipmentClicked;
    }

    public void SetShopActive(bool active)
    {
        gameObject.SetActive(active);

        if (active)
        {
            SetInteractable(true); // 상점 열릴 때 상호작용 잠금 해제

            _shopOwnerUI.ShowWelcome(); // 인사
            RollEquipmentItem(); // 열릴 때마다 장비 새로 뽑기
        }
    }

    /// <summary>
    /// DropGroup 8301에서 가중치 기반으로 장비 1개 랜덤 선택
    /// </summary>
    void RollEquipmentItem()
    {
        List<ItemDropGroupData> dropGroup = GameManager.Instance.DataManager.GetDropGroup(EquipDropGroupID);

        if (dropGroup == null || dropGroup.Count == 0)
        {
            SetEquipmentSlotVisible(false);
            return;
        }

        // SpawnWeight > 0인 항목만 필터링
        // HACK:지금 없는 장비 있어서(나중에 다 넣으면 없앨 부분)
        List<ItemDropGroupData> validItems = new();
        int totalWeight = 0;
        foreach (ItemDropGroupData data in dropGroup)
        {
            if (data.SpawnWeight > 0)
            {
                validItems.Add(data);
                totalWeight += data.SpawnWeight;
            }
        }

        // 뭔가 잘못된거
        if (validItems.Count == 0 || totalWeight == 0)
        {
            SetEquipmentSlotVisible(false);
            return;
        }

        // 가중치 기반 랜덤 뽑기
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        ItemDropGroupData selected = null;
        foreach (ItemDropGroupData data in validItems)
        {
            cumulative += data.SpawnWeight;
            if (roll < cumulative)
            {
                selected = data;
                break;
            }
        }

        if (selected == null)
        {
            SetEquipmentSlotVisible(false);
            return;
        }

        ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(selected.ItemID);
        if (config == null)
        {
            Debug.LogWarning($"[ShopUI] DropGroup {EquipDropGroupID}에서 뽑힌 ItemID {selected.ItemID}의 ItemConfig 없음");
            SetEquipmentSlotVisible(false);
            return;
        }

        SetEquipmentSlotVisible(true);
        _equipmentSlot.Initialize(config); // Initialize 내부에서 SetSoldOut(false) 리셋
    }

    /// <summary>
    /// 장비 슬롯(및 부모 오브젝트) 표시/숨김
    /// </summary>
    void SetEquipmentSlotVisible(bool visible)
    {
        // _equipmentSlotRoot가 있으면 그쪽을 제어 (라벨 등 포함), 없으면 슬롯 직접 제어
        if (_equipmentSlotRoot != null)
            _equipmentSlotRoot.SetActive(visible);
        else
            _equipmentSlot.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 소모품 구매
    /// </summary>
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
    /// 장비 구매 - 인벤토리에 추가 후 매진 처리
    /// </summary>
    void HandleEquipmentClicked(ItemConfig itemConfig)
    {
        if (GameManager.Instance.PlayerData.SpendGold(itemConfig.BuyPrice) == false)
        {
            _shopOwnerUI.ShowBuyFailGold();
            return;
        }

        ItemModel item = new ItemModel(itemConfig);
        if (_inventoryUI.Presenter.AddItem(item) == false)
        {
            GameManager.Instance.PlayerData.AddGold(itemConfig.BuyPrice); // 골드 환불
            _shopOwnerUI.ShowBuyFailSpace();
            return;
        }

        _shopOwnerUI.ShowBuySuccess();
        _equipmentSlot.SetSoldOut(true); // 구매 후 매진
    }

    /// <summary>
    /// 아이템 슬롯 및 상점 주인 상호작용 가능 여부 일괄 설정
    /// </summary>
    void SetInteractable(bool isInteractable)
    {
        foreach (ShopItemSlot itemSlot in _itemSlots)
        {
            itemSlot.SetInteractable(isInteractable);
        }

        _equipmentSlot.SetInteractable(isInteractable);
        _shopOwnerUI.SetInteractable(isInteractable);
    }

    /// <summary>
    /// 나가기 버튼
    /// </summary>
    public void OnClickExit()
    {
        SetInteractable(false); // 나가는 동안 구매/주인 클릭 막기

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