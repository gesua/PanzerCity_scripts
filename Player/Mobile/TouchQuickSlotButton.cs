using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모바일 터치 퀵슬롯 버튼 — 누르는 즉시 해당 슬롯 사용
/// 슬롯 순서는 QuickSlotUI와 동일: 0=기지 무적, 1=나 무적, 2=적 멈춤, 3=폭탄
/// </summary>
public class TouchQuickSlotButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] int _slotIndex; // 0~3, Inspector에서 버튼마다 지정

    public void OnPointerDown(PointerEventData eventData)
    {
        _inputSystemHandler.SetMobileQuickSlotInput(_slotIndex);
    }
}