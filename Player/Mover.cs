using System;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

[RequireComponent(typeof(Rigidbody))]
public class Mover : MonoBehaviour
{
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _forwardSpeed = 5f;  // 전진 속력
    [SerializeField] float _backwardSpeed = 2f; // 후진 속력
    [SerializeField] float _rotSpeed = 100f;    // 회전 속력
    [SerializeField] float _acceleration = 10f; // 가속도(속도 증가율)
    [SerializeField] float _deceleration = 5f;  // 감속도(키를 놓았을 때 천천히 멈추는 속도)

    public event Action<Vector3> OnMoved;
    Rigidbody _rigid;
    Vector3 _velocity;
    float _currentSpeed;   // 로컬 전진 방향에 대한 현재 속도(음수면 후진)
    float _targetSpeed;    // 목표 속도(스칼라)
    float _dirX;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
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
        _dirX = (_rigid.linearVelocity.z < -Util.Epsilon) ? -dir.x : dir.x; // 후진할 땐 좌우 반대
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

        _rigid.linearVelocity = _velocity;

        // 회전
        Quaternion deltaRotation = Quaternion.Euler(Vector3.up * _dirX * _rotSpeed * Time.fixedDeltaTime);
        _rigid.MoveRotation(_rigid.rotation * deltaRotation);

        _dirX = 0;
        OnMoved?.Invoke(_velocity);
    }
}
