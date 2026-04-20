using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 게임 데이터들 관리
/// </summary>
public class DataManager : MonoBehaviour
{
    Dictionary<int, TankData> _tankDataDict = new Dictionary<int, TankData>();

    [System.Serializable]
    class TankDataList { public List<TankData> list; }

    public void Initialize()
    {
        LoadAllData();
    }

    void LoadAllData()
    {
        LoadTankData();
        // 나중에 다른 데이터 추가
        // LoadItemData();
        // LoadStageData();
    }

    void LoadTankData()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/Tank_Status");
        TankDataList dataList = JsonUtility.FromJson<TankDataList>(json.text);
        foreach (TankData data in dataList.list)
        {
            _tankDataDict[data.TankID] = data;
        }
    }

    public TankData GetTankData(int tankID)
    {
        if (_tankDataDict.TryGetValue(tankID, out TankData data)) return data;
        Debug.LogWarning($"TankData 없음: {tankID}");
        return null;
    }
}