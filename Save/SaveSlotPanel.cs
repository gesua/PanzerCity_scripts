using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 세이브 슬롯 1개의 UI
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    [SerializeField] Button _selectButton;
    [SerializeField] Button _deleteButton;
    [SerializeField] TMP_InputField _nameInput;
    [SerializeField] TextMeshProUGUI _stageText;
    [SerializeField] TextMeshProUGUI _statsText;
    [SerializeField] TextMeshProUGUI _dateText;
    [SerializeField] GameObject _emptyLabel; // "빈 슬롯" 표시용

    public event Action OnSelectClicked;
    public event Action OnDeleteClicked;
    public event Action<string> OnNameEndEdit;

    void Awake()
    {
        _selectButton.onClick.AddListener(() => OnSelectClicked?.Invoke());
        _deleteButton.onClick.AddListener(() => OnDeleteClicked?.Invoke());
        _nameInput.onEndEdit.AddListener(newName => OnNameEndEdit?.Invoke(newName));
    }

    /// <summary>
    /// 슬롯 UI 갱신
    /// </summary>
    public void Refresh(SaveData data)
    {
        bool isEmpty = data.IsEmpty;

        _emptyLabel.SetActive(isEmpty);
        _nameInput.gameObject.SetActive(isEmpty == false);
        _stageText.gameObject.SetActive(isEmpty == false);
        _statsText.gameObject.SetActive(isEmpty == false);
        _dateText.gameObject.SetActive(isEmpty == false);
        _deleteButton.gameObject.SetActive(isEmpty == false);

        if (isEmpty) return;

        _nameInput.SetTextWithoutNotify(data.SaveName);
        _stageText.text = $"스테이지 {data.CurrentStageID - 7100}";
        _statsText.text = $"골드 {data.TotalGoldEarned} / 격파 {data.ShellKills} / 사망 {data.DeathCount}";
        _dateText.text = data.LastPlayedDate;
    }
}
