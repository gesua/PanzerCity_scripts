using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

/// <summary>
/// 폭발범위 피해를 받을 수 있는 대상(경전차, 벽)
/// </summary>
public interface IExplosionDamageable
{
    /// <summary>
    /// 피해 입음
    /// </summary>
    /// <param name="hitData">Hit정보</param>
    /// <param name="explosionForce">폭발력</param>
    /// <param name="pos">폭발 위치</param>
    void TakeHit(HitData hitData, float explosionForce, Vector3 pos);
}
