using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 아이템 드랍 관련
/// </summary>
public class ItemDropper : MonoBehaviour
{
    public event Action<DroppedItem> OnItemDropped;

    bool _isMultiplayer; // 멀티플레이 여부(EnemyTank가 SetNetworkOwner 시점에 전달)

    /// <summary>
    /// 멀티플레이 여부 설정(EnemyTank가 호출)
    /// </summary>
    public void SetNetworkMode(bool isMultiplayer)
    {
        _isMultiplayer = isMultiplayer;
    }

    /// <summary>
    /// 드랍 확률 체크 및 아이템 선택
    /// </summary>
    public void TryDrop(int dropChance, int dropGroupID)
    {
        if (dropChance <= 0 || dropGroupID <= 0) return;

        // 드랍 확률 체크
        if (UnityEngine.Random.Range(0, 100) >= dropChance) return;

        // 드랍 그룹에서 아이템 선택
        List<ItemDropGroupData> dropGroup = GameManager.Instance.DataManager.GetDropGroup(dropGroupID);
        if (dropGroup == null || dropGroup.Count == 0) return;

        // SpawnWeight 기반 랜덤 선택
        int totalWeight = 0;
        foreach (ItemDropGroupData data in dropGroup)
        {
            totalWeight += data.SpawnWeight;
        }

        int random = UnityEngine.Random.Range(0, totalWeight);
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

        string poolKey = (_isMultiplayer) ? "DroppedItem_Multi" : "DroppedItem";
        GameObject itemGo = GameManager.Instance.PoolManager.GetFromPool(poolKey);

        itemGo.transform.position = transform.position + Vector3.up;
        if (itemGo.TryGetComponent(out DroppedItem droppedItem))
        {
            droppedItem.Initialize(itemConfig);
        }

        // 멀티플레이:네트워크 오브젝트로 스폰(클라이언트에 자동 복제)
        if (_isMultiplayer && itemGo.TryGetComponent(out NetworkObject networkObject))
        {
            if (itemGo.TryGetComponent(out DroppedItemNetworkOwner networkOwner))
            {
                networkOwner.SetPendingItemId(itemID); // 클라이언트 아이콘 동기화용(스폰 전에 값 세팅 필요)
            }
            networkObject.Spawn();
        }

        // 아이템 드랍 소리(일괄 처치 중엔 개별 3D 재생 대신 큐잉 -> 종료 시 한 번만 2D 재생)
        if (GameManager.Instance.AudioManager.IsMassKillInProgress)
        {
            GameManager.Instance.AudioManager.QueueMassKillSfx(SfxType.ItemDrop);
        }
        else
        {
            GameManager.Instance.AudioManager.PlaySfxAtPoint(SfxType.ItemDrop, itemGo.transform.position);
        }

        OnItemDropped?.Invoke(droppedItem);
    }
}