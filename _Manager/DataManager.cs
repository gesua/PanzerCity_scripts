using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 게임 데이터들 관리
/// </summary>
public class DataManager : MonoBehaviour
{
    [System.Serializable]
    class TankDataList { public List<TankData> list; }
    Dictionary<int, TankData> _tankDataDict = new Dictionary<int, TankData>();

    [System.Serializable]
    class EnemySpawnDataList { public List<EnemySpawnData> list; }
    Dictionary<int, List<int>> _spawnDataDict = new Dictionary<int, List<int>>();

    public void Initialize()
    {
        LoadAllData();
    }

    void LoadAllData()
    {
        LoadTankData();
        LoadEnemySpawnData();
        // 나중에 다른 데이터 추가
        // LoadItemData();
        // LoadStageData();
    }

    /// <summary>
    /// 탱크 종류별 스탯들 가져오기(TankData)
    /// </summary>
    void LoadTankData()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/Tank_Status");
        TankDataList dataList = JsonUtility.FromJson<TankDataList>(json.text);
        foreach (TankData data in dataList.list)
        {
            _tankDataDict[data.TankID] = data;
        }
    }

    /// <summary>
    /// tankID로 TankData 가져오기
    /// </summary>
    public TankData GetTankData(int tankID)
    {
        if (_tankDataDict.TryGetValue(tankID, out TankData data)) return data;
        Debug.LogWarning($"TankData 없음:{tankID}");
        return null;
    }

    /// <summary>
    /// 스테이지 스폰 순서 가져오기(EnemySpawnData)
    /// </summary>
    void LoadEnemySpawnData()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/Stage_EnemySpawn");
        EnemySpawnDataList dataList = JsonUtility.FromJson<EnemySpawnDataList>(json.text);

        foreach (EnemySpawnData data in dataList.list)
        {
            if (_spawnDataDict.ContainsKey(data.StageID) == false) _spawnDataDict[data.StageID] = new List<int>();
            _spawnDataDict[data.StageID].Add(data.TankID);
        }
    }

    /// <summary>
    /// stageID로 스폰 순서 가져오기
    /// </summary>
    public List<int> GetSpawnList(int stageID)
    {
        if (_spawnDataDict.TryGetValue(stageID, out List<int> list)) return list;
        Debug.LogWarning($"SpawnData 없음:{stageID}");
        return null;
    }

}