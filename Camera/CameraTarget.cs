using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Cinemachine이 바라보게 할 카메라 타겟
/// 카메라 각도 및 감도 조절
/// </summary>
public class CameraTarget : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _target;
    [SerializeField] CinemachineThirdPersonFollow _cinema;

    // x축 회전:Pitch
    // y축 회전:Yaw
    // z축 회전:Roll
    [Header("----- 설정 데이터 -----")]
    [Tooltip("x축 회전 감도")]
    [SerializeField] float _pitchSense = 0.1f;
    [Tooltip("y축 회전 감도")]
    [SerializeField] float _yawSense = 0.1f;
    [SerializeField] float _minPitch = -30f;    // x축 회전 최소값
    [SerializeField] float _maxPitch = 45f;     // x축 회전 최대값
    [SerializeField] float _rotDamp = 10f;      // 회전 보간 속도
    [SerializeField] float _minZoom = 2f;       // 최소 줌 거리
    [SerializeField] float _zoomSmooth = 10f;   // 줌 보간 속도

    float _targetDistance; // 목표 거리
    float _pitch;
    float _yaw;

    private void Awake()
    {
        _targetDistance = _cinema.CameraDistance; // 시작 시 현재 거리로 초기화
    }

    private void LateUpdate()
    {
        transform.position = _target.position;

        // 현재 카메라 거리를 목표 거리로 부드럽게 보간
        _cinema.CameraDistance = Mathf.Lerp(_cinema.CameraDistance, _targetDistance, _zoomSmooth * Time.deltaTime);
    }

    /// <summary>
    /// 회전하는 함수
    /// </summary>
    /// <param name="rotInput">회전 입력</param>
    public void Rotate(Vector2 rotInput)
    {
        // 회전 입력값 제한(순간적인 큰 회전으로 인해 반대 방향으로 카메라가 회전하는 것을 방지)
        rotInput.x = Mathf.Clamp(rotInput.x, -30, 30);
        rotInput.y = Mathf.Clamp(rotInput.y, -30, 30);

        // x축 회전값 수정
        _pitch -= rotInput.y * _pitchSense;

        // y축 회전값 수정
        _yaw += rotInput.x * _yawSense;

        // x축 회전값 제한
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        // 목표 쿼터니언 계산
        Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0);

        // 회전 적용
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotDamp * Time.deltaTime);
    }

    /// <summary>
    /// 카메라 확대/축소
    /// </summary>
    /// <param name="scrollInput">마우스 휠 입력</param>
    public void Zoom(Vector2 scrollInput)
    {
        // 목표 거리 갱신
        _targetDistance = Mathf.Max(_minZoom, _targetDistance - scrollInput.y);
    }
}