using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 벽
/// 포탄에 부숴지는 기능을 가지고 있음
/// </summary>
public class Wall : MonoBehaviour, IExplosionDamageable
{
    [SerializeField] NavMeshObstacle _navMeshObstacle; // 네브메쉬 계산용
    [SerializeField] Collider _collider;

    bool _isBaseWall; // 기지 벽인지
    float _disappearDelay = 5f; // 사라지는 시간

    // 끄는 시간
    bool _isDisable;
    float _timer;

    // 자식 큐브들
    Rigidbody[] _cubeRigids;
    FragmentCube[] _cubes;

    public event Action OnDestroyed;

    void Awake()
    {
        _cubeRigids = GetComponentsInChildren<Rigidbody>();
        _cubes = GetComponentsInChildren<FragmentCube>();
    }

    public void TakeHit(HitData hitData, float explosionForce, Vector3 pos)
    {
        _collider.enabled = false; // 충돌 비활성화
        _navMeshObstacle.enabled = false; // 네브메시 장애물 비활성화

        foreach (Rigidbody rigid in _cubeRigids)
        {
            rigid.isKinematic = false; // 물리 활성화

            // 폭발 방향으로 날리기
            rigid.AddExplosionForce(explosionForce, pos, 1000f, 0f, ForceMode.Impulse);
        }

        // 큐브들 페이드 아웃
        foreach (FragmentCube fadeOut in _cubes)
        {
            fadeOut.StartFade(_disappearDelay);
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
        if(_isDisable)
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
        foreach (Rigidbody rigid in _cubeRigids)
        {
            rigid.isKinematic = true;
        }
        foreach (FragmentCube cube in _cubes)
        {
            cube.Reset();
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
