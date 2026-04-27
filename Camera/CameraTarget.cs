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

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _pitchSense = 0.1f;
    [SerializeField] float _yawSense = 0.1f;
    [SerializeField] float _minPitch = -30f;
    [SerializeField] float _maxPitch = 45f;
    [SerializeField] float _rotDamp = 10f;
    [SerializeField] float _minZoom = 2f;
    [SerializeField] float _zoomSmooth = 10f;

    float _targetDistance;
    float _pitch;
    float _yaw;

    private void Awake()
    {
        _targetDistance = _cinema.CameraDistance;
    }

    private void LateUpdate()
    {
        transform.position = _target.position;

        _cinema.CameraDistance = Mathf.Lerp(
            _cinema.CameraDistance,
            _targetDistance,
            _zoomSmooth * Time.deltaTime
        );
    }

    public void Rotate(Vector2 rotInput)
    {
        rotInput.x = Mathf.Clamp(rotInput.x, -30f, 30f);
        rotInput.y = Mathf.Clamp(rotInput.y, -30f, 30f);

        _pitch -= rotInput.y * _pitchSense;
        _yaw += rotInput.x * _yawSense;

        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotDamp * Time.deltaTime
        );
    }

    public void Zoom(Vector2 scrollInput)
    {
        _targetDistance = Mathf.Max(_minZoom, _targetDistance - scrollInput.y);
    }

    /// <summary>
    /// 특정 월드 지점이 특정 뷰포트 좌표에 오도록 CameraTarget 회전 보정.
    /// 반복 호출을 전제로 한 함수.
    /// </summary>
    public void AlignWorldPointToScreenPoint(Vector3 worldPoint, Vector2 targetViewportPoint)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        Vector3 viewportPoint = cam.WorldToViewportPoint(worldPoint);

        if (viewportPoint.z <= 0f)
        {
            return;
        }

        Vector2 error = new Vector2(
            viewportPoint.x - targetViewportPoint.x,
            viewportPoint.y - targetViewportPoint.y
        );

        float verticalFov = cam.fieldOfView;
        float horizontalFov = verticalFov * cam.aspect;

        // 부호가 반대로 움직이면 이 두 줄의 +/-만 반대로 바꾸면 됨.
        _yaw += error.x * horizontalFov;
        _pitch -= error.y * verticalFov;

        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void ResetRotation()
    {
        _pitch = 0f;
        _yaw = 0f;
        transform.rotation = Quaternion.identity;
    }
}