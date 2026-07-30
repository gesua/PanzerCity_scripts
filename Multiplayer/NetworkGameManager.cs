using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 멀티플레이 게임 관리 — 플레이어 스폰 담당
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    [SerializeField] GameObject _playerPrefab;

    // 모든 클라이언트의 씬 로드 완료 여부
    bool _waitForSceneLoaded;

    StageScene _stageScene;

    public static NetworkGameManager Instance { get; private set; }
    public StageScene StageScene => _stageScene;

    public event Action<PlayerTank> OnLocalPlayerSpawned; // 로컬 플레이어 스폰 완료 알림
    public event Action OnAllClientsReady; // 모든 클라이언트 씬 로드 완료(로딩창/적 스폰 동시 시작용, 로컬 신호)
    public event Action<int, int> OnPlayerLifeChanged; // 목숨 UI 갱신용(playerIndex, life)

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 스테이지 준비 완료 — GameScene이 명시적으로 호출
    /// (GameScene과 NetworkGameManager가 각자 sceneLoaded를 구독하면 실행 순서가 보장되지 않아서
    /// 씬 로드 이벤트에 의존하지 않고 직접 호출받는 방식으로 처리)
    /// </summary>
    public void OnStageReady(StageScene stage)
    {
        // 서버/클라이언트 공통 — 각자 로컬 StageScene 참조(클라이언트도 RPC 수신 시 자기 EnemySpawner에 접근해야 함)
        _stageScene = stage;

        // 서버만 플레이어 스폰 대기 처리
        if (IsServer == false) return;

        _waitForSceneLoaded = true;
    }

    /// <summary>
    /// 모든 클라이언트가 씬 로드를 완료하면 호출
    /// </summary>
    private void OnLoadEventCompleted(
        string sceneName,               // 어떤 씬이 완료됐는지
        LoadSceneMode loadSceneMode,    // 로드 모드
        List<ulong> clientsCompleted,   // 완료한 클라이언트 ID 목록
        List<ulong> clientsTimedOut)    // 시간 초과로 완료 못한 클라이언트 ID 목록
    {
        // 서버만 처리
        if (IsServer == false) return;

        // Stage 준비를 기다리는 중이 아니면 무시
        if (_waitForSceneLoaded == false) return;

        // Stage 씬만 처리
        if (_stageScene == null || sceneName != _stageScene.gameObject.scene.name)
        {
            return;
        }

        _waitForSceneLoaded = false;

        SpawnAllPlayers();

        // 전원 씬 로드 완료 시점 — 로딩창 종료 및 적 스폰을 동시에 시작하라는 신호
        NotifyAllClientsReadyClientRpc();
    }

    /// <summary>
    /// 모든 클라이언트에게 스테이지 시작 신호 전달(호스트 포함 전원에게 전달됨)
    /// </summary>
    [ClientRpc]
    void NotifyAllClientsReadyClientRpc()
    {
        OnAllClientsReady?.Invoke();
    }

    /// <summary>
    /// 적 스폰 UI 동기화 — 서버가 적을 스폰할 때마다 호출(EnemySpawner가 호출)
    /// 스폰 리스트 자체(OnSpawnListReady)는 클라이언트도 로컬로 동일하게 계산 가능해서 이미 정상 동작하지만,
    /// 개별 스폰 시점(OnEnemySpawned, UI 아이콘 제거용)은 서버만 알 수 있어서 별도 전달이 필요함
    /// </summary>
    public void NotifyEnemySpawned(int spawnedIndex)
    {
        NotifyEnemySpawnedClientRpc(spawnedIndex);
    }

    [ClientRpc]
    void NotifyEnemySpawnedClientRpc(int spawnedIndex)
    {
        // 호스트 자신은 서버 로컬에서 EnemySpawner.OnEnemySpawned가 이미 직접 발행했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.EnemySpawner.ReceiveEnemySpawned(spawnedIndex);
    }

    /// <summary>
    /// HQ 파괴 동기화 — 서버가 판정을 마친 뒤 호출(HQ가 호출)
    /// </summary>
    public void NotifyHQDestroyed()
    {
        NotifyHQDestroyedClientRpc();
    }

    [ClientRpc]
    void NotifyHQDestroyedClientRpc()
    {
        // 호스트 자신은 서버 로컬에서 HQ.TakeHit()이 이미 직접 처리했으므로 중복 방지
        if (IsServer) return;

        if (_stageScene == null) return;
        _stageScene.HQ.TriggerDestruction();
    }

    /// <summary>
    /// 포탄 폭발 범위 피해 동기화 — 서버가 폭발 판정을 마친 뒤 호출(Shell이 호출)
    /// 벽/큐브 파괴, 폭발 피해를 받는 대상(경전차 등)의 판정을 클라이언트에도 동일하게 재현시킴
    /// HitData의 AtkTank(MonoBehaviour 참조)는 RPC로 못 보내서 isPlayerAttack(bool)만 별도 전달
    /// </summary>
    public void NotifyExplosionDamage(Vector3 position, float radius, int hitLayerValue, int damage, bool isPlayerAttack)
    {
        NotifyExplosionDamageClientRpc(position, radius, hitLayerValue, damage, isPlayerAttack);
    }

    [ClientRpc]
    void NotifyExplosionDamageClientRpc(Vector3 position, float radius, int hitLayerValue, int damage, bool isPlayerAttack)
    {
        // 호스트 자신은 서버 로컬에서 Shell.Explode()가 이미 직접 판정했으므로 중복 방지
        if (IsServer) return;

        HitData hitData = new HitData(damage, position, isPlayerAttack);
        LayerMask hitLayer = hitLayerValue;
        Shell.ApplyExplosionDamage(position, radius, hitLayer, hitData);
    }

    /// <summary>
    /// 포탄 직격 데미지 동기화 — 서버가 직격 판정을 마친 뒤 호출(Shell이 호출)
    /// 폭발 판정과 달리 특정 대상 하나만 지정해야 해서, 위치 재탐색 대신 NetworkObjectId로 대상을 직접 지정함
    /// HitData의 AtkTank(MonoBehaviour 참조)는 RPC로 못 보내서 isPlayerAttack(bool)만 별도 전달
    /// </summary>
    public void NotifyDirectHitDamage(ulong targetNetworkObjectId, int damage, bool isPlayerAttack, Vector3 hitPosition)
    {
        NotifyDirectHitDamageClientRpc(targetNetworkObjectId, damage, isPlayerAttack, hitPosition);
    }

    [ClientRpc]
    void NotifyDirectHitDamageClientRpc(ulong targetNetworkObjectId, int damage, bool isPlayerAttack, Vector3 hitPosition)
    {
        // 호스트 자신은 서버 로컬에서 Shell.OnTriggerEnter()가 이미 직접 판정했으므로 중복 방지
        if (IsServer) return;

        // 대상이 이미 디스폰/파괴됐으면 무시(과거 신호가 뒤늦게 도착한 경우 방어)
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject) == false) return;

        // HitZone의 IDamageable 가져옴
        IDamageable damageable = targetObject.GetComponentInChildren<IDamageable>();
        if (damageable == null) return;

        HitData hitData = new HitData(damage, hitPosition, isPlayerAttack);
        damageable.TakeHit(hitData);
    }

    /// <summary>
    /// 목숨 UI 동기화 — 각 클라이언트가 자기 로컬에서 NetworkVariable 변경을 감지해서 호출(PlayerNetworkOwner가 호출)
    /// NetworkVariable 자체가 이미 네트워크 동기화를 처리하므로 여기서는 로컬 이벤트 발행만 담당
    /// </summary>
    public void NotifyLifeChanged(int playerIndex, int life)
    {
        OnPlayerLifeChanged?.Invoke(playerIndex, life);
    }

    /// <summary>
    /// 연결된 모든 클라이언트에 플레이어 스폰
    /// </summary>
    void SpawnAllPlayers()
    {
        int index = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId, index);
            index++;
        }
    }

    /// <summary>
    /// 해당 클라이언트에 플레이어 스폰
    /// </summary>
    void SpawnPlayer(ulong clientId, int spawnIndex)
    {
        if (_playerPrefab == null)
        {
            Debug.LogWarning("PlayerPrefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = _stageScene.GetSpawnPoint(spawnIndex);
        GameObject player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);

        if (player.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.SpawnAsPlayerObject(clientId);
        }
    }

    /// <summary>
    /// 로컬 플레이어 스폰 완료 알림(PlayerNetworkOwner가 호출)
    /// </summary>
    public void NotifyLocalPlayerSpawned(PlayerTank player)
    {
        OnLocalPlayerSpawned?.Invoke(player);
    }
}