using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerTank의 Netcode 소유권을 판별해서 PlayerTank/Turret에 전달하는 브릿지
/// TankBase/PlayerTank 상속 구조를 건드리지 않기 위해 별도 컴포넌트로 분리
/// </summary>
[RequireComponent(typeof(PlayerTank))]
public class PlayerNetworkOwner : NetworkBehaviour
{
    PlayerTank _playerTank;

    [Header("----- 피격 동기화 -----")]
    // 서버 권위 HP(실제 소스). TankModel._currentHp는 TakeDamage 호출을 통해서만 이 값을 뒤따라감(TankModel 자체는 수정하지 않음)
    NetworkVariable<int> _currentHp = new NetworkVariable<int>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // 마지막 공격자 참조(HitDirectionIndicator 등 AtkTank가 필요한 UI를 위해 별도 동기화)
    // TankBase는 네트워크 직렬화 대상이 아니므로, 공격자의 EnemyNetworkOwner를 참조로 저장했다가 양쪽에서 역참조해서 재구성
    NetworkVariable<NetworkBehaviourReference> _lastAtkTankRef = new NetworkVariable<NetworkBehaviourReference>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    void Awake()
    {
        TryGetComponent(out _playerTank);
    }

    public override void OnNetworkSpawn()
    {
        // Lobby 씬이 언로드돼도 파괴되지 않도록 보호(각 컴퓨터에서 로컬로 각자 적용됨)
        DontDestroyOnLoad(gameObject);

        Debug.Log(
        $"OnNetworkSpawn | {name} | " +
        $"Owner:{IsOwner} | " +
        $"Pos:{transform.position} | " +
        $"Active:{gameObject.activeInHierarchy}");

        Debug.Log($"Scene : {gameObject.scene.name}");
        Debug.Log($"InstanceID : {gameObject.GetInstanceID()}");

        Debug.Log(FindObjectsByType<PlayerNetworkOwner>(FindObjectsSortMode.None).Length);

        _playerTank.SetNetworkOwnership(IsOwner);
        _playerTank.SetNetworkOwner(this);

        InitializeHpSync();

        // 로컬 소유일 때만 GameScene에 스폰 완료를 알림
        if (IsOwner)
        {
            NetworkGameManager.Instance.NotifyLocalPlayerSpawned(_playerTank);
        }
    }

    public override void OnNetworkDespawn()
    {
        _currentHp.OnValueChanged -= HandleHpValueChanged;
    }


    /// <summary>
    /// 실제 포탄 생성 + 전원에게 연출 신호 전달(서버 전용)
    /// 호스트 자신의 발사(PlayerTank가 직접 호출) / 원격 클라이언트의 발사 요청(RequestAttackServerRpc) 양쪽에서 사용
    /// </summary>
    public void HandleAttackOnServer()
    {
        if (IsServer == false) return;

        _playerTank.SpawnShellOnServer(OwnerClientId); // 포탄 소유권을 발사자 본인에게 넘겨 직격 판정 주체가 되게 함
        NotifyAttackClientRpc();
    }

    /// <summary>
    /// 비호스트 클라이언트가 발사 입력을 받았을 때 서버에 요청
    /// </summary>
    [ServerRpc]
    public void RequestAttackServerRpc()
    {
        HandleAttackOnServer();
    }

    /// <summary>
    /// 서버가 발사 처리를 마친 뒤 전원에게 신호 전달
    /// 발사자 본인은 입력 즉시 로컬에서 이미 재생했으므로 제외
    /// </summary>
    [ClientRpc]
    void NotifyAttackClientRpc()
    {
        if (IsOwner) return;
        _playerTank.PlayLocalAttack();
    }

    /// <summary>
    /// HP NetworkVariable 초기화 및 구독 시작(OnNetworkSpawn에서 호출)
    /// </summary>
    void InitializeHpSync()
    {
        if (IsServer) _currentHp.Value = _playerTank.Model.CurrentHp; // 서버는 TankModel 초기값을 그대로 시작값으로 사용

        _currentHp.OnValueChanged += HandleHpValueChanged;
    }

    /// <summary>
    /// 서버 권위:이 플레이어에게 데미지 적용
    /// Shell.ReportHitServerRpc(서버 컨텍스트)에서만 호출됨 — 피격 당사자 클라가 보고한 히트를 서버가 확정 처리하는 지점
    /// 더 이상 ServerRpc가 아님(Shell 쪽 RPC 하나로 통합, 여긴 순수 데미지 적용 로직만 담당)
    /// </summary>
    /// <param name="atkTank">공격한 적(EnemyTank). HitDirectionIndicator 등 AtkTank 참조가 필요한 UI를 위해 필요</param>
    public void ApplyHit(int damage, Vector3 hitPoint, TankBase atkTank)
    {
        if (IsServer == false) return; // 방어적 가드(정상 경로로는 서버 컨텍스트에서만 호출됨)
        if (_playerTank.Model.IsAlive == false) return; // 이미 죽은 상태면 무시(중복 히트 등, 리스폰은 다음 단계)

        // 클라이언트가 역참조로 재구성할 수 있도록 공격자 참조를 먼저 갱신(HP보다 먼저 보내서 순서 어긋날 확률을 줄임)
        if (atkTank != null && atkTank.TryGetComponent(out EnemyNetworkOwner atkOwner))
        {
            _lastAtkTankRef.Value = new NetworkBehaviourReference(atkOwner);
        }

        // 서버 자신의 TankModel.TakeDamage를 그대로 호출해서 치트 체크(_noDamage/_infiniteHP)까지 정상 반영
        // 그 결과값을 그대로 NetworkVariable에 실어 클라이언트에 전파(서버 판정이 곧 네트워크 진실)
        HitData hitData = new HitData(damage, hitPoint, atkTank);
        _playerTank.Model.TakeDamage(hitData);

        _currentHp.Value = _playerTank.Model.CurrentHp;
    }

    /// <summary>
    /// HP NetworkVariable 값 변경 콜백
    /// 서버는 ApplyHit 안에서 이미 TakeDamage로 이벤트를 발화했으므로 여기서 또 호출하면 중복 재생됨 → 클라이언트에서만 처리
    /// 클라이언트는 서버가 확정한 델타를 그대로 TakeDamage에 흘려보내 기존 OnHpChanged/OnHit/OnDead 이벤트를 재사용(치트 필드는 로컬에 없다고 가정)
    /// </summary>
    void HandleHpValueChanged(int previousValue, int newValue)
    {
        if (IsServer) return;

        int damage = previousValue - newValue;
        if (damage <= 0) return; // 초기값 세팅 등 감소가 없는 경우는 스킵

        // 공격자 참조 역참조(실패하면 null로 진행 — HitDirectionIndicator 등은 null을 자체적으로 처리해야 함)
        TankBase atkTank = null;
        if (_lastAtkTankRef.Value.TryGet(out EnemyNetworkOwner atkOwner))
        {
            atkOwner.TryGetComponent(out EnemyTank enemyTank);
            atkTank = enemyTank;
        }

        HitData hitData = new HitData(damage, transform.position, atkTank);
        _playerTank.Model.TakeDamage(hitData);
    }
}