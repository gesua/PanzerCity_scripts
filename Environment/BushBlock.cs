using UnityEngine;

public class BushBlock : MonoBehaviour
{
    BushGroup _group;

    void Awake()
    {
        _group = GetComponentInParent<BushGroup>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out TankBase tank))
        {
            _group.OnPlayerEnter(tank);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out TankBase tank))
        {
            _group.OnPlayerExit(tank);
        }
    }
}
