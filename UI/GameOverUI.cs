using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임오버 UI
/// GameObject를 끄면 안되고 Canvas만 꺼놔야함
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Canvas _canvas;                // GameObject 대신 켜고 끌거
    [SerializeField] Image _backgroundImage;        // 게임오버 배경 이미지
    [SerializeField] Sprite[] _gameoverSprites;     // 0:HQ 파괴 1:탱크 파괴
    [SerializeField] Image _darkOverlay;            // 어둡게 깔거
    [SerializeField] CanvasGroup _buttonsGroup;     // 그룹(글자, 버튼)
    [SerializeField] TextMeshProUGUI _titleText;    // 제목 테스트
    [SerializeField] TextMeshProUGUI _messageText;  // 내용 텍스트

    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _bgFadeDuration = 2f;
    [SerializeField] float _darkFadeDuration = 1f;
    [SerializeField] float _buttonsFadeDuration = 0.5f;

    public event Action RestartRequested;
    public event Action TitleRequested;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        _canvas.enabled = false;

        // 전부 투명하게 초기화
        SetAlpha(_backgroundImage, 0f);
        SetAlpha(_darkOverlay, 0f);
        _buttonsGroup.alpha = 0f;
    }

    /// <summary>
    /// 패배 UI 보여줌
    /// </summary>
    /// <param name="isHQDestroyed">HQ 패배인지</param>
    public void Show(bool isHQDestroyed)
    {
        if (isHQDestroyed) // HQ 파괴
        {
            _messageText.text = "조각상이 파괴되었습니다.";
            _backgroundImage.sprite = _gameoverSprites[0];
        }
        else // 목숨 0
        {
            _messageText.text = "전차가 파괴되었습니다.";
            _backgroundImage.sprite = _gameoverSprites[1];
        }

        _canvas.enabled = true;
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

        // 그룹 페이드 인
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
    /// 재시작 버튼
    /// </summary>
    public void OnClickRestart()
    {
        Initialize();
        RestartRequested?.Invoke();
    }

    /// <summary>
    /// 메인화면 버튼
    /// </summary>
    public void OnClickTitle()
    {
        TitleRequested?.Invoke();
    }
}
