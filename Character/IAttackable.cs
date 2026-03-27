using UnityEngine;

public interface IAttackable
{
    bool CanAttack { get; }
    void Attack();
}
