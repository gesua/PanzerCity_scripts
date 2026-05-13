using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] Sprite _open;  // 열린거
    [SerializeField] Sprite _close; // 닫힌거
    [SerializeField] Image _btnImg; // 가방 버튼 이미지

    bool _isActive;

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
