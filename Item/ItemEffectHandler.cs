using System;
using UnityEngine;

/// <summary>
/// 아이템 효과 관리
/// </summary>
public class ItemEffectHandler : MonoBehaviour
{
    [SerializeField] float _hyperShieldDuration = 5f; // 무적 시간

    public event Action OnLifeUp;
    public event Action<float> OnHyperShield; // 나 무적<지속시간>
    public event Action OnAirSupport;

    public void Use(int itemID)
    {
        switch (itemID)
        {
            case 1001: // 목숨 증가
                UseLifeUp();
                break;
            case 1002: // 기지 무적
                break;
            case 1003: // 나 무적
                UseHyperShield();
                break;
            case 1004: // 적 멈춤
                break;
            case 1005: // 폭탄
                UseAirSupport();
                break;
        }
    }

    /// <summary>
    /// 목숨 증가
    /// </summary>
    void UseLifeUp()
    {
        OnLifeUp?.Invoke();
    }

    /// <summary>
    /// 나 무적
    /// </summary>
    void UseHyperShield()
    {
        OnHyperShield?.Invoke(_hyperShieldDuration);
    }

    /// <summary>
    /// 폭탄
    /// </summary>
    void UseAirSupport()
    {
        OnAirSupport?.Invoke();
    }
}
