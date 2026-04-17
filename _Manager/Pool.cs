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

    /// <summary>
    /// Pool 생성자
    /// </summary>
    /// <param name="prefab">Pooling할 프리팹</param>
    /// <param name="parent">Pool의 부모 트랜스폼</param>
    /// <param name="initSize">초기 Pool 크기</param>
    public Pool(GameObject prefab, Transform parent, int initSize)
    {
        _prefab = prefab;
        _parent = parent;
        _pool = new Stack<GameObject>(initSize);

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
        // Pool에 남은 게임오브젝트가 있는 경우
        if (_pool.Count > 0)
        {
            GameObject go = _pool.Pop();
            //go.SetActive(true); HACK:포탄 사라지는거 해결중
            return go;
        }

        // Pool에 남은 게임오브젝트가 없는 경우 새로 만들어서 반환
        GameObject newGo = Object.Instantiate(_prefab);

        Poolable poolable = newGo.GetComponent<Poolable>();
        if (poolable == null)
        {
            poolable = newGo.AddComponent<Poolable>();
        }
        poolable.Initialize(this);
        return newGo;
    }

    /// <summary>
    /// 게임오브젝트를 Pool로 되돌리는 함수
    /// </summary>
    /// <param name="go"></param>
    public void Push(GameObject go)
    {
        go.transform.SetParent(_parent);
        go.SetActive(false);
        _pool.Push(go);
    }
}
