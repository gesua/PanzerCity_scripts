using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP 하트 이미지 관리
/// </summary>
public class HeartSlot : MonoBehaviour
{
    [SerializeField] Image filled;
    [SerializeField] Image empty;

    const float PUNCH_SCALE = 1.5f;
    const float PUNCH_UP_DURATION = 0.1f;
    const float PUNCH_HOLD_DURATION = 0.04f;
    const float PUNCH_DOWN_DURATION = 0.1f;

    RectTransform _rectTransform;
    Coroutine _lostAnimationRoutine;
    bool _wasFilled;

    void Awake()
    {
        if (TryGetComponent(out _rectTransform) == false)
        {
            Debug.LogWarning($"[HeartSlot] RectTransform이 없습니다. ({name})");
        }
    }

    public void SetState(bool isFilled)
    {
        bool justLost = (_wasFilled == true) && (isFilled == false);

        if (justLost == true && gameObject.activeInHierarchy == true)
        {
            PlayLostAnimation();
        }
        else
        {
            filled.enabled = isFilled;
            empty.enabled = isFilled == false;

            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.one;
            }
        }

        _wasFilled = isFilled;
    }

    /// <summary>
    /// 하트를 잃는 순간의 연출: 확대 → 유지 → 축소하며 empty로 전환
    /// </summary>
    void PlayLostAnimation()
    {
        if (_lostAnimationRoutine != null)
        {
            StopCoroutine(_lostAnimationRoutine);
        }

        _lostAnimationRoutine = StartCoroutine(LostAnimationRoutine());
    }

    IEnumerator LostAnimationRoutine()
    {
        filled.enabled = true;
        empty.enabled = false;

        if (_rectTransform != null)
        {
            float elapsed = 0f;
            while (elapsed < PUNCH_UP_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PUNCH_UP_DURATION);
                _rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, PUNCH_SCALE, t);
                yield return null;
            }
            _rectTransform.localScale = Vector3.one * PUNCH_SCALE;

            yield return new WaitForSeconds(PUNCH_HOLD_DURATION);

            elapsed = 0f;
            while (elapsed < PUNCH_DOWN_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PUNCH_DOWN_DURATION);
                _rectTransform.localScale = Vector3.one * Mathf.Lerp(PUNCH_SCALE, 1f, t);
                yield return null;
            }
            _rectTransform.localScale = Vector3.one;
        }

        filled.enabled = false;
        empty.enabled = true;

        _lostAnimationRoutine = null;
    }
}