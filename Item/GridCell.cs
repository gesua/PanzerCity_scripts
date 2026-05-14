using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 그리드 1칸
/// 놓을 수 있는 위치인지 색 변경, 드롭 인식
/// </summary>
public class GridCell : MonoBehaviour, IDropHandler
{
    [SerializeField] Image _background;

    static readonly Color _normalColor = new Color(1f, 1f, 1f);  // 기본(파랑)
    static readonly Color _validColor = new Color(0f, 1f, 0f);   // 초록
    static readonly Color _invalidColor = new Color(1f, 0f, 0f); // 빨강

    Vector2Int _gridPos;
    public System.Action<ItemView, Vector2Int> OnDropped;

    public Vector2Int GridPos => _gridPos;

    public void Initialize(Vector2Int gridPos)
    {
        _gridPos = gridPos;
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemView itemView = eventData.pointerDrag?.GetComponent<ItemView>();
        if (itemView == null) return;
        Debug.Log("드롭된 셀 : " + transform.position);
        OnDropped?.Invoke(itemView, _gridPos);
    }

    public void SetNormal()
    {
        _background.color = _normalColor;
    }

    public void SetValid()
    {
        Debug.Log("초록색 셀 : " + transform.position);

        _background.color = _validColor;
    }

    public void SetInvalid()
    {
        _background.color = _invalidColor;
    }
}
