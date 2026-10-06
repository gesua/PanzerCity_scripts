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

    public OptionData OptionData => _optionData;
    public Resolution[] GetResolutions() => _resolutions;

    const string MobileQualityPrefix = "Mobile_"; // 모바일 전용 Quality 레벨 이름 접두사

    List<int> _qualityLevelIndices = new List<int>(); // 현재 플랫폼에서 선택 가능한 Quality 레벨의 전역 인덱스(옵션 드롭다운 순서와 동일)

    public void Initialize()
    {
        _audioMixer = Resources.Load<AudioMixer>("Audio/AudioMixer");
        _resolutions = GetUniqueResolutions();
        BuildQualityLevelList();
        _optionData.Load();
        Apply();

#if UNITY_ANDROID || UNITY_IOS
    Application.targetFrameRate = 60; // 모바일은 VSync를 끄므로 프레임을 직접 제한
#endif
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
        ApplyMouseSensitivity(_optionData.MouseSensitivity);
    }

    /// <summary>
    /// 언어 선택
    /// </summary>
    public void ApplyLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
        {
            language = string.IsNullOrEmpty(_optionData.Language) ? "ko" : _optionData.Language;
        }

        var locale = LocalizationSettings.AvailableLocales.Locales
            .FirstOrDefault(locale => locale.Identifier.Code == language);

        if (locale == null) return;

        _optionData.SetLanguage(language);

        // Localization 언어 변경
        if (LocalizationSettings.SelectedLocale != locale)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
    }

    /// <summary>
    /// 마스터 볼륨
    /// </summary>
    public void ApplyMasterVolume(float value)
    {
        _optionData.SetMasterVolume(value);

        // 슬라이더 값이 0이거나 0보다 작으면 확실하게 음소거(-80dB) 처리
        // Mathf.Log10(0) = NaN
        if (value <= 0.0001f)
        {
            _audioMixer.SetFloat("Master", -80f);
        }
        else
        {
            _audioMixer.SetFloat("Master", Mathf.Log10(value) * 20f);
        }
    }

    /// <summary>
    /// 배경음
    /// </summary>
    public void ApplyBGMVolume(float value)
    {
        _optionData.SetBGMVolume(value);

        // 음소거 처리
        if (value <= 0.0001f)
        {
            _audioMixer.SetFloat("BGM", -80f);
        }
        else
        {
            _audioMixer.SetFloat("BGM", Mathf.Log10(value) * 20f);
        }
    }

    /// <summary>
    /// 효과음
    /// </summary>
    public void ApplySFXVolume(float value)
    {
        _optionData.SetSFXVolume(value);

        // 음소거 처리
        if (value <= 0.0001f)
        {
            _audioMixer.SetFloat("SFX", -80f);
        }
        else
        {
            _audioMixer.SetFloat("SFX", Mathf.Log10(value) * 20f);
        }
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
    /// <param name="index">옵션 드롭다운 순서(전역 Quality 인덱스가 아님)</param>
    public void ApplyQuality(int index)
    {
        _optionData.SetQualityIndex(index);
        if (index < 0 || index >= _qualityLevelIndices.Count) return;

        QualitySettings.SetQualityLevel(_qualityLevelIndices[index]);
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

    /// <summary>
    /// 현재 플랫폼에서 선택 가능한 Quality 레벨 목록 구성
    /// 모바일 빌드는 "Mobile_" 접두사 레벨만, 그 외 플랫폼은 접두사가 없는 레벨만 사용
    /// </summary>
    void BuildQualityLevelList()
    {
#if UNITY_ANDROID || UNITY_IOS
    bool isMobilePlatform = true;
#else
        bool isMobilePlatform = false;
#endif

        _qualityLevelIndices.Clear();

        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            bool isMobileLevel = names[i].StartsWith(MobileQualityPrefix);
            if (isMobileLevel == isMobilePlatform) _qualityLevelIndices.Add(i);
        }

        // 해당 플랫폼용 레벨이 하나도 없으면(에디터 설정 누락) 전체 레벨을 그대로 사용
        if (_qualityLevelIndices.Count == 0)
        {
            Debug.LogWarning("현재 플랫폼에 맞는 Quality 레벨이 없어 전체 레벨을 사용합니다.");
            for (int i = 0; i < names.Length; i++)
            {
                _qualityLevelIndices.Add(i);
            }
        }
    }

    /// <summary>
    /// 옵션 드롭다운에 표시할 Quality 이름 목록 (모바일 접두사는 제거해서 Low/Medium/High로 표시)
    /// </summary>
    public List<string> GetQualityNames()
    {
        List<string> result = new List<string>();
        string[] names = QualitySettings.names;

        foreach (int levelIndex in _qualityLevelIndices)
        {
            string levelName = names[levelIndex];
            if (levelName.StartsWith(MobileQualityPrefix)) levelName = levelName.Substring(MobileQualityPrefix.Length);

            result.Add(levelName);
        }

        return result;
    }
}
