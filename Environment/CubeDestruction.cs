using UnityEngine;

public class CubeDestruction : MonoBehaviour
{
    Rigidbody _rigid;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    public void Destruction()
    {
        _rigid.isKinematic = false; // 물리 활성화
    }
}
