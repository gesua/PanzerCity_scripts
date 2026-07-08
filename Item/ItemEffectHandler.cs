using System;
using UnityEngine;

/// <summary>
/// 아이템 효과 관리
/// </summary>
public class ItemEffectHandler : MonoBehaviour
{
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

    public void Use(ItemConfig config)
    {
        // 아이템 사용 소리
        GameManager.Instance.AudioManager.PlaySfx(SfxType.ItemUse);

        switch (config.Id)
        {
            case 1001: // 목숨 증가
                UseLifeUp();
                break;
            case 1002: // 기지 무적
                UseBaseShield(config.Duration);
                break;
            case 1003: // 나 무적
                UseHyperShield(config.Duration);
                break;
            case 1004: // 적 멈춤
                UseEMPField(config.Duration);
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
    void UseBaseShield(float duration)
    {
        OnBaseShield?.Invoke(duration);
    }

    /// <summary>
    /// 나 무적
    /// </summary>
    void UseHyperShield(float duration)
    {
        OnHyperShield?.Invoke(duration);
    }

    /// <summary>
    /// 적 멈춤
    /// </summary>
    void UseEMPField(float duration)
    {
        OnEMPField?.Invoke(duration);
    }

    /// <summary>
    /// 폭탄
    /// </summary>
    void UseAirSupport()
    {
        OnAirSupport?.Invoke();
    }
}