using UnityEngine;

/// <summary>
/// 벽
/// </summary>
public class Wall : MonoBehaviour, IExplosionDamageable
{
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

    public void TakeDamage(int damage, float explosionForce, Vector3 pos)
    {
        _collider.enabled = false; // 충돌 비활성화

        foreach (Rigidbody rigid in _cubeRigids)
        {
            rigid.isKinematic = false; // 물리 활성화

            // 폭발 방향으로 날리기
            //Vector3 dir = (transform.position - transform.parent.position).normalized;
            //rigid.AddForce(transform.localPosition * explosionForce * 10f, ForceMode.Impulse);
            //Vector3 expPos = transform.position + Vector3.up * -1f;
            rigid.AddExplosionForce(explosionForce, pos, explosionForce * 100f);
        }

        // 큐브들 페이드 아웃
        foreach (FragmentCube fadeOut in _cubeFadeOuts)
        {
            fadeOut.StartFade(_disappearDelay);
        }

        Destroy(gameObject, _disappearDelay); // 일정시간 후 제거
    }
}
