using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Icons;

/// <summary>
/// 옵션 UI
/// </summary>
public class OptionUI : MonoBehaviour
{
    [Header("----- 언어 -----")]
    [SerializeField] TMP_Dropdown _languageDropdown;

    [Header("----- 음량 -----")]
    [SerializeField] Slider _masterVolumeSlider;
    [SerializeField] Slider _bgmVolumeSlider;
    [SerializeField] Slider _sfxVolumeSlider;

    [Header("----- 화면 -----")]
    [SerializeField] TMP_Dropdown _resolutionDropdown;
    [SerializeField] Toggle _fullscreenToggle;
    [SerializeField] TMP_Dropdown _qualityDropdown;

    [Header("----- 마우스 -----")]
    [SerializeField] Slider _mouseSensitivitySlider;

    OptionManager _optionManager;

    string language;

    void OnEnable()
    {
        InitUI();
        SubscribeEvents();
    }

    /// <summary>
    /// 저장된 값으로 UI 초기화
    /// </summary>
    void InitUI()
    {
        if (_optionManager == null) _optionManager = GameManager.Instance.OptionManager;

        OptionData data = _optionManager.OptionData;

        // 해상도 드롭다운 옵션 생성
        _resolutionDropdown.ClearOptions();
        Resolution[] resolutions = _optionManager.GetResolutions();
        foreach (Resolution res in resolutions)
        {
            _resolutionDropdown.options.Add(new TMP_Dropdown.OptionData($"{res.width} x {res.height}"));
        }

        // 그래픽 품질 드롭다운 옵션 생성
        _qualityDropdown.ClearOptions();
        foreach (string quality in QualitySettings.names)
        {
            _qualityDropdown.options.Add(new TMP_Dropdown.OptionData(quality));
        }

        // 설정 값 가져옴
        _languageDropdown.value = data.Language == "en" ? 0 : 1;
        _masterVolumeSlider.value = data.MasterVolume;
        _bgmVolumeSlider.value = data.BGMVolume;
        _sfxVolumeSlider.value = data.SFXVolume;
        _resolutionDropdown.value = data.ResolutionIndex;
        _fullscreenToggle.isOn = data.Fullscreen;
        _qualityDropdown.value = _optionManager.OptionData.QualityIndex;
        _mouseSensitivitySlider.value = data.MouseSensitivity;

        _resolutionDropdown.RefreshShownValue();
        _qualityDropdown.RefreshShownValue();
    }

    /// <summary>
    /// 이벤트 연결
    /// </summary>
    void SubscribeEvents()
    {
        _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        _masterVolumeSlider.onValueChanged.AddListener(_optionManager.ApplyMasterVolume);
        _bgmVolumeSlider.onValueChanged.AddListener(_optionManager.ApplyBGMVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(_optionManager.ApplySFXVolume);
        _resolutionDropdown.onValueChanged.AddListener(_optionManager.ApplyResolution);
        _fullscreenToggle.onValueChanged.AddListener(_optionManager.ApplyFullscreen);
        _qualityDropdown.onValueChanged.AddListener(_optionManager.ApplyQuality);
        _mouseSensitivitySlider.onValueChanged.AddListener(_optionManager.ApplyMouseSensitivity);
    }

    /// <summary>
    /// 이벤트 연결 해제
    /// </summary>
    void UnsubscribeEvents()
    {
        _languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        _masterVolumeSlider.onValueChanged.RemoveListener(_optionManager.ApplyMasterVolume);
        _bgmVolumeSlider.onValueChanged.RemoveListener(_optionManager.ApplyBGMVolume);
        _sfxVolumeSlider.onValueChanged.RemoveListener(_optionManager.ApplySFXVolume);
        _resolutionDropdown.onValueChanged.RemoveListener(_optionManager.ApplyResolution);
        _fullscreenToggle.onValueChanged.RemoveListener(_optionManager.ApplyFullscreen);
        _qualityDropdown.onValueChanged.RemoveListener(_optionManager.ApplyQuality);
        _mouseSensitivitySlider.onValueChanged.RemoveListener(_optionManager.ApplyMouseSensitivity);
    }

    void OnLanguageChanged(int index)
    {
        language = (index == 0) ? "en" : "ko";
    }

    /// <summary>
    /// 적용
    /// </summary>
    public void OnClickSave()
    {
        _optionManager.ApplyLanguage(language);
        _optionManager.Save();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 취소
    /// </summary>
    public void OnClickCancel()
    {
        // 원래 값으로 복구
        _optionManager.OptionData.Load();
        _optionManager.Apply();
        gameObject.SetActive(false);
    }
}