using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 이동 조이스틱
/// 고정 위치 배경 안에서 핸들이 손가락을 따라가고,
/// 배경 중심 기준 정규화된 방향 값을 InputSystemHandler에 매 프레임 전달함
/// </summary>
public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] RectTransform _handle; // 손가락을 따라가는 핸들
    [SerializeField] float _handleRange = 100f; // 핸들이 배경 중심에서 최대로 벗어날 수 있는 픽셀 거리

    RectTransform _background; // 조이스틱 배경
    Vector2 _handleOriginPos;  // 핸들 원래(중앙) 위치

    void Awake()
    {
        TryGetComponent(out _background);
        _handleOriginPos = _handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateJoystick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _handle.anchoredPosition = _handleOriginPos;
        _inputSystemHandler.SetMobileMoveInput(Vector2.zero);
    }

    /// <summary>
    /// 터치 위치를 배경 기준 로컬 좌표로 변환해 핸들 위치와 이동 입력 값을 갱신
    /// </summary>
    void UpdateJoystick(PointerEventData eventData)
    {
        bool hasLocalPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        if (hasLocalPoint == false) return;

        Vector2 offset = localPoint - _handleOriginPos;
        Vector2 clampedOffset = Vector2.ClampMagnitude(offset, _handleRange);

        _handle.anchoredPosition = _handleOriginPos + clampedOffset;

        Vector2 normalizedValue = clampedOffset / _handleRange;
        _inputSystemHandler.SetMobileMoveInput(normalizedValue);
    }
}
