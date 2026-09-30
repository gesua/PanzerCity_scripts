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

    void Awake()
    {
        bool isMobile = Application.isMobilePlatform;

#if UNITY_EDITOR
        // 에디터에서는 MobileTestMode가 활성화된 경우 모바일 UI 표시
        if (_mobileTestMode != null && _mobileTestMode.enabled)
            isMobile = true;
#endif

        foreach (GameObject uiObject in _mobileUIObjects)
        {
            if (uiObject != null)
                uiObject.SetActive(isMobile);
        }
    }
}