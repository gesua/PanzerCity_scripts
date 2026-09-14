using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 상점 UI에서 플레이어 마우스 커서 위치를 네트워크로 동기화하는 브릿지
/// PlayerNetworkOwner와 마찬가지로 PlayerTank 상속 구조를 건드리지 않기 위해 별도 컴포넌트로 분리
/// NetworkObject로 만들지 않고 값(Vector2, bool)만 동기화 — 이미 스폰된 플레이어 위에 NetworkVariable을 얹는 기존 패턴(_life, _isInvincible)과 동일
/// </summary>
public class PlayerCursorSync : NetworkBehaviour
{
    // 커서 위치:화면 비율(0~1) 좌표 — 해상도가 다른 클라이언트끼리도 정확히 맞아야 해서 픽셀 좌표 대신 정규화 좌표 사용
    NetworkVariable<Vector2> _cursorPosition = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 상점 UI 활성 여부:상점 닫힌 클라이언트의 커서 아이콘을 관찰자 화면에서 숨기기 위한 판별용
    NetworkVariable<bool> _isCursorActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public Vector2 CursorPosition => _cursorPosition.Value; // 관찰자가 조회용으로 사용
    public bool IsCursorActive => _isCursorActive.Value;    // 관찰자가 조회용으로 사용

    void Update()
    {
        if (IsOwner == false) return; // 소유자만 자기 마우스 위치를 씀
        if (_isCursorActive.Value == false) return; // 상점이 열려있을 때만 갱신
        if (Mouse.current == null) return; // 마우스 장치가 없으면 갱신하지 않음(방어적 가드)

        // 화면 비율로 정규화(좌하단 0,0 ~ 우상단 1,1) — 프로젝트가 New Input System 사용 중이라 Input.mousePosition 대신 Mouse.current 사용(좌표계는 동일)
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        _cursorPosition.Value = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
    }

    /// <summary>
    /// 상점 열림/닫힘에 맞춰 커서 활성 상태 갱신(소유자만 호출 가능, ShopUI가 호출)
    /// </summary>
    public void SetCursorActive(bool active)
    {
        if (IsOwner == false) return; // 방어적 가드

        _isCursorActive.Value = active;
    }
}