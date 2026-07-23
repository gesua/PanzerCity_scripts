using System.Collections.Generic;
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

    // 멀티플레이:Initialize()가 서버에서만 호출되어 _hitLayer가 일반 필드로는 클라이언트에 전달되지 않음
    // OnTriggerEnter 최초 필터에 클라이언트도 이 값이 필요해서 NetworkVariable로 별도 동기화
    NetworkVariable<int> _netHitLayerValue = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 멀티플레이:스폰 시점에 서버가 세팅한 _hitLayer 값을 반영(서버 자신은 Initialize에서 이미 세팅된 값과 동일해서 무해함)
    /// </summary>
    public override void OnNetworkSpawn()
    {
        _hitLayer = _netHitLayerValue.Value;
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
        _netHitLayerValue.Value = model.HitLayer.value; // 멀티플레이:클라이언트 동기화용
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
        if (_isReleased) return; // OnTrigger 여러번 들어오는거 방지
        if (_hitLayer.Contains(other.gameObject.layer) == false) return;

        string tag = other.tag;
        if (tag == "Untagged") return; // 없는 태그 무시

        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        // 탱크/HQ를 맞췄으면 각자 전용 피격음/파괴음이 따로 나므로 포탄 터지는 소리는 생략
        bool hitTankOrHQ = tag == "EnemyHitZone" || tag == "PlayerHitZone" || tag == "HQ";

        if (isMultiplayer == false)
        {
            // 싱글플레이:기존 그대로
            HitData hitData = new HitData(_damage, transform.position, _ownerTank);
            if (other.TryGetComponent(out IDamageable damageable)) damageable.TakeHit(hitData);

            Explode(hitData, hitTankOrHQ);
            Remove();
            return;
        }

        // 멀티플레이:탱크(HitZone 보유) 직격은 판정 주체 클라만 서버에 보고, 그 외(벽/HQ 등)는 아직 멀티 대응 대상이 아니므로 서버만 처리
        if (other.TryGetComponent(out HitZone hitZone))
        {
            if (TryReportDirectHit(hitZone) == false) return; // 판정 주체가 아니면 서버의 결과 전파(NetworkVariable/ClientRpc)를 기다리기만 함

            // 판정 주체 로컬 연출:체감 지연 없이 즉시 재생(서버 브로드캐스트에서는 이 클라이언트가 제외됨)
            PlayExplosionEffect(hitTankOrHQ);

            // Pool 반환 대신 렌더링/물리만 즉시 꺼서 화면에서 사라진 것처럼 보이게 함
            // 실제 Pool 반환은 서버의 Despawn 신호를 받았을 때 자동으로 처리됨
            _isReleased = true; // 중복 트리거 방지(Pool 반환 여부와 무관하게 판정 종료 표시)
            gameObject.SetActive(false);
        }
        else
        {
            if (IsServer == false) return;

            HitData hitData = new HitData(_damage, transform.position, _ownerTank);
            if (other.TryGetComponent(out IDamageable damageable)) damageable.TakeHit(hitData);

            ResolveHitOnServer(hitData, hitTankOrHQ, excludeClientId: null);
        }
    }

    /// <summary>
    /// 멀티플레이:탱크 직격 히트를 판정 주체 클라이언트가 서버에 보고
    /// 적:포탄 소유주(공격자) 클라 판정 / 플레이어:피격 당사자(Owner) 클라 판정
    /// (다른 클라이언트 화면에서도 각자 로컬로 같은 충돌을 감지하지만, 판정 주체가 아니면 false를 반환하고 아무것도 하지 않음)
    /// 부정행위 검증은 하지 않음(현재 개발 단계에서는 불필요로 판단)
    /// </summary>
    /// <returns>이 클라이언트가 판정 주체로 보고를 실행했는지 여부</returns>
    bool TryReportDirectHit(HitZone hitZone)
    {
        if (hitZone.Parent is EnemyTank enemyTank)
        {
            // 이 포탄의 소유주(발사자) 클라이언트만 판정 주체(적 포탄:서버 소유 → 서버만 통과 / 플레이어 포탄:발사자 클라 소유 → 그 클라만 통과)
            if (IsOwner == false) return false;
            if (enemyTank.TryGetComponent(out EnemyNetworkOwner networkOwner) == false) return false;

            ReportHitServerRpc(new NetworkBehaviourReference(networkOwner), transform.position);
            return true;
        }

        if (hitZone.Parent is PlayerTank playerTank)
        {
            if (playerTank.TryGetComponent(out PlayerNetworkOwner networkOwner) == false) return false;
            if (networkOwner.IsOwner == false) return false; // 피격 당사자 본인만 판정 주체

            ReportHitServerRpc(new NetworkBehaviourReference(networkOwner), transform.position);
            return true;
        }

        return false; // 알 수 없는 TankBase 파생 타입(방어적 처리)
    }

    /// <summary>
    /// 판정 주체 클라이언트가 보낸 직격 히트를 서버가 확정 처리
    /// 이 RPC는 항상 HitZone(탱크) 대상에 대해서만 호출되므로 hitTankOrHQ는 true로 고정
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    void ReportHitServerRpc(NetworkBehaviourReference targetRef, Vector3 hitPoint, ServerRpcParams rpcParams = default)
    {
        if (_isReleased) return; // 이미 처리된 히트면 무시(중복 보고 방지)

        if (targetRef.TryGet(out EnemyNetworkOwner enemyOwner))
        {
            enemyOwner.ApplyHit(_damage, hitPoint);
        }
        else if (targetRef.TryGet(out PlayerNetworkOwner playerOwner))
        {
            playerOwner.ApplyHit(_damage, hitPoint);
        }
        else
        {
            return; // 대상을 찾을 수 없음(이미 파괴된 경우 등)
        }

        HitData hitData = new HitData(_damage, hitPoint, _ownerTank);
        ResolveHitOnServer(hitData, hitTankOrHQ: true, rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// 서버 권위:범위 피해 적용 + 이펙트 브로드캐스트 + 포탄 정리(공용 로직)
    /// 탱크 직격(ReportHitServerRpc)과 비탱크 대상(벽/HQ 등, 서버 로컬 판정) 양쪽에서 재사용
    /// excludeClientId를 지정하면 그 클라이언트는 이펙트 브로드캐스트에서 제외(이미 로컬에서 즉시 재생했으므로)
    /// </summary>
    void ResolveHitOnServer(HitData hitData, bool hitTankOrHQ, ulong? excludeClientId)
    {
        ApplyExplosionDamage(transform.position, _explosionRadius, _hitLayer, hitData);

        // 멀티플레이:클라이언트에도 동일한 범위 판정을 재현하도록 신호 전달
        NetworkGameManager.Instance.NotifyExplosionDamage(
            transform.position, _explosionRadius, _hitLayer.value, hitData.Damage, hitData.IsPlayerAttack);

        if (excludeClientId.HasValue) NotifyExplosionEffectClientRpc(hitTankOrHQ, BuildExcludeClientParams(excludeClientId.Value));
        else NotifyExplosionEffectClientRpc(hitTankOrHQ);

        Remove(); // 서버:OnBeforeReturnToPool에서 Despawn(false) 처리
    }

    /// <summary>
    /// 특정 클라이언트 하나를 제외한 나머지 전체를 대상으로 하는 ClientRpcParams 생성
    /// </summary>
    ClientRpcParams BuildExcludeClientParams(ulong excludeClientId)
    {
        List<ulong> targetIds = new List<ulong>();
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == excludeClientId) continue;
            targetIds.Add(clientId);
        }

        return new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = targetIds } };
    }

    /// <summary>
    /// 폭발 계산(싱글플레이 전용 — 멀티플레이는 ResolveHitOnServer가 대신 처리)
    /// </summary>
    private void Explode(HitData hitData, bool hitTankOrHQ)
    {
        PlayExplosionEffect(hitTankOrHQ);
        ApplyExplosionDamage(transform.position, _explosionRadius, _hitLayer, hitData);
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
    /// 멀티플레이:서버가 폭발 이펙트/사운드 재생 신호를 전달
    /// clientRpcParams를 지정하지 않으면 전원에게, 지정하면 그 대상에게만(판정 주체 클라 제외 등)
    /// </summary>
    [ClientRpc]
    void NotifyExplosionEffectClientRpc(bool hitTankOrHQ, ClientRpcParams clientRpcParams = default)
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