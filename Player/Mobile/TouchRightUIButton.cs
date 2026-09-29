using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 오른쪽 UI 버튼 — 누르는 즉시 UI 열기/닫기 토글
/// </summary>
public class TouchRightUIButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileToggleRightUIInput();
    }
}