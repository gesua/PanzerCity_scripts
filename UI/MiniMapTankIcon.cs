using UnityEditor.Rendering;
using UnityEngine;

/// <summary>
/// 미니맵 아이콘 관리
/// 현재는 포탑만 연동해서 돌리고 있음
/// </summary>
public class MiniMapTankIcon : MonoBehaviour
{
    [SerializeField] Transform _turretIcon; // 포탑 아이콘
    [SerializeField] PlayerTank _player;    // 플레이어

    void Update()
    {
        // 차체 기준 포탑 상대 각도
        float relativeTurretY = _player.TurretTr.eulerAngles.y - transform.eulerAngles.y;
        _turretIcon.localEulerAngles = new Vector3(0f, 0f, relativeTurretY);
    }
}
