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

    public void TakeHit(HitData hitData)
    {
        Debug.Log($"{transform.parent.name}의 {transform.name} 부위 피격");

        hitData.setZoneType(_zoneType);
        _parent.TakeHit(hitData);
    }
}
