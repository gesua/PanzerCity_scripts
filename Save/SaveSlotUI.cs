using System;
using UnityEngine;

/// <summary>
/// 세이브 슬롯 선택 화면
/// 빈 슬롯 선택 시 초기값으로 생성, 저장된 슬롯 선택 시 이어하기
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] SaveSlotPanel[] _slots; // SaveManager의 슬롯 개수와 동일하게 인스펙터에서 설정

    public event Action<int> OnSlotSelected; // 슬롯 선택됨(새 게임/이어하기 판단은 TitleScene에서)

    void Awake()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            int slotIndex = i; // 클로저 캡처용 지역변수

            _slots[i].OnSelectClicked += () => HandleSlotClicked(slotIndex);
            _slots[i].OnDeleteClicked += () => HandleDeleteClicked(slotIndex);
            _slots[i].OnNameEndEdit += newName => HandleNameEndEdit(slotIndex, newName);
        }
    }

    /// <summary>
    /// 슬롯 선택 화면 표시
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        RefreshAll();
    }

    /// <summary>
    /// 슬롯 선택 화면 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 슬롯 전체 갱신
    /// </summary>
    void RefreshAll()
    {
        SaveManager saveManager = GameManager.Instance.SaveManager;

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Refresh(saveManager.GetSlotData(i));
        }
    }

    /// <summary>
    /// 슬롯 클릭 — 빈 슬롯이면 생성만 하고 끝, 저장된 슬롯이면 선택 이벤트 발행
    /// </summary>
    void HandleSlotClicked(int slotIndex)
    {
        SaveManager saveManager = GameManager.Instance.SaveManager;
        SaveData saveData = saveManager.GetSlotData(slotIndex);

        if (saveData.IsEmpty)
        {
            saveManager.CreateNewSlot(slotIndex);
            _slots[slotIndex].Refresh(saveManager.GetSlotData(slotIndex));
            _slots[slotIndex].OnClickNameEdit(); // 생성 직후 바로 이름 수정
            return;
        }

        OnSlotSelected?.Invoke(slotIndex);
    }

    /// <summary>
    /// 슬롯 삭제 버튼
    /// </summary>
    void HandleDeleteClicked(int slotIndex)
    {
        GameManager.Instance.SaveManager.DeleteSlot(slotIndex);
        _slots[slotIndex].Refresh(GameManager.Instance.SaveManager.GetSlotData(slotIndex));
    }

    /// <summary>
    /// 슬롯 이름 변경
    /// </summary>
    void HandleNameEndEdit(int slotIndex, string newName)
    {
        if (string.IsNullOrEmpty(newName)) return;

        GameManager.Instance.SaveManager.RenameSlot(slotIndex, newName);
    }
}