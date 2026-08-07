using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

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

    [Header("----- 멀티플레이 원격 커서 -----")]
    [SerializeField] RectTransform _cursorLayerRoot; // 커서 이미지들이 배치될 부모
    [SerializeField] Image[] _remoteCursorImages; // 색상별 커서 이미지(인덱스 0~3 = 1P~4P)

    Vector3 _tooltipOffset = new Vector3(0f, 200f, 0f); // 상점 아이템용 툴팁 위치 오프셋

    InventoryUI _inventoryUI;
    EquipmentUI _equipmentUI;

    bool _isMultiplayer;
    bool _isReadyForNextStage;

    PlayerCursorSync _localCursorSync; // 로컬 플레이어의 커서 송신용 컴포넌트
    PlayerCursorSync[] _remoteCursorSyncs; // 인덱스 = OwnerClientId, 값 = 해당 플레이어의 커서 동기화 컴포넌트(없으면 null)
    int _pendingCursorSlotCount; // 아직 참조를 못 채운 원격 슬롯 수 — 늦게 스폰 완료되는 클라이언트를 다음 프레임에 재시도로 채우기 위함

    const int EquipDropGroupID = 8301; // 장비 확률
    const int MaxPlayerCount = 4; // 멀티플레이 최대 인원(커서 색상 슬롯 수와 동일)

    public Transform RightPanelTr => _rightPanelTr;

    public event Action OnExitClicked;

    public void Initialize(InventoryUI inventoryUI, EquipmentUI equipmentUI)
    {
        _inventoryUI = inventoryUI;
        _equipmentUI = equipmentUI;
    }

    void Start()
    {
        // TEMP-LOG:원인 조사용, 확인 끝나면 제거
        Debug.Log($"[ShopUI] Start() 실행 | GameObject.activeInHierarchy:{gameObject.activeInHierarchy} | Frame:{Time.frameCount}");

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

            InitializeCursorSync();
        }

        HideAllRemoteCursors(); // 커서 이미지는 상점이 열리기 전까지 전부 숨김(싱글이거나 인원이 안 찬 슬롯 대비)
    }

    /// <summary>
    /// 멀티플레이:로컬/원격 플레이어의 커서 동기화 컴포넌트 참조 수집
    /// 클라이언트마다 PlayerObject 스폰 완료 시점이 달라 ShopUI.Start() 시점엔 일부가 아직 null일 수 있음
    /// (목숨 UI와 달리 커서는 값 변경 이벤트로 재갱신되지 않으므로) 아직 못 채운 슬롯만 골라 채우고,
    /// 남은 슬롯이 있으면 Update()에서 매 프레임 다시 호출해 늦게 스폰되는 플레이어도 따라잡음
    /// </summary>
    void InitializeCursorSync()
    {
        if (_remoteCursorImages.Length != MaxPlayerCount)
        {
            Debug.LogWarning($"[ShopUI] _remoteCursorImages는 {MaxPlayerCount}개(1P~4P)여야 합니다. 현재 {_remoteCursorImages.Length}개");
        }

        _remoteCursorSyncs ??= new PlayerCursorSync[MaxPlayerCount];

        // 로컬 플레이어(커서 송신 주체) — 아직 못 채웠으면 재시도
        if (_localCursorSync == null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerCursorSync localCursorSync))
        {
            _localCursorSync = localCursorSync;
        }

        // 원격 플레이어(커서 수신 대상) — OwnerClientId를 그대로 색상 인덱스로 사용(PlayerNetworkOwner.SetTankColorByIndex와 동일한 관례)
        int pendingCount = 0;
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            int colorIndex = (int)client.ClientId;
            if (colorIndex < 0 || colorIndex >= MaxPlayerCount) continue; // 방어적 가드

            if (_remoteCursorSyncs[colorIndex] != null) continue; // 이미 채워짐

            if (client.PlayerObject == null || client.PlayerObject.TryGetComponent(out PlayerCursorSync cursorSync) == false)
            {
                pendingCount++; // 이 클라이언트는 아직 스폰 미완료 — 다음 시도에서 다시 확인
                continue;
            }

            _remoteCursorSyncs[colorIndex] = cursorSync;
        }
        _pendingCursorSlotCount = pendingCount;

        // TEMP-LOG:원인 조사용, 확인 끝나면 제거
        string slotSummary = string.Join(", ", System.Array.ConvertAll(_remoteCursorSyncs, s => (s == null) ? "null" : $"ClientId{s.OwnerClientId}"));
        Debug.Log($"[ShopUI] InitializeCursorSync() 완료 | LocalCursorSync:{((_localCursorSync == null) ? "null" : "찾음")} | Slots:[{slotSummary}] | Pending:{pendingCount} | Frame:{Time.frameCount}");
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

    void Update()
    {
        if (_isMultiplayer == false) return; // 싱글플레이는 원격 커서 자체가 필요 없음
        if (_remoteCursorSyncs == null) return; // 초기화 전 방어적 가드

        // 늦게 스폰 완료되는 클라이언트가 있으면 빈 슬롯만 다시 채움(다 채워지면 더 이상 호출 안 됨)
        if (_pendingCursorSlotCount > 0) InitializeCursorSync();

        UpdateRemoteCursors();
    }

    public void SetShopActive(bool active)
    {
        // TEMP-LOG:원인 조사용, 확인 끝나면 제거
        Debug.Log($"[ShopUI] SetShopActive({active}) 호출 | _isMultiplayer:{_isMultiplayer} | Frame:{Time.frameCount}");

        gameObject.SetActive(active);
        _clickBlocker.SetActive(false);

        if (active)
        {
            SetInteractable(true); // 상점 열릴 때 상호작용 잠금 해제
            _isReadyForNextStage = false; // 새 상점이니 준비 상태 초기화(멀티)

            _shopOwnerUI.ShowWelcome(); // 인사
            RollEquipmentItem(); // 열릴 때마다 장비 새로 뽑기

            _readyCountText.text = ""; // 멀티 준비 완료 텍스트 비워줌

            // 멀티플레이:상점에서만 서로 커서가 보이도록 송신 시작
            if (_isMultiplayer && _localCursorSync != null)
            {
                _localCursorSync.SetCursorActive(true);
            }
        }
        else
        {
            // 상점이 닫힐 때 툴팁 숨김
            if (_itemTooltipUI != null) _itemTooltipUI.Hide();

            // 멀티플레이:다음 스테이지로 넘어가면 커서 송신 중단 + 화면에서도 즉시 제거
            if (_isMultiplayer && _localCursorSync != null)
            {
                _localCursorSync.SetCursorActive(false);
            }
            HideAllRemoteCursors();
        }
    }

    /// <summary>
    /// 원격 커서 이미지 전부 숨김(상점 닫힘, 씬 전환 등)
    /// </summary>
    void HideAllRemoteCursors()
    {
        foreach (Image cursorImage in _remoteCursorImages)
        {
            cursorImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 멀티플레이:원격 플레이어들의 커서 위치를 로컬 화면에 반영
    /// 화면 비율(0~1) 좌표를 커서 레이어의 로컬 픽셀 좌표로 변환해서 각 색상별 Image의 anchoredPosition에 적용
    /// </summary>
    void UpdateRemoteCursors()
    {
        Rect layerRect = _cursorLayerRoot.rect;

        for (int i = 0; i < MaxPlayerCount; i++)
        {
            PlayerCursorSync cursorSync = _remoteCursorSyncs[i];
            Image cursorImage = _remoteCursorImages[i];

            // 대상 플레이어가 없거나(접속 안 함) 로컬 자기 자신이거나 상점을 안 열어놨으면 숨김
            bool shouldShow = (cursorSync != null) && (cursorSync != _localCursorSync) && cursorSync.IsCursorActive;
            if (cursorImage.gameObject.activeSelf != shouldShow)
            {
                cursorImage.gameObject.SetActive(shouldShow);

                // TEMP-LOG:원인 조사용, 확인 끝나면 제거
                Debug.Log($"[ShopUI] 커서 슬롯[{i}] 활성 전환 → {shouldShow} | cursorSync:{((cursorSync == null) ? "null" : $"ClientId{cursorSync.OwnerClientId}")} | Frame:{Time.frameCount}");
            }
            if (shouldShow == false) continue;

            Vector2 normalizedPos = cursorSync.CursorPosition;
            Vector2 localPos = new Vector2(
                (normalizedPos.x - 0.5f) * layerRect.width,
                (normalizedPos.y - 0.5f) * layerRect.height);

            cursorImage.rectTransform.anchoredPosition = localPos;
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