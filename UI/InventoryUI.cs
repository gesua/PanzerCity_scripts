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
    [SerializeField] int _width = 4;
    [SerializeField] int _height = 5;
    [Header("----- 이미지 관련 -----")]
    [SerializeField] Sprite _open;  // 열린거
    [SerializeField] Sprite _close; // 닫힌거
    [SerializeField] Image _btnImg; // 가방 버튼 이미지

    InventoryModel _inventoryModel;
    InventoryPresenter _inventoryPresenter;

    Transform _orgParent; // 상점 갔다가 돌아올 때
    bool _isShop; // 상점일 때

    public InventoryPresenter Presenter => _inventoryPresenter;

    bool _isActive;

    private void Awake()
    {
        _inventoryModel = new InventoryModel(_width, _height);
        _inventoryPresenter = new InventoryPresenter(_inventoryModel, _inventoryView, _draggingItem, GameManager.Instance.EquipmentManager);

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
        if (_isActive) _btnImg.sprite = _open;
        else _btnImg.sprite = _close;
    }

    /// <summary>
    /// 상점 입장
    /// </summary>
    public void EnterStore(Transform shopTr)
    {
        _isShop = true;
        _inventoryPresenter.SetShopMode(_isShop);
        _playerStatusRect.SetParent(shopTr, false);
        _playerStatusRect.anchoredPosition = new Vector2(0, -380); // 위치 잡기
        _group.alpha = 1f;
        _group.interactable = true;
        _group.blocksRaycasts = true;
        _btnImg.sprite = _close;
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
    }
}
