using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 세이브 슬롯 1개의 UI 구성 요소(인스펙터에서 연결)
/// </summary>
[Serializable]
public class SaveSlotElement
{
    public Button SelectButton;
    public Button DeleteButton;
    public TMP_InputField NameInput;
    public TextMeshProUGUI StageText;
    public TextMeshProUGUI StatsText;
    public TextMeshProUGUI DateText;
    public GameObject EmptyLabel; // "빈 슬롯" 표시용
}

/// <summary>
/// 세이브 슬롯 선택 화면
/// 빈 슬롯 선택 시 새 게임, 저장된 슬롯 선택 시 이어하기
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] GameObject _root; // 전체 패널
    [SerializeField] SaveSlotElement[] _slotElements; // SaveManager의 슬롯 개수와 동일하게 인스펙터에서 설정

    public event Action<int> OnSlotSelected; // 슬롯 선택됨(새 게임/이어하기 판단은 TitleScene에서)

    void Awake()
    {
        for (int i = 0; i < _slotElements.Length; i++)
        {
            int slotIndex = i; // 클로저 캡처용 지역변수
            SaveSlotElement element = _slotElements[i];

            element.SelectButton.onClick.AddListener(() => HandleSlotClicked(slotIndex));
            element.DeleteButton.onClick.AddListener(() => HandleDeleteClicked(slotIndex));
            element.NameInput.onEndEdit.AddListener(newName => HandleNameEndEdit(slotIndex, newName));
        }
    }

    /// <summary>
    /// 슬롯 선택 화면 표시
    /// </summary>
    public void Show()
    {
        _root.SetActive(true);
        RefreshAll();
    }

    /// <summary>
    /// 슬롯 선택 화면 숨김
    /// </summary>
    public void Hide()
    {
        _root.SetActive(false);
    }

    /// <summary>
    /// 슬롯 전체 갱신
    /// </summary>
    void RefreshAll()
    {
        SaveManager saveManager = GameManager.Instance.SaveManager;

        for (int i = 0; i < _slotElements.Length; i++)
        {
            RefreshSlot(i, saveManager.GetSlotData(i));
        }
    }

    /// <summary>
    /// 슬롯 1개 UI 갱신
    /// </summary>
    void RefreshSlot(int slotIndex, SaveData data)
    {
        SaveSlotElement element = _slotElements[slotIndex];

        element.EmptyLabel.SetActive(data.IsEmpty);
        element.NameInput.gameObject.SetActive(data.IsEmpty == false);
        element.StageText.gameObject.SetActive(data.IsEmpty == false);
        element.StatsText.gameObject.SetActive(data.IsEmpty == false);
        element.DateText.gameObject.SetActive(data.IsEmpty == false);
        element.DeleteButton.gameObject.SetActive(data.IsEmpty == false);

        if (data.IsEmpty) return;

        element.NameInput.SetTextWithoutNotify(data.SaveName);
        element.StageText.text = $"스테이지 {data.CurrentStageID - 7100}";
        element.StatsText.text = $"골드 {data.TotalGoldEarned} / 격파 {data.ShellKills} / 사망 {data.DeathCount}";
        element.DateText.text = data.LastPlayedDate;
    }

    /// <summary>
    /// 슬롯 클릭 — 새 게임 또는 이어하기
    /// </summary>
    void HandleSlotClicked(int slotIndex)
    {
        OnSlotSelected?.Invoke(slotIndex);
    }

    /// <summary>
    /// 슬롯 삭제 버튼
    /// </summary>
    void HandleDeleteClicked(int slotIndex)
    {
        GameManager.Instance.SaveManager.DeleteSlot(slotIndex);
        RefreshSlot(slotIndex, GameManager.Instance.SaveManager.GetSlotData(slotIndex));
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
