using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Consumable, // 소모성
    Equipment,  // 장비
}

public enum EquipSlot
{
    None,
    Turret,  // 포탑
    Hull,    // 차체
    MainGun, // 주포
}

// 에디터 용도
[System.Serializable]
public class StatBonus
{
    public StatType StatType;
    public float Value;
}

public enum StatType
{
    ShellDamage,
    ShellSpeed,
    ExplosionRadius,
    Reload,
    TurretRotSpeed,
    ForwardSpeed,
    RotSpeed
}

/// <summary>
/// 아이템 설정 데이터
/// </summary>
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Item/ItemConfig")]
public class ItemConfig : ScriptableObject
{
    [Header("----- 기본 정보 -----")]
    [SerializeField] int _id;                       // ID
    [SerializeField] string _itemName;              // 이름
    [TextArea(3,5)][SerializeField] string _desc;   // 설명
    [SerializeField] string _nameKey;               // 이름 키 값
    [SerializeField] string _descKey;               // 설명 키 값
    [SerializeField] int _buyPrice;                 // 가격
    [SerializeField] string _skill;                 // 아이템 스킬
    [SerializeField] ItemType _itemType;            // 타입
    [SerializeField] Sprite _iconSprite;            // 스프라이트
    [SerializeField] bool _autoUse;                 // 줍자마자 즉시 사용

    [Header("----- 장비 설정 -----")]
    [SerializeField] EquipSlot _equipSlot; // 장착 슬롯 (장비 아이템만)
    [SerializeField] Vector2Int[] _occupiedCells;  // 차지하는 셀 좌표 배열

    [Header("----- 장비 스탯 -----")]
    [SerializeField] List<StatBonus> _statBonuses = new();

    public int Id => _id;
    public ItemType ItemType => _itemType;
    public string ItemName => _itemName;
    public string Desc => _desc;
    public int BuyPrice => _buyPrice;
    public Sprite IconSprite => _iconSprite;
    public bool AutoUse => _autoUse;

    public EquipSlot EquipSlot => _equipSlot;
    public Vector2Int[] OccupiedCells => _occupiedCells;
    public List<StatBonus> StatBonuses => _statBonuses;
}