using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 적 탱크 네트워크 브릿지 컴포넌트
/// 서버만 AI/이동을 판정하고, 클라이언트는 NetworkTransform으로 위치만 받음
/// (PlayerNetworkOwner와 대칭 구조)
/// </summary>
[RequireComponent(typeof(EnemyTank))]
public class EnemyNetworkOwner : NetworkBehaviour
{
    [SerializeField] EnemyTank _enemyTank;

    public override void OnNetworkSpawn()
    {
        // 서버만 AI/이동 판정 주체
        _enemyTank.SetNetworkControl(IsServer);

        // 스폰 연출(렌더러 토글 + 이펙트)은 각 클라이언트가 각자 로컬로 재생
        StartCoroutine(SpawnEffectRoutine());
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
