using UnityEngine;

/// <summary>
/// 맞는 부위 방향
/// </summary>
public enum HitZoneType
{
    None,   // HACK:모든 탱크에 콜라이더 달아놔야함(스테이지 루프할 때 체력 증가함)
    Front,  // 정면
    Side,   // 측면
    Rear,   // 후면
}

/// <summary>
/// 피격 부위 콜라이더
/// </summary>
public class HitZone : MonoBehaviour, IDamageable
{
    [SerializeField] GameObject _parent;
    [SerializeField] HitZoneType _zoneType;
    IDamageable _parentDamageable;

    void Awake()
    {
        _parentDamageable = _parent.GetComponent<IDamageable>();
    }

    public void TakeHit(HitData hitData)
    {
        Debug.Log($"{transform.parent.name}의 {transform.name} 부위 피격");

        hitData.setZoneType(_zoneType);
        _parentDamageable.TakeHit(hitData);
    }
}
