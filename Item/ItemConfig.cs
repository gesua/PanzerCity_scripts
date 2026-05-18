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
    Gun,     // 주포
}

/// <summary>
/// 아이템 설정 데이터
/// </summary>
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Item/ItemConfig")]
public class ItemConfig : ScriptableObject
{
    [Header("----- 기본 정보 -----")]
    [SerializeField] int _id;                       // ID
    [SerializeField] string _name;                  // 이름
    [SerializeField] ItemType _itemType;            // 타입
    [TextArea(3,5)][SerializeField] string _desc;   // 설명
    [SerializeField] int _price;                    // 가격
    [SerializeField] Sprite _iconSprite;            // 스프라이트

    [Header("----- 장비 설정 -----")]
    [SerializeField] EquipSlot _equipSlot; // 장착 슬롯 (장비 아이템만)
    [SerializeField] Vector2Int[] _occupiedCells;  // 차지하는 셀 좌표 배열

    // ItemEffect 만들고 작업
    //[Header("----- 소모성 설정 -----")]
    //public ItemEffect _useEffect; // 효과 수치

    public int Id => _id;
    public ItemType ItemType => _itemType;
    public string Name => _name;
    public string Desc => _desc;
    public int Price => _price;
    public Sprite IconSprite => _iconSprite;
    public Vector2Int[] OccupiedCells => _occupiedCells;
    //public ItemEffect UseEffect => _useEffect;
}