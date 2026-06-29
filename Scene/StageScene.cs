using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 씬 관리
/// </summary>
public class StageScene : MonoBehaviour
{
    [SerializeField] int _stageID;
    [SerializeField] EnemySpawner _enemySpawner; // 스테이지 ID값 넘겨줄거
    [SerializeField] Transform _playerSpawnPoint; // 플레이어 시작 지점
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
        OnStageLoaded?.Invoke(_playerSpawnPoint.position);
        _enemySpawner.Initialize(_stageID);

        _enemySpawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated; // 모든 적 격파
        _enemySpawner.OnItemDropped += item => _droppedItems.Add(item);

        _baseWall.OnBaseWallDestroyed += HandleBaseWallDestroyed;
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
}