using System.Collections.Generic;

/// <summary>
/// 아이템 관련 데이터 (Item_Master 참조)
/// </summary>
[System.Serializable]
public class ItemMasterData
{
    public int ItemID;
    public string ItemName;
    public string NameKey;
    public string DescKey;
    public string ItemType;
    public string EquipSlot;
    public int BuyPrice;
    public string Skill;
}

[System.Serializable]
public class ItemMasterList
{
    public List<ItemMasterData> list;
}