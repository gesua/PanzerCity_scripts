using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] DraggingItem _draggingItem;
    [SerializeField] InventoryView _inventoryView;
    [SerializeField] int _width = 4;
    [SerializeField] int _height = 5;
    [Header("----- 이미지 관련 -----")]
    [SerializeField] Sprite _open;  // 열린거
    [SerializeField] Sprite _close; // 닫힌거
    [SerializeField] Image _btnImg; // 가방 버튼 이미지

    InventoryModel _inventoryModel;
    InventoryPresenter _inventoryPresenter;
    public InventoryPresenter Presenter => _inventoryPresenter;

    bool _isActive;

    void Awake()
    {
        _inventoryModel = new InventoryModel(_width, _height);
        _inventoryPresenter = new InventoryPresenter(_inventoryModel, _inventoryView, _draggingItem);
    }

    /// <summary>
    /// 창 토글
    /// </summary>
    public void Toggle()
    {
        _isActive = !_isActive;
        gameObject.SetActive(_isActive);

        // 가방 이미지 변경
        if (_isActive) _btnImg.sprite = _open;
        else _btnImg.sprite = _close;
    }
}
