using UnityEngine;
using UnityEngine.Pool;

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
    [SerializeField] Transform _firePoint; // 포탄 생성 위치
    [SerializeField] LoopEffect _engineEffect; // 엔진 이펙트

    BushGroup _currentBush; // 현재 들어가있는 풀숲그룹
    int _bushEnterCount; // 풀 경계선에서 꼬이는거 방지

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
    }

    /// <summary>
    /// 엔진 이펙트 세팅
    /// </summary>
    public void SetEngineEffect(bool isMoving)
    {
        if (_engineEffect == null) return;


        if (ShowEffects == false)
        {
            _engineEffect.Stop();
            return;
        }

        if (isMoving)
        {
            _engineEffect.Play();
        }
        else if (isMoving == false)
        {
            _engineEffect.Stop();
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
}