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
public class Enemy : MonoBehaviour
{
    [Header("----- 타겟 -----")]
    [SerializeField] Transform _target; // 플레이어

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] Rigidbody _rigid;
    [SerializeField] TankModel _model;
    [SerializeField] float _roamSpan = 3f;      // 최대 배회 간격
    [SerializeField] float _attackSpan = 1f;    // 최대 공격 간격
    [SerializeField] float _deadDuration = 5f;  // 사망 상태 지속 시간

    Vector3 lookDir; // 이동할 방향
    bool isRot; // 회전해야 하는지 체크

    /// <summary>
    /// 적 제거 이벤트
    /// </summary>
    public event Action<Enemy> OnRemoved;

    /// <summary>
    /// 적 캐릭터 상태 객체들
    /// </summary>
    EnemyState[] _states = new EnemyState[(int)EnemyStateType.Count];

    /// <summary>
    /// 현재 상태
    /// </summary>
    EnemyState _currentState;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _model.Initialize();

        // 상태 객체들 생성
        // 1) 방치 상태 객체 생성
        _states[(int)EnemyStateType.Idle] = new IdleState(this, _roamSpan);

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

        Debug.Log("왜 안 움직임? "+isRot);

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
}
