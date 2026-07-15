using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 스테이지 씬 관리
/// </summary>
public class StageScene : MonoBehaviour
{
    [SerializeField] int _stageID;
    [SerializeField] EnemySpawner _enemySpawner; // 스테이지 ID값 넘겨줄거
    [SerializeField] Transform[] _playerSpawnPoints; // 플레이어 시작 지점 (싱글: [0], 멀티: 인덱스 순)
    [SerializeField] HQ _hq;
    [SerializeField] BaseWall _baseWall;

    string _sceneName;
    string _nextStageName;
    List<DroppedItem> _droppedItems = new(); // 씬 전환시 사라질 아이템들

    public int StageID => _stageID;
    public EnemySpawner EnemySpawner => _enemySpawner;
    public BaseWall BaseWall => _baseWall;
    public string SceneName => _sceneName;
    public string NextStageName => _nextStageName;

    public event Action OnHQDestroyed; // 아군 기지 파괴
    public event Action<Vector3> OnStageLoaded; // 스폰 위치 전달
    public event Action OnStageClear; // 스테이지 클리어
    public event Action OnBaseWallDestroyed; // 기지 벽 파괴

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _sceneName = GameManager.Instance.DataManager.StageIDToSceneName(_stageID); // 현재 Scene이름
        _nextStageName = GameManager.Instance.DataManager.StageIDToSceneName(_stageID + 1);

        _hq.OnDestroyed += () => OnHQDestroyed?.Invoke();
        OnStageLoaded?.Invoke(_playerSpawnPoints[0].position); // 싱글 전용(멀티에선 이 값 무시)

        _enemySpawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated; // 모든 적 격파
        _enemySpawner.OnItemDropped += item => _droppedItems.Add(item);

        _baseWall.OnBaseWallDestroyed += HandleBaseWallDestroyed;

        bool isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (isMultiplayer) // 멀티플레이:전원 씬 로드 완료 신호를 받은 뒤에 적 스폰 시작
        {
            NetworkGameManager.Instance.OnAllClientsReady += HandleAllClientsReady;
        }
        else // 싱글플레이:즉시 시작
        {
            _enemySpawner.Initialize(_stageID);
        }
    }

    /// <summary>
    /// 멀티플레이 전용 — 전원 씬 로드 완료 신호를 받으면 적 스폰 시작
    /// </summary>
    void HandleAllClientsReady()
    {
        NetworkGameManager.Instance.OnAllClientsReady -= HandleAllClientsReady;
        _enemySpawner.Initialize(_stageID);
    }

    void OnDestroy()
    {
        // 신호가 오기 전에 파괴되는 경우(씬 전환 등) 구독 해제
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnAllClientsReady -= HandleAllClientsReady;
        }
    }

    void HandleAllEnemiesDefeated()
    {
        StartCoroutine(StageClearRoutine());
    }

    /// <summary>
    /// 기지 벽 파괴됨
    /// </summary>
    void HandleBaseWallDestroyed()
    {
        OnBaseWallDestroyed?.Invoke();
    }

    IEnumerator StageClearRoutine()
    {
        yield return new WaitForSeconds(3f);
        Cleanup();
        OnStageClear?.Invoke();
    }

    /// <summary>
    /// 바닥에 있는 아이템들 초기화
    /// </summary>
    public void Cleanup()
    {
        foreach (DroppedItem item in _droppedItems)
        {
            item.gameObject.DestroyOrReturnToPool();
        }
        _droppedItems.Clear();
    }

    /// <summary>
    /// 플레이어 스폰 위치 반환 (멀티용)
    /// index가 범위 초과 시 마지막 포인트 반환
    /// </summary>
    public Vector3 GetSpawnPoint(int index)
    {
        int safeIndex = Mathf.Clamp(index, 0, _playerSpawnPoints.Length - 1);
        return _playerSpawnPoints[safeIndex].position;
    }

}