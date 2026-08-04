using System;
using UnityEngine;

/// <summary>
/// 플레이어 움직임 담당
/// 목표 속도를 정해놓고, 가속도를 서서히 올리는 방식
/// *드리프트를 구현하고 싶은데 AddTorque가 이상하게 움직여서 AddForce를 사용하지 않음
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Mover : MonoBehaviour
{
    // 기본 스탯
    float _forwardSpeed;    // 전진 속력
    float _backwardSpeed;   // 후진 속력
    float _rotSpeed;        // 회전 속력
    float _acceleration;    // 가속도(속도 증가율)
    float _deceleration;    // 감속도(키를 놓았을 때 천천히 멈추는 속도)
    float _speedMultiplier = 1f; // 진흙 등 지형 효과로 인한 속도 배율

    public event Action<Vector3> OnMoved;

    // 물리 연산 관련
    Rigidbody _rigid;
    Vector3 _velocity;
    private Vector3 _prevPosition; // 이전 위치(충돌 판정)
    float _currentSpeed;   // 로컬 전진 방향에 대한 현재 속도(음수면 후진)
    float _targetSpeed;    // 목표 속도
    float _dirX;           // 좌우 방향

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
        _prevPosition = _rigid.position;
    }

    public void Initialize(TankModel tankModel)
    {
        _forwardSpeed = tankModel.ForwardSpeed;
        _backwardSpeed = tankModel.BackwardSpeed;
        _rotSpeed = tankModel.RotSpeed;
        _acceleration = tankModel.Acceleration;
        _deceleration = tankModel.Deceleration;
    }

    public void Move(Vector3 dir)
    {
        if (dir.magnitude < Util.Epsilon)
        {
            // 입력이 완전히 없으면 회전 입력도 없다고 보고 목표 속도와 회전 입력을 제거
            _targetSpeed = 0f;
            _dirX = 0f;
            return;
        }

        // 회전 입력 처리 (x는 회전)
        if (dir.z >= 0)
        {
            // 후진 중에는 정방향 회전 안 되게
            Vector3 localVelocity = transform.InverseTransformDirection(_rigid.linearVelocity);
            if (localVelocity.z > -Util.Epsilon) _dirX = dir.x;
            else _dirX = 0f;
        }
        else // 후진할 땐 좌우 반전
        {
            _dirX = -dir.x;
        }
        dir.x = 0;

        // z 축(전/후진) 입력이 거의 0이면 목표 속도를 0으로 설정하되
        // 회전은 유지한다. (감속 중에도 transform.forward 방향으로 속도가 정렬되도록 스칼라 속도 사용)
        if (Mathf.Abs(dir.z) < Util.Epsilon)
        {
            _targetSpeed = 0f;
            return;
        }

        if (dir.z > Util.Epsilon) // 전진
        {
            _targetSpeed = _forwardSpeed;
        }
        else if (dir.z < -Util.Epsilon) // 후진
        {
            _targetSpeed = -_backwardSpeed;
        }
    }

    private void FixedUpdate()
    {
        // Kinematic이면(멀티에서 남의 탱크) 물리 연산 자체가 의미 없으므로 스킵
        if (_rigid.isKinematic) return;

        // 충돌 체크
        CheckMovementBlocked();

        // 현재 수직(중력) 속도는 유지
        float currentY = _rigid.linearVelocity.y;

        // 진흙 효과로 인한 배율 적용된 목표 속도
        float scaledSpeed = _targetSpeed * _speedMultiplier;

        // 가속/감속을 상황에 따라 다르게 적용 (절대값 기준)
        float rate = (Mathf.Abs(scaledSpeed) > Mathf.Abs(_currentSpeed)) ? _acceleration : _deceleration;

        // 스칼라 속도를 부드럽게 보간(천천히 멈추기 위해 MoveTowards 사용)
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, scaledSpeed, rate * Time.fixedDeltaTime);

        // 로컬 forward 방향으로 속도 설정 -> 회전 중일 때 transform.forward가 바뀌면 속도 방향도 따라간다
        _velocity = transform.forward * _currentSpeed;
        
        // y 성분은 물리 기반으로 유지
        _velocity.y = currentY;

        _rigid.linearVelocity = _velocity;

        // 회전
        Quaternion deltaRotation = Quaternion.Euler(Vector3.up * _dirX * _rotSpeed * _speedMultiplier * Time.fixedDeltaTime);
        _rigid.MoveRotation(_rigid.rotation * deltaRotation);

        _dirX = 0f;
        OnMoved?.Invoke(_velocity);
    }

    /// <summary>
    /// 충돌 체크(속력 0으로 만듦)
    /// </summary>
    void CheckMovementBlocked()
    {
        // 실제 이동 거리
        float actualMove = Vector3.Distance(_rigid.position, _prevPosition);

        // 기대 이동 거리
        float expectedMove = Mathf.Abs(_currentSpeed * _speedMultiplier) * Time.fixedDeltaTime;

        // 일정 속도 이상인데 거의 못 움직였으면 벽에 막힌 것으로 판단
        if (expectedMove > 0.1f && actualMove < expectedMove * 0.3f)
        {
            _currentSpeed = 0f;
            _targetSpeed = 0f;
        }

        // 다음 프레임 비교를 위해 위치 저장
        _prevPosition = _rigid.position;
    }

    /// <summary>
    /// 멈춤
    /// </summary>
    public void Stop()
    {
        _targetSpeed = 0f;
    }

    /// <summary>
    /// 속도 배율 설정(진흙)
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }

    /// <summary>
    /// 순간이동
    /// </summary>
    public void Teleport(Vector3 pos, Quaternion rotation)
    {
        _rigid.position = pos;
        _rigid.rotation = rotation;

        // 물리 연산 중일 때만 속도를 0으로 초기화 (isKinematic 경고 방지)
        if (_rigid.isKinematic == false)
        {
            _rigid.linearVelocity = Vector3.zero;
            _rigid.angularVelocity = Vector3.zero;
        }

        _currentSpeed = 0f;
        _targetSpeed = 0f;
    }

    /// <summary>
    /// 중력 설정
    /// </summary>
    public void SetGravity(bool enable)
    {
        _rigid.useGravity = enable;
    }
}