using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 드롭존 UI — 스테이지에서 아이템 드래그 드롭으로 바닥에 버리기
/// 상점에서는 GameScene이 비활성화시킴
/// </summary>
public class DropZoneUI : MonoBehaviour, IDropHandler, IPointerEnterHandler
{
    InventoryPresenter _presenter;

    public void Initialize(InventoryPresenter presenter)
    {
        _presenter = presenter;
    }

    public void SetActiveState(bool value)
    {
        gameObject.SetActive(value);
    }

    /// <summary>
    /// 아이템 드롭
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (!eventData.pointerDrag.TryGetComponent(out ItemView itemView)) return;

        _presenter.DropItemToGround(itemView.Item);
    }

    /// <summary>
    /// 드래그 중 아이템이 드롭존 위로 올라옴
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return; // 드래그 중일 때만 반응
    }
}
