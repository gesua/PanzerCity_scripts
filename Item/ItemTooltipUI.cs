using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 아이템 툴팁 UI
/// 인벤토리에서 아이템에 마우스를 올렸을 때 이름과 설명을 표시
/// 아이템 아이콘 기준으로 위치 고정
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _descText;
    [Header("----- 위치 -----")]
    [SerializeField] Vector3 _offset = new Vector3(0f, -10f, 0f); // 아이콘 기준 오프셋 (아이콘 바로 아래)

    void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 툴팁 표시
    /// </summary>
    /// <param name="config">표시할 아이템 설정</param>
    /// <param name="iconPos">아이콘 월드 위치</param>
    public void Show(ItemConfig config, Vector3 iconPos)

    {
        // 이름 로컬라이즈
        LocalizedString nameString = new LocalizedString("Localization", config.NameKey);
        _nameText.text = nameString.GetLocalizedString();

        // 설명 로컬라이즈 — 지속시간이 있으면 {0} 포맷 인자 적용
        LocalizedString descString = new LocalizedString("Localization", config.DescKey);
        _descText.text = config.Duration > 0f
            ? descString.GetLocalizedString(config.Duration)
            : descString.GetLocalizedString();

        // 아이콘 위치 기준으로 고정
        transform.position = iconPos + _offset;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 툴팁 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}