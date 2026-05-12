using UnityEngine;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class InventoryUI : MonoBehaviour
{
    bool _isActive;

    /// <summary>
    /// 창 토글
    /// </summary>
    public void Toggle()
    {
        _isActive = !_isActive;
        gameObject.SetActive(_isActive);
    }
}
