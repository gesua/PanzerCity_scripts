using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이템 드랍 관련
/// </summary>
public class ItemDropper : MonoBehaviour
{
    /// <summary>
    /// 드랍 확률 체크 및 아이템 선택
    /// </summary>
    public void TryDrop(int dropChance, int dropGroupID)
    {
        if (dropChance <= 0 || dropGroupID <= 0) return;

        // 드랍 확률 체크
        if (Random.Range(0, 100) >= dropChance) return;

        // 드랍 그룹에서 아이템 선택
        List<ItemDropGroupData> dropGroup = GameManager.Instance.DataManager.GetDropGroup(dropGroupID);
        if (dropGroup == null || dropGroup.Count == 0) return;

        // SpawnWeight 기반 랜덤 선택
        int totalWeight = 0;
        foreach (ItemDropGroupData data in dropGroup)
            totalWeight += data.SpawnWeight;

        int random = Random.Range(0, totalWeight);
        int cumulative = 0;
        ItemDropGroupData selectedDrop = null;

        foreach (ItemDropGroupData data in dropGroup)
        {
            cumulative += data.SpawnWeight;
            if (random < cumulative)
            {
                selectedDrop = data;
                break;
            }
        }

        if (selectedDrop == null) return;

        SpawnDroppedItem(selectedDrop.ItemID);
    }

    /// <summary>
    /// 아이템 스폰
    /// </summary>
    void SpawnDroppedItem(int itemID)
    {
        ItemConfig itemConfig = GameManager.Instance.DataManager.GetItemConfig(itemID);
        if (itemConfig == null) return;

        GameObject itemGo = GameManager.Instance.PoolManager.GetFromPool("DroppedItem");
        itemGo.transform.position = transform.position;
        itemGo.GetComponent<DroppedItem>().Initialize(itemConfig);
    }
}
