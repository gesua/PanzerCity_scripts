using System.Collections.Generic;
using UnityEngine;

public enum BgmType
{
    //Title,
}
public enum SfxType
{
    StageStart, // 스테이지 시작
    LifeUp,     // 1목숨 추가
    Pause,      // 일시정지(열릴 때만, 닫을 땐 안 남)

    // 미검수
    GameOver,   // 게임오버(HQ 파괴/목숨 소진 둘 다 동일) <----- flac로 가져오기
    StageClear, // +스테이지 클리어(적 전멸)
    GameClear,  // 게임 클리어(마지막 스테이지) <----- flac로 가져오기
}

[System.Serializable]
struct SfxEntry
{
    public SfxType type;
    public AudioClip clip;
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
    [SerializeField] SfxEntry[] _sfxEntries; // 인스펙터에서 타입-클립 쌍으로 등록
    Dictionary<SfxType, AudioClip> _sfxDict;

    void Awake()
    {
        _sfxDict = new Dictionary<SfxType, AudioClip>();
        foreach (var entry in _sfxEntries)
        {
            _sfxDict[entry.type] = entry.clip;
        }
    }

    [ContextMenu("빈 슬롯 자동 채우기")]
    void AutoFillSfxEntries()
    {
        var existing = new HashSet<SfxType>();
        foreach (var e in _sfxEntries) existing.Add(e.type);

        foreach (SfxType type in System.Enum.GetValues(typeof(SfxType)))
        {
            if (existing.Contains(type) == false)
            {
                System.Array.Resize(ref _sfxEntries, _sfxEntries.Length + 1);
                _sfxEntries[_sfxEntries.Length - 1] = new SfxEntry { type = type };
            }
        }
    }

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
        if (_sfxDict.TryGetValue(sfxType, out AudioClip clip) && clip != null)
        {
            _sfxAs.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"SFX 클립 없음: {sfxType}");
        }
    }
}
