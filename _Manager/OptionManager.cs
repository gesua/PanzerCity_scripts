using System;
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

    public OptionData OptionData => _optionData;
    public Resolution[] GetResolutions() => _resolutions;

    public void Initialize()
    {
        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");
        _resolutions = Screen.resolutions;
        _optionData.Load();
        Apply();
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
