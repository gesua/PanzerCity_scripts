using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// HQ 주변의 벽
/// 파괴될 때 경고 알림 표시
/// </summary>
public class BaseWall : MonoBehaviour
{
    [SerializeField] Wall[] _walls;
    [SerializeField] GameObject _normalWalls; // 기존 벽
    [SerializeField] GameObject _shieldWalls; // 흰색 벽

    // 기지무적 아이템 사용시 깜빡임 관련
    [SerializeField] float _blinkStartTime = 2f;
    [SerializeField] float _blinkInterval = 0.1f;

    Coroutine _shieldRoutine;

    public event Action OnBaseWallDestroyed;

    void Start()
    {
        foreach (Wall wall in _walls)
        {
            wall.OnDestroyed += HandleWallDestroyed;
            wall.SetAsBaseWall();
        }
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

    /// <summary>
    /// 기지 무적 발동
    /// </summary>
    /// <param name="duration">지속시간</param>
    public void ActivateShield(float duration)
    {
        if (_shieldRoutine != null) StopCoroutine(_shieldRoutine);
        _shieldRoutine = StartCoroutine(ShieldRoutine(duration));
    }

    IEnumerator ShieldRoutine(float duration)
    {
        // 기존 벽 복구
        foreach (Wall wall in _walls)
        {
            if (wall.gameObject.activeSelf == false)
            {
                wall.gameObject.SetActive(true); // HACK:이거 없으면 Reset 작동 안 하나?
                wall.Reset();
            }

            wall.gameObject.SetActive(false);
        }

        // 흰색 벽 활성화
        _shieldWalls.SetActive(true);

        yield return new WaitForSeconds(duration);

        // 흰색 벽 비활성화
        _shieldWalls.SetActive(false);
        _shieldRoutine = null;
    }
}
