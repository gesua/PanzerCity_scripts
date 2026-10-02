using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// 튜토리얼 힌트 메시지 UI
/// 진행도에 따라 달라지는 메시지
/// </summary>
public class TutorialHintUI : MonoBehaviour
{
    const string TableName = "Localization";
    const string MobileKeySuffix = "_MOBILE"; // 모바일 전용 문구는 기본 키 뒤에 _MOBILE을 붙여 등록(일부 스텝에만 존재)

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
        LocalizedString localizedString = new LocalizedString(TableName, ResolveKey(key));
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

    /// <summary>
    /// 모바일이면 기본 키 뒤에 _MOBILE이 붙은 전용 문구가 있는지 확인해서 있으면 그 키를, 없으면 원래 키를 반환
    /// 모바일 전용 문구는 일부 스텝에만 있으므로 없는 스텝은 PC와 같은 문구를 그대로 사용함
    /// </summary>
    string ResolveKey(string key)
    {
        if (MobileUIController.IsMobile == false) return key;

        string mobileKey = key + MobileKeySuffix;
        StringTableEntry entry = LocalizationSettings.StringDatabase.GetTableEntry(TableName, mobileKey).Entry;

        return (entry != null) ? mobileKey : key;
    }
}