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
    [SerializeField] Image _exitButtonImage; // 출격 버튼 - 클릭 시 색 변경으로 "내가 눌렀는지" 표시(멀티 대기 중 구분용)

    [Header("----- 멀티플레이 원격 커서 -----")]
    [SerializeField] RectTransform _cursorLayerRoot; // 커서 이미지들이 배치될 부모
    [SerializeField] Image[] _remoteCursorImages; // 색상별 커서 이미지(인덱스 0~3 = 1P~4P)

    Vector3 _tooltipOffset = new Vector3(0f, 200f, 0f); // 상점 아이템용 툴팁 위치 오프셋

    InventoryUI _inventoryUI;
    EquipmentUI _equipmentUI;
    SlideInDirector _slideInDirector; // 상점 UI(상점 주인 포함) 등장 연출

    bool _isMultiplayer;
    bool _isReadyForNextStage;
    bool _isInitialized; // EnsureShopInitialized() 중복 실행 방지 가드

    PlayerCursorSync _localCursorSync; // 로컬 플레이어의 커서 송신용 컴포넌트
    PlayerCursorSync[] _remoteCursorSyncs; // 인덱스 = OwnerClientId, 값 = 해당 플레이어의 커서 동기화 컴포넌트(없으면 null)
    int _pendingCursorSlotCount; // 아직 참조를 못 채운 원격 슬롯 수 — 늦게 스폰 완료되는 클라이언트를 다음 프레임에 재시도로 채우기 위함
    bool _previousCursorVisible; // 상점 진입 전 Cursor.visible 상태 저장 — 나갈 때 그대로 복원(다른 시스템이 커서를 어떻게 쓰고 있었는지 몰라도 안전하게 되돌리기 위함)

    const int EquipDropGroupID = 8301; // 장비 확률
    const int MaxPlayerCount = 4; // 멀티플레이 최대 인원(커서 색상 슬롯 수와 동일)

    public Transform RightPanelTr => _rightPanelTr;

    public event Action OnExitClicked;

    public void Initialize(InventoryUI inventoryUI, EquipmentUI equipmentUI, SlideInDirector slideInDirector)
    {
        _inventoryUI = inventoryUI;
        _equipmentUI = equipmentUI;
        _slideInDirector = slideInDirector;
    }

    void Start()
    {
        EquipmentManager equipmentManager = GameManager.Instance.EquipmentManager;

        _equipmentUI.Initialize(equipmentManager, _inventoryUI.Presenter, _itemTooltipUI);
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

        EnsureShopInitialized(); // SetShopActive()가 이미 호출했다면 가드로 스킵됨(정상 경로) — Start()가 먼저 도는 경우를 대비한 안전망
    }

    /// <summary>
    /// 멀티플레이 판별 + 이벤트 구독 + 커서 동기화 초기 수집(최초 1회만 실행)
    /// Unity는 GameObject가 처음 활성화된 프레임에 Start()를 그 자리에서 곧바로가 아니라
    /// 해당 프레임의 Update 처리 직전으로 미루기 때문에(Awake/OnEnable과 달리 Start만 지연됨),
    /// SetShopActive(true) 안에서 gameObject.SetActive(active) 직후 곧바로 _isMultiplayer/_localCursorSync를
    /// 참조하면 Start()가 아직 실행되기 전(기본값 false/null)인 상태를 보게 됨
    /// — 그래서 Start()에만 맡기지 않고 SetShopActive()가 최초 호출되는 시점에 직접 실행을 보장함
    /// </summary>
    void EnsureShopInitialized()
    {
        if (_isInitialized) return; // 이미 초기화됐으면 재실행 방지
        _isInitialized = true;

        _isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (_isMultiplayer)
        {
            NetworkGameManager.Instance.OnNextStageReadyCountChanged += HandleNextStageReadyCountChanged;
            NetworkGameManager.Instance.OnAllReadyForNextStage += HandleAllReadyForNextStage;

            InitializeCursorSync();
        }

        HideAllCursors(); // 커서 이미지는 상점이 열리기 전까지 전부 숨김(싱글이거나 인원이 안 찬 슬롯 대비)
    }

    /// <summary>
    /// 멀티플레이:로컬/원격 플레이어의 커서 동기화 컴포넌트 참조 수집
    /// 클라이언트마다 PlayerObject 스폰 완료 시점이 달라 최초 호출 시점엔 일부가 아직 null일 수 있음
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

            // 내 커서를 가장 위로 보이게 배치
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            _remoteCursorImages[localClientId].transform.SetAsLastSibling();
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

        UpdateCursors();
    }

    public void SetShopActive(bool active)
    {
        EnsureShopInitialized(); // Start()가 아직 실행되기 전(최초 오픈 시점)이어도 커서 동기화 상태를 먼저 확정

        gameObject.SetActive(active);

        // HUD 연출:상점 UI가 켜지는 그 즉시 화면 밖으로 세팅(한 프레임도 제자리에 노출되지 않도록)
        // SlideInDirector는 자체적으로 화면 밖 위치를 자동으로 잡지 않으므로 여기서 명시적으로 호출
        if (active && _slideInDirector != null) _slideInDirector.PrepareOffscreen();

        _clickBlocker.SetActive(false);

        // 멀티플레이:다른 플레이어들이 로컬에서 내 탱크를 숨기고 복원할 수 있도록 상점 열림/닫힘 신호 전달
        if (_isMultiplayer)
        {
            NetworkGameManager.Instance.NotifyShopActiveChanged(active);
        }

        if (active)
        {
            SetInteractable(true); // 상점 열릴 때 상호작용 잠금 해제
            _isReadyForNextStage = false; // 새 상점이니 준비 상태 초기화(멀티)
            _exitButtonImage.color = Color.white; // 출격 버튼 색 복구

            RollEquipmentItem(); // 열릴 때마다 장비 새로 뽑기

            _readyCountText.text = ""; // 멀티 준비 완료 텍스트 비워줌

            // 멀티플레이:상점에서만 서로 커서가 보이도록 송신 시작
            if (_isMultiplayer && _localCursorSync != null)
            {
                _localCursorSync.SetCursorActive(true);

                _previousCursorVisible = Cursor.visible; // 나갈 때 복원할 수 있도록 진입 전 상태 저장
                Cursor.visible = false; // 커스텀 이미지 커서와 겹쳐 보이지 않도록 OS 기본 커서만 숨김 — CursorLockMode는 그대로 None 유지(잠그면 마우스 자유 이동/클릭 자체가 막혀서 상점 조작과 위치 추적이 둘 다 깨짐)
            }

            // HUD 연출:상점 UI(상점 주인 포함) 슬라이드인 시작. 인사(ShowWelcome)는 슬라이드인이 끝난 뒤 재생
            if (_slideInDirector != null)
            {
                _slideInDirector.PlayIntro();
                StartCoroutine(ShowWelcomeAfterSlideIn());
            }
            else
            {
                _shopOwnerUI.ShowWelcome(); // 연출 컴포넌트가 없으면(미할당 등) 기존처럼 즉시 인사
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
                Cursor.visible = _previousCursorVisible; // 진입 전 상태로 복원
            }
            HideAllCursors();
        }
    }

    /// <summary>
    /// 커서 이미지 전부 숨김(상점 닫힘, 씬 전환 등)
    /// </summary>
    void HideAllCursors()
    {
        foreach (Image cursorImage in _remoteCursorImages)
        {
            cursorImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 멀티플레이:커서 위치를 로컬 화면에 반영 — 지금은 테스트 목적으로 로컬 플레이어 본인 슬롯도 함께 표시함(원래대로 자신만 숨기려면
    /// shouldShow 조건에 "cursorSync != _localCursorSync"를 다시 추가하면 됨). 상점 진입 시 Cursor.visible을 꺼서 OS 기본 커서와는 안 겹침
    /// RectTransformUtility.ScreenPointToWorldPointInRectangle + rectTransform.position(월드 좌표 직접 대입)으로 처리
    /// anchoredPosition 대신 position을 쓰는 이유:커서 이미지마다 Anchor/Pivot/Scale을 자유롭게 잡아도(핫스팟 정렬 등) 항상 정확한 위치에 그려짐 —
    /// anchoredPosition은 그 이미지의 앵커 기준점이 뭐냐에 따라 같은 값이 다른 위치로 해석돼서 이미지마다 앵커 설정을 신경 써야 하는 문제가 있었음
    /// Screen Space - Overlay 캔버스 기준으로 카메라 인자를 null로 전달함 — Screen Space - Camera라면 해당 렌더 카메라를 넘겨야 함
    /// </summary>
    void UpdateCursors()
    {
        for (int i = 0; i < MaxPlayerCount; i++)
        {
            PlayerCursorSync cursorSync = _remoteCursorSyncs[i];
            Image cursorImage = _remoteCursorImages[i];

            // 대상 플레이어가 없거나(접속 안 함) 상점을 안 열어놨으면 숨김
            bool shouldShow = (cursorSync != null) && cursorSync.IsCursorActive;
            if (cursorImage.gameObject.activeSelf != shouldShow)
            {
                cursorImage.gameObject.SetActive(shouldShow);
            }
            if (shouldShow == false) continue;

            Vector2 normalizedPos = cursorSync.CursorPosition;
            Vector2 screenPoint = new Vector2(normalizedPos.x * Screen.width, normalizedPos.y * Screen.height); // 수신자 자신의 해상도 기준으로 화면 좌표 복원

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_cursorLayerRoot, screenPoint, null, out Vector3 worldPos))
            {
                cursorImage.rectTransform.position = worldPos;
            }
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
        _exitButtonImage.color = Color.green; // 클릭 즉시 초록색으로 전환 - 대기 중에도 내가 눌렀는지 구분 가능

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
        SetShopActive(false); // 상점 정리 로직(커서 송신 중단/Cursor.visible 복원/원격 탱크 렌더러 복원 신호)이 전부 여기를 거쳐야 하므로 gameObject.SetActive 직접 호출 대신 이 메서드를 통해 닫음
        OnExitClicked?.Invoke();
    }

    /// <summary>
    /// HUD 연출:상점 UI(상점 주인 포함) 슬라이드인이 끝날 때까지 대기 후 상점 주인 인사 재생
    /// 상점 진입 시 Time.timeScale은 건드리지 않으므로(HandleStageClear 참고) SlideInDirector.IntroRoutine과
    /// 동일하게 Time.deltaTime 기준으로 대기해야 슬라이드인 종료 시점과 정확히 일치함
    /// </summary>
    IEnumerator ShowWelcomeAfterSlideIn()
    {
        yield return new WaitForSeconds(_slideInDirector.SlideDuration);
        _shopOwnerUI.ShowWelcome();
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