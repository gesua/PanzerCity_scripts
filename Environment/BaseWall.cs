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

    // 기지무적 아이템 사용시 밀어내기
    [SerializeField] Transform _hqCenter; // HQ 중심점
    [SerializeField] Collider _pushBounds; // 밀어낼 영역
    [SerializeField] float _pushOffset = 0.5f;
    [SerializeField] LayerMask _tankLayer;

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
        // 벽 안에 있는 탱크 밀어내기
        PushOutTanks();

        // 기존 벽 복구
        foreach (Wall wall in _walls)
        {
            wall.Reset();
            wall.SetCollidersEnabled(false); // 콜라이더 꺼놓음
        }

        _normalWalls.SetActive(false); // 기본 벽 비활성화
        _shieldWalls.SetActive(true); // 흰색 벽 활성화

        yield return new WaitForSeconds(duration - _blinkStartTime);

        // 깜빡이기
        float elapsed = 0f;
        while (elapsed < _blinkStartTime)
        {
            _shieldWalls.SetActive(false);
            _normalWalls.SetActive(true);

            yield return new WaitForSeconds(_blinkInterval);

            _shieldWalls.SetActive(true);
            _normalWalls.SetActive(false);

            yield return new WaitForSeconds(_blinkInterval);
            elapsed += _blinkInterval * 2f;
        }

        // 흰색 벽 비활성화
        _shieldWalls.SetActive(false);
        _normalWalls.SetActive(true);
        foreach (Wall wall in _walls)
        {
            wall.SetCollidersEnabled(true);
        }

        _shieldRoutine = null;
    }

    /// <summary>
    /// 벽 안에 있는 탱크 바깥으로 밀어내기
    /// </summary>
    void PushOutTanks()
    {
        Collider[] colliders = Physics.OverlapBox(_pushBounds.bounds.center, _pushBounds.bounds.extents, Quaternion.identity, _tankLayer);

        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent(out Rigidbody rigid))
            {
                // 중심에서 탱크 방향으로 밀어내기
                Vector3 dir = (col.transform.position - _hqCenter.position).normalized;
                dir.y = 0f;

                // 뒤쪽(-z)으로는 밀지 않음
                if (dir.z < 0f) dir.z = 0f;
                if (dir.sqrMagnitude < Util.Epsilon) dir = Vector3.forward;
                else dir = dir.normalized;

                float dist = Vector3.Distance(col.transform.position, _hqCenter.position);
                float pushStrength = Mathf.Clamp(10f / dist, 0.5f, 3f);

                rigid.position = rigid.position + dir * pushStrength * _pushOffset;
            }
        }
    }
}
