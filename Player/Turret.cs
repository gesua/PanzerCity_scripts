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
    [SerializeField] float _minAimDistance = 10f; // 최소 조준거리
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
        Debug.Log($"Turret.SetLocalControl({isLocal}) 호출됨 | GameObject:{name}");

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
        Debug.Log($"Turret.SetSceneReferences 호출됨 | centerCrosshair null 여부:{centerCrosshair == null} | GameObject:{name}");

        _cameraTarget = cameraTarget;
        _centerCrosshair = centerCrosshair;
        _turretCrosshair = turretCrosshair;
        _centerCrosshairImage = centerCrosshairImage;
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
        if (_isLocalControl == false)
        {
            return; // 로컬 소유가 아니면 카메라 기반 조준 로직 실행 안 함
        }
        if (_centerCrosshair == null)
        {
            Debug.Log($"Turret Update 막힘 | _isLocalControl:{_isLocalControl} | _centerCrosshair:null | GameObject:{name}");
            return;
        }

        if (_aimLocked)
        {
            Debug.Log($"Turret Update: _aimLocked=true라서 회전 스킵 | GameObject:{name}");

            TurretCrosshair();
            return;
        }

        RotateTurret();
        RotateBarrel();
        TurretCrosshair();
        UpdateCrosshairColor();
    }

    /// <summary>
    /// 화면 중앙에서 레이캐스트를 쏴서 조준 지점을 구함
    /// </summary>
    Vector3 GetAimPoint()
    {
        float screenY = GetCurrentScreenY();
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));

        // 카메라와 포탑 사이 거리보다 가까운 히트는 무시 (bedrock이 카메라 뒤에 걸리는 상황 방지)
        float minDist = Vector3.Distance(Camera.main.transform.position, _barrel.position);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            if (hit.distance >= minDist) // 카메라와 포탑 사이 무시
            {
                // 저격 모드에선 가까워도 제대로 조준
                if (hit.distance >= _minAimDistance || _isSniping)
                {
                    return hit.point;
                }
                else // 가까운 물체면 특정 거리 조준(포신이 과하게 들리는 것을 방지)
                {
                    return ray.origin + ray.direction * _fallbackAimDistance;
                }
            }
        }

        // 아무것도 안 맞으면 멀리 있는 지점을 기준으로 사용
        return ray.origin + ray.direction * 1000f;
    }

    /// <summary>
    /// 포탑 좌우 회전
    /// </summary>
    void RotateTurret()
    {
        Vector3 targetPoint = GetAimPoint();

        // 떨림 방지, LookRotation 에러 방지
        Vector3 direction = targetPoint - _turret.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < Util.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(_turret.rotation, targetRotation);

        if (angle > Util.Epsilon)
        {
            _turret.rotation = Quaternion.RotateTowards(_turret.rotation, targetRotation, _rotSpeed * Time.deltaTime);
        }
        else
        {
            _turret.rotation = targetRotation;
        }
    }

    /// <summary>
    /// 주포 상하 회전
    /// </summary>
    void RotateBarrel()
    {
        // 화면조준점 Y 위치는 현재 모드에 맞게 맞춤
        float screenY = GetCurrentScreenY();
        SetCenterCrosshairScreenY(screenY);

        Vector3 targetPoint = GetAimPoint();

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
    void UpdateCrosshairColor()
    {
        // 이전 타겟 실루엣 끄기
        ClearTargetSilhouette();

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, GetCurrentScreenY(), 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, _aimLayerMask);

        // 거리순 정렬
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.tag == "Untagged") continue; // 없는 태그 무시

            // HitZone인지 확인
            if (hit.collider.TryGetComponent(out HitZone hitZone))
            {
                // 실루엣 켜기
                if (hitZone.Parent.TryGetComponent(out _targetTank))
                {
                    _targetTank.ToggleEnemySilhouette(true);
                }

                // 조준점 색 변경
                _centerCrosshairImage.color = hitZone.ZoneType switch
                {
                    HitZoneType.Front => _frontColor,
                    HitZoneType.Side => _sideColor,
                    HitZoneType.Rear => _rearColor,
                    _ => _rearColor
                };
                return;
            }

            // 기본색
            break;
        }

        _centerCrosshairImage.color = _defaultColor;
    }

    /// <summary>
    /// 현재 켜져있는 적 실루엣 끄기(죽었을 때 등 강제 정리용)
    /// </summary>
    public void ClearTargetSilhouette()
    {
        if (_targetTank != null)
        {
            _targetTank.ToggleEnemySilhouette(false);
            _targetTank = null;
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
        float screenY = GetCurrentScreenY();

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, screenY, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _aimLayerMask))
        {
            return hit.point;
        }

        return ray.origin + ray.direction * 1000f;
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
        _centerCrosshair.anchorMin = new Vector2(0.5f, screenY);
        _centerCrosshair.anchorMax = new Vector2(0.5f, screenY);
    }
}