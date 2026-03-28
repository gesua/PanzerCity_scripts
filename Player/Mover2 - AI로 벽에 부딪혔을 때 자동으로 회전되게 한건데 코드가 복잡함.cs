using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Mover2 : MonoBehaviour
{
    float _forwardSpeed;    // 전진 속력
    float _backwardSpeed;   // 후진 속력
    float _rotSpeed;        // 회전 속력
    float _acceleration;    // 가속도(속도 증가율)
    float _deceleration;    // 감속도(키를 놓았을 때 천천히 멈추는 속도)

    // 자동 회전 튜닝 파라미터
    float _autoRotSpeedMultiplier = 2f;
    //float _contactDetectDot = 0.05f;

    // 헤드온(거의 정면 충돌) 판정과 제동용 파라미터
    float _minSlideMagnitude = 0.12f;     // 이 값보다 작으면 '거의 정면 충돌'로 간주
    float _headOnBrakeMultiplier = 3f;    // 정면 충돌 시 추가 제동 배율
    float _contactHoldTime = 0.12f;

    // 회전 차단 임계값: 회전 시 새 전방 방향이 벽을 향하면 회전을 막음
    float _rotationBlockIntoWallDotThreshold = 0.5f;

    public event Action<Vector3> OnMoved;

    Rigidbody _rigid;
    Vector3 _velocity;
    float _currentSpeed;   // 로컬 전진 방향에 대한 현재 속도(음수면 후진)
    float _targetSpeed;    // 목표 속도(스칼라)
    float _dirX;

    // 충돌 누적(각 FixedUpdate 프레임당 재설정)
    Vector3 _accumContactNormal;
    int _contactCount;

    // 스무딩: 충돌 정보 유지(짧게)해서 프레임간 깜박임/버벅임 방지
    Vector3 _lastContactNormal;
    float _contactTimer;

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

        // 로컬 forward 방향으로 기본 속도 설정 (일단 기본을 적용)
        _velocity = transform.forward * _currentSpeed;
        _velocity.y = currentY;
        _rigid.linearVelocity = _velocity;

        // 충돌 기반 자동 슬라이드/회전 처리 (스무딩 포함)
        bool handledByAutoSteer = false;

        // 결정할 평균 노말을 준비: 현재 프레임의 누적값이 있으면 사용, 없으면 타이머로 보존된 마지막 값을 사용
        Vector3 avgNormal = Vector3.zero;
        if (_contactCount > 0)
        {
            avgNormal = _accumContactNormal / (float)_contactCount;
            _lastContactNormal = avgNormal;
            _contactTimer = _contactHoldTime;
        }
        else if (_contactTimer > 0f)
        {
            _contactTimer -= Time.fixedDeltaTime;
            avgNormal = _lastContactNormal;
        }

        if (Mathf.Abs(_currentSpeed) > Util.Epsilon && avgNormal.sqrMagnitude > 0.0001f)
        {
            // 수직(바닥/천장) 노말은 제외
            if (Mathf.Abs(avgNormal.y) < 0.7f)
            {
                // forward를 표면의 평면으로 투영해서 슬라이드 방향 계산
                Vector3 slideDir = Vector3.ProjectOnPlane(transform.forward, avgNormal);
                float slideMag = slideDir.magnitude;

                if (slideMag < _minSlideMagnitude)
                {
                    // 거의 정면 충돌(헤드온)인 경우:
                    // - 자동으로 90도 회전시키지 않고 강하게 제동하여 멈추도록 함
                    float brakeRate = _deceleration * _headOnBrakeMultiplier;
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, brakeRate * Time.fixedDeltaTime);

                    _velocity = transform.forward * _currentSpeed;
                    _velocity.y = currentY;
                    _rigid.linearVelocity = _velocity;

                    // 자동 회전은 하지 않음. 사용자의 회전 입력은 계속 허용(단, 아래의 회전 차단 검사에서 막힐 수 있음).
                }
                else
                {
                    // 정상적인 슬라이드 처리: 표면에 평행한 방향으로 정규화
                    slideDir = slideDir.normalized;

                    float maxDegrees = _rotSpeed * _autoRotSpeedMultiplier * Time.fixedDeltaTime;
                    Quaternion targetRot = Quaternion.LookRotation(slideDir, Vector3.up);
                    _rigid.MoveRotation(Quaternion.RotateTowards(_rigid.rotation, targetRot, maxDegrees));

                    // 속도도 슬라이드 방향으로 정렬 (수직 성분 유지)
                    _velocity = slideDir * _currentSpeed;
                    _velocity.y = currentY;
                    _rigid.linearVelocity = _velocity;

                    handledByAutoSteer = true;
                }
            }
        }

        if (!handledByAutoSteer)
        {
            // 사용자의 입력 회전을 실제로 적용해도 되는지 검사(벽 쪽으로 회전하려는 경우 차단)
            bool canApplyInputRotation = true;
            if (Mathf.Abs(_dirX) > Util.Epsilon && avgNormal.sqrMagnitude > 0.0001f && Mathf.Abs(avgNormal.y) < 0.7f)
            {
                // 한 프레임에 적용될 회전량으로 예상 전방 벡터 계산
                float intendedYaw = _dirX * _rotSpeed * Time.fixedDeltaTime;
                Vector3 testForward = Quaternion.Euler(0f, intendedYaw, 0f) * transform.forward;

                // testForward가 '벽 쪽'(-avgNormal)으로 너무 향하면 회전 차단
                if (Vector3.Dot(testForward, -avgNormal) > _rotationBlockIntoWallDotThreshold)
                {
                    canApplyInputRotation = false;
                }
            }

            if (canApplyInputRotation)
            {
                Quaternion deltaRotation = Quaternion.Euler(Vector3.up * _dirX * _rotSpeed * Time.fixedDeltaTime);
                _rigid.MoveRotation(_rigid.rotation * deltaRotation);
            }
            else
            {
                // 회전 차단: _dirX는 소모하지 않고 다음 프레임에서 계속 입력을 받을 수 있도록 0으로 만들지 않음.
                // 단순히 물리 회전을 적용하지 않음으로써 덜덜거림(진동) 방지.
            }
        }

        // 다음 물리 프레임을 위해 충돌 누적 초기화 (단, _lastContactNormal은 유지)
        _accumContactNormal = Vector3.zero;
        _contactCount = 0;

        _dirX = 0;
        OnMoved?.Invoke(_velocity);
    }

    // Collider 충돌을 통해 벽 접촉을 감지 — OnCollisionStay에서 노말을 누적
    private void OnCollisionStay(Collision collision)
    {
        ContactPoint[] contacts = collision.contacts;
        for (int i = 0; i < contacts.Length; i++)
        {
            Vector3 normal = contacts[i].normal;
            // 바닥/천장 등 수직 성분이 큰 표면은 무시
            if (Mathf.Abs(normal.y) > 0.7f) continue;

            // 거의 비스듬한 경우에도 처리될 수 있도록 완화된 판정:
            _accumContactNormal += normal;
            _contactCount++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 충돌이 완전히 끊기면 타이머도 초기화
        _accumContactNormal = Vector3.zero;
        _contactCount = 0;
        _lastContactNormal = Vector3.zero;
        _contactTimer = 0f;
    }
}
