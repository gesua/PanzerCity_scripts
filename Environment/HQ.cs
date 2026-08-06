using System;
using Unity.Cinemachine;
using Unity.Netcode;
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

    public void TakeHit(ref HitData hitData)
    {
        if (hitData.IsPlayerAttack) return; // 아군이 직접 못 부수게 함(AtkTank는 RPC로 못 넘어가니 IsPlayerAttack 사용)

        if (_isDestroy) return;

        // 방어적 가드(정상 경로로는 클라이언트에서 호출될 일이 없음 — Shell.OnTriggerEnter가 이미 서버 전용)
        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (isMultiplayer && NetworkManager.Singleton.IsServer == false) return;

        TriggerDestruction();

        if (isMultiplayer)
        {
            NetworkGameManager.Instance.NotifyHQDestroyed();
        }
    }

    /// <summary>
    /// HQ 파괴 연출 + 이벤트 발행
    /// 서버는 판정 통과 직후 로컬로 직접 호출, 클라이언트는 NotifyHQDestroyedClientRpc로 재현(각자 로컬로 카메라 전환/시간 정지 적용)
    /// </summary>
    public void TriggerDestruction()
    {
        if (_isDestroy) return; // 중복 방지(클라 재현 경로 포함)

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

        // 파괴음 재생
        GameManager.Instance.AudioManager.PlaySfx(SfxType.HQDestroy);

        GameManager.Instance.EffectManager.SpawnEffect(EffectType.StatueExplosion, transform.position + Vector3.up * 2.5f);
    }
}