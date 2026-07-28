using UnityEngine;

/// <summary>
/// 미니맵 아이콘 관리
/// 포탑 연동해서 돌리고 있음
/// </summary>
public class MiniMapTankIcon : MonoBehaviour
{
    [SerializeField] Transform _turretIcon; // 포탑 아이콘
    [SerializeField] PlayerTank _player;    // 플레이어
    [Header("----- 구분 색상 -----")]
    [SerializeField] SpriteRenderer _hullIconRenderer;   // 차체 아이콘 렌더러
    [SerializeField] SpriteRenderer _turretIconRenderer; // 포탑 아이콘 렌더러

    Color[] _playerColors = // 1P~4P 구분 색, OwnerClientId를 인덱스로 사용
    {
        new Color(1f, 1f, 1f),       // 1P 기본(초록)
        new Color(0f, 0f, 1f),       // 2P 파랑
        new Color(0.2f, 0.2f, 0.2f), // 3P 회색
        new Color(1f, 1f, 0f)        // 4P 노랑
    };


    /// <summary>
    /// 아이콘 표시
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 아이콘 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 미니맵 아이콘 구분 색상 적용(PlayerTank가 몸체 색과 함께 전파)
    /// </summary>
    public void SetIconColorByIndex(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= _playerColors.Length) return;

        Color color = _playerColors[colorIndex];

        _hullIconRenderer.color = color;
        _turretIconRenderer.color = color;
    }

    void Update()
    {
        // 차체 기준 포탑 상대 각도
        float relativeTurretY = _player.TurretTr.eulerAngles.y - transform.eulerAngles.y;
        _turretIcon.localEulerAngles = new Vector3(0f, 0f, relativeTurretY);
    }
}