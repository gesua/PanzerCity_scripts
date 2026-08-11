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
    [SerializeField] ItemDropper _itemDropper; // 아이템 바닥에 버리는 용도
    [SerializeField] LoopEffect _shieldEffect; // 실드 이펙트
    [SerializeField] ParticleSystem _shieldParticle; // 실드 파티클 색 변경 용도
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _deadDuration = 5f; // 사망 상태 지속 시간
    [SerializeField] float _respawnShieldDuration = 3f; // 리스폰 무적 시간
    [Header("----- 리스폰 밀어내기 -----")]
    [SerializeField] float _respawnPushRadius = 2f; // 밀어낼 탱크 감지 반경
    [SerializeField] float _respawnPushOffset = 0.1f; // 밀어내기 강도 배율
    [SerializeField] LayerMask _respawnPushLayer = 1 << 8 | 1 << 9; // 밀어낼 대상 레이어(플레이어 + 적)

    // 멀티플레이 구분 색상
    Color[] _playerColors = // 1P~4P 구분 색(BaseMap), OwnerClientId를 인덱스로 사용
    {
        new Color(1f, 1f, 1f), // 1P 청록/하양
        new Color(1f, 0f, 1f), // 2P 파랑/보라
        new Color(1f, 0f, 0f), // 3P 검정/빨강
        new Color(1f, 1f, 0f)  // 4P 연두/노랑
    };

    bool _isAttack; // 좌클릭 누르는 중인지
    bool _isSniperMode; // 저격 모드인지(Shift)
    bool _isDead; // 죽었는지
    bool _isShopVisualHidden; // 멀티플레이:상점 UI에서 다른 플레이어에게 겹쳐 보이지 않도록 로컬에서만 숨김 여부(PlayerNetworkOwner가 설정)
    bool _isNetworkOwner = true; // 네트워크 소유자인지(싱글 플레이:항상 true)
    PlayerNetworkOwner _networkOwner; // 멀티플레이 여부 및 발사 요청 전달용

    float _reloadTimer; // 재장전 시간 잴거
    int _prevHp; // 이전 체력(피격 확인용)

    ParticleSystemRenderer _shieldRenderer;

    static readonly int _baseColorID = Shader.PropertyToID("_BaseColor"); // BaseMap 색상 셰이더 프로퍼티 ID(구분 색상용)

    public ItemPickup ItemPickup => _itemPickup;
    public Transform TurretTr => _turret.TurretTr;
    public TankModel Model => _model;
    protected override bool ShowEffects => !_isSniperMode;
    public bool IsDead => _isDead;
    public MeshRenderer[] NormalVisualRenderers => _normalVisualRenderers;
    public CommanderController Commander => _commander;
    public PlayerNetworkOwner NetworkOwner => _networkOwner;

    public event Action<int> OnDamaged;   // 대미지 받음<현재 HP>
    public event Action<HitData> OnHit;   // 피격
    public event Action OnPlayerRespawn;  // 리스폰
    public event Action<float> OnRespawnComplete; // 리스폰 완료<무적 지속시간>

    Coroutine _shieldVisualRoutine; // 재시작 시 중복 방지용 핸들

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

        PlayAttackEffects(); // 발사자 본인은 로컬에서 즉시 재생

        if (_networkOwner != null)
        {
            if (_networkOwner.IsServer) _networkOwner.HandleAttackOnServer(_firePoint.position, _firePoint.rotation); // 호스트 자신이면 바로 처리
            else _networkOwner.RequestAttackServerRpc(_firePoint.position, _firePoint.rotation); // 비호스트면 서버에 요청(라운드트립 지연 동안 밀린 위치 대신 발사 시점 위치를 그대로 전달)
        }
        else
        {
            SpawnShell(); // 싱글플레이
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
        _itemPickup.SetNetworkOwner(networkOwner);
        _itemDropper.SetNetworkMode(true);
    }

    /// <summary>
    /// 멀티플레이:플레이어 구분 색상 적용(PlayerNetworkOwner가 OwnerClientId를 인덱스로 호출)
    /// renderer.material로 접근하면 최초 호출 시 Unity가 자동으로 인스턴 복제해줘서 원본 메터리얼은 훼손되지 않음
    /// </summary>
    public void SetTankColorByIndex(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= _playerColors.Length) return;
        if (colorIndex == 0) return; // 1P는 기본색

        Color color = _playerColors[colorIndex];

        foreach (MeshRenderer renderer in _normalVisualRenderers)
        {
            if (renderer.name.Contains("Track")) continue; // 궤도 색은 유지

            renderer.material.SetColor(_baseColorID, color);
        }

        _miniMapTankIcon.SetIconSpriteByIndex(colorIndex);
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
    /// 발사자 본인이 보낸 위치/회전을 그대로 사용(서버 자신의 _firePoint를 다시 읽지 않음)
    /// </summary>
    public void SpawnShellOnServer(Vector3 firePosition, Quaternion fireRotation)
    {
        SpawnShell(firePosition, fireRotation);
    }

    /// <summary>
    /// 인벤토리 아이템 드롭
    /// 멀티플레이에선 서버에 실제 스폰을 요청(호스트면 직접 처리) — 공격과 달리 즉시 재생할 로컬 연출은 없음
    /// </summary>
    public void DropItem(ItemConfig config)
    {
        Vector3 dropPosition = transform.position + transform.forward * 2f + Vector3.up;

        if (_networkOwner != null)
        {
            if (_networkOwner.IsServer) _networkOwner.HandleDropItemOnServer(config.Id, dropPosition); // 호스트 자신이면 바로 처리
            else _networkOwner.RequestDropItemServerRpc(config.Id, dropPosition); // 비호스트면 서버에 요청
        }
        else
        {
            _itemDropper.DropItem(config.Id, dropPosition); // 싱글플레이
        }
    }

    /// <summary>
    /// 멀티플레이:서버가 드롭 요청을 처리하며 실제 아이템을 생성할 때 호출(PlayerNetworkOwner가 호출)
    /// </summary>
    public void DropItemOnServer(int itemID, Vector3 position)
    {
        _itemDropper.DropItem(itemID, position);
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
    /// 멀티플레이:_isShopVisualHidden이 true인 동안은 visible 인자와 무관하게 항상 숨김 상태 유지(SetShopVisualHidden 참고)
    /// </summary>
    void SetNormalVisualVisible(bool visible)
    {
        bool actualVisible = visible && (_isShopVisualHidden == false);

        foreach (MeshRenderer renderer in _normalVisualRenderers)
        {
            renderer.enabled = actualVisible;
        }
    }

    /// <summary>
    /// 멀티플레이:원격 관찰자(프록시) 전용 — GameScene의 최초 스폰 흐름(Initialize/RespawnPlayer)은 로컬 플레이어에게만 적용되어
    /// Awake()/Initialize()가 설정한 _isDead=true가 실제 사망 없이는 리스폰 전까지 영원히 안 풀림(프록시는 RespawnRoutine을 아예 안 탐)
    /// 실제로는 정상 생존 중이므로 연출 없이 내부 상태만 바로잡음(PlayerNetworkOwner가 스폰 시 호출)
    /// </summary>
    public void InitializeAliveState()
    {
        _isDead = false;
    }

    /// <summary>
    /// 멀티플레이:상점 UI에서 다른 플레이어에게 겹쳐 보이지 않도록 로컬에서만 시각적으로 숨김(네트워크 동기화 없이 각자 로컬 판단, PlayerNetworkOwner가 호출)
    /// 사망/리스폰 흐름과 독립적인 레이어로 관리 — 상점이 열려있는 동안 리스폰이 완료돼도 계속 숨겨진 채 유지되고,
    /// 상점이 닫힐 때 그 시점의 실제 생존 상태를 반영해 다시 보여줌(사망 중이면 계속 숨김)
    /// </summary>
    public void SetShopVisualHidden(bool hidden)
    {
        _isShopVisualHidden = hidden;
        SetNormalVisualVisible(_isDead == false);
    }

    // 관전 모드 격리 위치:콜라이더(HitZone)가 렌더러 토글만으로는 꺼지지 않아 적 공격이 계속 명중하는 문제를 막기 위해 완전히 먼 곳으로 이동시킴
    static readonly Vector3 _spectateIsolatedPos = new Vector3(0f, -1000f, -1000f);

    /// <summary>
    /// 멀티플레이:관전 모드 진입 시 본인 탱크를 완전히 숨김(GameScene이 호출)
    /// 정상 모델은 렌더러만 꺼진 상태라 콜라이더(HitZone)는 계속 활성 상태로 남아있어, 위치까지 격리해야 적 공격을 안 받음
    /// 전차장(_commander)은 HandleDead에서 사망 연출(SetDead)만 될 뿐 숨겨지진 않으므로 별도로 SetVisible(false) 필요
    /// hidden=false(해제)는 처리하지 않음 — 관전 해제는 항상 재도전 직후 리스폰으로 바로 이어지고, RespawnRoutine이 이 모든 상태(위치/포탑/모델/전차장/미니맵)를 다시 정확히 세팅하므로 여기서 복구하면 오히려 중복
    /// </summary>
    public void SetSpectatingVisualHidden(bool hidden)
    {
        if (hidden == false) return;

        _mover.Teleport(_spectateIsolatedPos, transform.rotation);
        _turret.ResetRotation();
        SetNormalVisualVisible(false);
        _commander.SetVisible(false);
        _destroyedVisual.SetActive(false);
        _miniMapTankIcon.Hide();
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
        _turret.DisableCameraDamping(); // 카메라 즉시 이동

        // 반짝 이펙트
        GameManager.Instance.EffectManager.SpawnEffect(EffectType.Twinkle, spawnPos);

        // 이펙트 지속시간 대기
        yield return new WaitForSeconds(1f);

        _turret.ResetCameraDamping(); // 카메라 덤핑 값 복구

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
    /// 무적 실드 연출 재생(이펙트 on → 색상 서서히 변화 → 이펙트 off)
    /// 로컬 재생(GameScene.HyperShieldRoutine)과 원격 관찰자 재현(PlayerNetworkOwner의 ClientRpc) 양쪽에서 공용으로 사용
    /// 소유 여부와 무관하게 항상 안전하게 호출 가능한 순수 시각 효과임
    /// </summary>
    public void PlayShieldVisual(float duration)
    {
        if (_shieldVisualRoutine != null) StopCoroutine(_shieldVisualRoutine);
        _shieldVisualRoutine = StartCoroutine(ShieldVisualRoutine(duration));
    }

    IEnumerator ShieldVisualRoutine(float duration)
    {
        SetShieldEffect(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = 1f - (elapsed / duration); // 1에서 0으로 감소
            UpdateShieldColor(ratio);
            yield return null;
        }

        SetShieldEffect(false);
        _shieldVisualRoutine = null;
    }

    /// <summary>
    /// 무적 연출 시작 — 본인 화면은 즉시 로컬로 재생하고, 멀티면 다른 클라이언트에도 전파함
    /// SetNoDamage(실제 무적 판정)는 호출자(GameScene)가 별도로 처리함
    /// </summary>
    public void ActivateShieldVisual(float duration)
    {
        PlayShieldVisual(duration); // 본인 화면 로컬 재생

        if (_networkOwner != null)
        {
            if (_networkOwner.IsServer) _networkOwner.HandleHyperShieldOnServer(duration); // 호스트 자신이면 바로 처리
            else _networkOwner.RequestHyperShieldServerRpc(duration); // 비호스트면 서버에 요청
        }
    }

    /// <summary>
    /// 무적 판정을 네트워크에 반영 — 멀티면 서버도 알아야 데미지 판정에서 실제로 걸러짐(싱글이면 아무 것도 안 함)
    /// SetNoDamage 자체는 호출자(GameScene)가 로컬에서 별도로 호출함
    /// </summary>
    public void SetNetworkInvincible(bool invincible)
    {
        if (_networkOwner != null) _networkOwner.SetInvincible(invincible);
    }

    /// <summary>
    /// 장비 스탯을 네트워크에 반영 — 멀티면 서버 쪽 TankModel 사본에도 적용되어야 데미지 판정(Shell)에서 실제로 걸러짐(싱글이면 아무 것도 안 함)
    /// 호스트 자신은 로컬 TankModel이 곧 서버 TankModel이라 EquipmentManager가 이미 적용했으므로 여기서 걸러냄(중복 적용 방지)
    /// </summary>
    public void SyncEquipmentToServer(int itemID, bool isEquip)
    {
        if (_networkOwner == null) return; // 싱글플레이
        if (_networkOwner.IsServer) return; // 호스트는 이미 로컬에서 적용됨

        _networkOwner.RequestEquipServerRpc(itemID, isEquip);
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