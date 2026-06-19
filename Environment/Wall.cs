using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 벽
/// 포탄에 부숴지는 기능을 가지고 있음
/// </summary>
public class Wall : MonoBehaviour, IExplosionDamageable
{
    [SerializeField] NavMeshObstacle _navMeshObstacle; // 네브메쉬 계산용
    [SerializeField] Collider _collider; // 본인 콜라이더
    [SerializeField] MeshRenderer _renderer; // 파괴시 사라질 본인 랜더러
    [SerializeField] GameObject _coverQuad; // 뚜껑 쿼드(UV값 쉐이더 수정 귀찮아서 뚜껑 덮음)
    [SerializeField] FragmentCube[] _cubes; // 자식 큐브들

    bool _isBaseWall; // 기지 벽인지
    float _disappearDelay = 5f; // 사라지는 시간

    // 끄는 시간
    bool _isDisable;
    float _timer;

    public event Action OnDestroyed;

    public void TakeHit(HitData hitData, float explosionForce, Vector3 pos)
    {
        if (_isBaseWall && hitData.AtkTank is PlayerTank) return; // 기지 벽은 아군이 직접 못 부수게 함

        _collider.enabled = false; // 충돌 비활성화
        _navMeshObstacle.enabled = false; // 네브메시 장애물 비활성화
        _renderer.enabled = false; // 랜더러 비활성화
        _coverQuad.SetActive(false); // 뚜껑 비활성화

        // 큐브들 상호작용
        foreach (FragmentCube cube in _cubes)
        {
            cube.SetActiveState(true);
            cube.StartFade(_disappearDelay);
            cube.TakeHit(hitData, explosionForce, pos);
        }

        OnDestroyed?.Invoke();

        if (_isBaseWall)
        {
            _isDisable = true;
        }
        else
        {
            Destroy(gameObject, _disappearDelay); // 일정시간 후 제거
        }
    }

    private void Update()
    {
        // 일정 시간 후 끄기
        if (_isDisable)
        {
            _timer += Time.deltaTime;
            if (_timer >= _disappearDelay)
            {
                gameObject.SetActive(false);
                _isDisable = false;
                _timer = 0;
            }
        }
    }

    public void SetAsBaseWall()
    {
        _isBaseWall = true;

        foreach (FragmentCube cube in _cubes)
        {
            cube.SetOriginal();
        }
    }

    /// <summary>
    /// 초기화
    /// </summary>
    public void Reset()
    {
        _isDisable = false;
        _timer = 0;

        _collider.enabled = true;
        _navMeshObstacle.enabled = true;
        _renderer.enabled = true;
        _coverQuad.SetActive(true);

        // 큐브들 초기화
        foreach (FragmentCube cube in _cubes)
        {
            cube.ResetToDefault(transform);
        }
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 콜라이더 설정
    /// </summary>
    public void SetCollidersEnabled(bool enabled)
    {
        _collider.enabled = enabled;
        _navMeshObstacle.enabled = enabled;
    }
}
