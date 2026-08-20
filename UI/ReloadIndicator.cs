using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재장전 진행을 표시하는 UI 컴포넌트
/// </summary>
public class ReloadIndicator : MonoBehaviour
{
    [SerializeField] Image _baseImage;      // 진행될 때 뒷판 이미지
    [SerializeField] Image _fillImage;      // 진행 이미지
    [SerializeField] Image _completeImage;  // 장전 완료 이미지
    [Header("----- 완료 연출 -----")]
    [SerializeField, Range(1f, 1.5f)] float _completionPulseScale = 1.16f; // 완료 순간 커지는 비율
    [SerializeField, Min(0.01f)] float _completionFeedbackDuration = 0.16f; // 팽창과 플래시 지속 시간

    RectTransform _centerCrosshair; // 화면 중앙 조준점
    Vector3 _centerCrosshairBaseScale = Vector3.one;
    Vector3 _completeIconBaseScale = Vector3.one;
    Color _completeIconBaseColor = new Color32(255, 255, 255, 76);
    Coroutine _completionFeedbackRoutine;

    void Awake()
    {
        // 완료 아이콘은 연출이 끝난 뒤 반드시 원래 크기와 투명도로 돌아와야 함
        if (_completeImage == null) return;

        Debug.Log("Awake");

        _completeIconBaseScale = _completeImage.rectTransform.localScale;
        _completeIconBaseColor = _completeImage.color;
    }

    void OnDisable()
    {
        // 사망, 씬 전환 중에 UI가 꺼져도 확대 상태가 다음 표시까지 남지 않도록 초기화
        StopCompletionFeedback();
    }

    /// <summary>
    /// 게임 씬의 중앙 조준점을 완료 연출 대상으로 연결
    /// </summary>
    public void SetCenterCrosshair(RectTransform centerCrosshair)
    {
        StopCompletionFeedback();

        _centerCrosshair = centerCrosshair;
        if (_centerCrosshair != null)
        {
            _centerCrosshairBaseScale = _centerCrosshair.localScale;
        }
    }

    public void UpdateReload(float current, float max)
    {
        if (gameObject.activeInHierarchy == false) return; // 오브젝트가 꺼진 상태에서 호출되면 무시(StartCoroutine 오류 방지)

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 1f;
        _fillImage.fillAmount = ratio;
        _baseImage.fillAmount = 1 - ratio; // 뒷판 그냥 띄우면 이상하게 보임

        // 장전 완료되면 이미지 변경
        if (ratio >= 1f)
        {
            // 완료 이미지가 처음 켜지는 한 번만 완료 연출 재생
            if (_completeImage.enabled == false)
            {
                _fillImage.enabled = false;
                _baseImage.enabled = false;
                _completeImage.enabled = true;
                PlayCompletionFeedback();
            }
        }
        else if (_completeImage.enabled) // 장전 중인데 완료 이미지가 켜져있으면 끔(1번만 들어오게 하는 용도)
        {
            _fillImage.enabled = true;
            _baseImage.enabled = true;
            _completeImage.enabled = false;
        }
    }

    /// <summary>
    /// 재장전 완료 순간의 짧은 확대와 흰색 플래시를 재생
    /// </summary>
    void PlayCompletionFeedback()
    {
        StopCompletionFeedback();
        _completionFeedbackRoutine = StartCoroutine(CompletionFeedbackRoutine());
    }

    IEnumerator CompletionFeedbackRoutine()
    {
        float elapsed = 0f;
        ApplyCompletionFeedback(0f); // 완료된 바로 그 프레임에는 가장 밝게 보이게 함

        while (elapsed < _completionFeedbackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyCompletionFeedback(Mathf.Clamp01(elapsed / _completionFeedbackDuration));
            yield return null;
        }

        ResetCompletionFeedback();
        _completionFeedbackRoutine = null;
    }

    void ApplyCompletionFeedback(float normalizedTime)
    {
        // 사인 곡선을 사용해 한 번만 부드럽게 커졌다가 원래 크기로 돌아오게 함
        float pulse = Mathf.Sin(normalizedTime * Mathf.PI);
        float scaleMultiplier = Mathf.Lerp(1f, _completionPulseScale, pulse);

        if (_centerCrosshair != null)
        {
            _centerCrosshair.localScale = _centerCrosshairBaseScale * scaleMultiplier;
        }

        if (_completeImage != null)
        {
            Debug.Log("ApplyCompletionFeedback");

            _completeImage.rectTransform.localScale = _completeIconBaseScale * scaleMultiplier;
            _completeImage.color = Color.Lerp(_completeIconBaseColor, Color.white, 1f - normalizedTime);
        }
    }

    void StopCompletionFeedback()
    {
        if (_completionFeedbackRoutine != null)
        {
            StopCoroutine(_completionFeedbackRoutine);
            _completionFeedbackRoutine = null;
        }

        ResetCompletionFeedback();
    }

    void ResetCompletionFeedback()
    {
        if (_centerCrosshair != null)
        {
            _centerCrosshair.localScale = _centerCrosshairBaseScale;
        }

        if (_completeImage != null)
        {
            Debug.Log("ResetCompletionFeedback");

            _completeImage.rectTransform.localScale = _completeIconBaseScale;
            _completeImage.color = _completeIconBaseColor;
        }
    }
}
