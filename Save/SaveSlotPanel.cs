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
    [SerializeField] Button _nameEditButton;
    [SerializeField] Button _deleteButton;
    [SerializeField] GameObject _hideGroup; // 빈 슬롯일 때 꺼놓을 그룹
    [SerializeField] TMP_InputField _nameInput;
    [SerializeField] TextMeshProUGUI _stageText;
    [SerializeField] TextMeshProUGUI _lifeText;
    [SerializeField] TextMeshProUGUI _playTimeText;

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
        _hideGroup.SetActive(isEmpty == false);

        if (isEmpty) return;

        _nameInput.SetTextWithoutNotify(data.SaveName);
        _stageText.text = $"{data.CurrentStageID - 7100}";
        _lifeText.text = $"{data.Life}";
        _playTimeText.text = data.PlayTime.ToTimeString();
    }
}