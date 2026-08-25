using UnityEngine;

/// <summary>
/// 전차장 LookAt 제어
/// Body는 Y축만 회전하고, Head Bone은 카메라를 바라보도록 회전
/// </summary>
public class CommanderLookAt : MonoBehaviour
{
    /// <summary>
    /// 로컬 축 종류(리그마다 다른 축 컨벤션 보정용)
    /// </summary>
    public enum LocalAxis
    {
        X,
        Y,
        Z
    }

    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _body;
    [SerializeField] Transform _head;

    [Header("----- 회전 관련 -----")]
    [SerializeField] float _bodyRotateSpeed = 180f;
    [SerializeField] float _headRotateSpeed = 8f;
    [SerializeField] float _maxPitch = 30f;

    [Header("----- 리그별 축 보정(헤드 피치) -----")]
    [SerializeField] LocalAxis _pitchInputAxis = LocalAxis.Z; // localDir에서 상하 각도 판단 기준 축(제네릭 리그 기본값: Z)
    [SerializeField] LocalAxis _pitchOutputAxis = LocalAxis.X; // 계산된 피치를 대입할 헤드 로컬 축(제네릭 리그 기본값: X)
    [SerializeField] bool _invertPitchOutput = true; // 대입 시 부호 반전 여부(제네릭 리그 기본값: 반전 필요)

    bool _lookAt;

    // Animator에 의해 값이 덮어씌워지는 것을 방지하기 위해 
    // 현재 회전 중인 각도를 직접 추적
    private float _currentPitch = 0f;

    /// <summary>
    /// LookAt 활성화
    /// </summary>
    public void SetLookAt(bool active)
    {
        _lookAt = active;
    }

    void LateUpdate()
    {
        if (_lookAt == false || Camera.main == null) return;

        RotateBody();
        RotateHead();
    }

    /// <summary>
    /// 몸통은 Y축만 회전
    /// </summary>
    void RotateBody()
    {
        Vector3 dir = Camera.main.transform.position - _body.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        _body.rotation = Quaternion.RotateTowards(
            _body.rotation,
            targetRotation,
            _bodyRotateSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 머리는 카메라를 바라봄
    /// </summary>
    void RotateHead()
    {
        // 머리에서 카메라를 향하는 월드 방향 벡터 구함
        Vector3 worldDir = Camera.main.transform.position - _head.position;

        // Parent Space 기준으로 상대 방향 벡터를 변환
        Vector3 localDir = _head.parent.InverseTransformDirection(worldDir);

        if (localDir.sqrMagnitude < Util.Epsilon) return;

        // 목표 상하 각도 계산
        // 리그마다 로컬 축 방향이 달라서 입력/출력 축을 필드로 분리함
        // (제네릭 리그 기본값: 입력 축=Z, 출력 축=X, 반전=true — 이 리그의 로컬 축은 X=Pitch(-위/+아래), Y=Roll, Z=Yaw(-좌/+우)로 Unity 월드 컨벤션과 다름)
        float pitchInputValue = localDir[(int)_pitchInputAxis];
        float horizontalDistance = GetHorizontalMagnitude(localDir, _pitchInputAxis);
        float targetPitch = Mathf.Atan2(pitchInputValue, horizontalDistance) * Mathf.Rad2Deg;

        // 목표 회전 한계값 적용
        targetPitch = Mathf.Clamp(targetPitch, -_maxPitch, _maxPitch);

        // 자체적으로 관리하는 변수를 부드럽게 보간
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, _headRotateSpeed * Time.deltaTime);

        // 매핑 대입
        Vector3 finalEuler = Vector3.zero;
        finalEuler[(int)_pitchOutputAxis] = (_invertPitchOutput) ? -_currentPitch : _currentPitch;

        // Animator 덮어씌움
        _head.localRotation = Quaternion.Euler(finalEuler);
    }

    /// <summary>
    /// 지정한 축을 제외한 나머지 두 축으로 수평 거리(피치 계산용 분모) 산출
    /// </summary>
    float GetHorizontalMagnitude(Vector3 v, LocalAxis excludeAxis)
    {
        float sum = 0f;

        for (int i = 0; i < 3; i++)
        {
            if (i == (int)excludeAxis) continue;

            sum += v[i] * v[i];
        }

        return Mathf.Sqrt(sum);
    }
}