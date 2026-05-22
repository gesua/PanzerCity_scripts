using System;
using UnityEngine;

/// <summary>
/// HQ 주변의 벽
/// 파괴될 때 경고 알림 표시
/// </summary>
public class BaseWall : MonoBehaviour
{
    [SerializeField] Wall[] _walls;

    public event Action OnBaseWallDestroyed;

    void OnEnable()
    {
        foreach (Wall wall in _walls)
            wall.OnDestroyed += HandleWallDestroyed;
    }

    void OnDisable()
    {
        foreach (Wall wall in _walls)
            wall.OnDestroyed -= HandleWallDestroyed;
    }

    /// <summary>
    /// 기지 주변 벽 파괴됨
    /// </summary>
    void HandleWallDestroyed()
    {
        OnBaseWallDestroyed?.Invoke();
    }
}
