using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 전체 클리어 UI
/// GameObject를 끄면 안되고 Canvas만 꺼놔야함
/// </summary>
public class GameClearUI : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] GameObject _gameClearPanel;
    [SerializeField] Image _backgroundImage;       // 클리어 배경 이미지
    [SerializeField] Image _darkOverlay;           // 어둡게 깔거
    [SerializeField] CanvasGroup _messageGroup;    // 메시지 그룹
    [SerializeField] CanvasGroup _statisticsGroup; // 통계 그룹

    [Header("----- 통계 텍스트 -----")]
    [SerializeField] TextMeshProUGUI _goldText;       // 획득한 총 골드
    [SerializeField] TextMeshProUGUI _itemsUsedText;  // 사용한 아이템 수
    [SerializeField] TextMeshProUGUI _shellKillsText; // 포탄으로 격파한 적 수
    [SerializeField] TextMeshProUGUI _deathCountText; // 죽은 횟수
    [SerializeField] TextMeshProUGUI _finalStageText; // 최종 스테이지
    [SerializeField] TextMeshProUGUI _playTimeText;   // 플레이 시간

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _bgFadeDuration = 2f;
    [SerializeField] float _darkFadeDuration = 1f;
    [SerializeField] float _groupFadeDuration = 0.5f;
    [SerializeField] float _waitTime = 5f;

    public event Action TitleRequested;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        _gameClearPanel.SetActive(false);
        SetAlpha(_backgroundImage, 0f);
        SetAlpha(_darkOverlay, 0f);
        _messageGroup.alpha = 0f;
        _statisticsGroup.alpha = 0f;
    }

    public void Show(int totalGoldEarned, int itemsUsed, int shellKills, int deathCount, int finalStage, float playTime)
    {
        // 통계 세팅
        _goldText.text = $"{totalGoldEarned}";
        _itemsUsedText.text = $"{itemsUsed}";
        _shellKillsText.text = $"{shellKills}";
        _deathCountText.text = $"{deathCount}";
        _finalStageText.text = $"{finalStage}";
        _playTimeText.text = FormatPlayTime(playTime);

        _gameClearPanel.SetActive(true);
        StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// 플레이 시간을 mm:ss 형식으로 변환
    /// </summary>
    string FormatPlayTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min:00}:{sec:00}";
    }

    IEnumerator ShowRoutine()
    {
        // 배경 페이드 인
        yield return FadeRoutine(_backgroundImage, _bgFadeDuration);
        // 어둡게 페이드 인 (알파 값 200까지만)
        yield return FadeRoutine(_darkOverlay, _darkFadeDuration, 200f / 255f);

        // 메시지 그룹 페이드 인
        yield return FadeCanvasGroupRoutine(_messageGroup, _groupFadeDuration);

        // 잠시 대기
        yield return new WaitForSeconds(_waitTime);

        // 통계 그룹 페이드 인
        yield return FadeCanvasGroupRoutine(_statisticsGroup, _groupFadeDuration);

        // 마우스 커서 보이게
        Cursor.lockState = CursorLockMode.None;
    }

    IEnumerator FadeRoutine(Graphic graphic, float duration, float targetAlpha = 1f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(graphic, Mathf.Lerp(0f, targetAlpha, elapsed / duration));
            yield return null;
        }
        SetAlpha(graphic, targetAlpha);
    }

    IEnumerator FadeCanvasGroupRoutine(CanvasGroup group, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        group.alpha = 1f;
    }

    void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    /// <summary>
    /// 메인화면 버튼
    /// </summary>
    public void OnClickTitle()
    {
        TitleRequested?.Invoke();
    }
}
