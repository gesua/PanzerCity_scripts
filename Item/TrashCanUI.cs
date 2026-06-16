using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 쓰레기통 UI — 상점에서 아이템 드래그 드롭으로 버리기
/// </summary>
public class TrashCanUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] Color _normalColor = Color.white;
    [SerializeField] Color _hoverColor = Color.red;

    InventoryPresenter _presenter;

    public void Initialize(InventoryPresenter presenter)
    {
        _presenter = presenter;
    }

    /// <summary>
    /// 아이템 드롭
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (!eventData.pointerDrag.TryGetComponent(out ItemView itemView)) return;

        _presenter.TrashItem(itemView.Item);
        _icon.color = _normalColor;
    }

    /// <summary>
    /// 드래그 중 아이템이 쓰레기통 위로 올라옴
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return; // 드래그 중일 때만 반응
        _icon.color = _hoverColor;
    }

    /// <summary>
    /// 드래그 중 아이템이 쓰레기통 밖으로 나감
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _icon.color = _normalColor;
    }
}
