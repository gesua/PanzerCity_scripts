using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] RectTransform _playerStatusRect; // 상점 열릴 때 위치 옮길 UI
    [SerializeField] CanvasGroup _group;
    [SerializeField] DraggingItemUI _draggingItem;
    [SerializeField] InventoryView _inventoryView;
    [SerializeField] ItemTooltipUI _tooltip; // 아이템 툴팁 UI
    [SerializeField] int _width = 4;
    [SerializeField] int _height = 5;
    [Header("----- 이미지 관련 -----")]
    [SerializeField] Sprite _open;  // 열린거
    [SerializeField] Sprite _close; // 닫힌거
    [SerializeField] Image _btnImg; // 가방 버튼 이미지
    [Header("----- 목숨 UI(상점 3인 이상 가림 보정) -----")]
    [SerializeField] RectTransform _lifeUI; // 3P 이상일 때 상점에서 아래가 가려지는 목숨 UI
    [SerializeField] float _lifeUIShopOffsetY = 30f; // 상점 진입 시 위로 올릴 Y값
    [SerializeField] int _lifeUIOffsetMinPlayerCount = 3; // 이 인원수부터 보정 적용(2P까지는 미적용)

    InventoryModel _inventoryModel;
    InventoryPresenter _inventoryPresenter;

    Transform _orgParent; // 상점 갔다가 돌아올 때
    bool _isShop; // 상점일 때

    Vector2 _lifeUIOriginalPos; // 상점 진입 전 원래 위치(퇴장 시 복구용)

    public InventoryPresenter Presenter => _inventoryPresenter;

    bool _isActive;

    private void Awake()
    {
        _inventoryModel = new InventoryModel(_width, _height);
        _inventoryPresenter = new InventoryPresenter(_inventoryModel, _inventoryView, _draggingItem, GameManager.Instance.EquipmentManager, _tooltip);

        _orgParent = _playerStatusRect.parent;
    }

    /// <summary>
    /// 창 토글
    /// </summary>
    public void Toggle()
    {
        if (_isShop) return; // 상점인 경우 막음

        _isActive = !_isActive;
        _group.alpha = _isActive ? 1f : 0f;
        _group.interactable = _isActive;
        _group.blocksRaycasts = _isActive;

        // 가방 이미지 변경
        if (_isActive)
        {
            _btnImg.sprite = _open;
        }
        else
        {
            _btnImg.sprite = _close;
            _tooltip.Hide(); // 인벤토리 닫을 때 툴팁 숨김
        }
    }

    /// <summary>
    /// 상점 입장
    /// </summary>
    public void EnterStore(Transform shopTr)
    {
        if (_isActive) Toggle(); // 가방 닫기

        _isShop = true;
        _inventoryPresenter.SetShopMode(_isShop);
        _playerStatusRect.SetParent(shopTr, false);
        _playerStatusRect.anchoredPosition = new Vector2(0, -380); // 위치 잡기
        _group.alpha = 1f;
        _group.interactable = true;
        _group.blocksRaycasts = true;

        ApplyLifeUIShopOffset(); // 3P 이상이면 목숨 UI가 가려지는 것 보정
    }

    /// <summary>
    /// 상점 퇴장
    /// </summary>
    public void ExitStore()
    {
        _isShop = false;
        _inventoryPresenter.SetShopMode(_isShop);
        _playerStatusRect.SetParent(_orgParent, false);
        _playerStatusRect.anchoredPosition = Vector2.zero; // 위치 잡기
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
        _tooltip.Hide(); // 상점 퇴장 시 툴팁 숨김

        RestoreLifeUIPosition(); // 목숨 UI 원래 위치로 복구
    }

    /// <summary>
    /// 접속 인원이 3P 이상이면 상점에서 목숨 UI 아래가 가려지므로 위로 올려서 보정
    /// 2P 이하(싱글 포함)는 원래 위치 그대로 둠
    /// </summary>
    void ApplyLifeUIShopOffset()
    {
        if (_lifeUI == null) return;

        _lifeUIOriginalPos = _lifeUI.anchoredPosition; // 진입 시점 위치를 원본으로 저장(복구용)

        if (GetConnectedPlayerCount() < _lifeUIOffsetMinPlayerCount) return;

        _lifeUI.anchoredPosition = _lifeUIOriginalPos + new Vector2(0f, _lifeUIShopOffsetY);
    }

    /// <summary>
    /// 목숨 UI를 상점 진입 전 위치로 복구
    /// </summary>
    void RestoreLifeUIPosition()
    {
        if (_lifeUI == null) return;

        _lifeUI.anchoredPosition = _lifeUIOriginalPos;
    }

    /// <summary>
    /// 현재 접속 인원 수 조회(싱글플레이면 1명으로 취급)
    /// </summary>
    int GetConnectedPlayerCount()
    {
        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (isMultiplayer == false) return 1;

        return NetworkManager.Singleton.ConnectedClientsIds.Count;
    }
}