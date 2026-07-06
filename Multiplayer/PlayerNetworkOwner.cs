using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerTank의 Netcode 소유권을 판별해서 PlayerTank/Turret에 전달하는 브릿지
/// TankBase/PlayerTank 상속 구조를 건드리지 않기 위해 별도 컴포넌트로 분리
/// </summary>
[RequireComponent(typeof(PlayerTank))]
public class PlayerNetworkOwner : NetworkBehaviour
{
    PlayerTank _playerTank;

    void Awake()
    {
        TryGetComponent(out _playerTank);
    }

    public override void OnNetworkSpawn()
    {
        _playerTank.SetNetworkOwnership(IsOwner);

        // 로컬(내) 소유일 때만 GameScene에 스폰 완료를 알림
        if (IsOwner)
        {
            NetworkGameManager.Instance.NotifyLocalPlayerSpawned(_playerTank);
        }
    }
}