using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// 상점 UI
/// </summary>
public class ShopUI : MonoBehaviour
{
    [SerializeField] Transform _rightPanelTr;
    [SerializeField] ShopOwnerUI _shopOwnerUI; // 상점 주인 대화
    [SerializeField] ShopItemSlot[] _itemSlots; // 상점에서 파는 소모품들
    [SerializeField] ShopItemSlot _equipmentSlot;   // 상점에서 파는 장비 (1개)
    [SerializeField] GameObject _equipmentSlotRoot; // 장비 슬롯 + 라벨 등 묶은 부모 오브젝트
    [SerializeField] TrashCanUI _trashCanUI; // 쓰레기통
    [SerializeField] GameObject _clickBlocker; // 종료 버튼 눌렀을 때 다른 거 못 누르게 막는 용도
    [SerializeField] ItemTooltipUI _itemTooltipUI; // 툴팁 UI
    [SerializeField] TextMeshProUGUI _readyCountText; // 다음 스테이지 준비 인원 표시(멀티 전용)

    Vector3 _tooltipOffset = new Vector3(0f, 200f, 0f); // 상점 아이템용 툴팁 위치 오프셋

    InventoryUI _inventoryUI;
    EquipmentUI _equipmentUI;

    bool _isMultiplayer;
    bool _isReadyForNextStage;

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
            _itemSlots[i].OnHoverEnter += HandleItemHoverEnter;
            _itemSlots[i].OnHoverExit += HandleItemHoverExit;
        }

        // 장비 슬롯 이벤트 구독
        _equipmentSlot.OnClicked += HandleEquipmentClicked;
        _equipmentSlot.OnHoverEnter += HandleItemHoverEnter;
        _equipmentSlot.OnHoverExit += HandleItemHoverExit;

        _isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (_isMultiplayer)
        {
            NetworkGameManager.Instance.OnNextStageReadyCountChanged += HandleNextStageReadyCountChanged;
            NetworkGameManager.Instance.OnAllReadyForNextStage += HandleAllReadyForNextStage;
        }
    }

    void OnDestroy()
    {
        // 신호가 오기 전에 파괴되는 경우(씬 전환 등) 구독 해제
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnNextStageReadyCountChanged -= HandleNextStageReadyCountChanged;
            NetworkGameManager.Instance.OnAllReadyForNextStage -= HandleAllReadyForNextStage;
        }
    }

    public void SetShopActive(bool active)
    {
        gameObject.SetActive(active);
        _clickBlocker.SetActive(false);

        if (active)
        {
            SetInteractable(true); // 상점 열릴 때 상호작용 잠금 해제
            _isReadyForNextStage = false; // 새 상점이니 준비 상태 초기화(멀티)

            _shopOwnerUI.ShowWelcome(); // 인사
            RollEquipmentItem(); // 열릴 때마다 장비 새로 뽑기

            _readyCountText.text = ""; // 멀티 준비 완료 텍스트 비워줌
        }
        else
        {
            // 상점이 닫힐 때 툴팁 숨김
            if (_itemTooltipUI != null) _itemTooltipUI.Hide();
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
    /// 아이템 호버 시작 (툴팁 켜기)
    /// </summary>
    void HandleItemHoverEnter(ItemConfig itemConfig, Vector3 pos)
    {
        if (_itemTooltipUI != null)
        {
            // 상점 전용 오프셋
            _itemTooltipUI.Show(itemConfig, pos, _tooltipOffset);
        }
    }

    /// <summary>
    /// 아이템 호버 종료 (툴팁 끄기)
    /// </summary>
    void HandleItemHoverExit()
    {
        if (_itemTooltipUI != null)
        {
            _itemTooltipUI.Hide();
        }
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

        // 목숨 증가는 인벤토리에 넣지 않고 즉시 적용
        if (itemConfig.Id == 1001)
        {
            GameManager.Instance.PlayerData.AddLife(1);
            GameManager.Instance.AudioManager.PlaySfx(SfxType.LifeUp);
            _shopOwnerUI.ShowBuySuccess();
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
        if (_itemTooltipUI != null) _itemTooltipUI.Hide(); // 툴팁 가림
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

        // 상호작용 막힐 때 툴팁도 바로 꺼줌
        if (isInteractable == false && _itemTooltipUI != null)
        {
            _itemTooltipUI.Hide();
        }
    }

    /// <summary>
    /// 나가기 버튼
    /// </summary>
    public void OnClickExit()
    {
        if (_isMultiplayer)
        {
            if (_isReadyForNextStage) return; // 중복 클릭 방지

            _isReadyForNextStage = true;
            SetInteractable(false); // 준비 완료 후 구매/주인 클릭 막기
            NetworkGameManager.Instance.RequestNextStageReadyServerRpc(); // 실제 퇴장은 전원 준비 완료 신호를 받은 뒤(HandleAllReadyForNextStage)
            return;
        }

        SetInteractable(false); // 나가는 동안 구매/주인 클릭 막기
        _clickBlocker.SetActive(true); // 그냥 물리적으로 다 막기

        _shopOwnerUI.ShowExit(); // 나가기 인사

        // 잠깐 대기 후 나가기
        StartCoroutine(ExitRoutine());
    }

    IEnumerator ExitRoutine()
    {
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
        OnExitClicked?.Invoke();
    }

    /// <summary>
    /// 멀티플레이:다음 스테이지 준비 인원 변경(카운트 표시용)
    /// </summary>
    void HandleNextStageReadyCountChanged(int readyCount, int totalCount)
    {
        UpdateReadyCountText(readyCount, totalCount);
    }

    /// <summary>
    /// 멀티플레이:전원 준비 완료 — 그제서야 실제 나가기 연출 시작
    /// </summary>
    void HandleAllReadyForNextStage()
    {
        _clickBlocker.SetActive(true); // 그냥 물리적으로 다 막기

        _shopOwnerUI.ShowExit(); // 나가기 인사
        StartCoroutine(ExitRoutine());
    }

    /// <summary>
    /// 다음 스테이지 준비 인원 텍스트 갱신(멀티 전용, 미할당 시 무시)
    /// </summary>
    void UpdateReadyCountText(int readyCount, int totalCount)
    {
        object[] args = new object[] { readyCount, totalCount };
        _readyCountText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Localization", "UI_MP_READY_COUNT", arguments: args);
    }
}