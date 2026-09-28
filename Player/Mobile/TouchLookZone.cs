using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 드래그로 카메라를 회전시키는 영역
/// 조이스틱/발사 버튼과 겹치지 않는 화면 영역에 배치해서 사용
/// 실제 마우스 델타는 손이 멈추면 자동으로 0이 되지만, uGUI OnDrag는 움직임이 없는 프레임엔
/// 아예 호출되지 않으므로 직접 흉내냄:매 프레임 누적값을 소비한 뒤 즉시 0으로 초기화함
/// </summary>
public class TouchLookZone : MonoBehaviour, IDragHandler, IPointerUpHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] float _sensitivity = 0.5f; // 터치 픽셀 델타는 마우스 델타보다 훨씬 커서 축소 보정 필요(CameraTarget의 ±30 클램프 대비)

    Vector2 _pendingDelta; // 이번 프레임에 누적된 드래그 델타(매 프레임 소비 후 초기화)

    public void OnDrag(PointerEventData eventData)
    {
        _pendingDelta += eventData.delta * _sensitivity;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pendingDelta = Vector2.zero;
        _inputSystemHandler.SetMobileLookInput(Vector2.zero);
    }

    void Update()
    {
        _inputSystemHandler.SetMobileLookInput(_pendingDelta);
        _pendingDelta = Vector2.zero;
    }
}
