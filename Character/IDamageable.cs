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
    private bool _isPlayerAttack; // 공격자가 플레이어인지(AtkTank 참조 없이도 판정 가능하게, 멀티플레이 RPC 전달용)

    public int Damage => _damage;
    public Vector3 HitPoint => _hitPoint;
    public HitZoneType ZoneType => _zoneType;
    public TankBase AtkTank => _atkTank;
    public bool IsPlayerAttack => _isPlayerAttack;

    /// <summary>
    /// 싱글플레이
    /// </summary>
    public HitData(int damage, Vector3 hitPoint, TankBase atkTank)
    {
        _damage = damage;
        _hitPoint = hitPoint;
        _atkTank = atkTank;
        _zoneType = HitZoneType.None;
        _isPlayerAttack = (atkTank is PlayerTank);
    }

    /// <summary>
    /// 멀티플레이:AtkTank 참조 없이 재구성할 때 사용(RPC로 전달받은 클라이언트가 로컬로 HitData를 다시 만들 때)
    /// </summary>
    public HitData(int damage, Vector3 hitPoint, bool isPlayerAttack)
    {
        _damage = damage;
        _hitPoint = hitPoint;
        _atkTank = null;
        _zoneType = HitZoneType.None;
        _isPlayerAttack = isPlayerAttack;
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