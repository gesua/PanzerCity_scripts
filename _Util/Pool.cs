using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임오브젝트 Pool(오브젝트 풀링)
/// 게임오브젝트들을 미리 생성해뒀다가 필요할 때 건네주고
/// 사용이 다 됐으면 다시 돌려받음
/// </summary>
public class Pool
{
    Stack<GameObject> _pool;    // 복제본 게임오브젝트 스택
    GameObject _prefab;         // Pooling할 원본 프리팹
    Transform _parent;          // Pooling 게임오브젝트들의 부모 트랜스폼
    HashSet<GameObject> _activeObjects; // 현재 Pool 밖에 나가있는(사용 중인) 오브젝트 추적
    bool _includeInReturnAll;   // PoolManager.ReturnAllPools() 강제 반환 대상인지(필드 아이템/포탄용 Pool: true, UI용 Pool 등: false)

    public bool IncludeInReturnAll => _includeInReturnAll;

    /// <summary>
    /// Pool 생성자
    /// </summary>
    /// <param name="prefab">Pooling할 프리팹</param>
    /// <param name="parent">Pool의 부모 트랜스폼</param>
    /// <param name="initSize">초기 Pool 크기</param>
    /// <param name="includeInReturnAll">ReturnAllPools() 강제 반환 대상 여부(기본 true)</param>
    public Pool(GameObject prefab, Transform parent, int initSize, bool includeInReturnAll = true)
    {
        _prefab = prefab;
        _parent = parent;
        _pool = new Stack<GameObject>(initSize);
        _activeObjects = new HashSet<GameObject>();
        _includeInReturnAll = includeInReturnAll;

        for (int i = 0; i < initSize; i++)
        {
            CreatePoolObj();
        }
    }

    /// <summary>
    /// Pool에 새 복제본 게임오브젝트를 추가하는 함수
    /// </summary>
    void CreatePoolObj()
    {
        // 원본 프리팹을 복제해서 새 게임오브젝트 생성
        GameObject go = Object.Instantiate(_prefab);

        // 새 게임오브젝트의 부모를 Pool의 부모로 설정
        go.transform.SetParent(_parent);

        // 새 게임오브젝트를 비활성화
        go.SetActive(false);

        // 새 게임오브젝트에서 Poolable 컴포넌트를 가져옴
        Poolable poolable = go.GetComponent<Poolable>();

        // Poolable 컴포넌트가 없으면 새로 만듦
        if (poolable == null)
        {
            poolable = go.AddComponent<Poolable>();
        }

        // Poolable 초기화
        poolable.Initialize(this);

        // 새 게임오브젝트를 스택에 추가
        _pool.Push(go);
    }

    /// <summary>
    /// Pool에서 게임오브젝트를 가져오는 함수
    /// Pool이 비었으면 새로 생성해 반환
    /// </summary>
    /// <returns></returns>
    public GameObject Pop()
    {
        if (_pool.Count == 0)
        {
            CreatePoolObj();
        }

        GameObject go = _pool.Pop();
        go.SetActive(true);

        // 나가는 오브젝트 추적 등록
        _activeObjects.Add(go);

        return go;
    }

    /// <summary>
    /// 게임오브젝트를 Pool로 되돌리는 함수
    /// </summary>
    /// <param name="go"></param>
    public void Push(GameObject go)
    {
        // 이미 Pool에 반환되어 있는 오브젝트면 무시(중복 반환 방지)
        if (_activeObjects.Contains(go) == false)
        {
            Debug.LogWarning($"{go.name}이 이미 Pool에 반환되어 있음(중복 Push 무시)");
            return;
        }
        // 씬 전환되면서 사라질 때 오류 방지
        if (go == null)
        {
            Debug.LogWarning("오브젝트가 이미 사라져서 반환 불가");
            return;
        }

        _activeObjects.Remove(go);

        // 반환 직전 정리 콜백(정상/강제 반환 모두 동일하게 호출됨)
        if (go.TryGetComponent(out IPoolReturnHandler handler))
        {
            handler.OnBeforeReturnToPool();
        }
        go.transform.SetParent(_parent);
        go.SetActive(false);
        _pool.Push(go);
    }

    /// <summary>
    /// 나가있는 모든 오브젝트를 강제로 Pool에 반환하는 함수
    /// 스테이지 전환/재시작 시 남아있는 포탄, 아이템 등을 정리할 때 사용
    /// </summary>
    public void ReturnAll()
    {
        // 순회 중 _activeObjects가 변경되므로 복사해서 순회
        GameObject[] activeArray = new GameObject[_activeObjects.Count];
        _activeObjects.CopyTo(activeArray);

        foreach (GameObject go in activeArray)
        {
            Push(go);
        }
    }
}