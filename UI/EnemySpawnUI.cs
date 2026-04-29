using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽에 적 스폰 현황을 보여주는 UI
/// </summary>
public class EnemySpawnUI : MonoBehaviour
{
    [SerializeField] Image[] _enemyIcons;           // 적 아이콘
    [SerializeField] Sprite[] _tankSprites;         // TankID별 스프라이트 (순서 중요)

    /// <summary>
    /// 스테이지 시작 시 아이콘 전부 생성
    /// </summary>
    public void Initialize(List<int> spawnList)
    {
        for (int i = 0; i < spawnList.Count; i++)
        {
            _enemyIcons[i].enabled = true;
            _enemyIcons[i].sprite = GetSprite(spawnList[i]);
        }
    }

    /// <summary>
    /// 적 스폰시 해당 아이콘 사라짐
    /// </summary>
    public void SetEnemySpawn(int order)
    {
        if (order >= _enemyIcons.Length) return;
        _enemyIcons[order].enabled = false;
    }

    /// <summary>
    /// tankID에 맞게 스프라이트 가져옴
    /// TODO:보스는 301부터 시작이니까 바꿔야함
    /// </summary>
    Sprite GetSprite(int tankID)
    {
        int id = tankID - 201;

        if (id < 0 || id >= _tankSprites.Length)
        {
            Debug.LogWarning($"TankSprite 없음:{tankID}");
            return null;
        }

        return _tankSprites[id];
    }
}
