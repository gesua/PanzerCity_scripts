using System;
using UnityEngine;

/// <summary>
/// 아군 HQ
/// 적에게 1대 맞으면 패배
/// </summary>
public class HQ : MonoBehaviour, IDamageable
{
    public event Action OnDestroyed;

    public void TakeDamage(int damage)
    {
        OnDestroyed?.Invoke();
    }
}