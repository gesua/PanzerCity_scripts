using UnityEngine;

/// <summary>
/// 전차장 LookAt 제어
/// Body는 Y축만 회전하고, Head Bone은 카메라를 바라보도록 회전
/// </summary>
public class CommanderLookAt : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _body;
    [SerializeField] Transform _head;

    [Header("----- 회전 관련 -----")]
    [SerializeField] float _bodyRotateSpeed = 180f;
    [SerializeField] float _headRotateSpeed = 8f;
    [SerializeField] float _maxPitch = 30f;

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
        // 이 리그의 로컬 축: X=Pitch(-위/+아래), Y=Roll, Z=Yaw(-좌/+우) — Unity 월드 컨벤션과 다름
        float horizontalDistance = Mathf.Sqrt(localDir.x * localDir.x + localDir.y * localDir.y);
        float targetPitch = Mathf.Atan2(localDir.z, horizontalDistance) * Mathf.Rad2Deg;

        // 목표 회전 한계값 적용
        targetPitch = Mathf.Clamp(targetPitch, -_maxPitch, _maxPitch);

        // 자체적으로 관리하는 변수를 부드럽게 보간
        _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, _headRotateSpeed * Time.deltaTime);

        // 매핑 대입
        Vector3 finalEuler = new Vector3(-_currentPitch, 0f, 0f);

        // Animator 덮어씌움
        _head.localRotation = Quaternion.Euler(finalEuler);
    }
}