using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드롭존 UI — 스테이지에서 아이템 드래그 드롭으로 바닥에 버리기
/// 상점에서는 GameScene이 비활성화시킴
/// </summary>
public class DropZoneUI : MonoBehaviour, IDropHandler, IPointerEnterHandler
{
    InventoryPresenter _presenter;
    Graphic _raycastGraphic; // 드롭 판정용 Raycast Target(같은 오브젝트의 Image) — 아이템을 드래그하는 동안에만 켬

    void Awake()
    {
        // 게임 화면 전체를 덮고 있어서 Raycast Target이 켜져 있으면 그 아래(모바일 조작 UI 등)의 입력을 전부 막음
        // Inspector 값과 무관하게 평소엔 꺼두고, 드래그 중일 때만 LateUpdate에서 켬
        if (TryGetComponent(out _raycastGraphic) == false)
        {
            Debug.LogWarning($"[DropZoneUI] Graphic(Image)이 없어 드롭 판정을 제어할 수 없습니다. ({name})");
            return;
        }

        _raycastGraphic.raycastTarget = false;
    }

    void OnDisable()
    {
        SetDropEnabled(false); // 상점 등으로 비활성화될 때 켜진 채로 남지 않게 정리
    }

    void LateUpdate()
    {
        if (_presenter == null) return; // 아직 Initialize 전(멀티플레이는 로컬 플레이어 스폰 후 초기화됨)

        // EventSystem이 Update에서 드래그 시작/드롭을 처리한 뒤의 상태를 같은 프레임에 반영하려고 LateUpdate에서 읽음(스크립트 실행 순서와 무관)
        SetDropEnabled(_presenter.IsDragging);
    }

    public void Initialize(InventoryPresenter presenter)
    {
        _presenter = presenter;
    }

    public void SetActiveState(bool value)
    {
        gameObject.SetActive(value);
    }

    /// <summary>
    /// 아이템 드롭
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.TryGetComponent(out ItemView itemView) == false) return;

        _presenter.DropItemToGround(itemView.Item);
    }

    /// <summary>
    /// 드래그 중 아이템이 드롭존 위로 올라옴
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return; // 드래그 중일 때만 반응
    }

    /// <summary>
    /// 드롭 판정(Raycast Target) 켜고 끄기 — 값이 바뀔 때만 적용
    /// </summary>
    void SetDropEnabled(bool value)
    {
        if (_raycastGraphic == null) return;
        if (_raycastGraphic.raycastTarget == value) return;

        _raycastGraphic.raycastTarget = value;
    }
}