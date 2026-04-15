using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Shell : MonoBehaviour
{
    int _damage;            // 포탄 공격력
    float _speed;           // 포탄 속도
    float _explosionRadius; // 폭발 반경
    float _lifeTime = 5f;   // 포탄 생존 시간
    Rigidbody _rigid;
    LayerMask _hitLayer; // 충돌할 레이어(적이 쏜 포탄은 적을 뚫고 감)

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 포탄 초기화
    /// </summary>
    /// <param name="damage">포탄 공격력</param>
    /// <param name="speed">포탄 속도</param>
    /// <param name="hitLayer">충돌할 레이어</param>
    /// <param name="ownerLayer">포탄 주인 레이어</param>
    public void Initialize(int damage, float speed, float explosionRadius, LayerMask hitLayer, int ownerLayer)
    {
        _damage = damage;
        _speed = speed;
        _explosionRadius = explosionRadius;
        _hitLayer = hitLayer;
        gameObject.layer = ownerLayer; // 적 포탄끼리 충돌 안되게
        _rigid.excludeLayers = ~hitLayer; // rigidbody도 hitlayer만 충돌되게

        _rigid.linearVelocity = transform.forward * _speed;
        Invoke(nameof(Remove), _lifeTime); // 생존시간 후 회수
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("충돌: " + other.gameObject.name);

        if (!_hitLayer.Contains(other.gameObject.layer)) return;

        // 충돌한 대상이 탱크면 피해 입히기
        other.gameObject.GetComponent<IDamageable>()?.TakeDamage(_damage);

        Explode();
        Remove();
    }

    /// <summary>
    /// 폭발 계산
    /// </summary>
    private void Explode()
    {
        // 이펙트 재생
        GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.CompleteShellExplosion, transform.position);

        // 범위 피해
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRadius, _hitLayer);
        foreach (Collider col in colliders)
        {
            col.GetComponent<IExplosionDamageable>()?.TakeDamage(_damage, _explosionRadius, transform.position);
        }
    }

    /// <summary>
    /// 포탄 없앰
    /// </summary>
    public void Remove()
    {
        CancelInvoke(nameof(Remove)); // invoke 끄기

        // rigidbody 초기화
        _rigid.linearVelocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        gameObject.DestroyOrReturnToPool();
    }
}
