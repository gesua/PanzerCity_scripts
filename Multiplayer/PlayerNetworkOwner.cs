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
        // Lobby 씬이 언로드돼도 파괴되지 않도록 보호(각 컴퓨터에서 로컬로 각자 적용됨)
        DontDestroyOnLoad(gameObject);

        Debug.Log(
        $"OnNetworkSpawn | {name} | " +
        $"Owner:{IsOwner} | " +
        $"Pos:{transform.position} | " +
        $"Active:{gameObject.activeInHierarchy}");

        Debug.Log($"Scene : {gameObject.scene.name}");
        Debug.Log($"InstanceID : {gameObject.GetInstanceID()}");

        Debug.Log(FindObjectsByType<PlayerNetworkOwner>(FindObjectsSortMode.None).Length);

        _playerTank.SetNetworkOwnership(IsOwner);

        // 로컬 소유일 때만 GameScene에 스폰 완료를 알림
        if (IsOwner)
        {
            NetworkGameManager.Instance.NotifyLocalPlayerSpawned(_playerTank);
        }
    }
}