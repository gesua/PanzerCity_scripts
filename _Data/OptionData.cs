using UnityEngine;

/// <summary>
/// 옵션 설정값 저장/불러오기
/// </summary>
public class OptionData
{
    const string KEY_LANGUAGE = "Language";
    const string KEY_MASTER_VOLUME = "MasterVolume";
    const string KEY_BGM_VOLUME = "BGMVolume";
    const string KEY_SFX_VOLUME = "SFXVolume";
    const string KEY_RESOLUTION_INDEX = "ResolutionIndex";
    const string KEY_FULLSCREEN = "Fullscreen";
    const string KEY_QUALITY_INDEX = "QualityIndex";
    const string KEY_MOUSE_SENSITIVITY = "MouseSensitivity";

    public string Language { get; private set; }
    public float MasterVolume { get; private set; }
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public int ResolutionIndex { get; private set; }
    public bool Fullscreen { get; private set; }
    public int QualityIndex { get; private set; }
    public float MouseSensitivity { get; private set; }

    /// <summary>
    /// 저장된 값 불러오기
    /// </summary>
    public void Load()
    {
        Language = PlayerPrefs.GetString(KEY_LANGUAGE, "ko");
        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1f);
        BGMVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 1f);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1f);
        ResolutionIndex = PlayerPrefs.GetInt(KEY_RESOLUTION_INDEX, 0);
        Fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        QualityIndex = PlayerPrefs.GetInt(KEY_QUALITY_INDEX, 2);
        MouseSensitivity = PlayerPrefs.GetFloat(KEY_MOUSE_SENSITIVITY, 0.1f);
    }

    /// <summary>
    /// 값 저장
    /// </summary>
    public void Save()
    {
        PlayerPrefs.SetString(KEY_LANGUAGE, Language);
        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, MasterVolume);
        PlayerPrefs.SetFloat(KEY_BGM_VOLUME, BGMVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, SFXVolume);
        PlayerPrefs.SetInt(KEY_RESOLUTION_INDEX, ResolutionIndex);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KEY_QUALITY_INDEX, QualityIndex);
        PlayerPrefs.SetFloat(KEY_MOUSE_SENSITIVITY, MouseSensitivity);
        PlayerPrefs.Save();
    }

    public void SetLanguage(string language) { Language = language; }
    public void SetMasterVolume(float value) { MasterVolume = value; }
    public void SetBGMVolume(float value) { BGMVolume = value; }
    public void SetSFXVolume(float value) { SFXVolume = value; }
    public void SetResolutionIndex(int index) { ResolutionIndex = index; }
    public void SetFullscreen(bool value) { Fullscreen = value; }
    public void SetQualityIndex(int index) { QualityIndex = index; }
    public void SetMouseSensitivity(float value) { MouseSensitivity = value; }
}
