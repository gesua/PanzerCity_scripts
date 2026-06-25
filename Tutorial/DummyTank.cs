using System;
using UnityEngine;

public enum DummyTankMode
{
    None,       // 안 움직이고, 안 쏨
    Stationary, // 안 움직이고, 쏨
    Roam,       // 움직이고, 안 쏨
}

/// <summary>
/// 튜토리얼용 더미 탱크
/// </summary>
public class DummyTank : EnemyTank
{
    [SerializeField] DummyTankMode _mode;

    public event Action OnDummyDead;

    private void OnEnable()
    {
        switch (_mode)
        {
            case DummyTankMode.None:
                break;
            case DummyTankMode.Stationary:
                Initialize();
                _personality = EnemyPersonality.Stationary;
                StartAI();
                break;
            case DummyTankMode.Roam:
                Initialize();
                _personality = EnemyPersonality.Ignore;
                StartAI();
                break;
        }
    }

    public override void ChangeState(EnemyStateType stateType)
    {
        // Stationary/Roam 모드에서 Idle 복귀 차단
        if (_mode != DummyTankMode.None && stateType == EnemyStateType.Idle) return;

        base.ChangeState(stateType);
    }

    protected override void HandleDead(HitData hitData)
    {
        // 엔진 끄기
        SetEngineEffect(false);

        // 폭발 이펙트 재생
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.SmallExplosion, Turret.position);

        // 바닥에 잔불 이펙트 생성
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.TinyFlames, transform.position);

        // 사망 상태로 변경
        _collider.enabled = false; // 콜라이더 비활성화
        ChangeState(EnemyStateType.Dead);

        // 사망 효과 재생
        _destructionEffect.Play();

        OnDummyDead?.Invoke();
    }
}