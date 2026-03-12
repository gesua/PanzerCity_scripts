using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일정 시간 간격으로 적 스폰
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("----- 적 생성 -----")]
    [SerializeField] string _enemyPrefabPath;   // Enemy 프리팹 리소스(에셋) 저장되어 있는 경로
    [SerializeField] float _spawnSpan = 2f;     // 스폰 시간 간격
    [SerializeField] int _maxSpawnCount = 4;    // 최대 스폰 수
    [SerializeField] int _stageSpawnCount = 20; // 스테이지당 스폰할 횟수
    [SerializeField] Transform[] _spawnPos;     // 스폰 위치

    [Header("----- 적 리스트(읽기 전용) -----")]
    [SerializeField] List<Enemy> _enemies = new(); // 생성된 적 리스트

    [SerializeField] int[] _spawnIndex;      // 스폰 순서
    int _spawnedCount = 0;  // 스폰된 수

    Coroutine _spawnEnemyRoutine;
    
    private void Start()
    {
        // 스폰 순서 세팅
        _spawnIndex = GenerateRandomArray(_stageSpawnCount, _spawnPos.Length);

        // 적 생성 코루틴 실행
        _spawnEnemyRoutine = StartCoroutine(SpawnEnemyRoutine());

        // Pool 생성
        GameManager.Instance.PoolManager.GetPool(_enemyPrefabPath, _maxSpawnCount);
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
        if(_spawnedCount >= _stageSpawnCount) return;

        // 프리팹 복제본 생성
        GameObject enemyGo = GameManager.Instance.PoolManager.GetFromPool(_enemyPrefabPath);
        enemyGo.transform.SetParent(transform);

        // 복제본 위치 설정
        enemyGo.transform.position = _spawnPos[_spawnIndex[_spawnedCount]].position;
        _spawnedCount++;

        // 복제본 초기화
        Enemy enemy = enemyGo.GetComponent<Enemy>();
        enemy.Initialize();

        // 복제본 리스트에 추가
        _enemies.Add(enemy);

        // 복제본의 제거 이벤트 구독
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

    void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[rand];
            array[rand] = temp;
        }
    }

    /// <summary>
    /// 적 제거 시 자동으로 실행되는 함수
    /// </summary>
    /// <param name="enemy">제거된 적</param>
    void HandleEnemyRemoved(Enemy enemy)
    {
        // 생성된 적 목록에서 제거된 적 제거
        _enemies.Remove(enemy);
    }
}
