using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중 마우스를 따라다니는 아이템 더미
/// </summary>
public class DraggingItemUI : MonoBehaviour
{
    [SerializeField] RectTransform _rectTransform;
    [SerializeField] Image _icon;

    void Awake()
    {
        _icon.enabled = false;
    }

    /// <summary>
    /// 드래그 시작 시 아이콘 설정 및 활성화
    /// </summary>
    public void Show(Sprite icon, Vector2 screenPos, Vector2 size)
    {
        _icon.sprite = icon;
        _rectTransform.sizeDelta = size;
        _icon.enabled = true;
        Follow(screenPos);
    }

    /// <summary>
    /// 마우스 따라다니기
    /// </summary>
    public void Follow(Vector2 screenPos)
    {
        transform.position = screenPos;
    }

    /// <summary>
    /// 드래그 종료 시 비활성화
    /// </summary>
    public void Hide()
    {
        _icon.enabled = false;
    }
}