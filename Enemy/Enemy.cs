using System.Collections;
using UnityEngine;

/// <summary>
/// 배회시 정면에 주기적으로 공격
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("----- 타겟 -----")]
    [SerializeField] Transform _target; // 플레이어

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _roamSpan = 3f;      // 최대 배회 간격(최소 1초)
    [SerializeField] float _attackSpan = 1f;    // 최대 공격 간격(최소 0초)

    IEnumerator RoamRoutine()
    {
        while (true)
        {

            yield return new WaitForSeconds(Random.Range(1f, _roamSpan));
        }
    }

    /*
    /// <summary>
    /// 배회 동작을 실행하는 함수
    /// </summary>
    public void Roam()
    {
        // 랜덤한 x, y 방향 벡터 생성
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _roamDistance;

        // 현재 위치 기준으로 랜덤하게 이동할 목표 지점 설정
        Vector3 targetPos = transform.position;
        targetPos.x += offset.x;
        targetPos.z += offset.y;

        // 설정된 목표 지점을 NavMeshAgent의 목표 지점으로 적용
        _agent.SetDestination(targetPos);
    }
    */
}
