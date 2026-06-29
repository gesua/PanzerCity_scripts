using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 슬롯 저장/불러오기 관리
/// </summary>
public class SaveManager : MonoBehaviour
{
    const int SLOT_COUNT = 6; // 슬롯 개수

    public enum SaveLoadMode { None, NewGame, Continue } // 현재 플레이 진입 방식(튜토리얼은 None)

    SaveData[] _slotCache; // 슬롯별 데이터 캐시(파일 입출력 최소화)

    public int SlotCount => SLOT_COUNT;
    public SaveLoadMode Mode { get; private set; }
    public int CurrentSlotIndex { get; private set; }

    /// <summary>
    /// 모든 슬롯 데이터를 디스크에서 읽어와 캐시에 보관
    /// </summary>
    public void Initialize()
    {
        _slotCache = new SaveData[SLOT_COUNT];

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            string path = GetSlotFilePath(i);

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _slotCache[i] = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                _slotCache[i] = new SaveData(); // 빈 슬롯 기본값
            }
        }
    }

    /// <summary>
    /// 슬롯 데이터 가져오기(UI 표시, 이어하기 판단용)
    /// </summary>
    public SaveData GetSlotData(int slotIndex)
    {
        return _slotCache[slotIndex];
    }

    /// <summary>
    /// 새 게임으로 진입
    /// </summary>
    public void StartNewGame(int slotIndex)
    {
        CurrentSlotIndex = slotIndex;
        Mode = SaveLoadMode.NewGame;
    }

    /// <summary>
    /// 저장된 슬롯으로 이어하기
    /// </summary>
    public void ContinueGame(int slotIndex)
    {
        CurrentSlotIndex = slotIndex;
        Mode = SaveLoadMode.Continue;
    }

    /// <summary>
    /// 세이브 시스템을 사용하지 않는 진입(튜토리얼 등)
    /// </summary>
    public void SetTutorialMode()
    {
        Mode = SaveLoadMode.None;
    }

    /// <summary>
    /// 스테이지 시작 시점 자동저장
    /// </summary>
    public void SaveCurrentProgress(int stageID, InventoryPresenter inventoryPresenter)
    {
        SaveData data = _slotCache[CurrentSlotIndex];

        data.IsEmpty = false;
        data.SaveName = (string.IsNullOrEmpty(data.SaveName) == false) ? data.SaveName : ("Save " + (CurrentSlotIndex + 1));
        data.LastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.CurrentStageID = stageID;

        PlayerData playerData = GameManager.Instance.PlayerData;
        data.Gold = playerData.Gold;
        data.Life = playerData.Life;

        data.Items = inventoryPresenter.ToSaveData();

        EquipmentManager equipmentManager = GameManager.Instance.EquipmentManager;
        data.MainGunItemID = (equipmentManager.MainGunSlot != null) ? equipmentManager.MainGunSlot.Config.Id : -1;
        data.TurretItemID = (equipmentManager.TurretSlot != null) ? equipmentManager.TurretSlot.Config.Id : -1;
        data.HullItemID = (equipmentManager.HullSlot != null) ? equipmentManager.HullSlot.Config.Id : -1;

        GameStatistics stats = GameManager.Instance.GameStatistics;
        data.TotalGoldEarned = stats.TotalGoldEarned;
        data.ItemsUsed = stats.ItemsUsed;
        data.ShellKills = stats.ShellKills;
        data.DeathCount = stats.DeathCount;
        data.PlayTime = stats.GetPlayTime();

        WriteSlotToDisk(CurrentSlotIndex);
    }

    /// <summary>
    /// 이어하기 — 저장된 데이터를 각 매니저에 적용
    /// </summary>
    public void ApplyLoadedData(InventoryPresenter inventoryPresenter)
    {
        SaveData data = _slotCache[CurrentSlotIndex];

        GameManager.Instance.PlayerData.LoadFromSaveData(data.Gold, data.Life);
        GameManager.Instance.EquipmentManager.LoadFromSaveData(data.MainGunItemID, data.TurretItemID, data.HullItemID);
        inventoryPresenter.LoadFromSaveData(data.Items);
        GameManager.Instance.GameStatistics.LoadFromSaveData(data.TotalGoldEarned, data.ItemsUsed, data.ShellKills, data.DeathCount, data.PlayTime);
    }

    /// <summary>
    /// 새 게임 — 직전 플레이 데이터 초기화(앱을 재시작하지 않고 다른 슬롯으로 새로 시작하는 경우 대비)
    /// </summary>
    public void ResetForNewGame()
    {
        GameManager.Instance.PlayerData.Initialize(0, 3);
        GameManager.Instance.EquipmentManager.UnequipAll();
        GameManager.Instance.GameStatistics.ResetAll();
    }

    /// <summary>
    /// 슬롯 삭제
    /// </summary>
    public void DeleteSlot(int slotIndex)
    {
        string path = GetSlotFilePath(slotIndex);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        _slotCache[slotIndex] = new SaveData();
    }

    /// <summary>
    /// 슬롯 이름 변경
    /// </summary>
    public void RenameSlot(int slotIndex, string newName)
    {
        _slotCache[slotIndex].SaveName = newName;

        if (_slotCache[slotIndex].IsEmpty) return; // 빈 슬롯이면 파일에 쓰지 않음

        WriteSlotToDisk(slotIndex);
    }

    /// <summary>
    /// 슬롯 데이터를 JSON 파일로 저장
    /// </summary>
    void WriteSlotToDisk(int slotIndex)
    {
        string json = JsonUtility.ToJson(_slotCache[slotIndex], true);
        File.WriteAllText(GetSlotFilePath(slotIndex), json);
    }

    /// <summary>
    /// 슬롯 파일 경로
    /// </summary>
    string GetSlotFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.json");
    }
}
