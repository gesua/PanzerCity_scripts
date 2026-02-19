using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class Util
{
    public const float Epsilon = 0.01f;

    /*// <summary>
    /// 게임오브젝트가 오브젝트 풀링을 사용하면 Pool로 되돌리고
    /// Pooling을 하지않는 게임오브젝트면 파괴하는 함수
    /// </summary>
    public static void DestroyOrReturnToPool(GameObject go)
    {
        Poolable poolable = go.GetComponent<Poolable>();
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
        else
        {
            Object.Destroy(go);
        }
    }*/

    // 제네릭(타입을 마치 변수처럼 다루는 방식)
    /// <summary>
    /// 요소들 중 랜덤한 1개 요소를 골라 반환하는 함수
    /// </summary>
    public static T ChooseRandom<T>(IReadOnlyList<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogError("비어있거나 없는 리스트입니다.");
            return default(T); // 해당 타입의 디폴트 타입을 반환 (0,false,null...)
        }

        int index = Random.Range(0, list.Count);
        return list[index];
    }

    /// <summary>
    /// 확률에 따라 선택된 순번(Index)를 반환하는 함수
    /// </summary>
    public static int Choose(IReadOnlyList<float> probs)
    {
        // 전체 확률 합
        float total = 0;

        foreach (var prob in probs)
        {
            if (prob > 0) total += prob; // 음수 제외
        }

        float randomValue = Random.value * total;

        for (int i = 0; i < probs.Count; i++)
        {
            if (probs[i] <= 0) continue; // 확률이 0이하면 당첨 제외

            if (randomValue <= probs[i])
                return i;
            else
                randomValue -= probs[i];
        }

        Debug.LogWarning("랜덤으로 골라진게 없음");
        return 0; // 당첨된게 없음
    }

    /// <summary>
    /// 리스트(배열)를 랜덤한 순서로 셔플하는 함수
    /// </summary>
    public static void Shuffle<T>(IList<T> list)
    {
        if (list == null || list.Count == 0) return;

        for (int i = 0; i < list.Count; i++)
        {
            int k = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[k];
            list[k] = temp;
        }
    }

    /// <summary>
    /// 게임 오브젝트에서 T타입의 컴포넌트를 찾아 반환하는 함수
    /// 없으면 새로 추가해서 반환
    /// </summary>
    /// <typeparam name="T">컴포넌트 타입</typeparam>
    /// <param name="go">대상 게임 오브젝트</param>
    /// <returns>찾거나 추가된 컴포넌트</returns>
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>(); // 찾고
        if (component == null) component = go.AddComponent<T>(); // 없으면 만듦
        return component;
    }
}
