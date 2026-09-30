using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 줍기 버튼 — 누르는 즉시 가까운 아이템 줍기
/// </summary>
public class TouchInteractButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileInteractInput();
    }
}