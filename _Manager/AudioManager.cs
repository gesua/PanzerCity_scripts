using UnityEngine;

public enum BgmType
{
    Title,
}
public enum SfxType
{
}

/// <summary>
/// 오디오 관리
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] AudioSource _bgmAs; // 배경음악 오디오소스
    [SerializeField] AudioSource _sfxAs; // 효과음 오디오소스
    [Header("----- 리소스 -----")]
    [SerializeField] AudioClip[] _bgmClips; // 배경음악 클립
    [SerializeField] AudioClip[] _sfxClips; // 효과음 클립

    /// <summary>
    /// 배경음악 재생
    /// </summary>
    public void PlayBgm(BgmType bgmType)
    {
        // 배경음악 종류에 맞는 클립 가져오기
        AudioClip clip = _bgmClips[(int)bgmType];

        _bgmAs.clip = clip;
        _bgmAs.Play();
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void PlaySfx(SfxType sfxType)
    {
        // 효과음 종류에 맞는 클립 가져오기
        AudioClip clip = _sfxClips[(int)sfxType];

        // 오디오 클립 일회성 재생
        _sfxAs.PlayOneShot(clip);
    }
}
