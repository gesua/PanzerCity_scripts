using UnityEngine;

/// <summary>
/// 모바일 플랫폼 또는 에디터 테스트 모드에서 모바일 UI를 활성화
/// </summary>
public class MobileUIController : MonoBehaviour
{
    [SerializeField] GameObject[] _mobileUIObjects;

#if UNITY_EDITOR
    [SerializeField] MobileTestMode _mobileTestMode;
#endif

    public static bool IsMobile { get; private set; } // 모바일 여부(줍기 힌트처럼 플랫폼별로 표시가 달라지는 스크립트가 참조)

    void Awake()
    {
        bool isMobile = Application.isMobilePlatform;

#if UNITY_EDITOR
        // 에디터에서는 MobileTestMode가 활성화된 경우 모바일 UI 표시
        if (_mobileTestMode != null && _mobileTestMode.enabled)
            isMobile = true;
#endif

        IsMobile = isMobile;

        foreach (GameObject uiObject in _mobileUIObjects)
        {
            if (uiObject != null)
                uiObject.SetActive(isMobile);
        }
    }
}