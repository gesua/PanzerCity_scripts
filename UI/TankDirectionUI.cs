using UnityEngine;

/// <summary>
/// 왼쪽 아래에 차체와 포탑 방향 보여주는 UI
/// </summary>
public class TankDirectionUI : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] RectTransform _hullDirection;   // 차체 방향
    [SerializeField] RectTransform _turretDirection; // 포탑 방향
    [SerializeField] PlayerTank _player; // 플레이어
    Transform _cameraTr;

    private void Awake()
    {
        _cameraTr = Camera.main.transform;
    }

    void Update()
    {
        // 카메라 보는 방향을 위쪽으로 함
        float cameraY = _cameraTr.eulerAngles.y;

        // 차체 회전값 (y축 회전을 z축 마이너스로)
        float hullY = _player.transform.eulerAngles.y - cameraY;
        _hullDirection.localEulerAngles = new Vector3(0f, 0f, -hullY);

        // 포탑 회전값
        float turretY = _player.TurretTr.eulerAngles.y - cameraY;
        _turretDirection.localEulerAngles = new Vector3(0f, 0f, -turretY);
    }

    public void SetActive(bool active)
    {
        enabled = active;
    }
}