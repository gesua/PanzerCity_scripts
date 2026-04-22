using System;
using UnityEngine;
using static UnityEngine.UI.Image;

/// <summary>
/// 배회할 방향
/// </summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// 적 탱크 AI
/// 상태머신으로 움직임
/// 배회, 감지 기능을 가지고 있음
/// 스폰 후 배회하다 플레이어를 감지하면 성격에 따라 다르게 행동
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyTankDestructionEffect))]
public class EnemyTank : TankBase
{
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _roamSpan = 3f;      // 최대 배회 간격
    [SerializeField] float _deadDuration = 5f;  // 사망 상태 지속 시간
    [Header("----- 감지 관련-----")]
    float _detectionRange = 1000f;                  // 감지 거리(걍 최대치로 할거임)
    [SerializeField] float _detectionAngle = 30f;   // 감지 각도 (부채꼴 반각)
    [SerializeField] LayerMask _playerLayer;        // 감지할 레이어
    [SerializeField] LayerMask _visionObstacleLayer;// 시야 차단 레이어(맵)
    [SerializeField] Transform _turret;             // 포탑
    [Header("----- 이동 관련 -----")]
    [SerializeField] float _movementCheckDistance = 1.2f; // 이동 체크 거리
    [SerializeField] float _raycastSideOffset; // 좌우 사이드 한번 더 체크(0.6, 0.75)
    [SerializeField] LayerMask _movementObstacleLayer;  // 이동 차단 레이어(플레이어, 적, 맵, 외곽벽)



    Transform _target; // 플레이어
    Rigidbody _rigid;
    Vector3 lookDir; // 이동할 방향
    bool isRot; // 회전해야 하는지 체크

    EnemyTankDestructionEffect _destructionEffect; // 파괴 연출
    BoxCollider _collider; // 파괴될 때 콜라이더 비활성화 용도

    /// <summary>
    /// 적 제거 이벤트
    /// </summary>
    public event Action<EnemyTank> OnRemoved;

    /// <summary>
    /// 적 캐릭터 상태 객체들
    /// </summary>
    EnemyState[] _states = new EnemyState[(int)EnemyStateType.Count];

    /// <summary>
    /// 현재 상태
    /// </summary>
    EnemyState _currentState;

    protected override void Awake()
    {
        base.Awake();

        _rigid = GetComponent<Rigidbody>();
        _destructionEffect = GetComponent<EnemyTankDestructionEffect>();
        _collider = GetComponent<BoxCollider>();

        _model.OnDead += HandleDead; // 사망 이벤트 구독

        Initialize();
    }

    public void Initialize()
    {
        _model.Initialize(); // 기본값들 초기화

        _collider.enabled = true;

        // 상태 객체들
        // 방치 상태 객체 생성
        _states[(int)EnemyStateType.Idle] = new IdleState(this, _roamSpan, _model.MinAttackTime, _model.MaxAttackTime);
        // 사망 상태 객체 생성
        _states[(int)EnemyStateType.Dead] = new DeadState(this, _deadDuration);

        // 현재 상태 설정
        _currentState = _states[(int)EnemyStateType.Idle];
        _currentState.Enter();
    }

    private void FixedUpdate()
    {
        // 현재 상태 갱신
        _currentState.Update();
    }

    /// <summary>
    /// 랜덤 방향 설정
    /// </summary>
    public void RandomDir()
    {
        Direction dir = (Direction)UnityEngine.Random.Range(0, 4);

        lookDir = dir switch
        {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.forward
        };

        isRot = true; // 방향 맞춰야함
    }

    /// <summary>
    /// 배회
    /// </summary>
    public void Roam()
    {
        // 0벡터 체크
        if (lookDir.sqrMagnitude < Mathf.Epsilon) return;

        // 방향 맞춰야 할 상황
        if (isRot)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);

