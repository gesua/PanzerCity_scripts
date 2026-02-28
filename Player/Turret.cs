using System.Threading;
using UnityEngine;

/// <summary>
/// 탱크 포탑
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _rotSpeed = 100f;

    void Update()
    {
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > Util.Epsilon)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotSpeed * Time.deltaTime);
        }
    }
}
