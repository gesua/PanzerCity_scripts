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
    protected Enemy _enemy;

    /// <summary>
    /// 현재 상태 종류 반환
    /// </summary>
    public abstract EnemyStateType StateType { get; }

    public EnemyState(Enemy enemy)
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
    float _timer; // 시간 잴거

    public override EnemyStateType StateType => EnemyStateType.Idle;
    public IdleState(Enemy enemy, float roamSpan) : base(enemy)
    {
        _roamSpan = roamSpan;
    }

    public override void Enter()
    {
        _timer = Random.Range(0, _roamSpan); // 타이머 랜덤
        _enemy.RandomDir();
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        // 방향 갱신
        if (_timer > _roamSpan)
        {
            _timer = Random.Range(0, _roamSpan); // 타이머 랜덤
            _enemy.RandomDir();
        }

        _enemy.Roam(); // 배회
    }
}