using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] MeshRenderer[] _normalVisualRenderers; // 플레이 모델 렌더러
    [SerializeField] GameObject _destroyedVisual; // 파괴된 모델
    [SerializeField] GameObject _destroyedTurret; // 파괴된 포탑
    [SerializeField] GameObject _destroyedBarrel; // 파괴된 주포
    [SerializeField] CommanderController _commander; // 전차장 캐릭터
    [SerializeField] ItemPickup _itemPickup; // 아이템 줍기 단축키
    [SerializeField] LoopEffect _shieldEffect; // 실드 이펙트
    [SerializeField] ParticleSystem _shieldParticle; // 실드 파티클 색 변경 용도
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _deadDuration = 5f; // 사망 상태 지속 시간
    [SerializeField] float _respawnShieldDuration = 3f; // 리스폰 무적 시간
    [Header("----- 리스폰 밀어내기 -----")]
    [SerializeField] float _respawnPushRadius = 2f; // 밀어낼 탱크 감지 반경
    [SerializeField] float _respawnPushOffset = 0.1f; // 밀어내기 강도 배율
    [SerializeField] LayerMask _respawnPushLayer = 1 << 8 | 1 << 9; // 밀어낼 대상 레이어(플레이어 + 적)

    bool _isAttack; // 좌클릭 누르는 중인지
    bool _isSniperMode; // 저격 모드인지(Shift)
    bool _isDead; // 죽었는지
    bool _isNetworkOwner = true; // 네트워크 소유자인지(싱글 플레이:항상 true)
    PlayerNetworkOwner _networkOwner; // 멀티플레이 여부 및 발사 요청 전달용

    float _reloadTimer; // 재장전 시간 잴거
    int _prevHp; // 이전 체력(피격 확인용)

    ParticleSystemRenderer _shieldRenderer;

    public ItemPickup ItemPickup => _itemPickup;
    public Transform TurretTr => _turret.TurretTr;
    public TankModel Model => _model;
    protected override bool ShowEffects => !_isSniperMode;
    public bool IsDead => _isDead;

    public event Action<int> OnDamaged;   // 대미지 받음<현재 HP>
    public event Action<HitData> OnHit;   // 피격
    public event Action OnPlayerRespawn;  // 리스폰
    public event Action<float> OnRespawnComplete; // 리스폰 완료<무적 지속시간>

    protected override void Awake()
    {
        base.Awake();

        // 이벤트 구독
        _model.OnHpChanged += HandleHpChanged; // HP 변경
        _model.OnHit += HandleHit;   // 피격
        _model.OnDead += HandleDead; // 사망

        // 실드 이펙트 색 변경 용도
        _shieldRenderer = _shieldParticle.GetComponent<ParticleSystemRenderer>();
        // 원본 메터리얼 훼손 방지용 복제본 생성 후 교체
        _shieldRenderer.trailMaterial = new Material(_shieldRenderer.trailMaterial);

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
        _isDead = true; // 리스폰 되기 전에 움직임 막는 용도
    }

    public void Move(Vector3 dir)
    {
        if (_isDead) return;
        _mover.Move(dir);

        // 버튼 누를 때만 엔진 연기 나옴
        SetEngineEffect(dir != Vector3.zero);
    }

    /// <summary>
    /// 차체 이동 속도 배율 적용
    /// </summary>
    protected override void ApplySpeedMultiplier(float multiplier)
    {
        base.ApplySpeedMultiplier(multiplier); // 모델에도 반영(상태 일관성용)
        _mover.SetSpeedMultiplier(multiplier);
    }

    /// <summary>
    /// 공격
    /// 멀티플레이에선 발사자 본인이 로컬 연출을 즉시 재생하고, 서버에 실제 포탄 생성을 요청(호스트면 직접 처리)
    /// </summary>
    public override void Attack()
    {
        if (_isDead) return;
        if (_isAttack == false) return; // 좌클릭 안 눌림

        PlayAttackEffects(); // 발사자 본인은 로컬에서 즉시 재생(서버 왕복 기다리지 않음)

        if (_networkOwner != null)
        {
            if (_networkOwner.IsServer) _networkOwner.HandleAttackOnServer(); // 호스트 자신이면 바로 처리
            else _networkOwner.RequestAttackServerRpc(); // 비호스트면 서버에 요청
        }
        else
        {
            SpawnShell(); // 싱글플레이(PlayAttackEffects는 위에서 이미 실행했으므로 SpawnShell만)
        }

        _reloadTimer = _model.MinAttackTime; // 플레이어는 min,max 아무거나 가져오기
    }

    /// <summary>
    /// 좌클릭 누른 여부
    /// </summary>
    public void SetIsAttack(bool isActive)
    {
        _isAttack = isActive;
    }

    /// <summary>
    /// 네트워크 소유권 설정(멀티플레이 전용, PlayerNetworkOwner가 호출)
    /// 소유자가 아니면 Update()의 로컬 로직(재장전/공격)을 실행하지 않음
    /// </summary>
    public void SetNetworkOwnership(bool isOwner)
    {
        _isNetworkOwner = isOwner;
        _turret.SetLocalControl(isOwner);
        _itemPickup.SetLocalControl(isOwner);
    }

    /// <summary>
    /// 멀티플레이:네트워크 오너 컴포넌트 참조 세팅(PlayerNetworkOwner가 자기 자신을 넘겨줌)
    /// null이 아니면 멀티플레이로 간주
    /// </summary>
    public void SetNetworkOwner(PlayerNetworkOwner networkOwner)
    {
        _networkOwner = networkOwner;
    }

    /// <summary>
    /// 멀티플레이:다른 클라이언트의 발사 신호를 받아 로컬로 연출만 재생할 때 호출(PlayerNetworkOwner가 호출)
    /// </summary>
    public void PlayLocalAttack()
    {
        PlayAttackEffects();
    }

    /// <summary>
    /// 멀티플레이:서버가 발사 요청을 처리하며 실제 포탄을 생성할 때 호출(PlayerNetworkOwner가 호출)
    /// </summary>
    public void SpawnShellOnServer()
    {
        SpawnShell();
    }

    /// <summary>
    /// 씬 전용 UI 참조 주입(멀티플레이 전용, GameScene이 로컬 플레이어 바인딩 시 호출)
    /// 프리팹은 씬 안의 오브젝트를 미리 연결할 수 없어서, 로컬 플레이어에게만 런타임에 연결함
    /// </summary>
    public void SetSceneReferences(
        CameraTarget cameraTarget,
        RectTransform centerCrosshair,
        RectTransform turretCrosshair,
        Image centerCrosshairImage,
        ReloadIndicator reloadIndicator)
    {
        _reloadIndicator = reloadIndicator;
        _turret.SetSceneReferences(cameraTarget, centerCrosshair, turretCrosshair, centerCrosshairImage);
    }



    private void Update()
    {
        if (_isNetworkOwner == false) return; // 네트워크 소유자가 아니면 로컬 로직 실행 안 함

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
        if (_isDead == false) _commander.PlayHitMotion(); // 피격 모션
        OnHit?.Invoke(hitData);
    }

    /// <summary>
    /// 플레이 모델(정상 상태) 표시 여부 설정
    /// GameObject 자체를 SetActive로 끄지 않고 렌더러만 껐다 켜서, 자식 오브젝트(Turret 등)가 계속 활성 상태를 유지하게 함
    /// (NGO의 nested NetworkTransform은 최초 Spawn 시점에 활성 상태였던 자식만 등록하므로, 중간에 SetActive(false)로 꺼지면 재활성화해도 동기화가 끊김)
    /// </summary>
    void SetNormalVisualVisible(bool visible)
    {
        foreach (MeshRenderer renderer in _normalVisualRenderers)
        {
            renderer.enabled = visible;
        }
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    void HandleDead(HitData hitData)
    {
        _isDead = true; // 죽었음

        // 사망 횟수 기록
        GameManager.Instance.GameStatistics.AddDeath();

        // 엔진 연기 끄기
        SetEngineEffect(false);
        // 포탑 끄기
        _turret.enabled = false;
        // 조준점 숨기기
        _turret.SetCrosshairVisible(false);
        // 켜져있던 적 실루엣 끄기
        _turret.ClearTargetSilhouette();
        // 서서히 멈추기
        _mover.Stop();

        // 전차장 세팅
        _commander.SetDead();
        _commander.CommanderRoot.SetParent(transform);
        // 전차장을 카메라 방향으로 회전
        _commander.SetLookAtCam(true);
        // 슬픈 표정
        _commander.SetSadFace();

        // 파괴된 모델로 교체
        SetNormalVisualVisible(false);
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
    public void Respawn(Vector3 spawnPos, CinemachineBrain brain)
    {
        StartCoroutine(RespawnRoutine(spawnPos, brain));
    }

    IEnumerator RespawnRoutine(Vector3 spawnPos, CinemachineBrain brain)
    {
        // 전차장 초기화
        _commander.CommanderRoot.SetParent(TurretTr);
        _commander.transform.localRotation = Quaternion.identity;
        _commander.Reset();

        PushOutTanks(spawnPos); // 리스폰 지점 주변 탱크 밀어내기

        _mover.Teleport(spawnPos, Quaternion.identity); // 시작 위치로
        _turret.ResetRotation(); // 포탑 초기화
        SetNormalVisualVisible(false); // 모델 비활성화
        _commander.SetVisible(false); // 전차장 비활성화
        _destroyedVisual.SetActive(false); // 파괴된 모델 비활성화
        _miniMapTankIcon.Hide(); // 미니맵 아이콘 숨기기
        _isAttack = false; // 공격 버튼 끄기
        _turret.CameraTarget.DisableDamping(); // 카메라 즉시 이동

        // 반짝 이펙트
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.Twinkle, spawnPos);

        // 이펙트 지속시간 대기
        yield return new WaitForSeconds(1f);

        _turret.CameraTarget.ResetDamping(); // 카메라 덤핑 값 복구

        // 탱크 생성
        _isDead = false; // 살았음
        _turret.enabled = true; // 포탑 켜기
        _turret.SetCrosshairVisible(true); // 조준점 보이기
        SetNormalVisualVisible(true); // 모델 활성화
        _commander.SetVisible(true); // 전차장 활성화
        _miniMapTankIcon.Show(); // 미니맵 아이콘 보이기
        _model.Initialize(); // HP 초기화
        OnRespawnComplete?.Invoke(_respawnShieldDuration); // 리스폰 무적 시작
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

    /// <summary>
    /// 실드 이펙트 켜기/끄기
    /// </summary>
    public void SetShieldEffect(bool active)
    {
        if (active) _shieldEffect.Play();
        else _shieldEffect.Stop();
    }

    /// <summary>
    /// 실드 이펙트 색 변경
    /// </summary>
    /// <param name="ratio">0 ~ 1 (0이 끝나갈 때)</param>
    public void UpdateShieldColor(float ratio)
    {
        // 초록 -> 노랑 -> 빨강
        Color color;
        if (ratio > 0.5f) color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
        else color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);

        // Material과 startColor 둘 다 바꿔야 색이 진하게 바뀜
        _shieldRenderer.trailMaterial.color = color;
        var main = _shieldParticle.main;
        main.startColor = color;
    }

    /// <summary>
    /// 중력 설정
    /// </summary>
    public void SetPlayerGravity(bool enable)
    {
        _mover.SetGravity(enable);
    }

    /// <summary>
    /// 리스폰 지점 주변의 탱크를 밀어냄
    /// </summary>
    void PushOutTanks(Vector3 center)
    {
        Collider[] colliders = Physics.OverlapSphere(center, _respawnPushRadius, _respawnPushLayer);

        foreach (Collider col in colliders)
        {
            // 자기 자신 제외
            if (col.GetComponentInParent<PlayerTank>() == this) continue;

            Rigidbody rigid = col.attachedRigidbody;
            if (rigid == null) continue;

            Vector3 dir = (col.transform.position - center).normalized;

            dir.y = 0f;

            // 리스폰 지점 뒤쪽(-z)으로는 밀지 않음
            if (dir.z < 0f) dir.z = 0f;

            // 클램프 후 방향이 0이면 기본 방향(앞쪽)으로
            if (dir.sqrMagnitude < Util.Epsilon) dir = Vector3.forward;
            else dir = dir.normalized;

            // 밀어냄
            float dist = Vector3.Distance(col.transform.position, center);
            float pushStrength = Mathf.Clamp(10f / dist, 0.5f, 3f);

            rigid.position = rigid.position + dir * pushStrength * _respawnPushOffset;

        }
    }

    /// <summary>
    /// 장착 뷰 모드
    /// </summary>
    public void EquipViewMod(bool enable)
    {
        if (enable)
        {
            _turret.ResetRotation();
            _turret.enabled = false;
        }
        else
        {
            _turret.enabled = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 리스폰 밀어내기 감지 반경
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _respawnPushRadius);

        // -z 방향 밀어내기 차단 경계선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            transform.position + Vector3.left * _respawnPushRadius,
            transform.position + Vector3.right * _respawnPushRadius
        );
    }
}