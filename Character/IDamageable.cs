using UnityEngine;

/// <summary>
/// 피격 정보
/// </summary>
public struct HitData
{
    public int Damage;       // 대미지 값
    public Vector3 HitPoint; // 피격 위치
    public TankBase AtkTank; // 공격한 탱크
}

/// <summary>
/// 피해를 받을 수 있는 대상
/// </summary>
public interface IDamageable
{
    void TakeHit(HitData hitData);
}
