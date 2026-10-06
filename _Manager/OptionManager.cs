using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 옵션 설정 관리
/// </summary>
public class OptionManager : MonoBehaviour
{
    AudioMixer _audioMixer;
    OptionData _optionData = new OptionData();
    Resolution[] _resolutions;

    ColorAdjustments _colorAdjustments; // 밝기(Post Exposure) 조절용 캐싱 — ScenePostProcessing이 씬 진입/퇴장 시 등록/해제함

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
    /// 밝기(감마) 조절 대상 등록 — 게임씬 Global Volume(SceneEffect가 쓰는 것과 같은 프로필)에 붙은
    /// ScenePostProcessing이 자기 씬 진입 시 호출함
    /// Volume 자체는 게임씬 로컬 오브젝트라 씬을 나가면 파괴되므로, 등록 즉시 현재 저장된 값을 반영해서
    /// 씬 진입 시점부터 바로 밝기가 맞게 보이도록 함
    /// </summary>
    public void RegisterColorAdjustments(ColorAdjustments colorAdjustments)
    {
        _colorAdjustments = colorAdjustments;
        ApplyBrightness(_optionData.Brightness);
    }

    /// <summary>
    /// 밝기(감마) 조절 대상 해제 — ScenePostProcessing이 자기 씬 퇴장(OnDestroy) 시 호출함
    /// 같은 인스턴스일 때만 null 처리 — 이미 다른 씬(재진입 등)이 새로 등록해둔 참조를
    /// 이전 씬의 파괴 타이밍에 잘못 덮어써서 지워버리는 것 방지
    /// </summary>
    public void UnregisterColorAdjustments(ColorAdjustments colorAdjustments)
    {
        if (_colorAdjustments != colorAdjustments) return;

        _colorAdjustments = null;
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
        ApplyBrightness(_optionData.Brightness);
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
    /// 화면 밝기(감마) — value는 0.5~2.0 배율(1f가 기본값 100%)
    /// Post Exposure는 EV(로그 스케일) 단위라 배율을 그대로 넣으면 안 되므로 밑이 2인 로그로 변환해서 대입
    /// (직접 테스트하면서 체감이 이상하면 이 변환식을 조정할 예정)
    /// </summary>
    public void ApplyBrightness(float value)
    {
        _optionData.SetBrightness(value);

        if (_colorAdjustments == null) return;

        _colorAdjustments.postExposure.value = Mathf.Log(value, 2f);
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
