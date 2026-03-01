using UnityEngine;

/// <summary>
/// 탱크 포탑
/// </summary>
public class Turret : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _turret; // 포탑

    float _rotSpeed; // 포탑 회전 속력

    public void SetRotSpeed(float rotSpeed)
    {
        _rotSpeed = rotSpeed;
    }

    private void Update()
    {
        // 포탑 회전
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        // 0벡터 체크
        if (direction.sqrMagnitude < Mathf.Epsilon) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(_turret.transform.rotation, targetRotation);

        if (angle > Util.Epsilon)
        {
            _turret.transform.rotation = Quaternion.RotateTowards(_turret.transform.rotation, targetRotation, _rotSpeed * Time.deltaTime);
        }
    }
}
