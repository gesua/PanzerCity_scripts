using UnityEngine;

/// <summary>
/// 구축전차
/// Rigidbody Mass:0.9
/// </summary>
public class DestroyerTank : EnemyTank
{
    public override void AimAtTarget()
    {
        if (Target == null) return;

        Vector3 dir = Target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < Mathf.Epsilon) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _model.RotSpeed * Time.deltaTime);
    }
}
