using UnityEngine;

/// <summary>
/// 공격할 수 있는 대상
/// </summary>
public interface IAttackable
{
    bool CanAttack { get; }
    void Attack();
}
