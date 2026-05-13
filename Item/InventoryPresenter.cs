using UnityEngine;

/// <summary>
/// 아이템 클릭, 드래그앤드롭 입력을 받아서 InventoryModel에 전달
/// InventoryModel 변경 사항을 InventoryView에 반영
/// </summary>
public class InventoryPresenter : MonoBehaviour
{
    InventoryModel _model;
    InventoryView _view;

    public InventoryPresenter(InventoryModel model, InventoryView view)
    {
        _model = model;
        _view = view;

        _view.OnItemMoved += HandleItemMoved;
        _view.OnItemClicked += HandleItemClicked;
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(ItemModel item)
    {
        if (!_model.TryGetEmptyPosition(item, out Vector2Int pos)) return false;
        if (!_model.TryAddItem(item, pos)) return false;

        _view.AddItemView(item);
        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public void RemoveItem(ItemModel item)
    {
        _model.RemoveItem(item);
        _view.RemoveItemView(item);
    }

    /// <summary>
    /// 아이템 이동 처리
    /// </summary>
    void HandleItemMoved(ItemModel item, Vector2Int newPos)
    {
        if (_model.TryMoveItem(item, newPos))
        {
            _view.UpdateItemViewPosition(item);
        }
        else
        {
            // 이동 실패 시 원래 위치로 복구
            _view.UpdateItemViewPosition(item);
        }
    }

    /// <summary>
    /// 아이템 클릭 처리
    /// </summary>
    void HandleItemClicked(ItemModel item)
    {
        if (item.Config.ItemType == ItemType.Consumable)
        {
            UseItem(item);
        }
    }

    /// <summary>
    /// 아이템 사용
    /// </summary>
    void UseItem(ItemModel item)
    {
        // HACK:아이템 효과 처리는 나중에 추가
        RemoveItem(item);
    }
}
