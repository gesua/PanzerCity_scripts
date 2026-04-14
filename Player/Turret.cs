using UnityEngine;

/// <summary>
/// 탱크 포탑
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _turret; // 포탑
    [SerializeField] Transform _barrel; // 주포
    [Header("----- 주포 각도 제한 -----")]
    [SerializeField] float _minAngle = -10f; // 주포 최소 각도 (내림)
    [SerializeField] float _maxAngle = 20f;  // 주포 최대 각도 (올림)

    float _rotSpeed; // 포탑 회전 속력

    public void SetRotSpeed(float rotSpeed)
    {
        _rotSpeed = rotSpeed;
    }

    private void Update()
    {
        RotateTurret();
        RotateBarrel();
    }

    /// <summary>
    /// 포탑 회전
    /// </summary>
    void RotateTurret()
    {
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        // 0벡터 체크
        if (direction.sqrMagnitude < Mathf.Epsilon) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(_turret.transform.rotation, targetRotation);

        if (angle > Util.Epsilon)
        {
            _turret.transform.rotation = Quaternion.RotateTowards(_turret.transform.rotation, targetRotation, _rotSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 주포 회전
    /// </summary>
    void RotateBarrel()
    {
        // 카메라 상하 각도
        float cameraAngle = Camera.main.transform.eulerAngles.x;
        // 카메라 X 각도는 360도 기준이라 -180 ~ 180으로 변환
        if (cameraAngle > 180f) cameraAngle -= 360f;
        float clampedAngle = Mathf.Clamp(cameraAngle, _minAngle, _maxAngle);
        _barrel.localEulerAngles = new Vector3(clampedAngle, 0f, 0f);
    }
}
