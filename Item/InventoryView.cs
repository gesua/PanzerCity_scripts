using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 그리드 UI를 표시하고 드래그앤드롭 입력 처리
/// </summary>
public class InventoryView : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] RectTransform _gridRoot;   // 그리드 루트
    [SerializeField] GameObject _cellPrefab;    // 셀 프리팹
    [SerializeField] GameObject _itemPrefab;    // 아이템 프리팹
    [SerializeField] float _cellSize = 64f;     // 셀 크기

    Dictionary<ItemModel, ItemView> _itemViews = new();

    public System.Action<ItemModel, Vector2Int> OnItemMoved;  // 아이템 이동 이벤트
    public System.Action<ItemModel> OnItemClicked;            // 아이템 클릭 이벤트

    /// <summary>
    /// 아이템 뷰 추가
    /// </summary>
    public void AddItemView(ItemModel item)
    {
        GameObject itemGo = Instantiate(_itemPrefab, _gridRoot);
        ItemView itemView = itemGo.GetComponent<ItemView>();
        itemView.Initialize(item, _cellSize);
        itemView.OnDragEnd += (pos) => OnItemMoved?.Invoke(item, ScreenToGridPos(pos));
        itemView.OnClicked += () => OnItemClicked?.Invoke(item);
        _itemViews[item] = itemView;
        UpdateItemViewPosition(item);
    }

    /// <summary>
    /// 아이템 뷰 제거
    /// </summary>
    public void RemoveItemView(ItemModel item)
    {
        if (_itemViews.TryGetValue(item, out ItemView view))
        {
            Destroy(view.gameObject);
            _itemViews.Remove(item);
        }
    }

    /// <summary>
    /// 아이템 뷰 위치 갱신
    /// </summary>
    public void UpdateItemViewPosition(ItemModel item)
    {
        if (_itemViews.TryGetValue(item, out ItemView view))
        {
            Vector2Int pos = item.GridPosition;
            view.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(pos.x * _cellSize, -pos.y * _cellSize);
        }
    }

    /// <summary>
    /// 스크린 좌표를 그리드 좌표로 변환
    /// </summary>
    Vector2Int ScreenToGridPos(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _gridRoot, screenPos, null, out Vector2 localPos);
        int x = Mathf.FloorToInt(localPos.x / _cellSize);
        int y = Mathf.FloorToInt(-localPos.y / _cellSize);
        return new Vector2Int(x, y);
    }
}
