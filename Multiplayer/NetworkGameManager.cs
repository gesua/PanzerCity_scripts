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
    public event Action<PlayerTank> OnLocalPlayerSpawned; // 로컬 플레이어 스폰 완료 알림
    public event Action OnAllClientsReady; // 모든 클라이언트 씬 로드 완료(로딩창/적 스폰 동시 시작용, 로컬 신호)

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
        // 서버만 플레이어 스폰 처리
        if (IsServer == false) return;

        _stageScene = stage;
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
        if (IsServer == false) return;

        // Stage 준비를 기다리는 중이 아니면 무시
        if (_waitForSceneLoaded == false) return;

        // Stage 씬만 처리
        if (_stageScene == null || sceneName != _stageScene.gameObject.scene.name)
            return;

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