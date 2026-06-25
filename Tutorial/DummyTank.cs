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
    [Header("----- 중전차 관련 -----")]
    [SerializeField] bool _isHeavy; // 중전차 색상 변화 여부
    [SerializeField] Renderer[] _renderers; // 색상 바꿀 렌더러들

    // 바뀔 색 (HeavyTank와 동일)
    Color[] _hpColors =
    {
        new Color(0f, 0.5f, 0f),    // 4/4 초록
        Color.yellow,               // 3/4 노랑
        new Color(1f, 0.5f, 0f),    // 2/4 주황
        Color.red                   // 1/4 빨강
    };


    public event Action OnDummyDead;

    protected override void Awake()
    {
        base.Awake();

        if (_isHeavy)
        {
            // 색 변경
            foreach (Renderer renderer in _renderers)
            {
                renderer.material.color = _hpColors[0];
            }

            _model.OnHpChanged += HandleHpChanged;
        }
    }

    void HandleHpChanged(int current, int max)
    {
        // current 1~4를 인덱스 3~0으로 변환
        int colorIndex = max - current;
        colorIndex = Mathf.Clamp(colorIndex, 0, _hpColors.Length - 1);

        // 색 변경
        foreach (Renderer renderer in _renderers)
        {
            renderer.material.color = _hpColors[colorIndex];
        }
    }

    public override void TakeHit(HitData hitData)
    {
        if (_isHeavy)
        {
            switch (hitData.ZoneType)
            {
                case HitZoneType.None:
                case HitZoneType.Front:
                    break;
                case HitZoneType.Side:
                    hitData.AddDamage(1);
                    break;
                case HitZoneType.Rear:
                    hitData.AddDamage(2);
                    break;
            }
        }

        base.TakeHit(hitData);
    }

    private void OnEnable()
    {
        switch (_mode)
        {
            case DummyTankMode.None:
                break;
            case DummyTankMode.Stationary:
                Initialize();
                _personality = EnemyPersonality.Stationary;
                StartInState(EnemyStateType.Combat);
                break;
            case DummyTankMode.Roam:
                Initialize();
                _personality = EnemyPersonality.Ignore;
                StartAI();
                break;
        }
    }

    /// <summary>
    /// 튜토리얼에선 상태 변환을 막음
    /// </summary>
    public override void ChangeState(EnemyStateType stateType)
    {
        return;
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