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

        //Debug.Log(
        //$"OnNetworkSpawn | {name} | " +
        //$"Owner:{IsOwner} | " +
        //$"Pos:{transform.position} | " +
        //$"Active:{gameObject.activeInHierarchy}");

        _playerTank.SetNetworkOwnership(IsOwner);
        _playerTank.SetNetworkOwner(this);

        // 로컬 소유일 때만 GameScene에 스폰 완료를 알림
        if (IsOwner)
        {
            NetworkGameManager.Instance.NotifyLocalPlayerSpawned(_playerTank);
        }
    }


    /// <summary>
    /// 실제 포탄 생성 + 전원에게 연출 신호 전달(서버 전용)
    /// 호스트 자신의 발사(PlayerTank가 직접 호출) / 원격 클라이언트의 발사 요청(RequestAttackServerRpc) 양쪽에서 사용
    /// 위치/회전은 발사자 본인이 보낸 값을 그대로 사용(서버가 자기 쪽에서 다시 읽으면 라운드트립 지연만큼 밀려 보임)
    /// </summary>
    public void HandleAttackOnServer(Vector3 firePosition, Quaternion fireRotation)
    {
        if (IsServer == false) return; // 방어적 가드

        _playerTank.SpawnShellOnServer(firePosition, fireRotation);
        NotifyAttackClientRpc();
    }

    /// <summary>
    /// 비호스트 클라이언트가 발사 입력을 받았을 때 서버에 요청
    /// </summary>
    [ServerRpc]
    public void RequestAttackServerRpc(Vector3 firePosition, Quaternion fireRotation)
    {
        HandleAttackOnServer(firePosition, fireRotation);
    }

    /// <summary>
    /// 서버가 발사 처리를 마친 뒤 전원에게 신호 전달
    /// 발사자 본인은 입력 즉시 로컬에서 이미 재생했으므로 제외
    /// </summary>
    [ClientRpc]
    void NotifyAttackClientRpc()
    {
        if (IsOwner) return;
        _playerTank.PlayLocalAttack();
    }
}