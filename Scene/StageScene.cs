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

    bool _isMultiplayer; // 멀티플레이 여부(스테이지 클리어 결과 전파용)

    public int StageID => _stageID;
    public EnemySpawner EnemySpawner => _enemySpawner;
    public BaseWall BaseWall => _baseWall;
    public HQ HQ => _hq;
    public string SceneName => _sceneName;
    public string NextStageName => _nextStageName;

    public event Action OnHQDestroyed; // 아군 기지 파괴
    public event Action<Vector3> OnStageLoaded; // 스폰 위치 전달
    public event Action OnStageClear; // 스테이지 클리어
    public event Action OnBaseWallDestroyed; // 기지 벽 파괴
    public event Action OnAllEnemiesDefeatedNotify; // 모든 적 격파 알림(UI용, 멀티 동기화 대상)

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

        _isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        if (_isMultiplayer) // 멀티플레이:전원 씬 로드 완료 신호를 받은 뒤에 적 스폰 시작
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
        StartCoroutine(AllEnemiesDefeatedRoutine());
    }

    IEnumerator AllEnemiesDefeatedRoutine()
    {
        yield return new WaitForSeconds(5f); // 적 시체 사라지는거 대기

        TriggerAllEnemiesDefeatedNotify();

        // 멀티플레이:이 메서드 자체가 서버(호스트)에서만 도달 가능 — 결과를 클라이언트에 전파
        if (_isMultiplayer) NetworkGameManager.Instance.NotifyAllEnemiesDefeated();

        StartCoroutine(StageClearRoutine());
    }

    /// <summary>
    /// 모든 적 격파 알림(배너 표시용) — 정리/카운트다운 시작 전, 즉시 전달되어야 하는 이벤트
    /// 싱글:위에서 직접 호출 / 멀티:서버는 위에서, 클라이언트는 NetworkGameManager의 신호를 받아 호출
    /// </summary>
    public void TriggerAllEnemiesDefeatedNotify()
    {
        OnAllEnemiesDefeatedNotify?.Invoke();
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
        yield return new WaitForSeconds(3f); // 클리어 배너 대기
        yield return TriggerStageClearRoutine(); // Cleanup 완료까지 기다린 뒤 다음 단계로

        // 멀티플레이:이 코루틴 자체가 서버(호스트)에서만 도달 가능 — 결과를 클라이언트에 전파
        if (_isMultiplayer) NetworkGameManager.Instance.NotifyStageCleared();
    }

    /// <summary>
    /// 스테이지 클리어 확정 처리 진입점 — Cleanup이 끝날 때까지 기다렸다가 이벤트를 발행해야 해서 코루틴으로 시작
    /// 싱글:위 코루틴에서 직접 호출 / 멀티:서버는 위 코루틴에서, 클라이언트는 NetworkGameManager의 신호를 받아 호출
    /// (서버가 이미 대기 및 정리까지 마친 뒤 보낸 신호이므로 클라이언트는 곧바로 반영)
    /// </summary>
    public void TriggerStageClear()
    {
        StartCoroutine(TriggerStageClearRoutine());
    }

    IEnumerator TriggerStageClearRoutine()
    {
        yield return CleanupRoutine();
        OnStageClear?.Invoke();
    }

    /// <summary>
    /// 바닥 아이템 정리 — Despawn을 한 프레임에 몰아 부르면 스톨이 생길 수 있어 여러 프레임에 나눠 처리
    /// TriggerStageClearRoutine에서 완료를 기다리므로, 정리 도중에 다음 스테이지로 못 넘어감이 보장됨
    /// (2→3스테이지 전환 중 호스트에서 실제로 관찰된 증상: Receive queue full + 씬 로드 지연)
    /// </summary>
    IEnumerator CleanupRoutine()
    {
        List<DroppedItem> items = new List<DroppedItem>(_droppedItems);
        _droppedItems.Clear();

        int processedCount = 0;
        foreach (DroppedItem item in items)
        {
            // 멀티플레이:네트워크 스폰된 아이템은 Despawn으로 정리해야 풀 반환 시 NGO 스폰 상태도 같이 정리됨
            if (item.TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
            else
            {
                item.gameObject.DestroyOrReturnToPool();
            }

            processedCount++;
            if (processedCount >= 5)
            {
                processedCount = 0;
                yield return null;
            }
        }
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

    void OnDrawGizmosSelected()
    {
        if (_playerSpawnPoints == null) return;

        Gizmos.color = Color.red;

        foreach (Transform spawnPoint in _playerSpawnPoints)
        {
            if (spawnPoint == null) continue;

            Vector3 pos = spawnPoint.position;

            // 플레이어 스폰 위치 표시
            Gizmos.DrawSphere(pos, 1f);

            // 위쪽 방향 표시
            Gizmos.DrawLine(pos, pos + spawnPoint.forward * 1.5f);
        }
    }
}