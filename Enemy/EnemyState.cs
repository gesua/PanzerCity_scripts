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
    
    float _minAttackTime; // 최소 공격 시간
    float _maxAttackTime; // 최대 공격 시간
    float _attackInterval; // 공격 간격
    float _attackTimer; // 공격 시간 잴거

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
        if(_playerDetectTimer > _playerDetectInterval)
        {
            _playerDetectTimer = 0f;
            if (_enemy.CanSeePlayer())
            {
                // Debug.Log("플레이어 감지!"); // 디버그용
                //_enemy.ChangeState(EnemyStateType.Trace); // 상태 전환
            }
        }
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