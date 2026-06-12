using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;

/// <summary>
/// 옵션 설정 관리
/// </summary>
public class OptionManager : MonoBehaviour
{
    AudioMixer _audioMixer;
    OptionData _optionData = new OptionData();
    Resolution[] _resolutions;

    public event Action<float> OnMouseSensitivityChanged;
    public event Action OnLanguageChanged;

    public OptionData OptionData => _optionData;
    public Resolution[] GetResolutions() => _resolutions;

    public void Initialize()
    {
        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");
        _resolutions = GetUniqueResolutions();
        _optionData.Load();
        Apply();
    }

    /// <summary>
    /// 중복된 해상도를 없애고 가장 높은 Hz만 보여줌
    /// </summary>
    Resolution[] GetUniqueResolutions()
    {
        var best = new Dictionary<(int, int), Resolution>();

        foreach (Resolution res in Screen.resolutions)
        {
            // 16:9 비율만 필터링(1366x768 같은 해상도 대응[오차 0.0119])
            if (Mathf.Abs((float)res.width / res.height - 16f / 9f) > 0.02f) continue;

            var key = (res.width, res.height);
            float hz = (float)res.refreshRateRatio.numerator / res.refreshRateRatio.denominator;

            float bestHz = 0f;
            if (best.ContainsKey(key))
            {
                bestHz = (float)best[key].refreshRateRatio.numerator / best[key].refreshRateRatio.denominator;
            }

            if (best.ContainsKey(key) == false || hz > bestHz)
            {
                best[key] = res;
            }
        }

        // 높을 순서대로 반환
        return best.Values
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ToArray();
    }

    /// <summary>
    /// 저장된 설정 전체 적용
    /// </summary>
    public void Apply()
    {
        ApplyLanguage(_optionData.Language);
        ApplyMasterVolume(_optionData.MasterVolume);
        ApplyBGMVolume(_optionData.BGMVolume);
        ApplySFXVolume(_optionData.SFXVolume);
        ApplyResolution(_optionData.ResolutionIndex);
        ApplyFullscreen(_optionData.Fullscreen);
        ApplyQuality(_optionData.QualityIndex);
        Save();
    }

    /// <summary>
    /// 언어 선택
    /// </summary>
    public void ApplyLanguage(string language)
    {
        _optionData.SetLanguage(language);
        
        // Localization 언어 변경
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == language)
            {
                LocalizationSettings.SelectedLocale = locale;
                break;
            }
        }

        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// 마스터 볼륨
    /// </summary>
    public void ApplyMasterVolume(float value)
    {
        _optionData.SetMasterVolume(value);
        _audioMixer.SetFloat("Master", Mathf.Log10(value) * 20f);
    }

    /// <summary>
    /// 배경음
    /// </summary>
    public void ApplyBGMVolume(float value)
    {
        _optionData.SetBGMVolume(value);
        _audioMixer.SetFloat("BGM", Mathf.Log10(value) * 20f);
    }

    /// <summary>
    /// 효과음
    /// </summary>
    public void ApplySFXVolume(float value)
    {
        _optionData.SetSFXVolume(value);
        _audioMixer.SetFloat("SFX", Mathf.Log10(value) * 20f);
    }

    /// <summary>
    /// 해상도 변경
    /// </summary>
    public void ApplyResolution(int index)
    {
        _optionData.SetResolutionIndex(index);
        if (_resolutions == null || index >= _resolutions.Length) return;
        Resolution resolution = _resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, _optionData.Fullscreen);
    }

    /// <summary>
    /// 전체화면
    /// </summary>
    public void ApplyFullscreen(bool value)
    {
        _optionData.SetFullscreen(value);
        Screen.fullScreen = value;
    }

    /// <summary>
    /// 그래픽 품질
    /// </summary>
    public void ApplyQuality(int index)
    {
        _optionData.SetQualityIndex(index);
        QualitySettings.SetQualityLevel(index);
    }

    /// <summary>
    /// 마우스 감도
    /// </summary>
    /// <param name="value"></param>
    public void ApplyMouseSensitivity(float value)
    {
        _optionData.SetMouseSensitivity(value);
        OnMouseSensitivityChanged?.Invoke(value);
    }

    /// <summary>
    /// 옵션 데이터 저장
    /// </summary>
    public void Save()
    {
        _optionData.Save();
    }
}
