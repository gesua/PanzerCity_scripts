using System;
using UnityEngine;

/// <summary>
/// 아군 HQ
/// 적에게 1대 맞으면 패배
/// </summary>
public class HQ : MonoBehaviour, IDamageable
{
    public event Action OnDestroyed;
    [SerializeField] Renderer _iconRenderer; // HQ 아이콘
    [SerializeField] Material _destroyedMaterial; // 파괴된 HQ용 머티리얼

    bool _isDestroy;

    public void TakeDamage(int damage)
    {
        if (_isDestroy) return;
        _isDestroy = true;

        _iconRenderer.material = _destroyedMaterial;
        OnDestroyed?.Invoke();
    }
}