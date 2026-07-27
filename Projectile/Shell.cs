using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 탱크 포탄
/// 적이 쏜 포탄에 적이 안 맞게 되어있음
/// 플레이어가 쏜 포탄으로 적 포탄을 없앨 수 있음
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Shell : NetworkBehaviour, IPoolReturnHandler
{
    int _damage;            // 포탄 공격력
    float _speed;           // 포탄 속도
    float _explosionRadius; // 폭발 반경

    float _lifeTime = 10f;  // 포탄 생존 시간
    float _timer;           // 생존시간 잴거

    bool _isReleased = false; // Pool에 2번 반환되지 않게 하기(Enter에 여러번 들어올 때 있음)

    Rigidbody _rigid;
    LayerMask _hitLayer;    // 충돌할 레이어(적이 쏜 포탄은 적을 뚫고 감)
    TankBase _ownerTank;    // 포탄 주인

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 포탄 초기화
    /// </summary>
    /// <param name="ownerLayer">포탄 주인 레이어</param>
    /// <param name="ownerTank">포탄 주인</param>
    public void Initialize(TankModel model, int ownerLayer, TankBase ownerTank)
    {
        _ownerTank = ownerTank;

        _damage = model.ShellDamage;
        _speed = model.ShellSpeed;
        _explosionRadius = model.ExplosionRadius;
        _hitLayer = model.HitLayer;
        gameObject.layer = ownerLayer; // 적 포탄끼리 충돌 안되게
        _rigid.excludeLayers = ~_hitLayer; // rigidbody도 hitlayer만 충돌되게

        _rigid.isKinematic = false;

        _timer = 0; // 생존 시간 타이머 세팅
        _rigid.linearVelocity = transform.forward * _speed;

        _isReleased = false;
    }

    private void Update()
    {
        // 멀티플레이:생존 시간 판정은 서버만 수행(클라이언트는 서버의 Despawn을 통해 자동 정리됨)
        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (isMultiplayer && IsServer == false) return;

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
        // 멀티플레이:충돌 판정은 서버만 수행(클라이언트 복제본은 물리적으로 겹쳐도 판정하지 않음)
        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (isMultiplayer && IsServer == false) return;

        if (_isReleased) return; // OnTrigger 여러번 들어오는거 방지
        if (_hitLayer.Contains(other.gameObject.layer) == false) return;

        string tag = other.tag;
        if (tag == "Untagged") return; // 없는 태그 무시

        // 포탄 정보 추가
        HitData hitData = new HitData(_damage, transform.position, _ownerTank);

        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeHit(hitData);

            // 멀티플레이:클라이언트에도 직격 판정을 재현하도록 신호 전달
            if (isMultiplayer)
            {
                // 피격 콜라이더(HitZone)에서 NetworkObject를 탐색
                NetworkObject targetNetworkObject = other.GetComponentInParent<NetworkObject>();
                if (targetNetworkObject != null)
                {
                    NetworkGameManager.Instance.NotifyDirectHitDamage(
                        targetNetworkObject.NetworkObjectId, hitData.Damage, hitData.IsPlayerAttack, transform.position);
                }
            }
        }


        // 탱크/HQ를 맞췄으면 각자 전용 피격음/파괴음이 따로 나므로 포탄 터지는 소리는 생략
        bool hitTankOrHQ = tag == "EnemyHitZone" || tag == "PlayerHitZone" || tag == "HQ";
        Explode(hitData, hitTankOrHQ);

        Remove();
    }

    /// <summary>
    /// 폭발 계산
    /// 멀티플레이에선 서버에서만 호출됨(OnTriggerEnter의 서버 가드로 보장)
    /// </summary>
    private void Explode(HitData hitData, bool hitTankOrHQ)
    {
        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);

        // 이펙트/사운드 재생(멀티면 전원에게 RPC로, 싱글이면 로컬에서 바로)
        if (isMultiplayer) NotifyExplosionEffectClientRpc(hitTankOrHQ);
        else PlayExplosionEffect(hitTankOrHQ);

        ApplyExplosionDamage(transform.position, _explosionRadius, _hitLayer, hitData);

        // 멀티플레이:클라이언트에도 동일한 판정을 재현하도록 신호 전달
        if (isMultiplayer)
        {
            NetworkGameManager.Instance.NotifyExplosionDamage(
                transform.position, _explosionRadius, _hitLayer.value, hitData.Damage, hitData.IsPlayerAttack);
        }
    }

    /// <summary>
    /// 폭발 이펙트/사운드 재생
    /// </summary>
    void PlayExplosionEffect(bool hitTankOrHQ)
    {
        // 이펙트 재생
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.CompleteShellExplosion, transform.position);

        // 포탄 터지는 소리(탱크/HQ를 맞췄으면 각자 전용 피격음/파괴음이 따로 나므로 생략)
        if (hitTankOrHQ == false)
        {
            GameManager.Instance.AudioManager.PlaySfxAtPoint(SfxType.ShellExplosion, transform.position);
        }
    }

    /// <summary>
    /// 멀티플레이:서버가 폭발 이펙트/사운드 재생 신호를 전원에게 전달(호스트 자신도 포함해서 받음)
    /// </summary>
    [ClientRpc]
    void NotifyExplosionEffectClientRpc(bool hitTankOrHQ)
    {
        PlayExplosionEffect(hitTankOrHQ);
    }

    /// <summary>
    /// 범위 피해 적용
    /// 서버(또는 싱글)의 로컬 판정과, 멀티에서 서버 신호를 받은 클라이언트의 재현 양쪽에서 재사용(NetworkGameManager가 호출)
    /// </summary>
    public static void ApplyExplosionDamage(Vector3 position, float radius, LayerMask hitLayer, HitData hitData)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, hitLayer);

        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent(out IExplosionDamageable damageable))
            {
                damageable.TakeHit(hitData, radius, position);
            }
        }
    }

    /// <summary>
    /// 풀에 반환되기 직전 정리
    /// </summary>
    public void OnBeforeReturnToPool()
    {
        _isReleased = true;

        // rigidbody 초기화
        _rigid.linearVelocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;

        // 멀티플레이:서버만 네트워크 디스폰(destroy: false → GameObject는 유지해서 Pool 재사용)
        // ReturnAllPools() 등으로 클라이언트에서 호출될 수도 있으므로 IsServer 가드 필요
        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (isMultiplayer && IsServer && TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Despawn(false);
        }
    }

    /// <summary>
    /// 포탄 없앰
    /// </summary>
    public void Remove()
    {
        gameObject.DestroyOrReturnToPool();
    }
}