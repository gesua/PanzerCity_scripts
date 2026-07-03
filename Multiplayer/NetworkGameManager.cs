using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 멀티플레이 게임 관리 — 플레이어 스폰 담당
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    [SerializeField] GameObject _playerPrefab; // NetworkObject + PlayerTank 포함 프리팹 (4단계에서 연결)

    public static NetworkGameManager Instance { get; private set; }

    StageScene _stageScene;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // 서버만 플레이어 스폰 처리
        if (IsServer == false) return;

        // 스테이지 씬 로드 감지
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer == false) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// 스테이지 씬 로드됨 — StageScene 캐시 후 플레이어 스폰
    /// </summary>
    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _stageScene = FindAnyObjectByType<StageScene>();
        if (_stageScene == null) return;

        // 스테이지 씬 로드 시에만 처리 (이후 씬 로드에서 중복 방지)
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        SpawnAllPlayers();
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
            Debug.LogWarning("PlayerPrefab이 연결되지 않았습니다. 4단계에서 연결 예정.");
            return;
        }

        Vector3 spawnPos = _stageScene.GetSpawnPoint(spawnIndex);
        GameObject player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);

        if (player.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.SpawnAsPlayerObject(clientId);
        }
    }
}