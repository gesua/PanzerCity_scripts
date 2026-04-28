using System;
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

    public int StageID => _stageID;
    public EnemySpawner EnemySpawner => _enemySpawner;

    public event Action OnHQDestroyed;
    public event Action<Vector3> OnStageLoaded; // 스폰 위치 전달

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _hq.OnDestroyed += () => OnHQDestroyed?.Invoke();
        OnStageLoaded?.Invoke(_playerSpawnPoint.position);
        _enemySpawner.Initialize(_stageID);
    }
}
