using UnityEngine;

/// <summary>
/// 플레이어와 적이 사용할 TankBase
/// 데이터테이블에서 _tankID로 TankModel을 불러옴
/// 공격 기능이 들어있음
/// </summary>
[RequireComponent(typeof(TankModel))]
public abstract class TankBase : MonoBehaviour, IAttackable
{
    [SerializeField] int _tankID; // TankData에서 데이터 가져올 ID
    [Header("----- 컴포넌트(TankBase) -----")]
    [SerializeField] protected Transform _firePoint; // 포탄 생성 위치
    [SerializeField] LoopEffect _engineEffect; // 엔진 이펙트
    [Header("----- 사운드 -----")]
    [SerializeField] AudioSource _shootAudioSource; // 포 발사 소리 전용
    [SerializeField] AudioSource _moveAudioSource; // 움직이는 소리 전용
    [SerializeField] AudioClip _shootClip; // 포 발사 소리
    [SerializeField] AudioClip _moveClip; // 움직이는 소리

    bool _prevIsMoving; // 엔진 이펙트 중복 호출 방지

    BushGroup _currentBush; // 현재 들어가있는 풀숲그룹
    int _bushEnterCount; // 풀 경계선에서 꼬이는거 방지
    int _mudEnterCount; // 진흙 경계선에서 꼬이는거 방지

    protected TankModel _model;
    protected TankData _tankData;
    string _shellPath = "Shell"; // 포탄 프리팹 위치

    public BushGroup CurrentBush => _currentBush;
    protected virtual bool ShowEffects => true; // 이펙트 보여줄지 여부(플레이어 저격 모드엔 안 보임)

    protected virtual void Awake()
    {
        _model = GetComponent<TankModel>();
        _tankData = GameManager.Instance.DataManager.GetTankData(_tankID);
        if (_tankData != null) _model.Initialize(_tankData);

        // 피격음/파괴음 공통 구독
        _model.OnHit += HandleHitSound;
        _model.OnDead += HandleDeadSound;
    }

    /// <summary>
    /// 피격음 재생
    /// </summary>
    void HandleHitSound(HitData hitData)
    {
        if (_model.IsAlive == false) return; // 사망 시엔 파괴음만 나야 하므로 스킵
        GameManager.Instance.AudioManager.PlaySfxAtPoint(SfxType.TankHit, transform.position);
    }

    /// <summary>
    /// 파괴음 재생
    /// </summary>
    void HandleDeadSound(HitData hitData)
    {
        // 일괄 처치 아이템 사용 중엔 개별 3D 재생을 스킵, 대신 2D 대표음이 한번만 남
        if (GameManager.Instance.AudioManager.IsMassKillInProgress) return;

        GameManager.Instance.AudioManager.PlaySfxAtPoint(SfxType.TankDestroy, transform.position);
    }

    public virtual void Attack()
    {
        // 저격 모드엔 포신 이펙트 안 보이게 함
        if (ShowEffects)
        {
            // 포신 이펙트 생성
            GameManager.Instance.EffectManager.SpawnEffect(EffectType.TinyExplosion, _firePoint.position);
        }

        // 포탄 생성
        GameObject shellGo = GameManager.Instance.PoolManager.GetFromPool(_shellPath);
        shellGo.transform.position = _firePoint.position;
        shellGo.transform.rotation = _firePoint.rotation;

        // 포탄 초기화
        if (shellGo.TryGetComponent(out Shell shell))
        {
            shell.Initialize(_model, gameObject.layer, this);
        }

        // 포 쏘는 소리
        _shootAudioSource.PlayOneShot(_shootClip);
    }

    /// <summary>
    /// 엔진 이펙트 세팅
    /// </summary>
    public void SetEngineEffect(bool isMoving)
    {
        if (_engineEffect == null) return;
        if (isMoving == _prevIsMoving) return; // 상태 안 바뀌었으면 스킵
        _prevIsMoving = isMoving;

        // 저격 모드엔 엔진 이펙트 안 보이게 함
        if (ShowEffects)
        {
            // 엔진 연기 재생
            if (isMoving)
            {
                _engineEffect.Play();
            }
            else
            {
                _engineEffect.Stop();
            }
        }
        else
        {
            _engineEffect.Stop();
        }

        // 움직이는 소리
        if (isMoving && _moveAudioSource.isPlaying == false)
        {
            _moveAudioSource.clip = _moveClip;
            _moveAudioSource.Play();
        }
        else if (isMoving == false)
        {
            _moveAudioSource.Stop();
        }
    }

    public virtual void TakeHit(HitData hitData)
    {
        _model.TakeDamage(hitData);
    }

    /// <summary>
    /// 풀숲 진입
    /// </summary>
    public void OnBushEnter(BushGroup bush)
    {
        _bushEnterCount++;
        _currentBush = bush;
    }

    /// <summary>
    /// 풀숲 퇴장
    /// </summary>
    public void OnBushExit(BushGroup bush)
    {
        _bushEnterCount--;
        if (_bushEnterCount <= 0)
        {
            _bushEnterCount = 0;
            _currentBush = null;
        }
    }

    /// <summary>
    /// 풀숲그룹 초기화
    /// </summary>
    public void ResetBush()
    {
        _bushEnterCount = 0;
        _currentBush = null;
    }

    /// <summary>
    /// 진흙 진입
    /// </summary>
    public void OnMudEnter(float speedMultiplier)
    {
        _mudEnterCount++;
        ApplySpeedMultiplier(speedMultiplier);
    }

    /// <summary>
    /// 진흙 퇴장
    /// </summary>
    public void OnMudExit()
    {
        _mudEnterCount--;
        if (_mudEnterCount <= 0)
        {
            _mudEnterCount = 0;
            ApplySpeedMultiplier(1f);
        }
    }

    /// <summary>
    /// 진흙 상태 초기화
    /// </summary>
    public void ResetMud()
    {
        _mudEnterCount = 0;
        ApplySpeedMultiplier(1f);
    }

    /// <summary>
    /// 차체 이동 속도 배율 적용(전진/후진/회전 속도, 포탑 회전속도엔 영향 없음)
    /// </summary>
    protected virtual void ApplySpeedMultiplier(float multiplier)
    {
        _model.SetSpeedMultiplier(multiplier);
    }
}