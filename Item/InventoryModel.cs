using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 그리드 전체 상태를 관리
/// 어느 칸이 비어있는지, 아이템 추가/제거/이동 로직을 담당
/// </summary>
public class InventoryModel
{
    // 가방 크기
    int _width;  // 가로
    int _height; // 세로

    // 그리드 각 셀에 어떤 아이템이 있는지
    ItemModel[,] _grid;
    // 인벤토리에 있는 아이템 목록
    List<ItemModel> _items = new List<ItemModel>();

    public int Width => _width;
    public IReadOnlyList<ItemModel> Items => _items;

    public InventoryModel(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new ItemModel[width, height];
    }

    /// <summary>
    /// 해당 위치에 아이템을 놓을 수 있는지 체크
    /// </summary>
    public bool CanPlace(ItemModel item, Vector2Int pos)
    {
        Vector2Int[] cells = item.GetOccupiedCells();
        foreach (Vector2Int cell in cells)
        {
            Vector2Int worldCell = pos + cell;

            // 그리드 범위 밖
            if (worldCell.x < 0 || worldCell.x >= _width ||
                worldCell.y < 0 || worldCell.y >= _height)
                return false;

            // 이미 다른 아이템이 있으면
            if (_grid[worldCell.x, worldCell.y] != null &&
                _grid[worldCell.x, worldCell.y] != item)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool TryAddItem(ItemModel item, Vector2Int pos)
    {
        if (!CanPlace(item, pos)) return false;

        item.SetGridPosition(pos);

        foreach (Vector2Int cell in item.GetWorldCells())
        {
            _grid[cell.x, cell.y] = item;
        }

        _items.Add(item);
        return true;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    public void RemoveItem(ItemModel item)
    {
        foreach (Vector2Int cell in item.GetWorldCells())
        {
            _grid[cell.x, cell.y] = null;
        }
        _items.Remove(item);
    }

    /// <summary>
    /// 아이템 이동
    /// </summary>
    public bool TryMoveItem(ItemModel item, Vector2Int newPos)
    {
        // 기존 위치 비우기
        foreach (Vector2Int cell in item.GetWorldCells())
        {
            _grid[cell.x, cell.y] = null;
        }

        // 새 위치에 놓을 수 있는지 체크
        if (!CanPlace(item, newPos))
        {
            // 못 놓으면 원래 위치로 복구
            foreach (Vector2Int cell in item.GetWorldCells())
            {
                _grid[cell.x, cell.y] = item;
            }
            return false;
        }

        item.SetGridPosition(newPos);
        foreach (Vector2Int cell in item.GetWorldCells())
        {
            _grid[cell.x, cell.y] = item;
        }
        return true;
    }

    /// <summary>
    /// 특정 셀에 있는 아이템 반환
    /// </summary>
    public ItemModel GetItemAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= _width || pos.y < 0 || pos.y >= _height) return null;
        return _grid[pos.x, pos.y];
    }

    /// <summary>
    /// 아이템을 놓을 수 있는 첫 번째 위치 탐색
    /// </summary>
    public bool TryGetEmptyPosition(ItemModel item, out Vector2Int result)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (CanPlace(item, pos))
                {
                    result = pos;
                    return true;
                }
            }
        }
        result = Vector2Int.zero;
        return false;
    }
}
