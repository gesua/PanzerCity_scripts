using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 플레이어 탱크
/// </summary>
public class PlayerTank : TankBase
{
    [Header("----- 컴포넌트(PlayerTank) -----")]
    [SerializeField] Mover _mover;
    [SerializeField] Turret _turret;
    [SerializeField] ReloadIndicator _reloadIndicator; // 재장전 표시 UI
    [SerializeField] MiniMapTankIcon _miniMapTankIcon; // 미니맵 아이콘 UI
    [SerializeField] GameObject _normalVisual; // 플레이 모델
    [SerializeField] GameObject _destroyedVisual; // 파괴된 모델
    [SerializeField] GameObject _destroyedTurret; // 파괴된 포탑
    [SerializeField] GameObject _destroyedBarrel; // 파괴된 주포
    [SerializeField] CommanderController _commander; // 전차장 캐릭터
    [SerializeField] ItemPickup _itemPickup;
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _deadDuration = 5f;  // 사망 상태 지속 시간

    bool _isAttack; // 좌클릭 누르는 중인지
    bool _isSniperMode; // 저격 모드인지(Shift)
    float _reloadTimer; // 재장전 시간 잴거
    int _prevHp; // 이전 체력(피격 확인용)
    bool _isDead; // 죽었는지

    public ItemPickup ItemPickup => _itemPickup;
    public Transform TurretTr => _turret.TurretTr;
    public TankModel Model => _model;
    protected override bool ShowEffects => !_isSniperMode;
    public bool IsDead => _isDead;

    public event Action<int> OnDamaged;   // 대미지 받음<현재 HP>
    public event Action<HitData> OnHit;   // 피격
    public event Action OnPlayerRespawn;  // 리스폰


    protected override void Awake()
    {
        base.Awake();

        // 이벤트 구독
        _model.OnHpChanged += HandleHpChanged; // HP 변경
        _model.OnHit += HandleHit;   // 피격
        _model.OnDead += HandleDead; // 사망

        Initialize();

        // 게임 시작 전까지 멈춰놓기
        DisablePlayerAndUI();
    }

    public void Initialize()
    {
        _mover.Initialize(_model);
        _prevHp = _model.CurrentHp;

        _turret.SetRotSpeed(_model.TurretRotSpeed);

        // 초기화
        _turret.SetCrosshairVisible(true);
        _turret.ResetRotation();
        _reloadTimer = 0;
        _isAttack = false;
        _isDead = false;
    }

    public void Move(Vector3 dir)
    {
        if (_isDead) return;
        _mover.Move(dir);

        // 버튼 누를 때만 엔진 연기 나옴
        SetEngineEffect(dir != Vector3.zero);
    }

    /// <summary>
    /// 공격
    /// </summary>
    public override void Attack()
    {
        if (_isDead) return;
        if (_isAttack == false) return; // 좌클릭 안 눌림

        base.Attack();
        _reloadTimer = _model.MinAttackTime; // 플레이어는 min,max 아무거나 가져오기
    }

    /// <summary>
    /// 좌클릭 누른 여부
    /// </summary>
    public void SetIsAttack(bool isActive)
    {
        _isAttack = isActive;
    }

    private void Update()
    {
        // 재장전 체크
        if (_reloadTimer > 0f)
        {
            _reloadTimer -= Time.deltaTime;
            _reloadIndicator.UpdateReload(_model.MinAttackTime - _reloadTimer, _model.MinAttackTime);
        }
        else
        {
            Attack();
        }
    }

    /// <summary>
    /// 저격 모드 여부
    /// </summary>
    public void SetSniperMode(bool isSniper)
    {
        if (_isDead) return;
        _isSniperMode = isSniper;
        _turret.SetSniperMode(isSniper);
    }

    /// <summary>
    /// 에임 고정(turret에 있는거 그대로)
    /// </summary>
    public void SetAimLocked(bool locked)
    {
        _turret.SetAimLocked(locked);
    }

    /// <summary>
    /// 대미지 받음
    /// </summary>
    void HandleHpChanged(int current, int max)
    {
        if (current < _prevHp) OnDamaged?.Invoke(current);
        _prevHp = current;
    }

    /// <summary>
    /// 피격됨
    /// </summary>
    void HandleHit(HitData hitData)
    {
        OnHit?.Invoke(hitData);
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    void HandleDead()
    {
        _isDead = true; // 죽었음

        // 포탑 끄기
        _turret.enabled = false;
        // 조준점 숨기기
        _turret.SetCrosshairVisible(false);
        // 서서히 멈추기
        _mover.Stop();

        // 전차장 세팅
        _commander.transform.SetParent(transform);
        // 전차장을 카메라 방향으로 회전
        //_commander.SetLookAtCam(true);
        _commander.LookAtCamera();
        // 슬픈 표정
        _commander.SetSadFace();

        // 파괴된 모델로 교체
        _normalVisual.SetActive(false);
        _destroyedVisual.SetActive(true);
        // 포탑 위치 맞춰줌
        _destroyedTurret.transform.localRotation = TurretTr.localRotation;
        _destroyedBarrel.transform.localRotation = _turret.BarrelTr.transform.localRotation;
        // 폭발 이펙트 재생
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.SmallExplosion, TurretTr.position);

        // 사망 지속시간 뒤에 Invoke
        StartCoroutine(DeadRoutine());
    }

    /// <summary>
    /// 사망 지속시간 뒤에 이벤트 발급
    /// </summary>
    IEnumerator DeadRoutine()
    {
        yield return new WaitForSeconds(_deadDuration);
        OnPlayerRespawn?.Invoke();
    }

    /// <summary>
    /// 리스폰
    /// </summary>
    public void Respawn(Vector3 spawnPos)
    {
        StartCoroutine(RespawnRoutine(spawnPos));
    }

    IEnumerator RespawnRoutine(Vector3 spawnPos)
    {
        // 전차장 초기화
        _commander.transform.SetParent(TurretTr);
        _commander.transform.localRotation = Quaternion.identity;
        _commander.Reset();

        // 카메라 이동
        _mover.Teleport(spawnPos, Quaternion.identity); // 시작 위치로
        _turret.ResetRotation(); // 포탑 초기화
        _normalVisual.SetActive(false); // 모델 비활성화
        _destroyedVisual.SetActive(false); // 파괴된 모델 비활성화
        _miniMapTankIcon.Hide(); // 미니맵 아이콘 숨기기
        _isAttack = false; // 공격 버튼 끄기

        // 반짝 이펙트
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.Twinkle, spawnPos);

        // 이펙트 지속시간 대기
        yield return new WaitForSeconds(1f);

        // 탱크 생성
        _isDead = false; // 살았음
        _turret.enabled = true; // 포탑 켜기
        _turret.SetCrosshairVisible(true); // 조준점 보이기
        _normalVisual.SetActive(true); // 모델 활성화
        _miniMapTankIcon.Show(); // 미니맵 아이콘 보이기
        _model.Initialize(); // HP 초기화
    }

    /// <summary>
    /// 플레이어의 움직임과 UI를 없앰(HQ 파괴로 패배)
    /// </summary>
    public void DisablePlayerAndUI()
    {
        _isDead = true;

        // 조준점 숨기기
        _turret.SetCrosshairVisible(false);

        // 서서히 멈추기
        _mover.Stop();

        // 포탑 끄기
        _turret.enabled = false;
    }
}
