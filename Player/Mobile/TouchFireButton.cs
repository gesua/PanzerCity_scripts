using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 발사 버튼 — 누르고 있는 동안 발사, 떼면 정지
/// </summary>
public class TouchFireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileAttackInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileAttackInput(false);
    }
}
