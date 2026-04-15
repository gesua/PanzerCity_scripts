using System;
using UnityEngine;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// 배회하면서 주기적으로 공격
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyTank : TankBase
{
    [Header("----- 타겟 -----")]
    [SerializeField] Transform _target; // 플레이어

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _roamSpan = 3f;      // 최대 배회 간격
    [SerializeField] float _deadDuration = 5f;  // 사망 상태 지속 시간
    // 감지 관련
    float _detectionRange = 1000f;                  // 감지 거리(걍 최대치로 할거임)
    [SerializeField] float _detectionAngle = 60f;   // 감지 각도 (부채꼴 반각)
    [SerializeField] LayerMask _playerLayer;        // 감지할 레이어
    [SerializeField] LayerMask _obstacleLayer;      // 시야 차단 레이어
    [SerializeField] Transform _turret;             // 포탑

    Rigidbody _rigid;
    Vector3 lookDir; // 이동할 방향
    bool isRot; // 회전해야 하는지 체크

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
        _model.OnDead += HandleDead; // 사망 이벤트 구독
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _model.Initialize();

        _rigid = GetComponent<Rigidbody>();

        // 상태 객체들 생성
        // 1) 방치 상태 객체 생성
        _states[(int)EnemyStateType.Idle] = new IdleState(this, _roamSpan, _model.MinAttackTime, _model.MaxAttackTime);

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
            Vector3 move = transform.forward * _model.ForwardSpeed * Time.fixedDeltaTime;
            _rigid.MovePosition(_rigid.position + move);
        }
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
            if (col.GetComponent<Shell>() != null) continue;

            Vector3 playerPos = col.transform.position;

            // 부채꼴 체크 (포탑 전방 기준)
            Vector3 dirToPlayer = (playerPos - _turret.position).normalized;
            float angle = Vector3.Angle(_turret.forward, dirToPlayer);
            if (angle > _detectionAngle) return false;

            // 시야 차단 체크 (벽 등에 가려져 있으면 감지 안 됨)
            float distance = Vector3.Distance(_turret.position, playerPos);
            if (Physics.Raycast(_turret.position, dirToPlayer, distance, _obstacleLayer)) return false;

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
        GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.SmallExplosion, transform.position);

        Remove();
    }

    /// <summary>
    /// 제거하는 함수
    /// </summary>
    public void Remove()
    {
        // 자신 제거 이벤트 발행
        OnRemoved?.Invoke(this);

        // 자신 제거 이벤트 전체 구독 해지
        OnRemoved = null;

        // 자신 게임오브젝트 제거
        gameObject.DestroyOrReturnToPool();
    }

    void OnDrawGizmos()
    {
        // 감지 범위 원
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(transform.position, _detectionRange);

        if (_turret == null) return;

        // 부채꼴 (포탑 전방 기준)
        Gizmos.color = Color.red;
        Vector3 forward = _turret.forward;
        Vector3 leftDir = Quaternion.Euler(0f, -_detectionAngle, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f, _detectionAngle, 0f) * forward;

        // Gizmos.DrawRay(_turret.position, forward * _detectionRange);
        // Gizmos.DrawRay(_turret.position, leftDir * _detectionRange);
        // Gizmos.DrawRay(_turret.position, rightDir * _detectionRange);

        // 부채꼴 호 그리기
        int segments = 20;
        float angleStep = (_detectionAngle * 2f) / segments;
        Vector3 prevPoint = _turret.position + leftDir * _detectionRange;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, -_detectionAngle + angleStep * i, 0f) * forward;
            Vector3 nextPoint = _turret.position + dir * _detectionRange;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}
