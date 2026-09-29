using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 저격 모드 버튼 — 누르는 즉시 저격 모드 토글
/// </summary>
public class TouchSniperButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileSniperInput();
    }
}