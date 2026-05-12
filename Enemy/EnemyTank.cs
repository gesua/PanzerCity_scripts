using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

/// <summary>
/// 적 성격
/// </summary>
public enum EnemyPersonality
{
    Stationary, // 고정형:그 자리에서 공격
    //Intercept,  // 요격형:포탄 요격 우선
    Ignore,     // 무시형:배회처럼 움직임
    Aggressive, // 공격형:플레이어에게 다가감
    Coward,     // 도주형:플레이어에게서 멀어짐
}

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
    [Header("----- 컴포넌트(EnemyTank) -----")]
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Rigidbody _rigid;
    [SerializeField] EnemyTankDestructionEffect _destructionEffect; // 파괴 연출
    [SerializeField] BoxCollider _collider; // 파괴될 때 콜라이더 비활성화 용도
    [SerializeField] GameObject _silhouetteModel; // 조준시 보일 실루엣
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _roamSpan = 3f; // 최대 배회 간격
    [SerializeField] float _deadDuration = 5f; // 사망 상태 지속 시간
    [SerializeField] EnemyPersonality _personality; // AI 성격
    [Header("----- 감지 관련 -----")]
    float _detectionRange = 1000f;                  // 감지 거리(걍 최대치로 할거임)
    [SerializeField] float _detectionAngle = 30f;   // 감지 각도(부채꼴 반각)
    [SerializeField] LayerMask _playerLayer = 1 << 8; // 감지할 레이어(플레이어)
    [SerializeField] LayerMask _visionObstacleLayer = 1 << 6; // 시야 차단 레이어(맵)
    [SerializeField] Transform _turret; // 포탑
    [Header("----- 이동 관련 -----")]
    [SerializeField] float _movementCheckDistance = 0.2f; // 장애물 체크 거리
    [SerializeField] Vector3 _raycastOffset = new Vector3(0f, 0f, 1.2f); // 본인 콜라이더보다 앞쪽에서 Ray쏘기
    [SerializeField] float _raycastSideOffset; // 좌우 사이드 한번 더 체크(0.6, 0.75)
    [SerializeField] LayerMask _movementObstacleLayer = 1 << 6 | 1 << 7 | 1 << 8 | 1 << 9;  // 이동 차단 레이어(맵, 외곽벽, 플레이어, 적)
    [SerializeField] LayerMask _agentObstacleLayer = 1 << 8 | 1 << 9; // 네브메시에이전트끼리 미는거 방지 레이어

    Transform _target; // 플레이어
    Vector3 _lookDir; // 이동할 방향
    bool _isRot; // 회전해야 하는지 체크
    float _stoppingDistance = 2.5f;  // 플레이어와 겹쳐져서 미세조절중

    // 시야에서 사라져도 일정시간 타겟 유지
    float _lostTargetTimer;
    float _lostTargetDuration = 3f; // 시야에서 벗어난 후 타겟 유지 시간

    bool __isAIActive; // 상태머신 활성화 여부
    bool _isFirstFlee; // 첫 도주 체크용

    public EnemyPersonality Personality => _personality;

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

    public Transform Target => _target;

    protected override void Awake()
    {
        base.Awake();

        // _agent 설정
        _agent.enabled = false;
        _agent.speed = 0;
        _agent.stoppingDistance = _stoppingDistance;

        // 전진/회전 못하게 막아놓기
        _agent.updatePosition = false; // 전진X
        _agent.updateRotation = false; // 회전X

        _model.OnDead += HandleDead; // 사망 이벤트 구독
    }

    public void Initialize()
    {
        _model.Initialize(); // 기본값들 초기화
        _collider.enabled = true;

        // 성격 랜덤 설정
        _personality = (EnemyPersonality)UnityEngine.Random.Range(0, Enum.GetValues(typeof(EnemyPersonality)).Length);

        // HACK:성격 테스트
        //_personality = EnemyPersonality.Stationary;

        // 상태 객체들 생성
        // 방치 상태
        _states[(int)EnemyStateType.Idle] = new IdleState(this, _roamSpan, _model.MinAttackTime, _model.MaxAttackTime);
        // 교전 상태
        _states[(int)EnemyStateType.Combat] = new CombatState(this, _model.MinAttackTime, _model.MaxAttackTime);
        // 사망 상태
        _states[(int)EnemyStateType.Dead] = new DeadState(this, _deadDuration);
    }

    /// <summary>
    /// AI 상태머신 시작
    /// </summary>
    public void StartAI()
    {
        // 현재 상태 설정
        _currentState = _states[(int)EnemyStateType.Idle];
        _currentState.Enter();

        __isAIActive = true;
    }

    /// <summary>
    /// 렌더러 표시 설정
    /// </summary>
    public void SetRenderersVisible(bool visible)
    {
        _destructionEffect.SetModelVisible(visible);
    }

    private void FixedUpdate()
    {
        if (__isAIActive == false) return;

        // 현재 상태 갱신
        _currentState.Update();
        UpdateLostTarget();
    }

    /// <summary>
    /// 상태 변경
    /// </summary>
    /// <param name="stateType">변경할 상태</param>
    public void ChangeState(EnemyStateType stateType)
    {
        // 현재 상태가 새로 바꾸려는 상태와 동일하면 종료
        if (_currentState.StateType == stateType) return;

        // 현재 상태가 사망 상태면 종료
        if (_currentState.StateType == EnemyStateType.Dead) return;

        // 존재하지 않는 상태로 바꾸려는 경우 종료
        int stateIndex = (int)stateType;
        if (stateIndex < 0 || stateIndex >= _states.Length) return;

        // 기존 상태 종료
        _currentState.Exit();

        // 새 상태 적용
        _currentState = _states[stateIndex];

        // 새 상태 실행
        _currentState.Enter();
    }

    /// <summary>
    /// 랜덤 방향 설정
    /// </summary>
    public void RandomDir()
    {
        Direction dir = (Direction)UnityEngine.Random.Range(0, 4);

        _lookDir = dir switch
        {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.forward
        };

        _isRot = true; // 방향 맞춰야함
    }

    /// <summary>
    /// 배회
    /// </summary>
    public void Roam()
    {
        // 0벡터 체크
        if (_lookDir.sqrMagnitude < Mathf.Epsilon) return;

        // 방향 맞춰야 할 상황
        if (_isRot)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDir);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);

            // 목표 방향으로 회전
            if (angle > Util.Epsilon)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _model.RotSpeed * Time.fixedDeltaTime);
            }
            else
            {
                transform.rotation = targetRotation;
                _isRot = false; // 회전 끝
            }
        }
        else // 전진
        {
            // 전진을 막는 장애물이 있으면 전진 안 함
            if (IsBlocked())
            {
                SetEngineEffect(false); // 장애물에 막히면 엔진 끔
                return;
            }

            Vector3 move = transform.forward * _model.ForwardSpeed * Time.fixedDeltaTime;
            _rigid.MovePosition(_rigid.position + move);
        }

        // 엔진 켬
        SetEngineEffect(true);
    }


    /// <summary>
    /// 가로막는게 있는지 체크
    /// </summary>
    /// <param name="checkAgentsOnly">탱크만 체크되게 할건지(맵 제외)</param>
    /// <returns></returns>
    public bool IsBlocked(bool checkAgentsOnly = false)
    {
        LayerMask mask = checkAgentsOnly ? _agentObstacleLayer : _movementObstacleLayer;

        Vector3 startCenter = _turret.position + transform.TransformDirection(_raycastOffset);
        Vector3 startLeft = startCenter - transform.right * _raycastSideOffset;
        Vector3 startRight = startCenter + transform.right * _raycastSideOffset;
        Vector3 startMidLeft = startCenter - transform.right * (_raycastSideOffset * 0.5f);
        Vector3 startMidRight = startCenter + transform.right * (_raycastSideOffset * 0.5f);

        return Physics.Raycast(startCenter, transform.forward, _movementCheckDistance, mask, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(startLeft, transform.forward, _movementCheckDistance, mask, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(startRight, transform.forward, _movementCheckDistance, mask, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(startMidLeft, transform.forward, _movementCheckDistance, mask, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(startMidRight, transform.forward, _movementCheckDistance, mask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 플레이어 감지
    /// </summary>
    public bool CanSeePlayer()
    {
        // 원형 범위 체크
        Collider[] colliders = Physics.OverlapSphere(_turret.position, _detectionRange, _playerLayer);
        if (colliders.Length == 0)
        {
            return false;
        }

        // 감지된 콜라이더들 중에서 플레이어 감지
        // TODO:멀티 기능 추가시 가장 가까운 플레이어 감지하도록 고치기)
        foreach (Collider col in colliders)
        {
            // 포탄이면 무시
            if (col.TryGetComponent(out Shell shell)) continue;

            // TODO:4군데 모서리로 한다면 이렇게 가져오면 안됨
            if (col.TryGetComponent(out Turret turret) == false) continue;
            Vector3 playerPos = turret.TurretTr.position;

            // 부채꼴 체크 (포탑 전방 기준)
            Vector3 dirToPlayer = (playerPos - _turret.position).normalized;
            float angle = Vector3.Angle(_turret.forward, dirToPlayer);
            if (angle > _detectionAngle) continue;

            // 시야 차단 체크 (벽 등에 가려져 있으면 감지 안 됨)
            float distance = Vector3.Distance(_turret.position, playerPos);
            if (Physics.Raycast(_turret.position, dirToPlayer, distance, _visionObstacleLayer)) continue;

            _lostTargetTimer = 0f; // 타이머 초기화
            _target = col.transform; // 타겟 설정
            return true;
        }

        return false;
    }

    /// <summary>
    /// 포탑을 타겟 방향으로 회전
    /// </summary>
    public void AimAtTarget()
    {
        Quaternion targetRotation;

        if (_target == null) // 타겟 없으면 정면 보기
        {
            targetRotation = Quaternion.identity;
            if (Quaternion.Angle(_turret.localRotation, targetRotation) > Util.Epsilon)
            {
                _turret.localRotation = Quaternion.RotateTowards(_turret.localRotation, targetRotation, _model.TurretRotSpeed * Time.deltaTime);
            }
            else
            {
                _turret.localRotation = targetRotation;
            }
        }
        else // 타겟 보기
        {
            Vector3 dir = (_target.position - _turret.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < Mathf.Epsilon) return;
            targetRotation = Quaternion.LookRotation(dir);

            if (Quaternion.Angle(_turret.rotation, targetRotation) > Util.Epsilon)
            {
                _turret.rotation = Quaternion.RotateTowards(_turret.rotation, targetRotation, _model.TurretRotSpeed * Time.deltaTime);
            }
            else
            {
                _turret.rotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// 타겟 설정
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
    }

    /// <summary>
    /// 일정시간 뒤 타겟 비우기
    /// </summary>
    void UpdateLostTarget()
    {
        if (_target == null) return;

        _lostTargetTimer += Time.deltaTime;
        if (_lostTargetTimer >= _lostTargetDuration)
        {
            _lostTargetTimer = 0f;
            ClearTarget();
        }
    }

    /// <summary>
    /// 타겟 비우기
    /// </summary>
    public void ClearTarget()
    {
        if (_target != null) _target = null;
    }

    /// <summary>
    /// 차체를 타겟 방향으로 회전
    /// </summary>
    public void RotateBodyToTarget()
    {
        if (_target == null) return;
        Vector3 dir = (_target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < Mathf.Epsilon) return;
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _model.RotSpeed * Time.deltaTime);
    }

    /// <summary>
    /// NavMeshAgent 활성화
    /// </summary>
    public void EnableAgent(bool enable)
    {
        _agent.enabled = enable;
    }

    /// <summary>
    /// 타겟에게 다가감
    /// </summary>
    public void MoveToTarget()
    {
        if (_target == null) return;
        _agent.nextPosition = transform.position; // 네브메시 위치 동기화
        _agent.SetDestination(_target.position);
    }

    /// <summary>
    /// 첫도주 리셋
    /// </summary>
    public void ResetFlee()
    {
        _isFirstFlee = true;
    }

    /// <summary>
    /// 타겟에게서 멀어짐
    /// </summary>
    public void FleeFromTarget()
    {
        if (_target == null) return;

        _agent.nextPosition = transform.position; // 네브메시 위치 동기화

        // 목적지에 도달했을 때만 새로 계산
        if (_isFirstFlee == false && _agent.remainingDistance > _stoppingDistance) return;
        _isFirstFlee = false;

        Vector3 bestPoint = transform.position;
        float bestDistance = 0f;
        float fleeDistance = 10f;

        // 플레이어 반대 방향 기준으로 5방향 체크 (-90, -45, 0, 45, 90)
        Vector3 awayDir = (transform.position - _target.position).normalized;
        awayDir.y = 0f;

        int[] angles = { -90, -45, 0, 45, 90 };
        foreach (int angle in angles)
        {
            // 도주 지점 계산
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * awayDir;
            Vector3 candidatePoint = transform.position + dir * fleeDistance;

            // NavMesh 위의 유효한 지점인지 체크
            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, 3f, NavMesh.AllAreas) == false) continue;

            // 플레이어와 거리가 가장 먼 지점 선택
            float distToPlayer = Vector3.Distance(hit.position, _target.position);
            if (distToPlayer > bestDistance)
            {
                bestDistance = distToPlayer;
                bestPoint = hit.position;
            }
        }

        _agent.SetDestination(bestPoint);
    }

    /// <summary>
    /// NavMesh 방향으로 회전 후 전진
    /// </summary>
    public void AgentMove()
    {
        if (_agent.pathPending) return;

        // 다음 웨이포인트 방향
        Vector3 nextPoint = _agent.steeringTarget;
        Vector3 dir = (nextPoint - transform.position).normalized;
        dir.y = 0f;

        // 목표 방향으로 회전
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _model.RotSpeed * Time.fixedDeltaTime);

        // 방향이 맞으면 전진
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle < 10f)
        {
            Vector3 move = transform.forward * _model.ForwardSpeed * Time.fixedDeltaTime;
            _rigid.MovePosition(_rigid.position + move);
        }
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    protected virtual void HandleDead()
    {
        // 엔진 끄기
        SetEngineEffect(false);

        // 폭발 이펙트 재생
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.SmallExplosion, _turret.position); // transform 위치로 하면 바닥에서 폭발함

        // 바닥에 잔불 이펙트 생성
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.TinyFlames, transform.position);

        // 사망 상태로 변경
        _collider.enabled = false; // 콜라이더 비활성화
        ChangeState(EnemyStateType.Dead);

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

        // AI 비활성화
        __isAIActive = false;

        // 자신 게임오브젝트 제거
        gameObject.DestroyOrReturnToPool();
    }

    /// <summary>
    /// 실루엣 모델 켜기(가려진 부분만 보임)
    /// </summary>
    public void SetSilhouette(bool enable)
    {
        _silhouetteModel.SetActive(enable);
    }

    void OnDrawGizmos()
    {
        //* 이동 체크 시각화
        if (_turret == null) return;

        Gizmos.color = Color.green;
        Vector3 startCenter = _turret.position + transform.TransformDirection(_raycastOffset);
        Vector3 startLeft = startCenter - transform.right * _raycastSideOffset;
        Vector3 startRight = startCenter + transform.right * _raycastSideOffset;
        Vector3 startMidLeft = startCenter - transform.right * (_raycastSideOffset * 0.5f);
        Vector3 startMidRight = startCenter + transform.right * (_raycastSideOffset * 0.5f);
        Vector3 dir = transform.forward;

        DrawRaycastGizmo(startCenter, dir);
        DrawRaycastGizmo(startLeft, dir);
        DrawRaycastGizmo(startRight, dir);
        DrawRaycastGizmo(startMidLeft, dir);
        DrawRaycastGizmo(startMidRight, dir);

        void DrawRaycastGizmo(Vector3 origin, Vector3 dir)
        {
            Gizmos.color = Color.green;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, _movementCheckDistance, _movementObstacleLayer, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.05f);
            }
            else
            {
                Gizmos.DrawLine(origin, origin + dir * _movementCheckDistance);
            }
        }
        // 이동 체크 시각화 */

        /* 감지범위 시각화
        if (_turret == null) return;

        // 부채꼴 (포탑 전방 기준)
        Vector3 origin = _turret.position;
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
            Vector3 detectDir = Quaternion.Euler(0f, -_detectionAngle + angleStep * i, 0f) * forward;
            Vector3 nextPoint = origin + detectDir * _detectionRange;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        // 감지범위 시각화 */
    }
}
