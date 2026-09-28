using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem.EnhancedTouch;
#endif

/// <summary>
/// 에디터 전용 모바일 테스트 헬퍼
/// 마우스를 가상 터치스크린 입력으로 변환하고, 커서 락을 매 프레임 풀어서
/// 에디터에서도 조이스틱/룩존/발사 버튼을 마우스로 눌러볼 수 있게 함
/// GameScene._OnCursor 같은 게임 상태는 건드리지 않으므로 기존 핸들러 흐름을 그대로 통과함
/// 빌드에서는 클래스만 남고 아무 동작도 하지 않음(에디터 전용 내용은 전부 #if UNITY_EDITOR 안에 있음)
/// </summary>
public class MobileTestMode : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] bool _simulateTouchFromMouse = true; // 마우스 → 가상 터치스크린 변환(네이티브 마우스는 비활성화됨, Device Simulator 뷰를 쓸 땐 꺼야 함, 재생 전에 설정)
    [SerializeField] bool _forceUnlockCursor = true; // 다른 스크립트가 Locked로 다시 걸어도 매 프레임 풀어줌

    bool _isSimulating; // 이 컴포넌트가 TouchSimulation을 켰는지(끌 때 남이 켜둔 설정을 건드리지 않기 위함)

    void OnEnable()
    {
        if (_simulateTouchFromMouse == false) return;

        TouchSimulation.Enable();
        _isSimulating = true;
    }

    void OnDisable()
    {
        if (_isSimulating == false) return;

        TouchSimulation.Disable();
        _isSimulating = false;
    }

    void LateUpdate()
    {
        if (_forceUnlockCursor == false) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
#endif
}