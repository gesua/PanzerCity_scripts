using UnityEngine;

/// <summary>
/// 플레이어 탱크 포탑
/// 카메라가 바라보는 곳으로 포탑을 회전시키고, 주포각을 맞춤
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _turret; // 포탑
    [SerializeField] Transform _barrel; // 주포
    [SerializeField] RectTransform _turretCrosshair; // 포탑 조준점 UI
    [Header("----- 주포 각도 제한 -----")]
    [SerializeField] float _minAngle = -10f; // 주포 최소 각도 (내림)
    [SerializeField] float _maxAngle = 20f;  // 주포 최대 각도 (올림)

    [SerializeField] LayerMask _aimLayerMask; // 에임용 레이어 마스크
    
    float _rotSpeed; // 포탑(주포) 회전 속력
    public Transform TurretTr => _turret; 

    public void SetRotSpeed(float rotSpeed)
    {
        _rotSpeed = rotSpeed;
    }

    private void Update()
    {
        RotateTurret();
        RotateBarrel();
        TurretCrosshair();
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
        // 화면 중앙에서 레이캐스트
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            targetPoint = hit.point; // 맞은 지점
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 1000f; // 아무것도 없으면 먼 지점
        }

        // 주포에서 타겟 지점으로 방향 계산
        Vector3 direction = targetPoint - _barrel.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // 주포 로컬 X 각도만 추출해서 제한
        float angle = targetRotation.eulerAngles.x;
        if (angle > 180f) angle -= 360f;
        float clampedAngle = Mathf.Clamp(angle, _minAngle, _maxAngle);

        Quaternion target = Quaternion.Euler(clampedAngle, 0f, 0f);
        _barrel.localRotation = Quaternion.RotateTowards(_barrel.localRotation, target, _rotSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 포탑 조준점
    /// </summary>
    void TurretCrosshair()
    {
        Ray ray = new Ray(_barrel.position, _barrel.forward);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 1000f;
        }

        // 월드 좌표를 스크린 좌표로 변환
        Vector2 screenPos = Camera.main.WorldToScreenPoint(targetPoint);
        _turretCrosshair.position = screenPos;
    }
}
