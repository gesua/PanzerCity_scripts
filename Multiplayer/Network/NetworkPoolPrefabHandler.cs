using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 풀링된 NetworkObject의 클라이언트 측 스폰/디스폰을 NGO 기본 동작 대신 Pool 시스템으로 위임
/// (등록 안 하면 클라이언트 사본이 Pool.CreatePoolObj()를 거치지 않아 Poolable._pool이 null로 남고,
/// 결국 Destroy(gameObject)로 빠지는데 이는 NGO가 명시적으로 금지하는 동작임 —
/// "you should never call Object.Destroy on any GameObject with a NetworkObject component")
/// 서버(authority)는 기존 방식(ItemDropper가 직접 Pool에서 꺼내 Spawn) 그대로 유지되고,
/// 이 핸들러는 비authority 클라이언트의 인스턴스 생성/제거에만 적용됨
/// </summary>
public class NetworkPoolPrefabHandler : INetworkPrefabInstanceHandler
{
    readonly string _poolKey;

    public NetworkPoolPrefabHandler(string poolKey)
    {
        _poolKey = poolKey;
    }

    /// <summary>
    /// 비authority 클라이언트 전용:서버 스폰 메시지를 받아 새 인스턴스가 필요할 때 NGO가 호출
    /// </summary>
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        GameObject go = GameManager.Instance.PoolManager.GetFromPool(_poolKey);
        go.transform.SetPositionAndRotation(position, rotation);

        go.TryGetComponent(out NetworkObject networkObject);
        return networkObject;
    }

    /// <summary>
    /// 비authority 클라이언트 전용:디스폰 시 파괴 대신 풀로 반환
    /// (DroppedItemNetworkOwner.OnNetworkDespawn()에서도 동일하게 호출하지만,
    /// Pool.Push()의 중복 반환 가드가 있어 두 경로 모두 안전함)
    /// </summary>
    public void Destroy(NetworkObject networkObject)
    {
        if (networkObject.TryGetComponent(out Poolable poolable))
        {
            poolable.ReturnToPool();
        }
    }
}