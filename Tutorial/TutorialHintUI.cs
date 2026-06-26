using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 튜토리얼 힌트 메시지 UI
/// </summary>
public class TutorialHintUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _messageText;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 메시지 표시
    /// </summary>
    public void Show(string key)
    {
        LocalizedString localizedString = new LocalizedString("Localization", key);
        _messageText.text = localizedString.GetLocalizedString();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 메시지 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
