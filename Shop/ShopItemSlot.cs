using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 아이템 슬롯
/// </summary>
public class ShopItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _priceText;
    [SerializeField] GameObject _soldOutOverlay; // 매진 오버레이

    ItemConfig _itemConfig;
    bool _isSoldOut;
    bool _isInteractable = true;

    public event Action<ItemConfig> OnClicked;

    // 툴팁을 위한 호버 이벤트 추가
    public event Action<ItemConfig, Vector3> OnHoverEnter;
    public event Action OnHoverExit;

    public void Initialize(ItemConfig itemConfig)
    {
        _itemConfig = itemConfig;
        _icon.sprite = itemConfig.IconSprite;
        _priceText.text = itemConfig.BuyPrice.ToString() + "G";
        SetSoldOut(false); // 초기화할 때 매진 상태 리셋
    }

    /// <summary>
    /// 매진 상태 설정
    /// </summary>
    public void SetSoldOut(bool isSoldOut)
    {
        _isSoldOut = isSoldOut;
        if (_soldOutOverlay != null) // 일반 소모품은 매진 없음
            _soldOutOverlay.SetActive(isSoldOut);
    }

    /// <summary>
    /// 상호작용 가능 여부 설정
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isSoldOut) return;
        if (_isInteractable == false) return;
        OnClicked?.Invoke(_itemConfig);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_itemConfig != null && _isInteractable)
        {
            // 아이콘의 월드 위치
            OnHoverEnter?.Invoke(_itemConfig, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isInteractable)
        {
            OnHoverExit?.Invoke();
        }
    }
}