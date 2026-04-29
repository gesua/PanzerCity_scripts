using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 화면 전체에 주는 이펙트
/// </summary>
public class SceneEffect : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Volume _volume;
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _duration = 1.5f; // 지속 시간

    float _maxIntensity; // 화면 강도

    Vignette _vignette;
    Coroutine _routine;

    void Awake()
    {
        _volume.profile.TryGet(out _vignette);
        _vignette.color.Override(Color.red);
        _vignette.intensity.Override(0f);
    }

    /// <summary>
    /// 피격시 화면 붉어지는 이펙트
    /// </summary>
    /// <param name="curHP">남은 체력</param>
    public void ShowDamageEffect(int curHP)
    {
        // 남은 체력에 따라 강도를 다르게 함
        switch (curHP)
        {
            case 0: // 사망
                _maxIntensity = 5f;
                break;
            case 1:
                _maxIntensity = 1f;
                break;
            case 2:
                _maxIntensity = 0.5f;
                break;
            default:
                _maxIntensity = 0.5f;
                break;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        // 빠르게 올라오고
        float elapsed = 0f;
        float fadeInDuration = _duration * 0.2f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _vignette.intensity.Override(Mathf.Lerp(0f, _maxIntensity, elapsed / fadeInDuration));
            yield return null;
        }

        // 천천히 사라지고
        elapsed = 0f;
        float fadeOutDuration = _duration * 0.8f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _vignette.intensity.Override(Mathf.Lerp(_maxIntensity, 0f, elapsed / fadeOutDuration));
            yield return null;
        }

        _vignette.intensity.Override(0f);
    }
}
