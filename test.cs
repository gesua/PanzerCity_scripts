using UnityEngine;

public class test : MonoBehaviour
{
    [SerializeField] bool isActive = false;
    [SerializeField] Rigidbody rigid;
    [SerializeField] float speed = 10f;

    void Update()
    {
        if (isActive)
        {
            if(rigid == null)
            {
                rigid = GetComponent<Rigidbody>();
                return;
            }
            rigid.AddTorque(Vector3.up * speed, ForceMode.Acceleration);
        }
    }
}
