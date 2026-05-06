using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

/// <summary>
/// 경전차
/// Rigidbody Mass:0.8
/// </summary>
public class LightTank : EnemyTank, IExplosionDamageable
{
    /// <summary>
    /// 폭발 범위에 대미지 받음
    /// </summary>
    public void TakeHit(HitData hitData, float explosionForce, Vector3 pos)
    {
        _model.TakeDamage(hitData);
    }
}
