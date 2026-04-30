using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Analytics.IAnalytic;

/// <summary>
/// 벽
/// 포탄에 부숴지는 기능을 가지고 있음
/// </summary>
public class Wall : MonoBehaviour, IExplosionDamageable
{
    [SerializeField] NavMeshObstacle _navMeshObstacle; // 네브메쉬 계산용

    float _disappearDelay = 5f; // 사라지는 시간
    Collider _collider;

    // 자식 큐브들
    Rigidbody[] _cubeRigids;
    FragmentCube[] _cubeFadeOuts;

    void Awake()
    {
        _cubeRigids = GetComponentsInChildren<Rigidbody>();
        _cubeFadeOuts = GetComponentsInChildren<FragmentCube>();
        _collider = GetComponent<Collider>();
    }

    public void TakeHit(HitData hitData, float explosionForce, Vector3 pos)
    {
        _collider.enabled = false; // 충돌 비활성화
        _navMeshObstacle.enabled = false;

        foreach (Rigidbody rigid in _cubeRigids)
        {
            rigid.isKinematic = false; // 물리 활성화

            // 폭발 방향으로 날리기
            rigid.AddExplosionForce(explosionForce, pos, 1000f, 0f, ForceMode.Impulse);
        }

        // 큐브들 페이드 아웃
        foreach (FragmentCube fadeOut in _cubeFadeOuts)
        {
            fadeOut.StartFade(_disappearDelay);
        }

        Destroy(gameObject, _disappearDelay); // 일정시간 후 제거
    }
}
