using System.Collections;
using UnityEngine;

/// <summary>
/// 바닥에 떨어진 아이템
/// </summary>
public class DroppedItem : MonoBehaviour
{
    [SerializeField] SpriteRenderer _icon;
    [SerializeField] SpriteRenderer _minimapIcon;
    ItemConfig _itemConfig;

    // 미니맵에서 아이콘 깜빡임
    float _blinkInterval = 0.5f;
    Coroutine _blinkRoutine;

    public ItemConfig ItemConfig => _itemConfig;

    public void Initialize(ItemConfig itemConfig)
    {
        _itemConfig = itemConfig;
        _icon.sprite = itemConfig.IconSprite;
        _minimapIcon.sprite = itemConfig.IconSprite;
    }
    void OnEnable()
    {
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    void OnDisable()
    {
        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
    }

    /// <summary>
    /// 획득
    /// </summary>
    public void Pickup()
    {
        gameObject.DestroyOrReturnToPool();
    }

    /// <summary>
    /// 미니맵에서 아이콘 깜빡임
    /// </summary>
    /// <returns></returns>
    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            _minimapIcon.enabled = !_minimapIcon.enabled;
            yield return new WaitForSeconds(_blinkInterval);
        }
    }
}
