using System;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 아이템 1개의 저장 데이터
/// </summary>
[Serializable]
public class ItemSaveData
{
    public int ItemID;
    public int PosX;
    public int PosY;
    public bool IsRotated;
}

/// <summary>
/// 세이브 슬롯 1개의 데이터
/// </summary>
[Serializable]
public class SaveData
{
    public bool IsEmpty = true;        // 빈 슬롯 여부
    public string SaveName = "";       // 세이브 이름(사용자 수정 가능)
    public string LastPlayedDate = ""; // 마지막 플레이 시각(표시용)

    public int CurrentStageID; // 이어할 스테이지 ID

    // PlayerData
    public int Gold;
    public int Life;

    // 인벤토리
    public List<ItemSaveData> Items = new List<ItemSaveData>();

    // 장비(미장착이면 -1)
    public int MainGunItemID = -1;
    public int TurretItemID = -1;
    public int HullItemID = -1;

    // GameStatistics
    public int TotalGoldEarned;
    public int ItemsUsed;
    public int ShellKills;
    public int DeathCount;
    public float PlayTime;
}
