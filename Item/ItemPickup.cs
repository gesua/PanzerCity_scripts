using System;
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

    bool _isLocalControl = true; // 로컬 소유인지(싱글 플레이:항상 true)
    bool _isInitialized; // Initialize() 호출 여부(멀티에서 스폰 직후 몇 프레임 동안 아직 안 됐을 수 있음)

    public event Action<ItemConfig> OnAutoUsed; // 즉시 사용 아이템 획득
    Func<bool> _isDeadCheck; // 플레이어 죽었는지 넘겨받는 용도

    public void Initialize(InventoryPresenter inventoryPresenter, Func<bool> isDeadCheck)
    {
        _inventoryPresenter = inventoryPresenter;
        _isDeadCheck = isDeadCheck;
        _isInitialized = true;
    }

    /// <summary>
    /// 로컬 제어 여부 설정(멀티플레이 전용, PlayerTank가 호출)
    /// 소유자가 아니면 아이템 탐색/줍기 로직을 실행하지 않음
    /// </summary>
    public void SetLocalControl(bool isLocal)
    {
        _isLocalControl = isLocal;
    }

    void Update()
    {
        if (_isInitialized == false) return; // 아직 Initialize()가 호출되지 않았으면 스킵
        if (_isLocalControl == false) return; // 로컬 소유가 아니면 아이템 탐색 안 함

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
        if (_isDeadCheck()) // 죽었으면 UI 안 띄움
        {
            _pickupUI.transform.SetParent(transform); // 복귀
            _pickupUI.SetActive(false);
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, _pickupRange, _itemLayer);

        if (colliders.Length == 0)
        {
            _nearestItem = null;
            _pickupUI.transform.SetParent(transform); // 복귀
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

        // 줍기 단축키 띄우기
        if (_nearestItem != null && _nearestItem.gameObject != null)
        {
            _pickupUI.transform.SetParent(_nearestItem.transform); // 해당 아이템에 붙이기
            _pickupUI.transform.localPosition = Vector3.up * 2f;
            _pickupUI.SetActive(true);
        }
        else
        {
            _pickupUI.transform.SetParent(transform); // 복귀
            _pickupUI.SetActive(false);
        }
    }

    /// <summary>
    /// 아이템 줍기
    /// </summary>
    public void TryPickup()
    {
        if (_isDeadCheck()) return;
        if (_nearestItem == null) return;

        // 아이템 파괴 전에 UI를 먼저 원래 부모로 복귀
        _pickupUI.transform.SetParent(transform);
        _pickupUI.SetActive(false);

        // 즉시 사용 아이템 — 인벤토리 거치지 않고 바로 효과 발동
        if (_nearestItem.ItemConfig.AutoUse)
        {
            OnAutoUsed?.Invoke(_nearestItem.ItemConfig);
            _nearestItem.Pickup();
            _nearestItem = null;
            return;
        }

        // 일반 아이템 — 인벤토리에 추가
        ItemModel item = new ItemModel(_nearestItem.ItemConfig);
        if (_inventoryPresenter.AddItem(item))
        {
            _nearestItem.Pickup();
            _nearestItem = null;
        }
    }
}