            // 목표 방향으로 회전
            if (angle > Util.Epsilon)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _model.RotSpeed * Time.deltaTime);
            }
            else
            {
                isRot = false; // 회전 끝
            }
        }
        else // 전진
        {
            // 전진을 막는 장애물이 있으면 전진 안 함
            if (IsPathBlocked()) return;

            Vector3 move = transform.forward * _model.ForwardSpeed * Time.fixedDeltaTime;
            _rigid.MovePosition(_rigid.position + move);
        }
    }

    /// <summary>
    /// 전진을 막는 장애물이 있는지 체크
    /// </summary>
    bool IsPathBlocked()
    {
        Vector3 startCenter = _turret.position;
        Vector3 startLeft = startCenter - transform.right * _raycastSideOffset;
        Vector3 startRight = startCenter + transform.right * _raycastSideOffset;

        return Physics.Raycast(startCenter, transform.forward, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(startLeft, transform.forward, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(startRight, transform.forward, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 플레이어 감지
    /// </summary>
    public bool CanSeePlayer()
    {
        // 원형 범위 체크
        Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRange, _playerLayer);
        if (colliders.Length == 0) return false;

        // 감지된 콜라이더들 중에서 플레이어 감지 (TODO:멀티 기능 추가시 가장 가까운 플레이어 감지하도록 고치기)
        foreach (Collider col in colliders)
        {
            // 포탄이면 무시
            if (col.TryGetComponent(out Shell shell)) continue;

            // TODO:4군데 모서리로 한다면 이렇게 가져오면 안됨
            if (_target == null)
            {
                if (col.TryGetComponent(out Turret turret) == false) continue;
                _target = turret.TurretTr;
            }
            Vector3 playerPos = _target.position;

            // 부채꼴 체크 (포탑 전방 기준)
            Vector3 dirToPlayer = (playerPos - _turret.position).normalized;
            float angle = Vector3.Angle(_turret.forward, dirToPlayer);
            if (angle > _detectionAngle) continue;

            // 시야 차단 체크 (벽 등에 가려져 있으면 감지 안 됨)
            float distance = Vector3.Distance(_turret.position, playerPos);
            if (Physics.Raycast(_turret.position, dirToPlayer, distance, _visionObstacleLayer)) continue;

            _target = col.transform; // 타겟 설정
            return true;
        }
        return false;
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    protected virtual void HandleDead()
    {
        // 폭발 이펙트 재생
        GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.SmallExplosion, _turret.position); // transform 위치로 하면 바닥에서 폭발함

        // 사망 상태로 변경
        _collider.enabled = false; // 콜라이더 비활성화
        _currentState = _states[(int)EnemyStateType.Dead];
        _currentState.Enter();

        // 사망 효과 재생
        _destructionEffect.Play();
    }

    /// <summary>
    /// 제거하는 함수
    /// </summary>
    public void Remove()
    {
        // 사망 효과 리셋
        _destructionEffect.ResetState();

        // 자신 제거 이벤트 발행
        OnRemoved?.Invoke(this);

        // 자신 제거 이벤트 전체 구독 해지
        OnRemoved = null;

        // 자신 게임오브젝트 제거
        gameObject.DestroyOrReturnToPool();
    }

    void OnDrawGizmos()
    {
        //* 이동 체크 시각화
        if (_turret == null) return;

        Gizmos.color = Color.green;
        Vector3 startCenter = _turret.position;
        Vector3 startLeft = startCenter - transform.right * _raycastSideOffset;
        Vector3 startRight = startCenter + transform.right * _raycastSideOffset;
        Vector3 dir = transform.forward;

        // 중앙 Ray
        Gizmos.color = Color.green;
        if (Physics.Raycast(startCenter, dir, out RaycastHit hitCenter, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startCenter, hitCenter.point);
            Gizmos.DrawSphere(hitCenter.point, 0.05f);
        }
        else
        {
            Gizmos.DrawLine(startCenter, startCenter + dir * _movementCheckDistance);
        }

        // 좌측 Ray
        Gizmos.color = Color.green;
        if (Physics.Raycast(startLeft, dir, out RaycastHit hitLeft, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startLeft, hitLeft.point);
            Gizmos.DrawSphere(hitLeft.point, 0.05f);
        }
        else
        {
            Gizmos.DrawLine(startLeft, startLeft + dir * _movementCheckDistance);
        }

        // 우측 Ray
        Gizmos.color = Color.green;
        if (Physics.Raycast(startRight, dir, out RaycastHit hitRight, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startRight, hitRight.point);
            Gizmos.DrawSphere(hitRight.point, 0.05f);
        }
        else
        {
            Gizmos.DrawLine(startRight, startRight + dir * _movementCheckDistance);
        }
        // 이동 체크 시각화 */

        /* 감지범위 시각화
        if (_turret == null) return;

        // 부채꼴 (포탑 전방 기준)
        Vector3 origin = transform.position;
        Gizmos.color = Color.yellow;
        Vector3 forward = _turret.forward;
        Vector3 leftDir = Quaternion.Euler(0f, -_detectionAngle, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f, _detectionAngle, 0f) * forward;

        Gizmos.DrawRay(origin, forward * _detectionRange);
        Gizmos.DrawRay(origin, leftDir * _detectionRange);
        Gizmos.DrawRay(origin, rightDir * _detectionRange);

        // 부채꼴 호 그리기
        int segments = 20;
        float angleStep = (_detectionAngle * 2f) / segments;
        Vector3 prevPoint = origin + leftDir * _detectionRange;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, -_detectionAngle + angleStep * i, 0f) * forward;
            Vector3 nextPoint = origin + dir * _detectionRange;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        // 감지범위 시각화 */
    }
}
