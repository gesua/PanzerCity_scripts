using System;
using UnityEngine;

/// <summary>
/// 아군 HQ
/// 적에게 1대 맞으면 패배
/// </summary>
public class HQ : MonoBehaviour, IDamageable
{
    [Header("----- 치트 -----")]
    [SerializeField] bool DontDestroy; // 무적

    public event Action OnDestroyed;

    public void TakeDamage(int damage)
    {
        if (DontDestroy) return;
        OnDestroyed?.Invoke();
    }
}