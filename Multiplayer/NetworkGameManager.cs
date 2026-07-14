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
    [SerializeField] GameObject _playerPrefab; // NetworkObject + PlayerTank 포함 프리팹 (4단계에서 연결)

    // 모든 클라이언트의 씬 로드 완료 여부
    bool _waitForSceneLoaded;

    // 씬 로드 완료한 클라이언트 목록
    readonly HashSet<ulong> _loadedClients = new();

    public static NetworkGameManager Instance { get; private set; }

    StageScene _stageScene;

    public event Action<PlayerTank> OnLocalPlayerSpawned; // 로컬 플레이어 스폰 완료 알림(GameScene이 구독)

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

    /// <summary>
    /// 스테이지 준비 완료 — GameScene이 명시적으로 호출
    /// (GameScene과 NetworkGameManager가 각자 sceneLoaded를 구독하면 실행 순서가 보장되지 않아서
    /// 씬 로드 이벤트에 의존하지 않고 직접 호출받는 방식으로 처리)
    /// </summary>
    public void OnStageReady(StageScene stage)
    {
        Debug.Log("OnStageReady");

        // 서버만 플레이어 스폰 처리
        if (IsServer == false) return;

        _stageScene = stage;

        _loadedClients.Clear();
        _waitForSceneLoaded = true;
    }

    /// <summary>
    /// 모든 클라이언트가 씬 로드를 완료하면 호출된다.
    /// </summary>
    private void OnLoadEventCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        // 서버만 처리
        if (!IsServer)
            return;

        // Stage 준비를 기다리는 중이 아니면 무시
        if (!_waitForSceneLoaded)
            return;

        // Stage 씬만 처리
        if (_stageScene == null || sceneName != _stageScene.gameObject.scene.name)
            return;

        _waitForSceneLoaded = false;

        SpawnAllPlayers();
    }

    /// <summary>
    /// 연결된 모든 클라이언트에 플레이어 스폰
    /// </summary>
    void SpawnAllPlayers()
    {
        Debug.Log("SpawnAllPlayers");

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
        Debug.Log("SpawnPlayer");

        if (_playerPrefab == null)
        {
            Debug.LogWarning("PlayerPrefab이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = _stageScene.GetSpawnPoint(spawnIndex);
        GameObject player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);

        if (player.TryGetComponent(out NetworkObject networkObject))
        {
            Debug.Log($"Spawn : {clientId}");

            networkObject.SpawnAsPlayerObject(clientId);

            Debug.Log($"Spawned : {networkObject.IsSpawned}");
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