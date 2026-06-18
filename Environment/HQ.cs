using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 아군 HQ (원래는 조각상이 아니라 건물로 하려 했음)
/// 적에게 1대 맞으면 패배
/// </summary>
public class HQ : MonoBehaviour, IDamageable
{
    public event Action OnDestroyed;
    [SerializeField] GameObject _normalStatue; // 기본 조각상
    [SerializeField] GameObject _brokenStatue; // 파괴된 조각상
    [SerializeField] Renderer _iconRenderer; // HQ 아이콘
    [SerializeField] Material _destroyedMaterial; // 파괴된 HQ 아이콘 머터리얼
    [SerializeField] CinemachineCamera _hqCamera; // 파괴 모습 보여주는 카메라

    bool _isDestroy;

    public void TakeHit(HitData hitData)
    {
        if (hitData.AtkTank is PlayerTank) return; // 아군이 직접 못 부수게 함

        if (_isDestroy) return;
        _isDestroy = true;

        Time.timeScale = 0f; // 카메라 전환될 동안 시간 멈추기

        _hqCamera.enabled = true; // 카메라 전환

        _iconRenderer.material = _destroyedMaterial;
        OnDestroyed?.Invoke();

        Invoke(nameof(TriggerDestructionEffect), 0.1f);
    }

    /// <summary>
    /// 폭발 이펙트 재생
    /// </summary>
    void TriggerDestructionEffect()
    {
        // 조각상 모델 교체
        _normalStatue.SetActive(false);
        _brokenStatue.SetActive(true);

        GameManager.Instance.EffectManager.SpawnEffect(EffectType.StatueExplosion, transform.position + Vector3.up * 2.5f);
    }
}