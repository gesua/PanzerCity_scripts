using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 두 손가락 핀치 줌 — 손가락 사이 거리에 따라 카메라 확대/축소
/// uGUI OnDrag는 움직임이 없는 프레임엔 호출되지 않는데 InputSystemHandler는 저장된 값을 매 프레임 내보내므로,
/// 드래그 중 값을 누적해뒀다가 매 프레임 소비한 뒤 즉시 0으로 초기화함(TouchLookZone과 동일한 방식)
/// </summary>
public class TouchPinchZoom : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] float _sensitivity = 0.05f; // 손가락 사이 거리 1픽셀당 줌 값

    readonly Dictionary<int, Vector2> _touchPositions = new();

    float _previousDistance;
    float _pendingZoom; // 이번 프레임에 누적된 줌 값(매 프레임 소비 후 초기화)

    public void OnPointerDown(PointerEventData eventData)
    {
        _touchPositions[eventData.pointerId] = eventData.position;
        RefreshPreviousDistance();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_touchPositions.ContainsKey(eventData.pointerId) == false) return;

        _touchPositions[eventData.pointerId] = eventData.position;

        if (_touchPositions.Count != 2) return;

        float currentDistance = GetTouchDistance();
        float delta = currentDistance - _previousDistance;

        _pendingZoom += delta * _sensitivity;
        _previousDistance = currentDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _touchPositions.Remove(eventData.pointerId);
        RefreshPreviousDistance();

        _pendingZoom = 0f;
        _inputSystemHandler.SetMobileZoomInput(Vector2.zero);
    }

    void Update()
    {
        _inputSystemHandler.SetMobileZoomInput(new Vector2(0f, _pendingZoom));
        _pendingZoom = 0f;
    }

    void OnDisable()
    {
        // 손가락을 누른 채 꺼지면 OnPointerUp이 오지 않으므로 손가락 정보와 줌 값을 정리
        _touchPositions.Clear();
        _previousDistance = 0f;
        _pendingZoom = 0f;
        _inputSystemHandler.SetMobileZoomInput(Vector2.zero);
    }

    /// <summary>
    /// 손가락이 정확히 2개일 때만 기준 거리를 다시 잡음
    /// 3번째 손가락이 닿았다 떨어져 다시 2개가 된 경우에도 거리가 0 기준으로 계산돼 줌이 튀는 것을 방지
    /// </summary>
    void RefreshPreviousDistance()
    {
        _previousDistance = (_touchPositions.Count == 2) ? GetTouchDistance() : 0f;
    }

    float GetTouchDistance()
    {
        var enumerator = _touchPositions.Values.GetEnumerator();

        enumerator.MoveNext();
        Vector2 first = enumerator.Current;

        enumerator.MoveNext();
        Vector2 second = enumerator.Current;

        return Vector2.Distance(first, second);
    }
}