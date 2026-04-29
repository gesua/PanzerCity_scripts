using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 탱크 포탑
/// 카메라가 바라보는 곳으로 포탑을 회전시키고, 주포각을 맞춤
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _turret;
    [SerializeField] Transform _barrel;
    [SerializeField] RectTransform _centerCrosshair;
    [SerializeField] RectTransform _turretCrosshair;
    [SerializeField] CameraTarget _cameraTarget;

    [Header("----- 주포 각도 제한 -----")]
    [SerializeField] float _minAngle = -10f;
    [SerializeField] float _maxAngle = 20f;

    [SerializeField] LayerMask _aimLayerMask = 1 << 6 | 1 << 9;

    float _rotSpeed;
    bool _isSniping;
    bool _aimLocked;
    Coroutine _sniperRoutine;

    public Transform TurretTr => _turret;
    public Transform BarrelTr => _barrel;
    public Vector3 BarrelForward => _barrel.forward;

    public void SetRotSpeed(float rotSpeed)
    {
        _rotSpeed = rotSpeed;
    }

    /// <summary>
    /// 저격 모드 여부
    /// </summary>
    public void SetSniperMode(bool isSniper)
    {
        if (_sniperRoutine != null)
        {
            StopCoroutine(_sniperRoutine);
        }

        _sniperRoutine = StartCoroutine(SetSniperModeRoutine(isSniper));
    }

    IEnumerator SetSniperModeRoutine(bool isSniper)
    {
        // 전환 직전 _centerCrosshair가 바라보던 월드 지점 저장
        Vector3 oldCenterAimPoint = GetCenterAimPoint();

        _isSniping = isSniper;

        float newScreenY = GetCurrentScreenY();
        SetCenterCrosshairScreenY(newScreenY);

        // 카메라 보정 중에는 터렛/주포가 새 Ray를 따라가면 안 됨
        _aimLocked = true;

        // Cinemachine 갱신 타이밍을 고려해서 여러 프레임 보정
        for (int i = 0; i < 2; i++)
        {
            _cameraTarget.AlignWorldPointToScreenPoint(
                oldCenterAimPoint,
                new Vector2(0.5f, newScreenY)
            );

            yield return null;
        }

        _aimLocked = false;
        _sniperRoutine = null;
    }

    private void Update()
    {
        if (_aimLocked)
        {
            TurretCrosshair();
            return;
        }

        RotateTurret();
        RotateBarrel();
        TurretCrosshair();
    }

    void RotateTurret()
    {
        float screenY = GetCurrentScreenY();
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 1000f;
        }

        Vector3 direction = targetPoint - _turret.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(_turret.rotation, targetRotation);

        if (angle > Util.Epsilon)
        {
            _turret.rotation = Quaternion.RotateTowards(
                _turret.rotation,
                targetRotation,
                _rotSpeed * Time.deltaTime
            );
        }
    }

    void RotateBarrel()
    {
        float screenY = GetCurrentScreenY();
        SetCenterCrosshairScreenY(screenY);

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 1000f;
        }

        Vector3 direction = targetPoint - _barrel.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float angle = targetRotation.eulerAngles.x;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        float clampedAngle = Mathf.Clamp(angle, _minAngle, _maxAngle);
        Quaternion target = Quaternion.Euler(clampedAngle, 0f, 0f);

        _barrel.localRotation = Quaternion.RotateTowards(
            _barrel.localRotation,
            target,
            _rotSpeed * Time.deltaTime
        );
    }

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

        _turretCrosshair.position = Camera.main.WorldToScreenPoint(targetPoint);

        // z 보정
        Vector3 pos = _turretCrosshair.position;
        pos.z = 0;
        _turretCrosshair.position = pos;
    }

    public void ResetRotation()
    {
        _turret.localRotation = Quaternion.identity;
        _barrel.localRotation = Quaternion.identity;
    }

    public void SetCrosshairVisible(bool visible)
    {
        _centerCrosshair.gameObject.SetActive(visible);
        _turretCrosshair.gameObject.SetActive(visible);
    }

    Vector3 GetCenterAimPoint()
    {
        float screenY = GetCurrentScreenY();

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * 1000f;
    }

    float GetCurrentScreenY()
    {
        return _isSniping ? 0.5f : 0.75f;
    }

    void SetCenterCrosshairScreenY(float screenY)
    {
        _centerCrosshair.anchorMin = new Vector2(0.5f, screenY);
        _centerCrosshair.anchorMax = new Vector2(0.5f, screenY);
    }
}