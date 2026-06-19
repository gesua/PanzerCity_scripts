using UnityEngine;

/// <summary>
/// 실제 인벤토리에 존재하는 아이템 인스턴스
/// ItemConfig를 참조하고, 현재 위치(그리드 좌표), 수량, 내구도
/// </summary>
public class ItemModel
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] ItemConfig _config;

    Vector2Int _gridPosition; // 그리드 내 위치
    bool _isRotated;          // 90도 회전 여부
                              // HACK:ㄱ,ㅗ 같은거 넣으면 0,90,180,270 다 봐야함

    public ItemConfig Config => _config;
    public Vector2Int GridPosition => _gridPosition;
    public bool IsRotated => _isRotated;

    public ItemModel(ItemConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 그리드 위치 설정
    /// </summary>
    public void SetGridPosition(Vector2Int pos)
    {
        _gridPosition = pos;
    }

    /// <summary>
    /// 회전
    /// </summary>
    public void Rotate()
    {
        _isRotated = !_isRotated;
    }

    /// <summary>
    /// 회전 상태 직접 세팅 (스냅샷 복구용)
    /// </summary>
    public void SetRotated(bool isRotated)
    {
        _isRotated = isRotated;
    }

    /// <summary>
    /// 현재 회전 상태 기준으로 차지하는 셀 좌표 반환
    /// </summary>
    public Vector2Int[] GetOccupiedCells()
    {
        Vector2Int[] cells = _config.OccupiedCells;
        if (!_isRotated) return cells;

        // 90도 회전 적용
        Vector2Int[] rotatedCells = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            rotatedCells[i] = new Vector2Int(-cells[i].y, cells[i].x);
        }
        return rotatedCells;
    }

    /// <summary>
    /// 실제 그리드 좌표 반환 (GridPosition 기준)
    /// </summary>
    public Vector2Int[] GetWorldCells()
    {
        Vector2Int[] cells = GetOccupiedCells();
        Vector2Int[] worldCells = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            worldCells[i] = _gridPosition + cells[i];
        }
        return worldCells;
    }
}
