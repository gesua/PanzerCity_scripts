using UnityEngine;

/// <summary>
/// 적 상태 종류
/// </summary>
public enum EnemyStateType
{
    Idle,   // 평소
    Combat, // 교전
    Dead,   // 사망

    Count   // 상태 종류 개수(카운트용)
}

/// <summary>
/// 적 AI 상태머신
/// </summary>
public abstract class EnemyState
{
    protected EnemyTank _enemy;

    /// <summary>
    /// 현재 상태 종류 반환
    /// </summary>
    public abstract EnemyStateType StateType { get; }

    public EnemyState(EnemyTank enemy)
    {
        _enemy = enemy;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

/// <summary>
/// 스폰시 방치 상태
/// 배회하고, 일정 간격으로 공격하며, 플레이어 감지 시 교전 상태로 전환
/// </summary>
public class IdleState : EnemyState
{
    float _roamSpan; // 최대 배회할 시간
    float _roamTimer; // 배회 시간 잴거

    float _minAttackTime;  // 최소 공격 시간
    float _maxAttackTime;  // 최대 공격 시간
    float _attackInterval; // 공격 간격
    float _attackTimer;    // 공격 시간 잴거

    float _playerDetectInterval = 0.2f; // 플레이어 체크 간격
    float _playerDetectTimer; // 플레이어 감지 시간 잴거

    public override EnemyStateType StateType => EnemyStateType.Idle;
    public IdleState(EnemyTank enemy, float roamSpan, float minAttackTime, float maxAttackTime) : base(enemy)
    {
        _roamSpan = roamSpan;
        _minAttackTime = minAttackTime;
        _maxAttackTime = maxAttackTime;
    }

    public override void Enter()
    {
        _roamTimer = Random.Range(0, _roamSpan); // 배회 간격 랜덤
        _enemy.RandomDir();

        _attackInterval = Random.Range(_minAttackTime, _maxAttackTime); // 공격 간격 랜덤
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        // 포탑 원위치
        _enemy.AimAtTarget();

        // 배회 관련
        _roamTimer += Time.deltaTime;
        if (_roamTimer > _roamSpan)
        {
            _roamTimer = Random.Range(0, _roamSpan); // 배회 간격 랜덤
            _enemy.RandomDir(); // 방향 갱신
        }
        _enemy.Roam(); // 배회

        // 공격 관련
        _attackTimer += Time.deltaTime;
        if (_attackTimer > _attackInterval)
        {
            _attackTimer = 0f;
            _attackInterval = Random.Range(_minAttackTime, _maxAttackTime); // 공격 간격 랜덤
            _enemy.Attack(); // 공격
        }

        // 플레이어 감지 관련
        _playerDetectTimer += Time.deltaTime;
        if (_playerDetectTimer > _playerDetectInterval)
        {
            _playerDetectTimer = 0f;
            if (_enemy.CanSeePlayer())
            {
                _enemy.ChangeState(EnemyStateType.Combat); // 상태 전환
            }
        }
    }
}

/// <summary>
/// 교전 상태
/// 성격에 따라 다르게 교전
/// </summary>
public class CombatState : EnemyState
{
    float _minAttackTime;  // 최소 공격 시간
    float _maxAttackTime;  // 최대 공격 시간
    float _attackInterval; // 공격 간격
    float _attackTimer;    // 공격 시간 잴거

    float _playerDetectInterval = 0.2f; // 플레이어 체크 간격
    float _playerDetectTimer; // 플레이어 감지 시간 잴거

    // 공격형 성격이 플레이어 위치 갱신하는 시간
    float _pathUpdateTimer;
    float _pathUpdateInterval = 0.5f;

    public override EnemyStateType StateType => EnemyStateType.Combat;

    public CombatState(EnemyTank enemy, float minAttackTime, float maxAttackTime) : base(enemy)
    {
        _minAttackTime = minAttackTime;
        _maxAttackTime = maxAttackTime;
    }

    public override void Enter()
    {
        _attackInterval = Random.Range(_minAttackTime, _maxAttackTime);


        // 네브메시 쓰는 성격은 켜기
        switch (_enemy.Personality)
        {
            case EnemyPersonality.Aggressive:
                _enemy.EnableAgent(true);
                _pathUpdateTimer = _pathUpdateInterval; // 첫 위치 잡기
                break;
            case EnemyPersonality.Coward:
                _enemy.EnableAgent(true);
                _enemy.ResetFlee(); // 첫도주 리셋
                break;
        }
    }

    public override void Exit()
    {
        _enemy.ClearTarget();
        _enemy.EnableAgent(false);
    }

    public override void Update()
    {
        // 플레이어 감지 체크 - 놓치면 Idle로 복귀
        _playerDetectTimer += Time.deltaTime;
        if (_playerDetectTimer > _playerDetectInterval)
        {
            _playerDetectTimer = 0f;
            if (_enemy.CanSeePlayer() == false && _enemy.Target == null)
            {
                _enemy.ChangeState(EnemyStateType.Idle);
                return;
            }
        }

        // 성격에 따라 행동
        switch (_enemy.Personality)
        {
            case EnemyPersonality.Stationary: // 고정형
                UpdateStationary();
                break;
            case EnemyPersonality.Ignore: // 무시형
                UpdateIgnore();
                break;
            case EnemyPersonality.Aggressive: // 공격형
                UpdateAggressive();
                break;
            case EnemyPersonality.Coward: // 도주형
                UpdateCoward();
                break;
        }

        // 공격
        _attackTimer += Time.deltaTime;
        if (_attackTimer > _attackInterval)
        {
            _attackTimer = 0f;
            _attackInterval = Random.Range(_minAttackTime, _maxAttackTime);
            _enemy.Attack();
        }
    }

    /// <summary>
    /// 고정형:그 자리에 멈추고 포탑만 플레이어 방향으로 조준
    /// </summary>
    public void UpdateStationary()
    {
        _enemy.SetEngineEffect(false);
        _enemy.AimAtTarget();
        _enemy.RotateBodyToTarget();
    }

    /// <summary>
    /// 무시형:차체는 배회, 포탑만 플레이어 조준
    /// </summary>
    void UpdateIgnore()
    {
        _enemy.Roam();
        _enemy.AimAtTarget();
    }

    /// <summary>
    /// 공격형:플레이어에게 다가감
    /// </summary>
    void UpdateAggressive()
    {
        _enemy.AimAtTarget();
        if (HandleAgentMovement() == false) return;

        // 위치 계산
        _pathUpdateTimer += Time.deltaTime;
        if (_pathUpdateTimer >= _pathUpdateInterval)
        {
            _pathUpdateTimer = 0f;
            _enemy.MoveToTarget();
        }

        // 이동
        _enemy.AgentMove();
    }

    /// <summary>
    /// 도주형:플레이어에게서 멀어짐
    /// </summary>
    void UpdateCoward()
    {
        _enemy.AimAtTarget(); // 포탑은 플레이어 조준 유지
        if (HandleAgentMovement() == false) return;

        _enemy.FleeFromTarget(); // 차체는 반대 방향으로 도망

        _enemy.AgentMove(); // 이동
    }

    /// <summary>
    /// 에이전트로 이동 가능 여부 체크 및 처리
    /// </summary>
    bool HandleAgentMovement()
    {
        // 탱크가 막고 있는지 체크(맵 제외)
        if (_enemy.IsBlocked(true))
        {
            _enemy.SetEngineEffect(false);
            return false;
        }

        _enemy.SetEngineEffect(true);
        return true;
    }
}


/// <summary>
/// 사망 상태
/// 파괴된 모델로 바꿔주고 일정시간 후 제거
/// </summary>
public class DeadState : EnemyState
{
    float _duration; // 시체 지속시간
    float _timer; // 시간 잴거

    public override EnemyStateType StateType => EnemyStateType.Dead;

    public DeadState(EnemyTank enemy, float duration) : base(enemy)
    {
        _duration = duration;
    }

    public override void Enter()
    {
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        if (_timer > _duration)
        {
            _timer = 0;
            _enemy.Remove();
        }
    }
}