using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 확장형으로 아무렇게나 .찍고 쓸 수 있음
/// </summary>
public static class Extension
{
    /*// <summary>
    /// 게임오브젝트가 오브젝트 풀링을 사용하면 Pool로 되돌리고
    /// Pooling을 하지않는 게임오브젝트면 파괴하는 함수
    /// </summary>
    public static void DestroyOrReturnToPool(this GameObject go)
    {
        Util.DestroyOrReturnToPool(go);
    }*/

    /// <summary>
    /// 요소들 중 랜덤한 1개 요소를 골라 반환하는 함수
    /// </summary>
    public static T ChooseRandom<T>(this IReadOnlyList<T> list)
    {
        return Util.ChooseRandom<T>(list);
    }

    /// <summary>
    /// 확률에 따라 선택된 순번(Index)를 반환하는 함수
    /// </summary>
    public static int Choose(this IReadOnlyList<float> probs)
    {
        return Util.Choose(probs);
    }

    /// <summary>
    /// 리스트(배열)를 랜덤한 순서로 셔플하는 함수
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
        Util.Shuffle<T>(list);
    }

    /// <summary>
    /// 게임 오브젝트에서 T타입의 컴포넌트를 찾아 반환하는 함수
    /// 없으면 새로 추가해서 반환
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <param name="go">대상 게임 오브젝트</param>
    /// <returns>찾거나 추가된 컴포넌트</returns>
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return Util.GetOrAddComponent<T>(go);
    }

    /// <summary>
    /// layerMask가 layer를 포함하고 있는지 확인하는 함수
    /// </summary>
    public static bool Contains(this LayerMask layerMask, int layer)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}