using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 포탄 네트워크 브릿지 컴포넌트
/// 서버만 생존시간/충돌 판정을 하고, 클라이언트는 NetworkTransform(또는 NetworkRigidbody)으로 위치만 받음
/// </summary>
[RequireComponent(typeof(Shell))]
public class ShellNetworkOwner : NetworkBehaviour
{
    [SerializeField] Shell _shell;

    public override void OnNetworkSpawn()
    {
        // 서버만 생존시간/충돌 판정 주체
        _shell.SetNetworkControl(IsServer);

        // 발사/피격 신호 전달용 참조 세팅
        _shell.SetNetworkOwner(this);
    }

    /// <summary>
    /// 서버가 폭발 이펙트/사운드 재생 신호를 전원에게 전달(호스트 자신도 포함해서 받음)
    /// </summary>
    [ClientRpc]
    public void NotifyExplosionEffectClientRpc(bool hitTankOrHQ)
    {
        _shell.PlayExplosionEffect(hitTankOrHQ);
    }

    /// <summary>
    /// 클라이언트에도 직격 판정을 재현하도록 NetworkGameManager에 신호 전달
    /// 피격 콜라이더(HitZone)에서 NetworkObject를 탐색
    /// </summary>
    public void NotifyDirectHitDamage(Collider hitCollider, HitData hitData)
    {
        NetworkObject targetNetworkObject = hitCollider.GetComponentInParent<NetworkObject>();
        if (targetNetworkObject != null)
        {
            NetworkGameManager.Instance.NotifyDirectHitDamage(
                targetNetworkObject.NetworkObjectId, hitData.Damage, hitData.IsPlayerAttack, transform.position);
        }
    }

    /// <summary>
    /// 클라이언트에도 폭발 범위 피해 판정을 재현하도록 NetworkGameManager에 신호 전달
    /// </summary>
    public void NotifyExplosionDamage(float explosionRadius, LayerMask hitLayer, HitData hitData)
    {
        NetworkGameManager.Instance.NotifyExplosionDamage(
            transform.position, explosionRadius, hitLayer.value, hitData.Damage, hitData.IsPlayerAttack);
    }

    /// <summary>
    /// 서버만 네트워크 디스폰(destroy: false → GameObject는 유지해서 Pool 재사용)
    /// ReturnAllPools() 등으로 클라이언트에서 호출될 수도 있으므로 IsServer 가드 필요
    /// </summary>
    public void RequestDespawn()
    {
        if (IsServer == false) return;

        if (TryGetComponent(out NetworkObject networkObject) == false) return;

        // 이미 despawn 처리 중(또는 완료)인 상태면 재시도하지 않음(DroppedItem과 동일한 이유의 방어)
        if (networkObject.IsSpawned == false)
        {
            Debug.Log($"[NetTrace] DESPAWN-SKIP(이미 처리됨) {name} id={networkObject.NetworkObjectId} frame={Time.frameCount}");
            return;
        }

        Debug.Log($"[NetTrace] DESPAWN {name} id={networkObject.NetworkObjectId} isSpawned={networkObject.IsSpawned} frame={Time.frameCount}");
        networkObject.Despawn(false);
    }

    /// <summary>
    /// 클라이언트 전용 Pool 반환 — despawn 시점에 destroy 값과 무관하게 항상 호출됨
    /// 서버는 Remove() → OnBeforeReturnToPool() → RequestDespawn() 경로에서 이미 처리하므로 제외
    /// (destroy:false라 INetworkPrefabInstanceHandler.Destroy()는 호출되지 않아, 클라이언트 반환은 여기서 별도 처리해야 함)
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (IsServer) return;

        if (TryGetComponent(out Poolable poolable))
        {
            poolable.ReturnToPool();
        }
    }
}