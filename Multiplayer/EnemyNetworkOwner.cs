using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 적 탱크 네트워크 브릿지 컴포넌트
/// 서버만 AI/이동을 판정하고, 클라이언트는 NetworkTransform으로 위치만 받음
/// </summary>
[RequireComponent(typeof(EnemyTank))]
public class EnemyNetworkOwner : NetworkBehaviour
{
    [SerializeField] EnemyTank _enemyTank;

    [Header("----- 클라이언트 이동 감지(엔진 이펙트용) -----")]
    [SerializeField] float _movementThreshold = 0.05f; // 초당 이동 거리 기준(이 값보다 크면 '움직이는 중'으로 판단)

    Vector3 _lastPosition;
    bool _wasMovingLocally;

    [Header("----- 피격 동기화 -----")]
    // 서버 권위 HP(실제 소스). TankModel._currentHp는 TakeDamage 호출을 통해서만 이 값을 뒤따라감(TankModel 자체는 수정하지 않음)
    NetworkVariable<int> _currentHp = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    TankModel _model;

    public override void OnNetworkSpawn()
    {
        // 서버만 AI/이동 판정 주체
        _enemyTank.SetNetworkControl(IsServer);

        // 멀티플레이 여부 판단 및 발사 신호 전달용 참조 세팅
        _enemyTank.SetNetworkOwner(this);

        // 클라이언트 로컬 이동 감지 초기화(엔진 이펙트용)
        _lastPosition = transform.position;

        // HP 동기화 초기화
        _enemyTank.TryGetComponent(out _model);
        if (IsServer) _currentHp.Value = _model.CurrentHp; // 서버는 TankModel 초기값(Initialize에서 세팅됨)을 그대로 시작값으로 사용

        _currentHp.OnValueChanged += HandleHpValueChanged;

        // 스폰 연출(렌더러 토글 + 이펙트)은 각 클라이언트가 각자 로컬로 재생
        StartCoroutine(SpawnEffectRoutine());
    }

    public override void OnNetworkDespawn()
    {
        _currentHp.OnValueChanged -= HandleHpValueChanged;
    }

    /// <summary>
    /// 서버 권위:이 적에게 데미지 적용
    /// Shell.ReportHitServerRpc(서버 컨텍스트)에서만 호출됨 — 판정 주체 클라가 보고한 히트를 서버가 확정 처리하는 지점
    /// 더 이상 ServerRpc가 아님(Shell 쪽 RPC 하나로 통합, 여긴 순수 데미지 적용 로직만 담당)
    /// </summary>
    public void ApplyHit(int damage, Vector3 hitPoint)
    {
        if (IsServer == false) return; // 방어적 가드(정상 경로로는 서버 컨텍스트에서만 호출됨)
        if (_model.IsAlive == false) return; // 이미 죽은 상태면 무시(중복 히트 등)

        // 서버 자신의 TankModel.TakeDamage를 그대로 호출해서 치트 체크(_noDamage/_infiniteHP)까지 정상 반영
        // 그 결과값을 그대로 NetworkVariable에 실어 클라이언트에 전파(서버 판정이 곧 네트워크 진실)
        HitData hitData = new HitData(damage, hitPoint, isPlayerAttack: true);
        _model.TakeDamage(hitData);

        _currentHp.Value = _model.CurrentHp;
    }

    /// <summary>
    /// HP NetworkVariable 값 변경 콜백
    /// 서버는 ApplyHit 안에서 이미 TakeDamage로 이벤트를 발화했으므로 여기서 또 호출하면 중복 재생됨 → 클라이언트에서만 처리
    /// 클라이언트는 서버가 확정한 델타를 그대로 TakeDamage에 흘려보내 기존 OnHpChanged/OnHit/OnDead 이벤트를 재사용(치트 필드는 로컬에 없다고 가정)
    /// </summary>
    void HandleHpValueChanged(int previousValue, int newValue)
    {
        if (IsServer) return;

        int damage = previousValue - newValue;
        if (damage <= 0) return; // 초기값 세팅 등 감소가 없는 경우는 스킵

        HitData hitData = new HitData(damage, transform.position, isPlayerAttack: true);
        _model.TakeDamage(hitData);
    }

    /// <summary>
    /// 클라이언트는 서버 AI 로직(SetEngineEffect 호출 포함)이 전혀 돌지 않으므로,
    /// NetworkTransform으로 받은 위치 변화를 직접 관찰해서 엔진 이펙트 여부를 스스로 판단
    /// (서버는 이미 자체 AI 로직에서 SetEngineEffect를 호출하므로 스킵)
    /// </summary>
    void Update()
    {
        if (IsServer) return;
        if (Time.deltaTime <= 0f) return; // 일시정지 등으로 deltaTime이 0이면 스킵(0으로 나누기 방지)

        float speed = Vector3.Distance(transform.position, _lastPosition) / Time.deltaTime;
        bool isMoving = (speed > _movementThreshold);

        if (isMoving != _wasMovingLocally)
        {
            _wasMovingLocally = isMoving;
            _enemyTank.SetEngineEffect(isMoving);
        }

        _lastPosition = transform.position;
    }

    /// <summary>
    /// 서버(AI 판정 주체)가 발사했을 때 전원에게 신호 전달 — 각자 로컬로 발사 연출을 재생함
    /// (포탄 오브젝트 자체는 네트워크 동기화 대상이 아니라 각자 로컬 Pool에서 재생됨)
    /// </summary>
    [ClientRpc]
    public void NotifyAttackClientRpc()
    {
        _enemyTank.PlayLocalAttack();
    }

    /// <summary>
    /// 서버가 이 적을 네트워크에서 제거(EnemyTank.Remove()가 호출)
    /// Despawn()이 클라이언트에도 자동 전파되어 정리됨(destroy: true 기본값 — Pool 재사용은 하지 않음)
    /// </summary>
    public void RequestDespawn()
    {
        if (IsServer == false) return; // 방어적 가드(정상 경로로는 클라이언트에서 호출될 일이 없음)

        if (TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Despawn();
        }
    }

    /// <summary>
    /// 스폰 연출: 렌더러 끄기 → 이펙트 재생 → 대기 → 렌더러 켜기
    /// 서버/클라이언트 각자 로컬로 실행(연출은 네트워크 동기화 대상이 아님)
    /// 지속시간은 EnemyTank._spawnEffectTime(프리팹 값)을 그대로 사용 —
    /// EnemySpawner도 멀티에서는 동일한 값을 대기하므로 AI 시작 타이밍과 자연히 일치함
    /// </summary>
    IEnumerator SpawnEffectRoutine()
    {
        _enemyTank.SetRenderersVisible(false);

        GameManager.Instance.EffectManager.SpawnEffect(EffectType.Twinkle, transform.position);

        yield return new WaitForSeconds(_enemyTank.SpawnEffectTime);

        _enemyTank.SetRenderersVisible(true);
    }
}