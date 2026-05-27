using System;
using UnityEngine;

/// <summary>
/// 아이템 효과 관리
/// </summary>
public class ItemEffectHandler : MonoBehaviour
{
    [SerializeField] float _baseShieldDuration = 10f; // 기지 무적 시간
    [SerializeField] float _hyperShieldDuration = 5f; // 나 무적 시간
    [SerializeField] float _empFieldDuration = 10f; // 적 멈추는 시간

    public event Action OnLifeUp;
    /// <summary>
    /// 기지 무적 [지속시간]
    /// </summary>
    public event Action<float> OnBaseShield;
    /// <summary>
    /// 나 무적 [지속시간]
    /// </summary>
    public event Action<float> OnHyperShield;
    /// <summary>
    /// 적 멈춤 [지속시간]
    /// </summary>
    public event Action<float> OnEMPField;
    public event Action OnAirSupport;

    public void Use(int itemID)
    {
        switch (itemID)
        {
            case 1001: // 목숨 증가
                UseLifeUp();
                break;
            case 1002: // 기지 무적
                UseBaseShield();
                break;
            case 1003: // 나 무적
                UseHyperShield();
                break;
            case 1004: // 적 멈춤
                UseEMPField();
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
    /// 기지 무적
    /// </summary>
    void UseBaseShield()
    {
        OnBaseShield?.Invoke(_baseShieldDuration);
    }

    /// <summary>
    /// 나 무적
    /// </summary>
    void UseHyperShield()
    {
        OnHyperShield?.Invoke(_hyperShieldDuration);
    }

    /// <summary>
    /// 적 멈춤
    /// </summary>
    void UseEMPField()
    {
        OnEMPField?.Invoke(_empFieldDuration);
    }

    /// <summary>
    /// 폭탄
    /// </summary>
    void UseAirSupport()
    {
        OnAirSupport?.Invoke();
    }
}
