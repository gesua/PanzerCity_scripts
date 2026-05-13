using UnityEngine;

/// <summary>
/// 바닥에 떨어진 아이템
/// </summary>
public class DroppedItem : MonoBehaviour
{
    [SerializeField] SpriteRenderer _icon;
    [SerializeField] ItemConfig _itemConfig;

    public ItemConfig ItemConfig => _itemConfig;

    public void Initialize(ItemConfig itemConfig)
    {
        _itemConfig = itemConfig;
        _icon.sprite = itemConfig.IconSprite;
    }

    /// <summary>
    /// 획득
    /// </summary>
    public void Pickup()
    {
        Debug.Log("아이템 풀로 되돌아감 // 현재 막아놓음");

        //gameObject.DestroyOrReturnToPool();
    }
}
