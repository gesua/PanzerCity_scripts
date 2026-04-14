using UnityEngine;

/// <summary>
/// 폭발범위 피해를 받을 수 있는 대상(경전차, 벽)
/// </summary>
public interface IExplosionDamageable
{
    /// <summary>
    /// 피해 입음
    /// </summary>
    /// <param name="damage">입는 피해량</param>
    /// <param name="explosionForce">폭발력</param>
    void TakeDamage(int damage, float explosionForce);
}
