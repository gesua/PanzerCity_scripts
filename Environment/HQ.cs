using System;
using Unity.Cinemachine;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

/// <summary>
/// 아군 HQ
/// 적에게 1대 맞으면 패배
/// </summary>
public class HQ : MonoBehaviour, IDamageable
{
    public event Action OnDestroyed;
    [SerializeField] Renderer _iconRenderer; // HQ 아이콘
    [SerializeField] Material _destroyedMaterial; // 파괴된 HQ용 머티리얼
    [SerializeField] CinemachineCamera _hqCamera; // 파괴 모습 보여주는 카메라

    bool _isDestroy;

    public void TakeHit(HitData hitData)
    {
        if (_isDestroy) return;
        _isDestroy = true;

        Time.timeScale = 0f; // 카메라 전환될 동안 시간 멈추기

        _hqCamera.enabled = true; // 카메라 전환

        _iconRenderer.material = _destroyedMaterial;
        OnDestroyed?.Invoke();

        //GameManager.Instance.EffectSpawner.SpawnEffect(EffectType.TinyExplosion, _firePoint.position);
    }
}