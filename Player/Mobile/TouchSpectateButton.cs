using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 관전 전환 버튼 — 누르는 즉시 다음 관전 대상으로 전환
/// 표시/숨김은 GameScene이 관전 상태에 맞춰 제어함
/// </summary>
public class TouchSpectateButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileSpectateNextInput();
    }
}