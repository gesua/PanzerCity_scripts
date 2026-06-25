using UnityEngine;

/// <summary>
/// 튜토리얼용 더미 탱크
/// </summary>
public class DummyTank : EnemyTank
{
    protected override void HandleDead(HitData hitData)
    {
        Debug.Log("더미 탱크 사망");

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
    }
}
