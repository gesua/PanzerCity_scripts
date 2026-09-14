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



    public override void OnNetworkSpawn()
    {
        // 클라이언트는 Pool에서 재사용된 오브젝트를 받을 수 있어 상태 초기화가 필요함
        // (서버는 EnemySpawner.SpawnRoutine()에서 GetFromPool 직후 이미 Initialize()를 호출하므로 여기서는 제외)
        if (IsServer == false)
        {
            _enemyTank.Initialize();
        }

        // 서버만 AI/이동 판정 주체
        _enemyTank.SetNetworkControl(IsServer);

        // 멀티플레이 여부 판단 및 발사 신호 전달용 참조 세팅
        _enemyTank.SetNetworkOwner(this);

        // 클라이언트 로컬 이동 감지 초기화(엔진 이펙트용)
        _lastPosition = transform.position;

        // 스폰 연출(렌더러 토글 + 이펙트)은 각 클라이언트가 각자 로컬로 재생
        StartCoroutine(SpawnEffectRoutine());
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