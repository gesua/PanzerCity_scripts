using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 드랍 아이템 네트워크 브릿지 컴포넌트
/// 픽업은 클라이언트가 요청하고 서버가 승인하는 구조(동시 픽업 경합 방지)
/// </summary>
[RequireComponent(typeof(DroppedItem))]
public class DroppedItemNetworkOwner : NetworkBehaviour
{
    [SerializeField] DroppedItem _droppedItem;

    bool _isPickedUp; // 중복 픽업 방지(서버 전용 판정, 클라에 노출할 필요 없어서 NetworkVariable 아님)

    // 서버만 쓰기 가능, 전원이 읽음(스폰 값이 그대로 동기화되어 늦게 접속한 클라이언트도 자동 수신)
    NetworkVariable<int> _itemId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// 스폰 전 아이템 ID 설정(ItemDropper가 Spawn() 호출 직전 호출)
    /// </summary>
    public void SetItemId(int itemId)
    {
        if (IsServer == false) return; // 방어적 가드(정상 경로로는 서버에서만 호출됨)
        _itemId.Value = itemId;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) return; // 호스트 자신은 로컬에서 Initialize()가 이미 처리했으므로 중복 방지

        // 클라이언트:아이콘/데이터 동기화(스폰 값이 이미 반영된 상태라 늦게 접속한 클라이언트도 정상 동작)
        ItemConfig itemConfig = GameManager.Instance.DataManager.GetItemConfig(_itemId.Value);
        if (itemConfig == null) return;

        _droppedItem.Initialize(itemConfig);
    }

    /// <summary>
    /// 클라이언트가 픽업 요청 — 소유자가 아닌 임의의 클라이언트도 호출 가능해야 함
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPickupServerRpc(RpcParams rpcParams = default)
    {
        if (_isPickedUp) return; // 이미 처리됨 — 늦게 도착한 요청은 조용히 무시
        _isPickedUp = true;

        // 요청한 클라이언트에게만 결과 통보(다른 클라는 despawn만 보면 됨)
        ClientRpcParams targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
        };
        NotifyPickupApprovedClientRpc(targetParams);

        if (TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Despawn(false); // Pool 재사용을 위해 false
        }
    }

    /// <summary>
    /// 요청자 로컬에서만 실행 — 로컬 플레이어의 ItemPickup에 결과 적용
    /// </summary>
    [ClientRpc]
    void NotifyPickupApprovedClientRpc(ClientRpcParams rpcParams = default)
    {
        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;
        if (localPlayer.TryGetComponent(out PlayerTank playerTank) == false) return;

        playerTank.ItemPickup.ApplyPickupResult(_droppedItem.ItemConfig);
    }

    /// <summary>
    /// 전원(호스트 포함) 공통 정리 — despawn 시점에 자동 호출됨
    /// 픽업 UI 등이 실수로 자식으로 붙어있는 경우 함께 파괴되지 않도록 먼저 분리
    /// </summary>
    public override void OnNetworkDespawn()
    {
        while (transform.childCount > 0)
        {
            transform.GetChild(0).SetParent(null);
        }

        if (TryGetComponent(out Poolable poolable))
        {
            poolable.ReturnToPool();
        }
    }
}