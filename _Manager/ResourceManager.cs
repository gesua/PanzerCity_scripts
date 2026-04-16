using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유니티의 Resources 폴더를 활용해 게임의 리소스를 관리하는 매니저
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // 로드한 프리팹 리소스들을 저장하는 딕셔너리(캐시)
    Dictionary<string, GameObject> _prefabMap = new(); // new 다 안 써도 됨

    /// <summary>
    /// 지정 경로의 프리팹을 로드해 반환하는 함수
    /// 이미 로드되어 있으면 맵에서 찾아 반환하고, 맵에 없으면 새로 로드
    /// </summary>
    /// <param name="path">Resources 폴더 안의 프리팹 경로</param>
    /// <returns></returns>
    public GameObject GetPrefab(string path)
    {
        // 이미 캐시에 로드한 프리팹이 저장되어 있으면
        if (_prefabMap.ContainsKey(path) == true)
        {
            // 있는거 반환
            return _prefabMap[path];
        }

        // 없으면 새로 로드
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"{path} 경로에 프리팹이 존재하지 않습니다.");
        }
        else
        {
            _prefabMap[path] = prefab;
        }
        return prefab;
    }
}