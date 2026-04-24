using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 탱크 포탄
/// 적이 쏜 포탄에 적이 안 맞게 되어있음
/// 플레이어가 쏜 포탄으로 적 포탄을 없앨 수 있음
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Shell : MonoBehaviour
{
    int _damage;            // 포탄 공격력
    float _speed;           // 포탄 속도
    float _explosionRadius; // 폭발 반경

    float _lifeTime = 10f;  // 포탄 생존 시간
    float _timer;           // 생존시간 잴거
    
    bool _isReleased = false; // Pool에 2번 반환되지 않게 하기(Enter에 여러번 들어올 때 있음)

    Rigidbody _rigid;
    LayerMask _hitLayer;    // 충돌할 레이어(적이 쏜 포탄은 적을 뚫고 감)

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
    public void Initialize(TankModel model, int ownerLayer)
    {
        _damage = model.ShellDamage;
        _speed = model.ShellSpeed;
        _explosionRadius = model.ExplosionRadius;
        _hitLayer = model.HitLayer;
        gameObject.layer = ownerLayer; // 적 포탄끼리 충돌 안되게
        _rigid.excludeLayers = ~_hitLayer; // rigidbody도 hitlayer만 충돌되게

        _timer = 0; // 생존 시간 타이머 세팅
        _rigid.linearVelocity = transform.forward * _speed;

        _isReleased = false;
    }
    private void Update()
    {
        // 포탄 생존 시간 체크
        if (_timer < _lifeTime)
        {
            _timer += Time.deltaTime;
        }
        else // 포탄 사라지게
        {
            Remove();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
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
        if (_isReleased) return;
        _isReleased = true;

        // rigidbody 초기화
        _rigid.linearVelocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        gameObject.DestroyOrReturnToPool();
    }
}
