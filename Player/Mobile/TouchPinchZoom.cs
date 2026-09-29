using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 두 손가락 핀치 줌 — 손가락 사이 거리에 따라 카메라 확대/축소
/// </summary>
public class TouchPinchZoom : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] float _sensitivity = 0.05f;

    readonly Dictionary<int, Vector2> _touchPositions = new();

    float _previousDistance;

    public void OnPointerDown(PointerEventData eventData)
    {
        _touchPositions[eventData.pointerId] = eventData.position;

        if (_touchPositions.Count == 2)
        {
            _previousDistance = GetTouchDistance();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_touchPositions.ContainsKey(eventData.pointerId) == false) return;

        _touchPositions[eventData.pointerId] = eventData.position;

        if (_touchPositions.Count != 2) return;

        float currentDistance = GetTouchDistance();
        float delta = currentDistance - _previousDistance;

        _inputSystemHandler.SetMobileZoomInput(new Vector2(0f, delta * _sensitivity));

        _previousDistance = currentDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _touchPositions.Remove(eventData.pointerId);

        _previousDistance = 0f;
        _inputSystemHandler.SetMobileZoomInput(Vector2.zero);
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