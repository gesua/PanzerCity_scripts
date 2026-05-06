using UnityEngine;

/// <summary>
/// 피격 정보
/// </summary>
public struct HitData
{
    private int _damage;       // 대미지 값
    private Vector3 _hitPoint; // 피격 위치
    private HitZoneType _zoneType; // 피격 방향
    private TankBase _atkTank; // 공격한 탱크

    public int Damage => _damage;
    public Vector3 HitPoint => _hitPoint;
    public HitZoneType ZoneType => _zoneType;
    public TankBase AtkTank => _atkTank;

    public HitData(int damage, Vector3 hitPoint, TankBase atkTank)
    {
        _damage = damage;
        _hitPoint = hitPoint;
        _atkTank = atkTank;
        _zoneType = HitZoneType.None;
    }

    public void setZoneType(HitZoneType zoneType)
    {
        _zoneType = zoneType;
    }

    /// <summary>
    /// 대미지 추가
    /// </summary>
    public void AddDamage(int amount)
    {
        _damage += amount;
    }
}

/// <summary>
/// 피해를 받을 수 있는 대상
/// </summary>
public interface IDamageable
{
    void TakeHit(HitData hitData);
}
