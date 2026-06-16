using System;
using System.Collections;
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
    [SerializeField] Image _backgroundImage;  // 클리어 배경 이미지
    [SerializeField] Image _darkOverlay;      // 어둡게 깔거
    [SerializeField] CanvasGroup _buttonsGroup; // 버튼 그룹

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _bgFadeDuration = 2f;
    [SerializeField] float _darkFadeDuration = 1f;
    [SerializeField] float _buttonsFadeDuration = 0.5f;

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
        _buttonsGroup.alpha = 0f;
    }

    public void Show()
    {
        _gameClearPanel.SetActive(true);
        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        // 배경 페이드 인
        yield return FadeRoutine(_backgroundImage, _bgFadeDuration);
        // 어둡게 페이드 인 (알파 값 200까지만)
        yield return FadeRoutine(_darkOverlay, _darkFadeDuration, 200f / 255f);

        // 마우스 커서 보이게
        Cursor.lockState = CursorLockMode.None;

        // 버튼 그룹 페이드 인
        yield return FadeCanvasGroupRoutine(_buttonsGroup, _buttonsFadeDuration);
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
