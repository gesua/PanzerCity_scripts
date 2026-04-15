using UnityEngine;

public enum EnemyStateType
{
    Idle,   // 평소
    Trace,  // 추적
    Combat, // 전투
    Dead,   // 사망

    Count   // 상태 종류 개수(카운트용)
}

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

public class IdleState : EnemyState
{
    float _roamSpan; // 최대 배회할 시간
    float _roamTimer; // 배회 시간 잴거
    
    float _minAttackTime; // 최소 공격 시간
    float _maxAttackTime; // 최대 공격 시간
    float _attackInterval; // 공격 간격
    float _attackTimer; // 공격 시간 잴거

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

        // 방향 갱신
        if (_roamTimer > _roamSpan)
        {
            _roamTimer = Random.Range(0, _roamSpan); // 배회 간격 랜덤
            _enemy.RandomDir();
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

        // 감지 관련
        if (_enemy.CanSeePlayer())
        {
            // Debug.Log("플레이어 감지!"); // 디버그용
            //_enemy.ChangeState(EnemyStateType.Combat); // 상태 전환
        }
    }
}