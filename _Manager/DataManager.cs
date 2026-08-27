using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 게임 데이터들 관리
/// </summary>
public class DataManager : MonoBehaviour
{
    [System.Serializable] class TankDataList { public List<TankData> list; }
    [System.Serializable] class EnemySpawnDataList { public List<EnemySpawnData> list; }
    [System.Serializable] class ItemDropGroupList { public List<ItemDropGroupData> list; }
    [System.Serializable] class StageStatusDataList { public List<StageStatusData> list; }

    Dictionary<int, TankData> _tankDataDict = new Dictionary<int, TankData>();
    Dictionary<int, List<int>> _spawnDataDict = new Dictionary<int, List<int>>();
    Dictionary<int, ItemMasterData> _itemMasterDict = new();
    Dictionary<int, List<ItemDropGroupData>> _itemDropGroupDict = new();
    Dictionary<int, ItemConfig> _itemConfigDict = new();
    Dictionary<int, StageStatusData> _stageStatusDict = new();

    int _lastStageID;

    public int LastStageID => _lastStageID;

    public void Initialize()
    {
        LoadAllData();
    }

    void LoadAllData()
    {
        LoadTankData();
        LoadEnemySpawnData();
        LoadStageStatusData();
        LoadItemData();
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

            if (data.StageID > _lastStageID) _lastStageID = data.StageID; // 마지막 스테이지 입력
        }

        // HACK:임시로 마지막 스테이지 변경
        //_lastStageID = 7125;
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

    /// <summary>
    /// 스테이지 ID를 씬 이름으로 변환 (예: 7101 → "Stage01")
    /// </summary>
    public string StageIDToSceneName(int stageID)
    {
        return "Stage" + (stageID - 7100).ToString("D2");
    }

    /// <summary>
    /// 스테이지 기본 설정 데이터 가져오기
    /// </summary>
    void LoadStageStatusData()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/Stage_Status");
        StageStatusDataList dataList = JsonUtility.FromJson<StageStatusDataList>(json.text);

        foreach (StageStatusData data in dataList.list)
        {
            _stageStatusDict[data.StageID] = data;
        }
    }

    /// <summary>
    /// stageID로 스테이지 기본 설정 가져오기
    /// </summary>
    public StageStatusData GetStageStatusData(int stageID)
    {
        if (_stageStatusDict.TryGetValue(stageID, out StageStatusData data)) return data;

        Debug.LogWarning($"StageStatusData 없음:{stageID}");
        return null;
    }

    /// <summary>
    /// 아이템 관련 Json 가져오기
    /// </summary>
    void LoadItemData()
    {
        // Item_Master 로드
        TextAsset masterJson = Resources.Load<TextAsset>("Data/Item_Master");
        ItemMasterList masterList = JsonUtility.FromJson<ItemMasterList>(masterJson.text);
        foreach (ItemMasterData data in masterList.list)
        {
            _itemMasterDict[data.ItemID] = data;
        }

        // Item_DropGroup 로드
        TextAsset dropJson = Resources.Load<TextAsset>("Data/Item_DropGroup");
        ItemDropGroupList dropList = JsonUtility.FromJson<ItemDropGroupList>(dropJson.text);
        foreach (ItemDropGroupData data in dropList.list)
        {
            if (_itemDropGroupDict.ContainsKey(data.DropGroupID) == false)
            {
                _itemDropGroupDict[data.DropGroupID] = new List<ItemDropGroupData>();
            }
            _itemDropGroupDict[data.DropGroupID].Add(data);
        }

        // ItemConfig 에셋들 로드
        ItemConfig[] itemConfigs = Resources.LoadAll<ItemConfig>("Items");
        foreach (ItemConfig itemConfig in itemConfigs)
        {
            _itemConfigDict[itemConfig.Id] = itemConfig;
        }
    }

    public ItemMasterData GetItemMasterData(int itemID)
    {
        _itemMasterDict.TryGetValue(itemID, out ItemMasterData data);
        return data;
    }

    public List<ItemDropGroupData> GetDropGroup(int dropGroupID)
    {
        _itemDropGroupDict.TryGetValue(dropGroupID, out List<ItemDropGroupData> list);
        return list;
    }

    public ItemConfig GetItemConfig(int itemID)
    {
        _itemConfigDict.TryGetValue(itemID, out ItemConfig config);
        return config;
    }
}