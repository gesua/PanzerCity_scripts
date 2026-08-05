using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 탱크 포탑
/// 카메라가 바라보는 곳으로 포탑을 회전시키고, 주포각을 맞춤
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _turret;
    [SerializeField] Transform _barrel;
    [SerializeField] Image _centerCrosshairImage;
    [SerializeField] RectTransform _centerCrosshair;
    [SerializeField] RectTransform _turretCrosshair;
    [SerializeField] CameraTarget _cameraTarget;

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _minAngle = -10f;
    [SerializeField] float _maxAngle = 20f;
    [SerializeField] float _minAimDistance = 4f; // 최소 조준거리
    [SerializeField] float _fallbackAimDistance = 10f; // 너무 가까울 때 대신 사용할 거리

    [SerializeField] LayerMask _aimLayerMask = 1 << 6 | 1 << 7 | 1 << 9; // 맵, 외곽벽, 적

    public CameraTarget CameraTarget => _cameraTarget;

    float _rotSpeed;
    bool _isSniping;
    bool _aimLocked;
    bool _isLocalControl = true; // 로컬 소유인지(싱글 플레이:항상 true)
    Coroutine _sniperRoutine;

    // 조준점 색 변경
    Color _frontColor = new Color(1f, 0, 0, 0.3f);          // 빨강
    Color _sideColor = new Color(1f, 0.92f, 0.016f, 0.3f);  // 노랑
    Color _rearColor = new Color(0, 1f, 0, 0.3f);           // 초록
    Color _defaultColor = new Color(1f, 1f, 1f, 0.3f);      // 하양

    EnemyTank _targetTank; // 실루엣 켜는 용도

    const float AimRayDistance = 1000f;
    const int InitialAimHitBufferSize = 32;

    // 카메라 레이는 프레임당 한 번만 쏘고, 충돌 수가 많을 때만 버퍼를 확장한다.
    RaycastHit[] _aimHits = new RaycastHit[InitialAimHitBufferSize];
    Camera _aimCamera;

    public Transform TurretTr => _turret;
    public Transform BarrelTr => _barrel;
    public Vector3 BarrelForward => _barrel.forward;

    public void SetRotSpeed(float rotSpeed)
    {
        _rotSpeed = rotSpeed;
    }

    /// <summary>
    /// 에임 고정
    /// </summary>
    public void SetAimLocked(bool locked)
    {
        _aimLocked = locked;
    }

    /// <summary>
    /// 로컬(내 소유) 제어 여부 설정(멀티플레이 전용, PlayerTank가 호출)
    /// 소유자가 아니면 카메라 기반 조준/조준점 로직을 실행하지 않고, NetworkTransform 복제 값만 표시함
    /// </summary>
    public void SetLocalControl(bool isLocal)
    {
        _isLocalControl = isLocal;
    }

    /// <summary>
    /// 씬 전용 UI 참조 주입(멀티플레이 전용, PlayerTank가 호출)
    /// 프리팹은 씬 안의 오브젝트를 미리 연결할 수 없어서, 로컬 플레이어에게만 런타임에 연결함
    /// </summary>
    public void SetSceneReferences(
        CameraTarget cameraTarget,
        RectTransform centerCrosshair,
        RectTransform turretCrosshair,
        Image centerCrosshairImage)
    {
        _cameraTarget = cameraTarget;
        _centerCrosshair = centerCrosshair;
        _turretCrosshair = turretCrosshair;
        _centerCrosshairImage = centerCrosshairImage;
    }

    /// <summary>
    /// 카메라 댐핑 즉시 해제(멀티플레이:원격 관찰자는 _cameraTarget이 주입되지 않으므로 내부적으로 스킵됨)
    /// </summary>
    public void DisableCameraDamping()
    {
        if (_cameraTarget == null) return;
        _cameraTarget.DisableDamping();
    }

    /// <summary>
    /// 카메라 댐핑 값 복구(멀티플레이:원격 관찰자는 _cameraTarget이 주입되지 않으므로 내부적으로 스킵됨)
    /// </summary>
    public void ResetCameraDamping()
    {
        if (_cameraTarget == null) return;
        _cameraTarget.ResetDamping();
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
        // 로컬 소유가 아니면 카메라 기반 조준 로직 실행 안 함
        if (_isLocalControl == false) return;
        if (_centerCrosshair == null) return;

        Camera aimCamera = GetAimCamera();
        if (aimCamera == null) return;

        if (_aimLocked)
        {
            TurretCrosshair(aimCamera);
            return;
        }

        float screenY = GetCurrentScreenY();
        Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));
        int hitCount = GetAimRayHits(aimRay);

        // 포탑 회전 전/후 포신 위치가 달라질 수 있으므로, 같은 충돌 결과를 각각의 현재 위치로 해석한다.
        RotateTurret(GetAimPoint(aimCamera, aimRay, hitCount));
        RotateBarrel(GetAimPoint(aimCamera, aimRay, hitCount), screenY);
        TurretCrosshair(aimCamera);
        UpdateCrosshairColor(hitCount);
    }

    /// <summary>
    /// 화면 중앙 레이의 충돌 결과를 가져옴
    /// </summary>
    int GetAimRayHits(Ray ray)
    {
        int hitCount = Physics.RaycastNonAlloc(ray, _aimHits, AimRayDistance, _aimLayerMask);

        // NonAlloc은 버퍼가 꽉 찼을 때 일부 충돌이 잘릴 수 있으므로, 필요한 경우에만 한 번씩 확장한다.
        while (hitCount == _aimHits.Length)
        {
            System.Array.Resize(ref _aimHits, _aimHits.Length * 2);
            hitCount = Physics.RaycastNonAlloc(ray, _aimHits, AimRayDistance, _aimLayerMask);
        }

        return hitCount;
    }

    /// <summary>
    /// 화면 중앙 레이의 충돌 결과에서 포신이 겨눌 지점을 구함
    /// </summary>
    Vector3 GetAimPoint(Camera aimCamera, Ray ray, int hitCount)
    {
        // 카메라와 포탑 사이 거리
        float minDist = Vector3.Distance(aimCamera.transform.position, _barrel.position);

        // RaycastNonAlloc의 결과 순서는 보장되지 않으므로, 정렬 대신 조건을 만족하는 가장 가까운 충돌만 찾는다.
        bool hasAimHit = false;
        RaycastHit nearestAimHit = default;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _aimHits[i];

            // 카메라와 탱크 사이에 걸린 물체는 무시
            if (hit.distance < minDist) continue;
            if (hit.distance >= nearestDistance) continue;

            nearestAimHit = hit;
            nearestDistance = hit.distance;
            hasAimHit = true;
        }

        if (hasAimHit)
        {
            // 거리를 비교할 때 '카메라' 기준이 아닌 '포탑' 기준으로 실제 거리를 다시 계산
            float distFromBarrel = Vector3.Distance(_barrel.position, nearestAimHit.point);

            // 저격 모드, 물체가 포탑에서 충분히 멀리 떨어져 있다면 정상 조준
            if (distFromBarrel >= _minAimDistance || _isSniping)
            {
                return nearestAimHit.point;
            }

            // 탱크 앞의 벽을 조준했을 때 포신이 과하게 들리는 현상 방지
            // 카메라 광선을 따라 '탱크 앞쪽(_fallbackAimDistance)'을 조준하도록 보정
            return ray.origin + ray.direction * (minDist + _fallbackAimDistance);
        }

        // 아무것도 안 맞으면 멀리 조준
        return ray.origin + ray.direction * AimRayDistance;
    }

    /// <summary>
    /// 포탑 좌우 회전
    /// </summary>
    void RotateTurret(Vector3 targetPoint)
    {
        // 떨림 방지, LookRotation 에러 방지
        Vector3 direction = targetPoint - _turret.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < Util.Epsilon)
        {
            return;
        }

        // NetworkTransform이 In Local Space로 동기화하므로, world rotation이 아닌 localRotation을 직접 갱신함
        // (parent 기준 상대 회전으로 변환 후 대입 — parent가 기울어져 있어도 기존과 동일한 world 조준 방향 유지)
        Quaternion targetWorldRotation = Quaternion.LookRotation(direction);
        Quaternion targetLocalRotation = Quaternion.Inverse(_turret.parent.rotation) * targetWorldRotation;

        float angle = Quaternion.Angle(_turret.localRotation, targetLocalRotation);

        if (angle > Util.Epsilon)
        {
            _turret.localRotation = Quaternion.RotateTowards(_turret.localRotation, targetLocalRotation, _rotSpeed * Time.deltaTime);
        }
        else
        {
            _turret.localRotation = targetLocalRotation;
        }
    }

    /// <summary>
    /// 주포 상하 회전
    /// </summary>
    void RotateBarrel(Vector3 targetPoint, float screenY)
    {
        // 화면조준점 Y 위치는 현재 모드에 맞게 맞춤
        SetCenterCrosshairScreenY(screenY);

        Vector3 direction = targetPoint - _barrel.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float angle = targetRotation.eulerAngles.x;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        float clampedAngle = Mathf.Clamp(angle, _minAngle, _maxAngle);
        Quaternion target = Quaternion.Euler(clampedAngle, 0f, 0f);

        if (Quaternion.Angle(_barrel.localRotation, target) > Util.Epsilon)
        {
            _barrel.localRotation = Quaternion.RotateTowards(_barrel.localRotation, target, _rotSpeed * Time.deltaTime);
        }
        else
        {
            _barrel.localRotation = target;
        }
    }

    /// <summary>
    /// 포탑조준점(O) 위치 조절
    /// </summary>
    void TurretCrosshair(Camera aimCamera)
    {
        if (_turretCrosshair == null) return;

        Ray ray = new Ray(_barrel.position, _barrel.forward);

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, AimRayDistance, _aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * AimRayDistance;
        }

        // z 보정
        Vector3 screenPosition = aimCamera.WorldToScreenPoint(targetPoint);
        screenPosition.z = 0f;
        _turretCrosshair.position = screenPosition;
    }

    /// <summary>
    /// 포탑 초기화
    /// </summary>
    public void ResetRotation()
    {
        _turret.localRotation = Quaternion.identity;
        _barrel.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 화면조준점(+) 색 조절
    /// </summary>
    void UpdateCrosshairColor(int hitCount)
    {
        // 기존 구현은 거리순 첫 번째 '태그가 있는' 충돌만 판정했다.
        // NonAlloc 결과는 정렬되지 않으므로, 같은 기준의 충돌을 직접 찾는다.
        bool hasTaggedHit = false;
        RaycastHit nearestTaggedHit = default;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _aimHits[i];
            Collider hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.CompareTag("Untagged")) continue;
            if (hit.distance >= nearestDistance) continue;

            nearestTaggedHit = hit;
            nearestDistance = hit.distance;
            hasTaggedHit = true;
        }

        if (hasTaggedHit && nearestTaggedHit.collider.TryGetComponent(out HitZone hitZone))
        {
            EnemyTank targetTank = null;
            if (hitZone.Parent != null)
            {
                hitZone.Parent.TryGetComponent(out targetTank);
            }

            SetTargetSilhouette(targetTank);

            // 조준점 색 변경
            SetCrosshairColor(hitZone.ZoneType switch
            {
                HitZoneType.Front => _frontColor,
                HitZoneType.Side => _sideColor,
                HitZoneType.Rear => _rearColor,
                _ => _rearColor
            });
            return;
        }

        SetTargetSilhouette(null);
        SetCrosshairColor(_defaultColor);
    }

    /// <summary>
    /// 현재 켜져있는 적 실루엣 끄기(죽었을 때 등 강제 정리용)
    /// </summary>
    public void ClearTargetSilhouette()
    {
        SetTargetSilhouette(null);
    }

    /// <summary>
    /// 조준 대상이 바뀔 때만 실루엣 상태를 변경
    /// </summary>
    void SetTargetSilhouette(EnemyTank targetTank)
    {
        if (_targetTank == targetTank)
        {
            // Unity에서 파괴된 오브젝트 참조는 null처럼 비교되므로 필드도 정리한다.
            if (targetTank == null) _targetTank = null;
            return;
        }

        if (_targetTank != null)
        {
            _targetTank.ToggleEnemySilhouette(false);
        }

        _targetTank = targetTank;

        if (_targetTank != null)
        {
            _targetTank.ToggleEnemySilhouette(true);
        }
    }

    /// <summary>
    /// 실제 색이 바뀔 때만 UI Graphic을 갱신
    /// </summary>
    void SetCrosshairColor(Color color)
    {
        if (_centerCrosshairImage != null && _centerCrosshairImage.color != color)
        {
            _centerCrosshairImage.color = color;
        }
    }

    /// <summary>
    /// 조준점 UI 표시 설정
    /// </summary>
    public void SetCrosshairVisible(bool visible)
    {
        if (_centerCrosshair == null) return;
        _centerCrosshair.gameObject.SetActive(visible);
        _turretCrosshair.gameObject.SetActive(visible);
    }

    Vector3 GetCenterAimPoint()
    {
        Camera aimCamera = GetAimCamera();
        if (aimCamera == null)
        {
            return _barrel.position + _barrel.forward * AimRayDistance;
        }

        float screenY = GetCurrentScreenY();

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, AimRayDistance, _aimLayerMask))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * AimRayDistance;
    }

    /// <summary>
    /// 3인칭/1인칭 화면조준점 위치
    /// </summary>
    float GetCurrentScreenY()
    {
        return _isSniping ? 0.5f : 0.75f;
    }

    void SetCenterCrosshairScreenY(float screenY)
    {
        if (_centerCrosshair == null) return;

        Vector2 anchor = new Vector2(0.5f, screenY);
        if (_centerCrosshair.anchorMin == anchor && _centerCrosshair.anchorMax == anchor) return;

        _centerCrosshair.anchorMin = anchor;
        _centerCrosshair.anchorMax = anchor;
    }

    Camera GetAimCamera()
    {
        if (_aimCamera == null || _aimCamera.isActiveAndEnabled == false)
        {
            _aimCamera = Camera.main;
        }

        return _aimCamera;
    }
}
