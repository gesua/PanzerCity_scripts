using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using System.Text;

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
    [SerializeField] Vector3 _offset = new Vector3(0f, -200f, 0f); // 아이콘 기준 오프셋
    [SerializeField] float _extraXPerCell = 0f;   // 가로 2칸 이상일 때 추가 x 오프셋
    [SerializeField] float _extraYPerCell = -75f; // 세로 2칸 이상일 때 추가 y 오프셋

    // 낮을수록 좋은 스탯 (색상 반전 적용)
    static readonly StatType[] _invertedStats = { StatType.Reload };

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
        string desc = (config.Duration > 0f)
            ? descString.GetLocalizedString(config.Duration)
            : descString.GetLocalizedString();

        // 장비 아이템이면 스탯 증감 텍스트 추가
        if (config.ItemType == ItemType.Equipment && config.StatBonuses.Count > 0)
        {
            StringBuilder sb = new StringBuilder(desc);
            sb.AppendLine();
            foreach (StatBonus bonus in config.StatBonuses)
            {
                sb.AppendLine();
                sb.Append(GetStatText(bonus));
            }
            _descText.text = sb.ToString();
        }
        else
        {
            _descText.text = desc;
        }

        // 아이콘 위치 기준으로 고정 (아이템 크기에 따라 위치 보정)
        transform.position = iconPos + _offset + GetSizeOffset(config);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 툴팁 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 아이템 크기에 따른 위치 보정값 계산
    /// </summary>
    Vector3 GetSizeOffset(ItemConfig config)
    {
        int maxX = 0, maxY = 0;
        foreach (Vector2Int cell in config.OccupiedCells)
        {
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        int width = maxX + 1;
        int height = maxY + 1;

        float extraX = width >= 2 ? _extraXPerCell * (1f + (width - 2) * 0.5f) : 0f;
        float extraY = height >= 2 ? _extraYPerCell * (height - 1) : 0f;

        return new Vector3(extraX, extraY, 0f);
    }

    /// <summary>
    /// StatBonus 한 줄 텍스트 생성
    /// 예) <color=#aaaaff>포탄 공격력</color> <color=#00ff00>+2</color>
    /// </summary>
    string GetStatText(StatBonus bonus)
    {
        // 스탯 이름 로컬라이즈
        string key = "UI_STAT_" + bonus.StatType.ToString().ToUpper();
        LocalizedString statName = new LocalizedString("Localization", key);
        string localizedName = statName.GetLocalizedString();

        // 낮을수록 좋은 스탯이면 색상 반전
        bool isInverted = System.Array.IndexOf(_invertedStats, bonus.StatType) >= 0;
        bool isPositive = isInverted ? bonus.Value < 0f : bonus.Value > 0f;

        string valueColor = isPositive ? "#00ff00" : "#ff4444";
        string sign = bonus.Value > 0f ? "+" : "";

        return $"<color=#aaaaff>{localizedName}</color> <color={valueColor}>{sign}{bonus.Value}</color>";
    }
}