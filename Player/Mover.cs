using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Mover : MonoBehaviour
{
    // 기본 스탯
    float _forwardSpeed;    // 전진 속력
    float _backwardSpeed;   // 후진 속력
    float _rotSpeed;        // 회전 속력
    float _acceleration;    // 가속도(속도 증가율)
    float _deceleration;    // 감속도(키를 놓았을 때 천천히 멈추는 속도)

    public event Action<Vector3> OnMoved;

    // 가속 관련
    Rigidbody _rigid;
    Vector3 _velocity;
    float _currentSpeed;   // 로컬 전진 방향에 대한 현재 속도(음수면 후진)
    float _targetSpeed;    // 목표 속도(스칼라)
    float _dirX;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    public void Initialize(float forwardSpeed, float backwardSpeed, float rotSpeed, float acceleration, float deceleration)
    {
        _forwardSpeed = forwardSpeed;
        _backwardSpeed = backwardSpeed;
        _rotSpeed = rotSpeed;
        _acceleration = acceleration;
        _deceleration = deceleration;
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

        // 테스트 중인 회전
        Debug.Log($"Angular Velocity: {Mathf.Abs(_rigid.angularVelocity.y)}");
        if (Mathf.Abs(_rigid.angularVelocity.y) < _testMaxRotSpeed)
        {
            Debug.Log($"뭔 값이길래 맛이 감? : {Vector3.up * _dirX * _testRotSpeed}");
            _rigid.AddTorque(Vector3.up * _dirX * _testRotSpeed, ForceMode.Acceleration);
        }
        _dirX = 0f;

        // z 축(전/후진) 입력이 거의 0이면 목표 속도를 0으로 설정하되
        // 회전은 유지한다. (감속 중에도 transform.forward 방향으로 속도가 정렬되도록 스칼라 속도 사용)
        if (Mathf.Abs(dir.z) < Util.Epsilon)
        {
            _targetSpeed = 0f;
            return;
        }

        if (dir.z > Util.Epsilon) // 전진
        {
            //_targetSpeed = _forwardSpeed;
            _rigid.AddForce(transform.forward * _testForwardSpeed, ForceMode.Acceleration);
        }
        else if (dir.z < -Util.Epsilon) // 후진
        {
            //_targetSpeed = -_backwardSpeed;
            _rigid.AddForce(-transform.forward * _testBackwardSpeed, ForceMode.Acceleration);
        }
    }

    [SerializeField] float _testForwardSpeed = 10f;
    [SerializeField] float _testBackwardSpeed = 5f;
    [SerializeField] float _testRotSpeed = 100f;
    [SerializeField] float _testMaxRotSpeed = 5f;

    private void FixedUpdate()
    {
        /*
        // 현재 수직(중력) 속도는 유지
        float currentY = _rigid.linearVelocity.y;

        // 가속/감속을 상황에 따라 다르게 적용 (절대값 기준)
        float rate = (Mathf.Abs(_targetSpeed) > Mathf.Abs(_currentSpeed)) ? _acceleration : _deceleration;

        // 스칼라 속도를 부드럽게 보간(천천히 멈추기 위해 MoveTowards 사용)
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, rate * Time.fixedDeltaTime);

        // 로컬 forward 방향으로 속도 설정 -> 회전 중일 때 transform.forward가 바뀌면 속도 방향도 따라간다
        _velocity = transform.forward * _currentSpeed;
        
        // y 성분은 물리 기반으로 유지
        _velocity.y = currentY;

        //_rigid.AddTorque 회전값 주는거

        _rigid.linearVelocity = _velocity;

        */

        // 회전
        //Quaternion deltaRotation = Quaternion.Euler(Vector3.up * _dirX * _rotSpeed * Time.fixedDeltaTime);
        //_rigid.MoveRotation(_rigid.rotation * deltaRotation);



        //_dirX = 0f;
        OnMoved?.Invoke(_velocity);
    }
}