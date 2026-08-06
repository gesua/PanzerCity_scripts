using UnityEngine;

/// <summary>
/// 맞는 부위 방향
/// </summary>
public enum HitZoneType
{
    None,
    Front,  // 정면
    Side,   // 측면
    Rear,   // 후면
}

/// <summary>
/// 피격 부위 콜라이더
/// </summary>
public class HitZone : MonoBehaviour, IDamageable
{
    [SerializeField] TankBase _parent;
    [SerializeField] HitZoneType _zoneType;

    public TankBase Parent => _parent;
    public HitZoneType ZoneType => _zoneType;

    public void TakeHit(ref HitData hitData)
    {
        hitData.setZoneType(_zoneType);
        _parent.TakeHit(ref hitData);
    }
}
