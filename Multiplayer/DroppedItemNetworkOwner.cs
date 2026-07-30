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
    int _pendingItemId; // 스폰 전 임시 저장(일반 필드라 스폰 타이밍 제약이 없음)

    // 서버만 쓰기 가능, 전원이 읽음(스폰 시점 값이 동기화되고 늦게 접속한 클라이언트도 자동 수신)
    NetworkVariable<int> _itemId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// 스폰 전 아이템 ID 임시 저장(ItemDropper가 Spawn() 호출 직전 호출)
    /// NetworkVariable은 스폰 전에 쓰면 안 되므로, 일반 필드에 담아뒀다가 OnNetworkSpawn에서 옮겨 담음
    /// </summary>
    public void SetPendingItemId(int itemId)
    {
        _pendingItemId = itemId;
    }

    public override void OnNetworkSpawn()
    {
        // 서버:스폰된 시점(IsServer가 유효해진 시점)에 NetworkVariable로 옮겨 담음
        if (IsServer)
        {
            _itemId.Value = _pendingItemId;
        }

        // 초기 스폰 메시지에 값이 아직 안 실렸을 수 있어 변경 이벤트도 함께 구독(공식 권장 패턴)
        _itemId.OnValueChanged += HandleItemIdChanged;
        ApplyItemConfig(_itemId.Value);
    }

    void HandleItemIdChanged(int previousValue, int currentValue)
    {
        ApplyItemConfig(currentValue);
    }

    void ApplyItemConfig(int itemId)
    {
        if (IsServer) return; // 호스트 자신은 로컬에서 Initialize()가 이미 처리했으므로 중복 방지
        ItemConfig itemConfig = GameManager.Instance.DataManager.GetItemConfig(itemId);

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
        _itemId.OnValueChanged -= HandleItemIdChanged; // 풀 재사용 시 중복 구독 방지

        _droppedItem.DetachForeignChildren(); // 픽업 UI 등 외부에서 붙은 자식만 분리(아이콘 등 원본 자식은 보존)

        if (TryGetComponent(out Poolable poolable))
        {
            poolable.ReturnToPool();
        }
    }
}