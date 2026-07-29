using UnityEngine;

/// <summary>
/// 미니맵 아이콘 관리
/// 포탑 연동해서 돌리고 있음
/// </summary>
public class MiniMapTankIcon : MonoBehaviour
{
    [SerializeField] Transform _turretIcon; // 포탑 아이콘
    [SerializeField] PlayerTank _player;    // 플레이어
    [Header("----- 구분 아이콘 -----")]
    [SerializeField] SpriteRenderer _hullIconRenderer;   // 차체 아이콘 렌더러
    [SerializeField] SpriteRenderer _turretIconRenderer; // 포탑 아이콘 렌더러
    [SerializeField] PlayerIconSprites[] _playerIcons;   // 1P ~ 4P 차체 + 포탑 아이콘 세트, OwnerClientId를 인덱스로 사용

    [System.Serializable]
    class PlayerIconSprites
    {
        public Sprite Hull;   // 차체 아이콘
        public Sprite Turret; // 포탑 아이콘
    }

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
    /// 미니맵 아이콘 구분 스프라이트 적용(PlayerTank가 호출)
    /// </summary>
    public void SetIconSpriteByIndex(int spriteIndex)
    {
        if (spriteIndex < 0 || spriteIndex >= _playerIcons.Length) return;

        PlayerIconSprites icons = _playerIcons[spriteIndex];

        _hullIconRenderer.sprite = icons.Hull;
        _turretIconRenderer.sprite = icons.Turret;
    }

    void Update()
    {
        // 차체 기준 포탑 상대 각도
        float relativeTurretY = _player.TurretTr.eulerAngles.y - transform.eulerAngles.y;
        _turretIcon.localEulerAngles = new Vector3(0f, 0f, relativeTurretY);
    }
}