using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퀵슬롯 하나의 상태(아이콘, 어둡기) 담당
/// </summary>
public class QuickSlot : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] GameObject _CountImg; // 갯수 부모 오브젝트
    [SerializeField] TextMeshProUGUI _countText; // 아이템 갯수
    [SerializeField] Image _durationRing; // 지속형 아이템 남은 시간 링

    const float PunchScaleDownTime = 0.06f; // 축소 소요 시간
    const float PunchScaleUpTime = 0.14f; // 복귀 소요 시간
    const float PunchScaleAmount = 0.85f; // 축소 시 배율

    const float AcquireFlashUpTime = 0.08f; // 획득 점등 상승 시간
    const float AcquireFlashDownTime = 0.15f; // 획득 점등 하강 시간
    static readonly Color AcquireFlashColor = new Color(1f, 0.965f, 0.69f); // #FFF6B0(연노랑)

    const float RingDangerThreshold = 0.2f; // 이 비율 이하로 남으면 위험색으로 전환
    static readonly Color32 RingDangerColor = new Color32(255, 75, 75, 100); // #FF4B4B(빨강)
    static readonly Color32 RingNormalColor = new Color32(255, 255, 255, 100); // 하양

    RectTransform _rectTransform; // 펀치 스케일용 캐싱
    Coroutine _punchScaleRoutine;
    Coroutine _durationRingRoutine;
    Coroutine _acquireFlashRoutine;
    int _previousCount; // 획득 점등 판단용(0에서 증가한 순간 감지)

    void Awake()
    {
        _rectTransform = (RectTransform)transform;
    }

    public void SetCount(int count)
    {
        bool available = (count > 0);
        bool isAcquired = (_previousCount == 0 && count > 0);

        if (available) // 아이콘 밝게
        {
            if (isAcquired) // 0에서 증가한 순간엔 점등 연출이 흰색까지 정리하므로 즉시 대입은 생략
            {
                if (_acquireFlashRoutine != null)
                {
                    StopCoroutine(_acquireFlashRoutine);
                }
                _acquireFlashRoutine = StartCoroutine(AcquireFlashRoutine());
            }
            else
            {
                _icon.color = Color.white;
            }
        }
        else // 아이콘 어둡게
        {
            _icon.color = Color.gray2;
        }

        _CountImg.SetActive(available);
        _countText.text = count.ToString();
        _previousCount = count;
    }

    /// <summary>
    /// 아이템 획득 시(0개에서 증가한 순간) 아이콘이 밝게 튀었다가 원래 흰색으로 가라앉는 연출
    /// </summary>
    IEnumerator AcquireFlashRoutine()
    {
        // 상승: 흰색 -> 점등색
        float elapsed = 0f;
        while (elapsed < AcquireFlashUpTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / AcquireFlashUpTime);
            _icon.color = Color.Lerp(Color.white, AcquireFlashColor, t);
            yield return null;
        }
        _icon.color = AcquireFlashColor;

        // 하강: 점등색 -> 흰색
        elapsed = 0f;
        while (elapsed < AcquireFlashDownTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / AcquireFlashDownTime);
            _icon.color = Color.Lerp(AcquireFlashColor, Color.white, t);
            yield return null;
        }
        _icon.color = Color.white;

        _acquireFlashRoutine = null;
    }

    /// <summary>
    /// 아이템 사용 시 연출 재생 — 눌린 듯 축소 후 복귀, 지속형이면 남은 시간 링도 함께
    /// </summary>
    /// <param name="duration">지속 시간(초). 0 이하면 링은 생략하고 펀치 스케일만 재생</param>
    public void PlayUseFeedback(float duration)
    {
        if (_punchScaleRoutine != null)
        {
            StopCoroutine(_punchScaleRoutine);
        }
        _punchScaleRoutine = StartCoroutine(PunchScaleRoutine());

        bool isDurationItem = (duration > 0f);
        if (isDurationItem == false) return;

        if (_durationRingRoutine != null)
        {
            StopCoroutine(_durationRingRoutine);
        }
        _durationRingRoutine = StartCoroutine(DurationRingRoutine(duration));
    }

    /// <summary>
    /// 눌린 듯 축소했다가 복귀하는 펀치 스케일 연출
    /// 일시정지 중에도 재생되도록 Time.unscaledDeltaTime 기준으로 진행
    /// </summary>
    IEnumerator PunchScaleRoutine()
    {
        Vector3 originalScale = Vector3.one;
        Vector3 shrunkScale = originalScale * PunchScaleAmount;

        // 축소
        float elapsed = 0f;
        while (elapsed < PunchScaleDownTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PunchScaleDownTime);
            _rectTransform.localScale = Vector3.Lerp(originalScale, shrunkScale, t);
            yield return null;
        }
        _rectTransform.localScale = shrunkScale;

        // 복귀
        elapsed = 0f;
        while (elapsed < PunchScaleUpTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PunchScaleUpTime);
            _rectTransform.localScale = Vector3.Lerp(shrunkScale, originalScale, t);
            yield return null;
        }
        _rectTransform.localScale = originalScale;

        _punchScaleRoutine = null;
    }

    /// <summary>
    /// 지속형 아이템의 남은 시간을 링으로 표시 (가득 참 → 서서히 비워짐)
    /// 일시정지 중에도 흘러가야 하는지는 게임 정책에 따라 Time.unscaledDeltaTime 기준으로 진행
    /// </summary>
    IEnumerator DurationRingRoutine(float duration)
    {
        _durationRing.gameObject.SetActive(true);
        _durationRing.fillAmount = 1f;
        _durationRing.color = RingNormalColor;

        float dangerStartRatio = (1f - RingDangerThreshold); // 이 비율(t)을 넘으면 위험색 전환 시작

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _durationRing.fillAmount = (1f - t);

            bool isDangerZone = (t > dangerStartRatio);
            if (isDangerZone) // 위험 구간에서는 남은 비율(1-t)이 줄어들수록 점점 더 붉게
            {
                float dangerT = Mathf.Clamp01((t - dangerStartRatio) / RingDangerThreshold);
                _durationRing.color = Color32.Lerp(RingNormalColor, RingDangerColor, dangerT);
            }

            yield return null;
        }

        _durationRing.fillAmount = 0f;
        _durationRing.color = RingNormalColor; // 다음 재생을 위해 원복
        _durationRing.gameObject.SetActive(false);
        _durationRingRoutine = null;
    }
}