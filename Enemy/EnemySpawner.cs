using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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
    float _empEndTime; // EMP 종료 예정 시각(멀티:늦게 스폰된 적의 남은 지속시간 계산용)
    bool _isMultiplayer; // 멀티플레이 여부(풀 경로, 서버 권위 판정용)

    static bool _isNetworkPoolHandlerRegistered; // 네트워크 프리팹 풀링 핸들러 중복 등록 방지용(NetworkManager.Singleton 기준 세션당 1회만 등록되면 됨)

    public event Action<List<int>> OnSpawnListReady; // 스폰 리스트 준비됨
    public event Action<int> OnEnemySpawned;         // 적 스폰됨
    public event Action OnAllEnemiesDefeated;        // 모든 적 격파
    public event Action<DroppedItem> OnItemDropped;  // StageScene에 연결 용도

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

        // 멀티플레이 여부(풀 경로 분기용)
        _isMultiplayer = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);

        // Pool 생성(멀티는 네트워크 컴포넌트가 붙은 별도 프리팹 사용)
        string suffix = _isMultiplayer ? "_Multi" : "";
        GameManager.Instance.PoolManager.GetPool($"Tank/201Light{suffix}");
        GameManager.Instance.PoolManager.GetPool($"Tank/202Medium{suffix}");
        GameManager.Instance.PoolManager.GetPool($"Tank/203Heavy{suffix}");
        GameManager.Instance.PoolManager.GetPool($"Tank/204Destroyer{suffix}");

        // 멀티플레이:네트워크 프리팹의 스폰/디스폰이 Pool을 타도록 핸들러 등록(서버·클라이언트 모두 실행)
        // 세션당 1회만 등록하면 되므로 static 플래그로 중복 등록 방지
        if (_isMultiplayer && _isNetworkPoolHandlerRegistered == false)
        {
            RegisterNetworkPoolHandler($"Tank/201Light{suffix}");
            RegisterNetworkPoolHandler($"Tank/202Medium{suffix}");
            RegisterNetworkPoolHandler($"Tank/203Heavy{suffix}");
            RegisterNetworkPoolHandler($"Tank/204Destroyer{suffix}");
            _isNetworkPoolHandlerRegistered = true;
        }

        // 멀티플레이:적 스폰 판정은 서버만 수행(클라이언트는 NetworkObject.Spawn()으로 자동 복제됨)
        if (_isMultiplayer && NetworkManager.Singleton.IsServer == false) return;

        // 적 생성 코루틴 실행
        StartCoroutine(SpawnEnemyRoutine());
    }

    /// <summary>
    /// 멀티플레이:네트워크 프리팹(_Multi)에 Pool 기반 스폰/디스폰 핸들러 등록
    /// DroppedItem_Multi와 동일하게 NetworkPoolPrefabHandler를 재사용함
    /// 등록 후에는 non-authority 클라이언트의 Instantiate와, 서버·클라이언트 공통의 Destroy(Despawn destroy:true)가
    /// 모두 PoolManager를 거치게 됨
    /// </summary>
    void RegisterNetworkPoolHandler(string prefabPath)
    {
        Pool pool = GameManager.Instance.PoolManager.GetPool(prefabPath);
        if (pool == null) return;

        NetworkManager.Singleton.PrefabHandler.AddHandler(pool.Prefab, new NetworkPoolPrefabHandler(prefabPath));
    }

    /// <summary>
    /// 주기적으로 적을 생성하는 코루틴
    /// </summary>
    IEnumerator SpawnEnemyRoutine()
    {
        // 멀티플레이:클라이언트가 씬 전환 직후 스폰 메시지를 받을 준비를 마칠 시간을 위해 약간의 유예
        // (전원 로드 완료 신호 직후 곧바로 스폰하면 첫 번째 적이 일부 클라이언트에 누락되는 경우가 있어서 추가)
        if (_isMultiplayer)
        {
            yield return new WaitForSeconds(1f);
        }

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
        string suffix = _isMultiplayer ? "_Multi" : "";
        string prefabPath = $"Tank/{tankData.TankID}{tankData.TankType}{suffix}";

        // 탱크 생성
        StartCoroutine(SpawnRoutine(prefabPath, spawnPos.Value));

        // 적 스폰 UI에서 아이콘 제거
        OnEnemySpawned?.Invoke(_spawnedCount);

        // 멀티플레이:클라이언트에도 UI 갱신 신호 전달
        if (_isMultiplayer)
        {
            NetworkGameManager.Instance.NotifyEnemySpawned(_spawnedCount);
        }

        // 카운트 증가
        _spawnedCount++;
    }

    /// <summary>
    /// 멀티플레이:클라이언트가 서버의 스폰 신호를 받았을 때 호출(NetworkGameManager가 호출)
    /// 로컬에서 직접 스폰 판정을 못 하는 클라이언트를 위해 UI 이벤트만 대신 발행해줌
    /// </summary>
    public void ReceiveEnemySpawned(int spawnedIndex)
    {
        OnEnemySpawned?.Invoke(spawnedIndex);
    }

    /// <summary>
    /// 탱크 생성 코루틴
    /// </summary>
    IEnumerator SpawnRoutine(string prefabPath, Vector3 spawnPos)
    {
        // 탱크 미리 생성
        GameObject enemyGo = GameManager.Instance.PoolManager.GetFromPool(prefabPath);

        // 멀티(NetworkObject 포함)는 Pool이 부모를 직접 관리(생성 시 DontDestroyOnLoad)하므로 재부모화하지 않음
        // 여기서 재부모화하면 Despawn 시 Pool.Push()가 NetworkObject는 부모를 되돌리지 않아서
        // 씬 로컬 부모 밑에 남게 되고, 스테이지 전환 때 DontDestroyOnLoad 보호를 잃고 함께 파괴돼버림
        if (_isMultiplayer == false)
        {
            enemyGo.transform.SetParent(transform);
        }
        enemyGo.transform.position = spawnPos;

        // 초기화
        enemyGo.TryGetComponent(out EnemyTank enemy);
        enemy.Initialize();

        if (enemy.TryGetComponent(out ItemDropper itemDropper))
        {
            itemDropper.OnItemDropped += item => OnItemDropped?.Invoke(item);
        }

        // 멀티플레이:네트워크 스폰(클라이언트에도 자동 복제됨)
        // 렌더러 토글/이펙트 연출은 EnemyNetworkOwner.OnNetworkSpawn()에서 각자 로컬로 처리하므로 여기선 생략
        if (_isMultiplayer)
        {
            if (enemyGo.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }
        else
        {
            // 렌더러 끄기
            enemy.SetRenderersVisible(false);

            // 스폰 이펙트 먼저 재생
            GameManager.Instance.EffectManager.SpawnEffect(EffectType.Twinkle, spawnPos);
        }

        // 이펙트 지속시간 대기
        // 멀티일 때는 EnemyNetworkOwner가 로컬 연출에 쓰는 값(enemy.SpawnEffectTime, 프리팹 원본)과 동일하게 맞춰서
        // AI 시작 타이밍이 클라이언트 연출 종료 타이밍과 어긋나지 않게 함
        float waitTime = _isMultiplayer ? enemy.SpawnEffectTime : _enemySpawnEffectTime;
        yield return new WaitForSeconds(waitTime);

        if (_isMultiplayer == false)
        {
            // 렌더러 켜기
            enemy.SetRenderersVisible(true);
        }

        // AI 시작
        enemy.StartAI();

        // 적 멈춤 아이템 사용중이면 멈춰놓음
        if (_isEMPActive)
        {
            enemy.SetAIActive(false);
            enemy.SetEMPEffect(true);

            // 멀티플레이:이 적은 EMP 시작 시점의 스냅샷에 없었으므로 남은 지속시간만큼 별도로 알림(호스트 자신은 위에서 이미 적용함)
            if (_isMultiplayer && enemy.TryGetComponent(out NetworkObject enemyNetworkObject))
            {
                float remainingDuration = _empEndTime - Time.time;
                NetworkGameManager.Instance.NotifyLateEMPVisual(enemyNetworkObject.NetworkObjectId, remainingDuration);
            }
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
        // 멀티플레이:AI 판정은 서버만 수행(호출 주체는 NetworkGameManager)
        if (_isMultiplayer && NetworkManager.Singleton.IsServer == false) return;

        if (_empRoutine != null)
        {
            StopCoroutine(_empRoutine);
            SetAllEnemiesVisible(true); // 깜빡임 도중 꺼진 렌더러 복구
        }
        _empEndTime = Time.time + duration;
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
    /// 멀티플레이:현재 생존한 적들의 NetworkObjectId 목록 반환(EMP 시각 연출 브로드캐스트용)
    /// </summary>
    public ulong[] GetActiveEnemyNetworkObjectIds()
    {
        List<ulong> ids = new List<ulong>();
        foreach (EnemyTank enemy in _enemies)
        {
            if (enemy.IsAlive == false) continue; // 이미 죽은 적은 제외

            if (enemy.TryGetComponent(out NetworkObject networkObject) == false) continue;
            ids.Add(networkObject.NetworkObjectId);
        }
        return ids.ToArray();
    }

    /// <summary>
    /// 멀티플레이:클라이언트가 서버의 EMP 신호를 받았을 때 호출 — 판정 없이 시각 연출(이펙트+깜빡임)만 재생
    /// AI 정지 자체는 서버 권위 이동이 NetworkTransform으로 그대로 반영되므로 클라이언트에서 별도 처리 불필요
    /// </summary>
    public void PlayEMPVisual(List<EnemyTank> targets, float duration)
    {
        StartCoroutine(EMPVisualRoutine(targets, duration));
    }

    IEnumerator EMPVisualRoutine(List<EnemyTank> targets, float duration)
    {
        foreach (EnemyTank enemy in targets)
            enemy.SetEMPEffect(true);

        yield return new WaitForSeconds(duration - _blinkStartTime);

        // 깜빡이기
        float elapsed = 0f;
        while (elapsed < _blinkStartTime)
        {
            foreach (EnemyTank enemy in targets)
                enemy.SetRenderersVisible(false);

            yield return new WaitForSeconds(_blinkInterval);

            foreach (EnemyTank enemy in targets)
                enemy.SetRenderersVisible(true);

            yield return new WaitForSeconds(_blinkInterval);
            elapsed += _blinkInterval * 2f;
        }

        foreach (EnemyTank enemy in targets)
            enemy.SetEMPEffect(false);
    }

    /// <summary>
    /// 모든 적의 AI 켜고 끄기
    /// </summary>
    public void SetAllEnemiesAIActive(bool active)
    {
        foreach (EnemyTank enemy in _enemies)
        {
            if (enemy.IsAlive == false) continue; // 이미 죽은 적은 제외(AI 비활성화가 사망 상태 타이머까지 멈춰버림)

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
        {
            if (enemy.IsAlive == false) continue; // 이미 죽은 적은 깜빡임 대상에서 제외
            enemy.SetRenderersVisible(visible);
        }
    }

    /// <summary>
    /// 폭탄 아이템 효과
    /// </summary>
    public void DestroyAllEnemies()
    {
        // 멀티플레이:데미지 판정은 서버만 수행(TakeDamage 자체는 전파되지 않으므로 아래에서 대상을 별도로 알림)
        if (_isMultiplayer && NetworkManager.Singleton.IsServer == false) return;

        List<EnemyTank> targets = _enemies.ToList();
        bool killedAny = targets.Count > 0;

        // 멀티플레이:죽기 전에 대상 스냅샷 확보(호스트 자신의 처리와 별개로 클라이언트에 알릴 목적)
        ulong[] killedIds = (_isMultiplayer) ? GetActiveEnemyNetworkObjectIds() : null;

        GameManager.Instance.AudioManager.StartMassKillMode();

        foreach (EnemyTank enemy in targets)
        {
            if (enemy.TryGetComponent(out TankModel tankModel))
            {
                tankModel.TakeDamage(new HitData(9999, enemy.transform.position, null));
            }
        }

        // 탱크 터지는 소리
        GameManager.Instance.AudioManager.EndMassKillMode(killedAny);

        // 멀티플레이:각 클라이언트가 로컬로 동일한 사망 연출(폭발/시체/디스폰 타이머)을 재생하도록 대상을 알림
        if (_isMultiplayer)
        {
            NetworkGameManager.Instance.NotifyAirSupportKill(killedIds);
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