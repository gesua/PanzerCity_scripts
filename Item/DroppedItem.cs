using UnityEngine;

/// <summary>
/// 바닥에 떨어진 아이템
/// </summary>
public class DroppedItem : MonoBehaviour
{
    [SerializeField] SpriteRenderer _icon;

    ItemConfig _itemConfig;

    public ItemConfig ItemConfig => _itemConfig;

    public void Initialize(ItemConfig itemConfig)
    {
        _itemConfig = itemConfig;
        _icon.sprite = itemConfig.IconSprite;
    }

    public void Pickup()
    {
        gameObject.DestroyOrReturnToPool();
    }
}
