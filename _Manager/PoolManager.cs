using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 Pool들을 관리하는 매니저
/// </summary>
public class PoolManager : MonoBehaviour
{
    ResourceManager _resourceManager;           // 리소스 매니저(에셋번들, 어드레서블 에셋 같은거); 인터페이스로 하는게 더 나을 수 있음
    Dictionary<string, Pool> _poolMap = new();  // Pool맵

    /// <summary>
    /// 초기화 함수
    /// </summary>
    public void Initialize(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// 프리팹 경로에 해당하는 Pool을 반환하는 함수
    /// 아직 생성되지 않은 Pool이면 새로 생성해 반환
    /// </summary>
    /// <param name="prefabPath">프리팹 경로</param>
    /// <param name="size">Pool 초기 사이즈</param>
    /// <param name="includeInReturnAll">ReturnAllPools() 강제 반환 대상 여부(기본 true). 필드 아이템/포탄처럼 스테이지 전환 시 정리돼야 하면 true, 인벤토리 UI처럼 유지돼야 하면 false</param>
    /// <returns></returns>
    public Pool GetPool(string prefabPath, int size = 10, bool includeInReturnAll = true)
    {
        // 프리팹 경로에 해당하는 Pool이 없으면
        if (_poolMap.ContainsKey(prefabPath) == false)
        {
            // 리소스 매니저에서 프리팹 리소스를 로드
            GameObject prefab = _resourceManager.GetPrefab(prefabPath);

            if (prefab == null) return null; // 실패

            // Pool의 부모 게임오브젝트 생성
            GameObject parentGo = new GameObject($"Pool_{prefabPath}");

            // Pool의 부모 게임오브젝트를 DontDestroyOnLoad로 설정
            DontDestroyOnLoad(parentGo);

            // Pool 생성
            Pool pool = new Pool(prefab, parentGo.transform, size, includeInReturnAll);

            // 생성된 Pool을 Pool맵에 추가
            _poolMap[prefabPath] = pool;
        }

        // 있으면 반환
        return _poolMap[prefabPath];
    }

    /// <summary>
    /// Pool에서 게임오브젝트를 가져오는 함수
    /// </summary>
    /// <param name="prefabPath">프리팹 경로</param>
    /// <returns></returns>
    public GameObject GetFromPool(string prefabPath)
    {
        Pool pool = GetPool(prefabPath);
        if (pool == null) return null;
        return pool.Pop();
    }

    /// <summary>
    /// 모든 Pool에서 나가있는 오브젝트들을 강제로 반환하는 함수
    /// 스테이지 전환/재시작 시 호출
    /// </summary>
    public void ReturnAllPools()
    {
        foreach (Pool pool in _poolMap.Values)
        {
            // 인벤토리 UI는 대상에서 제외
            if (pool.IncludeInReturnAll == false) continue;

            pool.ReturnAll();
        }
    }
}