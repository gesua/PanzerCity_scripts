using UnityEngine;

/// <summary>
/// 탱크 포탑
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _turret; // 포탑

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _rotSpeed = 100f; // 포탑 회전 속력

    void Update()
    {
        // 포탑 회전
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > Util.Epsilon)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _turret.transform.rotation = Quaternion.RotateTowards(_turret.transform.rotation, targetRotation, _rotSpeed * Time.deltaTime);
        }
    }
}
