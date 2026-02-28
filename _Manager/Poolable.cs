using UnityEngine;

/// <summary>
/// Pooling된 게임오브젝트를 관리하는 역할
/// Pool에서 가져온 게임오브젝트를 Pool로 반환하는 기능
/// </summary>
public class Poolable : MonoBehaviour
{
    /// <summary>
    /// 자신 게임오브젝트가 생성된 Pool
    /// </summary>
    Pool _pool;

    /// <summary>
    /// 초기화 함수
    /// </summary>
    /// <param name="pool"></param>
    public void Initialize(Pool pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// 자신 게임오브젝트를 Pool로 되돌리는 함수
    /// </summary>
    public void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Push(gameObject);
        }
        else // Pool이 없으면 파괴
        {
            Destroy(gameObject);
        }
    }
}
