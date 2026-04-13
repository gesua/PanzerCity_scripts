using UnityEngine;

/// <summary>
/// 벽
/// </summary>
public class Wall : MonoBehaviour, IExplosionDamageable
{
    float _disappearDelay = 5f; // 사라지는 시간
    float _explosionForce = 10f; // 폭발력
    Rigidbody[] _cubeRigids;
    Collider _collider;

    void Awake()
    {
        _cubeRigids = GetComponentsInChildren<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public void TakeDamage(int damage)
    {
        _collider.enabled = false; // 충돌 비활성화

        foreach (Rigidbody rigid in _cubeRigids)
        {
            rigid.isKinematic = false; // 물리 활성화
            // 폭발 방향으로 날리기
            Vector3 dir = (transform.position - transform.parent.position).normalized;
            rigid.AddForce(dir * _explosionForce, ForceMode.Impulse);
        }

        Destroy(gameObject, _disappearDelay); // 사라지는 시간 후 제거
        //Invoke(nameof(Disappear), _disappearDelay);
    }

    void Disappear()
    {
        gameObject.SetActive(false);
    }
}
