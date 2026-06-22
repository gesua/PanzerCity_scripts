using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] TMP_Text _valueText;

    OptionManager _optionManager;

    bool _subscribed;

    void OnEnable()
    {
        InitUI();
        SubscribeEvents();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// 저장된 값으로 UI 초기화
    /// </summary>
    void InitUI()
    {
        if (_optionManager == null) _optionManager = GameManager.Instance.OptionManager;

        OptionData data = _optionManager.OptionData;

        /*/ 모바일에선 해상도와 전체화면이 의미 없음(근데 아직 확인은 안 해봄
#if UNITY_ANDROID || UNITY_IOS
    _resolutionDropdown.gameObject.SetActive(false);
    _fullscreenToggle.gameObject.SetActive(false);
#endif
        //*/

        // 해상도 드롭다운 옵션 생성
        _resolutionDropdown.ClearOptions();
        Resolution[] resolutions = _optionManager.GetResolutions();
        foreach (Resolution res in resolutions)
        {
            int hz = Mathf.RoundToInt((float)res.refreshRateRatio.numerator / res.refreshRateRatio.denominator);
            _resolutionDropdown.options.Add(new TMP_Dropdown.OptionData($"{res.width} x {res.height} @ {hz}Hz"));
        }

        // 그래픽 품질 드롭다운 옵션 생성
        _qualityDropdown.ClearOptions();
        foreach (string quality in QualitySettings.names)
        {
            _qualityDropdown.options.Add(new TMP_Dropdown.OptionData(quality));
        }

        // 설정 값 가져옴
        _languageDropdown.SetValueWithoutNotify(data.Language == "en" ? 0 : 1);
        _masterVolumeSlider.value = data.MasterVolume;
        _bgmVolumeSlider.value = data.BGMVolume;
        _sfxVolumeSlider.value = data.SFXVolume;
        _resolutionDropdown.value = data.ResolutionIndex;
        _fullscreenToggle.isOn = data.Fullscreen;
        _qualityDropdown.value = _optionManager.OptionData.QualityIndex;
        _mouseSensitivitySlider.value = data.MouseSensitivity;
        _valueText.text = data.MouseSensitivity.ToString("F2");

        _resolutionDropdown.RefreshShownValue();
        _languageDropdown.RefreshShownValue();
        _qualityDropdown.RefreshShownValue();
    }

    /// <summary>
    /// 이벤트 연결
    /// </summary>
    void SubscribeEvents()
    {
        if (_subscribed) return;

        _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        _masterVolumeSlider.onValueChanged.AddListener(_optionManager.ApplyMasterVolume);
        _bgmVolumeSlider.onValueChanged.AddListener(_optionManager.ApplyBGMVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(_optionManager.ApplySFXVolume);
        _resolutionDropdown.onValueChanged.AddListener(_optionManager.ApplyResolution);
        _fullscreenToggle.onValueChanged.AddListener(_optionManager.ApplyFullscreen);
        _qualityDropdown.onValueChanged.AddListener(_optionManager.ApplyQuality);
        _mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        _subscribed = true;
    }

    /// <summary>
    /// 이벤트 연결 해제
    /// </summary>
    void UnsubscribeEvents()
    {
        if (_subscribed == false) return;

        _languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        _masterVolumeSlider.onValueChanged.RemoveListener(_optionManager.ApplyMasterVolume);
        _bgmVolumeSlider.onValueChanged.RemoveListener(_optionManager.ApplyBGMVolume);
        _sfxVolumeSlider.onValueChanged.RemoveListener(_optionManager.ApplySFXVolume);
        _resolutionDropdown.onValueChanged.RemoveListener(_optionManager.ApplyResolution);
        _fullscreenToggle.onValueChanged.RemoveListener(_optionManager.ApplyFullscreen);
        _qualityDropdown.onValueChanged.RemoveListener(_optionManager.ApplyQuality);
        _mouseSensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);

        _subscribed = false;
    }

    /// <summary>
    /// 언어 변경
    /// </summary>
    void OnLanguageChanged(int index)
    {
        string language = (index == 0) ? "en" : "ko";
        _optionManager.ApplyLanguage(language);
    }

    /// <summary>
    /// 마우스 감도 변경
    /// </summary>
    void OnSensitivityChanged(float value)
    {
        _valueText.text = value.ToString("F2");
        _optionManager.ApplyMouseSensitivity(value);
    }

    /// <summary>
    /// 적용
    /// </summary>
    public void OnClickSave()
    {
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