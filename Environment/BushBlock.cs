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
        if (other.TryGetComponent(out PlayerTank player))
        {
            _group.OnPlayerEnter();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerTank player))
        {
            _group.OnPlayerExit();
        }
    }
}
