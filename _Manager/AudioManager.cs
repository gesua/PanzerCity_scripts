using System.Collections.Generic;
using UnityEngine;

public enum BgmType
{
    Title,
}
public enum SfxType
{
    StageStart,  // 스테이지 시작(2D)
    LifeUp,      // 1목숨 추가(2D)
    Pause,       // 일시정지(2D)
    GameOver,    // 게임오버(2D) : 소리 키우기, 주변 소리 다 없애기
    StageClear,  // 스테이지 클리어(2D)
    GameClear,   // 게임 클리어(마지막 스테이지)(2D)
    TankHit,     // 탱크 피격음
    TankDestroy, // 탱크 파괴음 : 소리 키우기, 아이템 써서 동시에 죽을 때는 1번만 들리게 하기
    HQDestroy,   // HQ 파괴음(2D)
    ShellExplosion, // 포탄 터지는 소리(3D)
    ItemDrop,    // 아이템 드랍음(3D)
    ItemUse,     // 아이템 사용음(2D)
}

[System.Serializable]
struct BgmEntry
{
    public BgmType type;
    public AudioClip clip;
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
    [SerializeField] BgmEntry[] _bgmEntries;
    [SerializeField] SfxEntry[] _sfxEntries;
    Dictionary<SfxType, AudioClip> _sfxDict;

    string _pooledSfxPrefabPath = "Audio/SFX_Pool"; // PooledSfx 컴포넌트가 붙은 프리팹 경로
    Dictionary<BgmType, AudioClip> _bgmDict;

    bool _isMassKillInProgress; // 일괄 처치 아이템 사용 중인지(개별 3D 재생을 스킵시키는 용도)
    HashSet<SfxType> _pendingMassKillSfx = new(); // 일괄 처치 중 큐잉된 타입(종료 시 타입별 한 번씩만 재생)

    public bool IsMassKillInProgress => _isMassKillInProgress;

    void Awake()
    {
        _bgmDict = new Dictionary<BgmType, AudioClip>();
        foreach (var entry in _bgmEntries)
        {
            _bgmDict[entry.type] = entry.clip;
        }

        _sfxDict = new Dictionary<SfxType, AudioClip>();
        foreach (var entry in _sfxEntries)
        {
            _sfxDict[entry.type] = entry.clip;
        }

        GameManager.Instance.PoolManager.GetPool(_pooledSfxPrefabPath); // Pool 미리 만들어놓기
    }

    [ContextMenu("BGM 빈 슬롯 자동 채우기")]
    void AutoFillBgmEntries()
    {
        var existing = new HashSet<BgmType>();
        foreach (var e in _bgmEntries) existing.Add(e.type);

        foreach (BgmType type in System.Enum.GetValues(typeof(BgmType)))
        {
            if (existing.Contains(type) == false)
            {
                System.Array.Resize(ref _bgmEntries, _bgmEntries.Length + 1);
                _bgmEntries[_bgmEntries.Length - 1] = new BgmEntry { type = type };
            }
        }
    }

    [ContextMenu("SFX 빈 슬롯 자동 채우기")]
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
        if (_bgmDict.TryGetValue(bgmType, out AudioClip clip) && clip != null)
        {
            _bgmAs.clip = clip;
            _bgmAs.loop = true; // BGM은 항상 반복 재생(그냥 명시적으로 적어둠)
            _bgmAs.Play();
        }
        else
        {
            Debug.LogWarning($"BGM 클립 없음: {bgmType}");
        }
    }

    /// <summary>
    /// 배경음악 정지
    /// </summary>
    public void StopBgm()
    {
        _bgmAs.Stop();
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void PlaySfx(SfxType sfxType)
    {
        if (TryGetSfxClip(sfxType, out AudioClip clip))
        {
            _sfxAs.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 일괄 처치 모드 시작(적 모두 격파 아이템 등)
    /// 진행 중엔 개별 3D 재생 대신 QueueMassKillSfx로 등록하고, 종료 시 타입별로 한 번씩만 재생됨
    /// </summary>
    public void StartMassKillMode()
    {
        _isMassKillInProgress = true;
        _pendingMassKillSfx.Clear();
    }

    /// <summary>
    /// 일괄 처치 중 개별 재생 대신 호출. 같은 타입은 한 번만 기록됨
    /// </summary>
    public void QueueMassKillSfx(SfxType sfxType)
    {
        _pendingMassKillSfx.Add(sfxType);
    }

    /// <summary>
    /// 일괄 처치 모드 종료. 큐잉된 타입들을 2D로 한 번씩 재생
    /// </summary>
    public void EndMassKillMode()
    {
        _isMassKillInProgress = false;

        foreach (SfxType sfxType in _pendingMassKillSfx)
        {
            PlaySfx(sfxType);
        }
        _pendingMassKillSfx.Clear();
    }

    /// <summary>
    /// 지정 위치에서 3D로 효과음 재생(풀링됨)
    /// 위치가 매번 다르고 동시에 여러 개 재생될 수 있는 SFX(포탄 터지는 소리 등)에 사용
    /// </summary>
    public void PlaySfxAtPoint(SfxType sfxType, Vector3 position)
    {
        if (TryGetSfxClip(sfxType, out AudioClip clip) == false) return;

        GameObject sfxGo = GameManager.Instance.PoolManager.GetFromPool(_pooledSfxPrefabPath);
        if (sfxGo == null) return;

        if (sfxGo.TryGetComponent(out PooledSfx pooledSfx))
        {
            pooledSfx.Play(clip, position);
        }
    }

    /// <summary>
    /// SfxType에 등록된 클립 조회(없으면 경고 로그)
    /// </summary>
    bool TryGetSfxClip(SfxType sfxType, out AudioClip clip)
    {
        if (_sfxDict.TryGetValue(sfxType, out clip) && clip != null) return true;

        Debug.LogWarning($"SFX 클립 없음: {sfxType}");
        return false;
    }
}