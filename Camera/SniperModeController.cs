using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 저격 모드 관리
/// 카메라 전환, 탱크 렌더러 비활성화
/// </summary>
public class SniperModeController : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] CinemachineCamera _normalCam;  // 평소 카메라
    [SerializeField] CinemachineCamera _sniperCam;  // 저격 모드 카메라
    [SerializeField] Renderer[] _tankRenderers;     // 탱크 렌더러들 (저격 모드에서 비활성화)

    bool _isSniper;

    public bool IsSniper => _isSniper;

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

        // 내 탱크 렌더러들
        foreach (Renderer renderer in _tankRenderers)
        {
            renderer.enabled = !active;
        }
    }
}
