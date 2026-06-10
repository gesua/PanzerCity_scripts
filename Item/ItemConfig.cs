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

    [Header("----- 장비 설정 -----")]
    [SerializeField] EquipSlot _equipSlot; // 장착 슬롯 (장비 아이템만)
    [SerializeField] Vector2Int[] _occupiedCells;  // 차지하는 셀 좌표 배열

    [Header("----- 장비 스탯(주포) -----")]
    [SerializeField] float _shellSpeedBonus;      // 포탄 속력
    [SerializeField] float _explosionRadiusBonus; // 폭발 범위
    [SerializeField] float _reloadBonus;          // 최소 장전 시간
    [SerializeField] int _shellDamageBonus;       // 포탄 대미지
    [Header("----- 장비 스탯(포탑) -----")]
    [SerializeField] float _turretRotSpeedBonus;  // 포탑 회전 속력
    [Header("----- 장비 스탯(차체) -----")]
    [SerializeField] float _forwardSpeedBonus;    // 최대 전진 속력
    [SerializeField] float _rotSpeedBonus;        // 차체 회전 속력

    public int Id => _id;
    public ItemType ItemType => _itemType;
    public string ItemName => _itemName;
    public string Desc => _desc;
    public int BuyPrice => _buyPrice;
    public Sprite IconSprite => _iconSprite;
    public EquipSlot EquipSlot => _equipSlot;
    public Vector2Int[] OccupiedCells => _occupiedCells;

    public float ShellSpeedBonus => _shellSpeedBonus;
    public float ExplosionRadiusBonus => _explosionRadiusBonus;
    public float ReloadBonus => _reloadBonus;
    public int ShellDamageBonus => _shellDamageBonus;
    public float TurretRotSpeedBonus => _turretRotSpeedBonus;
    public float ForwardSpeedBonus => _forwardSpeedBonus;
    public float RotSpeedBonus => _rotSpeedBonus;
}