using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 미니맵 버튼 — 누르는 즉시 미니맵 확대/축소 토글
/// </summary>
public class TouchMiniMapButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileMapInput();
    }
}