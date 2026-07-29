using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 저격 모드 관리
/// 카메라 전환, 탱크 렌더러 비활성화
/// </summary>
public class SniperModeController : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] CinemachineCamera _normalCam;   // 평소 카메라
    [SerializeField] CinemachineCamera _sniperCam;   // 저격 모드 카메라
    [SerializeField] PlayerTank _player;             // 플레이어 참조 (사망 상태 확인용)
    [SerializeField] Renderer[] _tankRenderers;      // 탱크 렌더러들 (저격 모드에서 비활성화)
    [SerializeField] CommanderController _commander; // 전차장 캐릭터 (저격 모드에서 비활성화)
    [SerializeField] Transform _mainCameraTr; // 거리 체크용 메인 카메라
    [SerializeField] Transform _playerTarget; // 카메라와 거리 비교할 기준점

    [Header("----- 카메라 근접 숨김 -----")]
    [SerializeField] float _hideDistance = 3f; // 이 거리보다 가까우면 모델 숨김
    [SerializeField] float _showDistance = 3.4f; // 이 거리보다 멀어지면 모델 다시 표시

    bool _isSniper;
    bool _isCameraTooClose;
    bool _lastVisualHidden;
    bool _hasAppliedVisual;

    public bool IsSniper => _isSniper;

    /// <summary>
    /// 멀티플레이:로컬 플레이어의 실제 참조로 교체(GameScene이 호출)
    /// </summary>
    public void SetPlayerVisualReferences(PlayerTank player)
    {
        _player = player;
        _tankRenderers = player.NormalVisualRenderers;
        _commander = player.Commander;
        _playerTarget = player.transform;

        // 참조가 바뀌었으니 캐시된 적용 상태를 무시하고 강제로 재적용
        _hasAppliedVisual = false;
        ApplyPlayerVisual();
    }

    private void LateUpdate()
    {
        UpdateCameraCloseState();
    }

    /// <summary>
    /// 저격 모드 토글
    /// </summary>
    public void ToggleSniperMode()
    {
        SetSniperMode(!_isSniper);
    }

    /// <summary>
    /// 저격 모드 설정
    /// </summary>
    public void SetSniperMode(bool active)
    {
        _isSniper = active;

        // 우선순위 설정
        _normalCam.Priority = active ? 0 : 10;
        _sniperCam.Priority = active ? 10 : 0;

        ApplyPlayerVisual();
    }

    /// <summary>
    /// 카메라가 플레이어 모델에 가까운지 확인
    /// </summary>
    void UpdateCameraCloseState()
    {
        if (_mainCameraTr == null || _playerTarget == null) return;

        float hideDistance = Mathf.Max(0f, _hideDistance);
        float showDistance = Mathf.Max(hideDistance, _showDistance);
        float threshold = (_isCameraTooClose) ? showDistance : hideDistance;
        float sqrThreshold = threshold * threshold;
        bool isCameraTooClose = (_mainCameraTr.position - _playerTarget.position).sqrMagnitude < sqrThreshold;

        if (_isCameraTooClose == isCameraTooClose) return;

        _isCameraTooClose = isCameraTooClose;
        ApplyPlayerVisual();
    }

    /// <summary>
    /// 실제 플레이어 모델 표시 상태 적용
    /// </summary>
    void ApplyPlayerVisual()
    {
        // 카메라 상태에 따라 숨길지 결정
        bool isCameraHide = _isSniper || _isCameraTooClose;

        // 탱크와 전차장의 숨김 여부를 각각 분리
        bool tankShouldHide = isCameraHide;
        bool commanderShouldHide = isCameraHide;

        // 사망 시 예외 처리
        if (_player != null && _player.IsDead)
        {
            tankShouldHide = true;       // 죽으면 탱크 모델링은 무조건 숨김
            commanderShouldHide = false; // 죽으면 전차장은 무조건 보이게 함
        }

        // 탱크 렌더러 상태가 이전과 동일하다면 전차장 상태만 갱신하고 반환
        if (_hasAppliedVisual && _lastVisualHidden == tankShouldHide)
        {
            if (_commander != null) _commander.SetVisible(!commanderShouldHide);
            return;
        }

        _hasAppliedVisual = true;
        _lastVisualHidden = tankShouldHide;

        if (_tankRenderers != null)
        {
            foreach (Renderer renderer in _tankRenderers)
            {
                if (renderer != null) renderer.enabled = !tankShouldHide;
            }
        }

        // 전차장 업데이트
        if (_commander != null) _commander.SetVisible(!commanderShouldHide);
    }
}
