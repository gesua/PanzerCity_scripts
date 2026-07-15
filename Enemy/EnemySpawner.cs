using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 적 스폰 데이터테이블
/// </summary>
[System.Serializable]
public class EnemySpawnData
{
    public int StageID;
    public int Order;
    public int TankID;
}

/// <summary>
/// 일정 시간마다 적 스폰
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("----- 적 생성 -----")]
    [SerializeField] float _spawnSpan = 2f;  // 스폰 시간 간격
    [SerializeField] int _maxSpawnCount = 4; // 최대 스폰 수
    [SerializeField] Transform[] _spawnPos;  // 스폰 위치
    [SerializeField] float _spawnCheckRadius = 2f; // 스폰 위치 체크 반경
    [SerializeField] LayerMask _spawnCheckLayer = 1 << 8 | 1 << 9; // 탱크 레이어(플레이어, 적)
    [Header("----- 아이템 효과 -----")]
    [SerializeField] float _blinkStartTime = 2f;  // 깜빡이기 시작할 시간
    [SerializeField] float _blinkInterval = 0.1f; // 깜빡임 간격

    List<EnemyTank> _enemies = new(); // 생성된 적 리스트

    int[] _spawnPosIndex; // 스폰 위치 순서
    int _spawnedCount = 0; // 스폰된 수
    float _enemySpawnEffectTime = 1f; // 스폰 이펙트 지속시간

    List<int> _spawnList; // TankID 순서 리스트
    int _stageSpawnCount; // 스테이지당 스폰할 횟수
    
    bool _isEMPActive; // 적 멈추는 아이템 사용했는지

    public event Action<List<int>> OnSpawnListReady; // 스폰 리스트 준비됨
    public event Action<int> OnEnemySpawned;         // 적 스폰됨
    public event Action OnAllEnemiesDefeated;        // 모든 적 격파
    public event Action<DroppedItem> OnItemDropped;  // StageScene에 연결 용도

    Coroutine _spawnEnemyRoutine;
    Coroutine _retryRoutine;
    Coroutine _empRoutine;

    public void Initialize(int stageID)
    {
        _spawnList = GameManager.Instance.DataManager.GetSpawnList(stageID);
        _stageSpawnCount = _spawnList.Count;

        // 스폰 위치 순서 세팅
        _spawnPosIndex = GenerateRandomArray(_stageSpawnCount, _spawnPos.Length);

        // 적 스폰 UI 세팅
        OnSpawnListReady?.Invoke(_spawnList);

        // Pool 생성
        GameManager.Instance.PoolManager.GetPool("Tank/201Light");
        GameManager.Instance.PoolManager.GetPool("Tank/202Medium");
        GameManager.Instance.PoolManager.GetPool("Tank/203Heavy");

        // 적 생성 코루틴 실행
        _spawnEnemyRoutine = StartCoroutine(SpawnEnemyRoutine());
    }

    /// <summary>
    /// 주기적으로 적을 생성하는 코루틴
    /// </summary>
    IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(_spawnSpan);
        }
    }

    /// <summary>
    /// 비어있는 스폰 위치 탐색
    /// </summary>
    /// <returns></returns>
    Vector3? GetAvailableSpawnPos()
    {
        // 현재 스폰 인덱스부터 순서대로 탐색
        for (int i = 0; i < _spawnPos.Length; i++)
        {
            int index = (_spawnPosIndex[_spawnedCount] + i) % _spawnPos.Length;
            Vector3 pos = _spawnPos[index].position;

            if (Physics.OverlapSphere(pos, _spawnCheckRadius, _spawnCheckLayer).Length == 0)
            {
                return pos;
            }
        }
        return null; // 모든 위치가 막혀있음
    }

    /// <summary>
    /// 일정시간 후 스폰 재시도
    /// </summary>
    IEnumerator RetrySpawn()
    {
        yield return new WaitForSeconds(0.1f);
        _retryRoutine = null;
        SpawnEnemy();
    }

    /// <summary>
    /// 적을 생성하는 함수
    /// </summary>
    public void SpawnEnemy()
    {
        // 이미 생성된 적 수가 생성 가능한 최대 적 수보다 크거나 같으면 종료
        if (_enemies.Count >= _maxSpawnCount) return;
        // 스테이지 스폰 수만큼 생성했으면 종료
        if (_spawnedCount >= _spawnList.Count) return;

        // 스폰 위치 비어있는지 확인
        Vector3? spawnPos = GetAvailableSpawnPos();

        // 모든 위치가 막혀있으면 잠시 후 재시도
        if (spawnPos == null)
        {
            // 이미 재시도 중이면 새로 시작하지 않음
            if (_retryRoutine == null) _retryRoutine = StartCoroutine(RetrySpawn());
            return;
        }

        // 재시도 코루틴 종료
        if (_retryRoutine != null)
        {
            StopCoroutine(_retryRoutine);
            _retryRoutine = null;
        }

        // Order 순서대로 TankID로 프리팹 경로 가져오기
        int tankID = _spawnList[_spawnedCount];
        TankData tankData = GameManager.Instance.DataManager.GetTankData(tankID);
        string prefabPath = $"Tank/{tankData.TankID}{tankData.TankType}";

        // 탱크 생성
        StartCoroutine(SpawnRoutine(prefabPath, spawnPos.Value));

        // 적 스폰 UI에서 아이콘 제거
        OnEnemySpawned?.Invoke(_spawnedCount);

        // 카운트 증가
        _spawnedCount++;
    }

    /// <summary>
    /// 탱크 생성 코루틴
    /// </summary>
    IEnumerator SpawnRoutine(string prefabPath, Vector3 spawnPos)
    {
        // 탱크 미리 생성
        GameObject enemyGo = GameManager.Instance.PoolManager.GetFromPool(prefabPath);
        enemyGo.transform.SetParent(transform);
        enemyGo.transform.position = spawnPos;

        // 초기화
        enemyGo.TryGetComponent(out EnemyTank enemy);
        enemy.Initialize();

        if (enemy.TryGetComponent(out ItemDropper itemDropper))
        {
            itemDropper.OnItemDropped += item => OnItemDropped?.Invoke(item);
        }

        // 렌더러 끄기
        enemy.SetRenderersVisible(false);

        // 스폰 이펙트 먼저 재생
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.Twinkle, spawnPos);

        // 이펙트 지속시간 대기
        yield return new WaitForSeconds(_enemySpawnEffectTime);

        // 렌더러 켜기
        enemy.SetRenderersVisible(true);

        // AI 시작
        enemy.StartAI();

        // 적 멈춤 아이템 사용중이면 멈춰놓음
        if (_isEMPActive)
        {
            enemy.SetAIActive(false);
            enemy.SetEMPEffect(true);
        }

        _enemies.Add(enemy); // 리스트에 추가
        enemy.OnRemoved += HandleEnemyRemoved; // 제거 이벤트 구독
    }


    /// <summary>
    /// 랜덤한 set로 겹치지 않게 셔플해서 배열을 만들어주는 함수
    /// </summary>
    /// <param name="size">배열 크기</param>
    /// <param name="setSize">set 크기(3이면 0~2, 5면 0~4)</param>
    /// <returns></returns>
    int[] GenerateRandomArray(int size, int setSize)
    {
        int[] result = new int[size];
        int index = 0;
        int prevLast = -1;

        while (index < size)
        {
            // 세트 크기만큼 숫자 채우기
            int[] set = new int[setSize];
            for (int i = 0; i < setSize; i++)
            {
                set[i] = i;
            }

            // 조건 만족할 때까지 셔플
            do
            {
                set.Shuffle<int>();
                //Shuffle(set);
            } while (prevLast == set[0]);

            // 셔플된 거 result에 추가
            foreach (int num in set)
            {
                if (index < size)
                {
                    result[index++] = num;
                }
            }

            prevLast = set[set.Length - 1];
        }

        return result;
    }

    /// <summary>
    /// 적 제거 시 자동으로 실행되는 함수
    /// </summary>
    /// <param name="enemy">제거된 적</param>
    void HandleEnemyRemoved(EnemyTank enemy)
    {
        // 생성된 적 목록에서 제거된 적 제거
        _enemies.Remove(enemy);

        // 스폰할 적도 없고 남은 적도 없으면 이벤트 발행
        if (_spawnedCount >= _stageSpawnCount && _enemies.Count == 0)
        {
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    /// <summary>
    /// 적 멈춤 아이템 효과
    /// </summary>
    public void StartEMPField(float duration)
    {
        if (_empRoutine != null)
        {
            StopCoroutine(_empRoutine);
            SetAllEnemiesVisible(true); // 깜빡임 도중 꺼진 렌더러 복구
        }
        _empRoutine = StartCoroutine(EMPFieldRoutine(duration));
    }

    IEnumerator EMPFieldRoutine(float duration)
    {
        _isEMPActive = true;
        SetAllEnemiesAIActive(false);

        // 깜빡이기 전 대기
        yield return new WaitForSeconds(duration - _blinkStartTime);

        // 깜빡이기
        float elapsed = 0f;
        while (elapsed < _blinkStartTime)
        {
            SetAllEnemiesVisible(false);
            yield return new WaitForSeconds(_blinkInterval);
            SetAllEnemiesVisible(true);
            yield return new WaitForSeconds(_blinkInterval);
            elapsed += _blinkInterval * 2f;
        }

        _isEMPActive = false;
        SetAllEnemiesAIActive(true);

        _empRoutine = null;
    }

    /// <summary>
    /// 모든 적의 AI 켜고 끄기
    /// </summary>
    public void SetAllEnemiesAIActive(bool active)
    {
        foreach (EnemyTank enemy in _enemies)
        {
            enemy.SetAIActive(active);
            enemy.SetEMPEffect(!active); // 파직거리는 이펙트
        }
    }

    /// <summary>
    /// 모든 적의 모습 켜고 끄기
    /// </summary>
    void SetAllEnemiesVisible(bool visible)
    {
        foreach (EnemyTank enemy in _enemies)
            enemy.SetRenderersVisible(visible);
    }

    /// <summary>
    /// 폭탄 아이템 효과
    /// </summary>
    public void DestroyAllEnemies()
    {
        foreach (EnemyTank enemy in _enemies.ToList())
        {
            if (enemy.TryGetComponent(out TankModel tankModel))
            {
                tankModel.TakeDamage(new HitData(9999, enemy.transform.position, null));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 스폰 체크 반경
        if (_spawnPos == null) return;

        Gizmos.color = Color.green;

        foreach (var t in _spawnPos)
        {
            if (t == null) continue;

            Vector3 pos = t.position;

            Gizmos.DrawWireSphere(pos, _spawnCheckRadius);
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}