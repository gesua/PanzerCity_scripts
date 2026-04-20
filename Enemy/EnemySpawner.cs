using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] int _stageID; // 현재 스테이지 ID
    [Header("----- 적 생성 -----")]
    [SerializeField] string _enemyPrefabPath;   // Enemy 프리팹 리소스(에셋) 저장되어 있는 경로
    [SerializeField] float _spawnSpan = 2f;     // 스폰 시간 간격
    [SerializeField] int _maxSpawnCount = 4;    // 최대 스폰 수
    [SerializeField] Transform[] _spawnPos;     // 스폰 위치

    [Header("----- 적 리스트(읽기 전용) -----")]
    [SerializeField] List<EnemyTank> _enemies = new(); // 생성된 적 리스트

    [SerializeField] int[] _spawnIndex;      // 스폰 순서
    int _spawnedCount = 0;  // 스폰된 수

    List<int> _spawnList; // TankID 순서 리스트
    int _stageSpawnCount; // 스테이지당 스폰할 횟수

    Coroutine _spawnEnemyRoutine;

    private void Start()
    {
        _spawnList = GameManager.Instance.DataManager.GetSpawnList(_stageID);
        _stageSpawnCount = _spawnList.Count;

        // 스폰 순서 세팅
        _spawnIndex = GenerateRandomArray(_stageSpawnCount, _spawnPos.Length);

        // 적 생성 코루틴 실행
        _spawnEnemyRoutine = StartCoroutine(SpawnEnemyRoutine());

        // Pool 생성
        //GameManager.Instance.PoolManager.GetPool(_enemyPrefabPath, _maxSpawnCount);
    }

    /// <summary>
    /// 주기적으로 적을 생성하는 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(_spawnSpan);
        }
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

        // Order 순서대로 TankID로 프리팹 경로 가져오기
        int tankID = _spawnList[_spawnedCount];
        TankData tankData = GameManager.Instance.DataManager.GetTankData(tankID);
        string prefabPath = $"Tank/{tankData.TankID}{tankData.TankType}";

        // 생성
        GameObject enemyGo = GameManager.Instance.PoolManager.GetFromPool(prefabPath);
        enemyGo.transform.SetParent(transform);

        // 위치 설정
        enemyGo.transform.position = _spawnPos[_spawnIndex[_spawnedCount]].position;
        _spawnedCount++;

        // 초기화
        EnemyTank enemy = enemyGo.GetComponent<EnemyTank>();
        enemy.Initialize();

        // 리스트에 추가
        _enemies.Add(enemy);

        // 제거 이벤트 구독
        enemy.OnRemoved += HandleEnemyRemoved;
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
    }
}
