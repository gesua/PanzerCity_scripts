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
    [SerializeField] RectTransform _centerCrosshair; // 화면 조준점(+)
    [SerializeField] RectTransform _turretCrosshair; // 포탑 조준점(O)
    [Header("----- 주포 각도 제한 -----")]
    [SerializeField] float _minAngle = -10f; // 주포 최소 각도 (내림)
    [SerializeField] float _maxAngle = 20f;  // 주포 최대 각도 (올림)

    [SerializeField] LayerMask _aimLayerMask = 1 << 6 | 1 << 9; // 에임용 레이어 마스크(맵, 적) *외곽벽 넣으면 안됨[외곽 투명 됐을 때 외곽 조준해서 이상해짐]

    float _rotSpeed; // 포탑(주포) 회전 속력
    bool _isSniping = false; // 저격 모드 중엔 조준점 위치 달라짐

    public Transform TurretTr => _turret;
    public Vector3 BarrelForward => _barrel.forward; // HACK:조준점 맞추는거 해결중

    /// <summary>
    /// 포탑 속력 세팅
    /// </summary>
    public void SetRotSpeed(float rotSpeed)
    {
        _rotSpeed = rotSpeed;
    }

    /// <summary>
    /// 저격 모드 여부
    /// </summary>
    public void SetSniperMode(bool isSniper)
    {
        _isSniping = isSniper;
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
        // 화면 조준점(+) 정확하게 맞추기
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * (_isSniping ? 0.5f : 0.75f), 0f));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 1000f;

        // 수평 방향만 추출
        Vector3 direction = targetPoint - _turret.position;
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
        // 저격 상태(Shift)가 아닐 땐 화면 위쪽을 조준
        float screenY = _isSniping ? 0.5f : 0.75f;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * screenY, 0f));

        // 화면 조준점(+) 옮김
        _centerCrosshair.anchorMin = new Vector2(0.5f, screenY);
        _centerCrosshair.anchorMax = new Vector2(0.5f, screenY);

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
