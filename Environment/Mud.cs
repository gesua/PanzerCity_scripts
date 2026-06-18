using UnityEngine;

/// <summary>
/// 진흙 타일
/// 위를 지나가는 탱크의 차체 이동 속도(전진/후진/회전)를 느리게 만듦
/// </summary>
public class Mud : MonoBehaviour
{
    [SerializeField] float _speedMultiplier = 0.5f; // 적용할 속도 배율

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out TankBase tank))
        {
            tank.OnMudEnter(_speedMultiplier);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out TankBase tank))
        {
            tank.OnMudExit();
        }
    }
}
