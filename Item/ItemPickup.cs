using UnityEngine;

/// <summary>
/// 아이템 획득하는 스크립트
/// 플레이어한테 붙임
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [SerializeField] GameObject _pickupUI; // 획득 UI
    [SerializeField] float _pickupRange = 2f; // 획득 거리
    [SerializeField] LayerMask _itemLayer;

    float _detectInterval = 0.2f; // 아이템 재탐색 시간
    float _detectTimer;

    DroppedItem _nearestItem; // 획득할 가까운 아이템
    InventoryPresenter _inventoryPresenter;

    public void Initialize(InventoryPresenter inventoryPresenter)
    {
        _inventoryPresenter = inventoryPresenter;
    }

    void Update()
    {
        // 주울 수 있는 아이템 체크
        _detectTimer += Time.deltaTime;
        if (_detectTimer >= _detectInterval)
        {
            _detectTimer = 0f;
            DetectNearbyItem();
        }
    }

    /// <summary>
    /// 주울 수 있는 아이템 체크
    /// </summary>
    void DetectNearbyItem()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _pickupRange, _itemLayer);

        if (colliders.Length == 0)
        {
            _nearestItem = null;
            _pickupUI.SetActive(false);
            return;
        }

        // 가장 가까운 아이템 찾기
        float minDist = float.MaxValue;
        foreach (Collider col in colliders)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                col.TryGetComponent(out _nearestItem);
            }
        }

        _pickupUI.SetActive(_nearestItem != null);
    }

    /// <summary>
    /// 아이템 줍기
    /// </summary>
    public void TryPickup()
    {
        if (_nearestItem == null) return;

        ItemModel item = new ItemModel(_nearestItem.ItemConfig);
        if (_inventoryPresenter.AddItem(item))
        {
            _nearestItem.Pickup();
            _nearestItem = null;
            _pickupUI.SetActive(false);
        }
    }
}
