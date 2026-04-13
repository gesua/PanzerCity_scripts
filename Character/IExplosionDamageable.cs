using UnityEngine;

/// <summary>
/// 폭발 범위 피해를 받을 수 있는 대상(경전차, 벽)
/// </summary>
public interface IExplosionDamageable
{
    void TakeDamage(int damage);
}
