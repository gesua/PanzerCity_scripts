using UnityEngine;

/// <summary>
/// 경전차
/// Rigidbody Mass:0.8
/// </summary>
public class LightTank : EnemyTank, IExplosionDamageable
{
    /// <summary>
    /// 폭발 범위에 대미지 받음
    /// </summary>
    public void TakeDamage(int damage, float explosionForce, Vector3 pos)
    {
        Debug.Log("폭발 대미지 들어옴");

        _model.TakeDamage(damage);
    }
}
