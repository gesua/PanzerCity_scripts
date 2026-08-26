using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// PlayerTank의 Netcode 소유권을 판별해서 PlayerTank/Turret에 전달하는 브릿지
/// TankBase/PlayerTank 상속 구조를 건드리지 않기 위해 별도 컴포넌트로 분리
/// </summary>
[RequireComponent(typeof(PlayerTank))]
public class PlayerNetworkOwner : NetworkBehaviour
{
    [SerializeField] Camera _equipCamera; // 상점 장비칸 탱크 미리보기용 카메라
    PlayerTank _playerTank;

    [Header("----- 클라이언트 이동 감지(엔진 이펙트용) -----")]
    [SerializeField] float _movementThreshold = 0.05f; // 초당 이동 거리 기준(이 값보다 크면 '움직이는 중'으로 판단)

    Vector3 _lastPosition;
    bool _wasMovingLocally;

    // 목숨 UI 동기화용:Owner만 로컬에서 직접 쓸 수 있음(목숨은 서버 권위가 아니라 각자 로컬 판단 기반)
    NetworkVariable<int> _life = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // HP 동기화용
    NetworkVariable<int> _hp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 무적 판정 동기화용:서버가 자기 쪽 TankModel 사본에도 반영해야 데미지 판정에서 실제로 걸러짐(연출과 별개)
    NetworkVariable<bool> _isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 채팅 발신자 닉네임 표시용:Owner만 로컬에서 직접 쓸 수 있음, 스폰 시 1회 세팅 후 값이 바뀌지 않음
    NetworkVariable<FixedString64Bytes> _nickname = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 탱크 색상/채팅 색상 통일용:룸에서 배정받은 자리
    NetworkVariable<int> _playerIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int CurrentLife => _life.Value; // 다른 클라이언트가 초기 UI 세팅 시 조회용
    public int SyncedHp => _hp.Value;
    public string Nickname => _nickname.Value.ToString(); // 채팅 UI가 발신자 닉네임 조회 시 사용
    public int PlayerIndex => _playerIndex.Value; // 채팅 UI가 발신자 색상 조회 시 사용

    // 완전히 패배(목숨 0 상태에서 한 번 더 사망)했음을 나타내는 네트워크 동기화 전용 값
    // PlayerData.Life는 UI 등 다른 소비자가 있어 0 밑으로 못 내려가므로, 네트워크 동기화 값(_life)에만 별도로 표시
    public const int EliminatedLife = -1;

    void Awake()
    {
        TryGetComponent(out _playerTank);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn, {_life.Value}, {_playerIndex.Value}");

        // Lobby 씬이 언로드돼도 파괴되지 않도록 보호(각 컴퓨터에서 로컬로 각자 적용됨)
        DontDestroyOnLoad(gameObject);

        _playerTank.SetNetworkOwnership(IsOwner);
        _playerTank.SetNetworkOwner(this);
        _equipCamera.enabled = IsOwner; // 상점 장비칸 미리보기

        _playerTank.OnPlayerRespawn += HandleRemoteRespawn;
        // 목숨 UI:전원이 이 값의 변경을 받아서 NetworkGameManager로 릴레이
        _life.OnValueChanged += HandleLifeValueChanged;
        // HP
        _hp.OnValueChanged += HandleHpValueChanged;
        // 무적 판정:전원(서버 포함)이 이 값의 변경을 받아서 각자 로컬 TankModel에 반영
        _isInvincible.OnValueChanged += HandleInvincibleValueChanged;
        // 탱크 색상:clientId 대신 룸 자리 기반이라 값이 늦게 동기화될 수 있어 반응형으로 처리(값이 도착하는 즉시 적용)
        _playerIndex.OnValueChanged += HandlePlayerIndexValueChanged;
        // 상점 열림/닫힘:로컬에서 다른 플레이어의 탱크 모델을 숨기고 복원하기 위해 구독
        NetworkGameManager.Instance.OnShopActiveChanged += HandleShopActiveChanged;

        // 프록시(원격 관찰자) 인스턴스는 GameScene의 최초 스폰 흐름을 안 타므로, Awake()/Initialize()가 설정한 _isDead=true가
        // 실제 사망 없이는 리스폰 전까지 영원히 안 풀림 — 실제로는 생존 중이므로 여기서 바로잡음(PlayerTank.InitializeAliveState 참고)
        if (IsOwner == false)
        {
            _playerTank.InitializeAliveState();

            // 소유자 쪽 OnNetworkSpawn(playerIndex/life를 세팅하는 코드)도 스폰 후 네트워크를 한 바퀴 거쳐야 실행되므로,
            // 이 관찰자가 스폰을 받은 시점엔 아직 소유자의 초기값이 도착 전일 수 있음(호스트 자신을 제외한 모든 원격 클라이언트에서 발생)
            // playerIndex는 -1이 "미도착"을 명확히 구분해주는 값이라, 실제로 도착할 때까지 기다렸다가 동기화
            StartCoroutine(SyncInitialStateWhenReady());

            // 원격 관찰자 로컬 이동 감지 초기화(엔진 이펙트용)
            _lastPosition = transform.position;
        }

        // 로컬 소유일 때만 GameScene에 스폰 완료를 알림
        if (IsOwner)
        {
            // 룸 자리 인덱스 동기화:탱크 색상/채팅 색상뿐 아니라 GameScene.Initialize()(바로 아래 NotifyLocalPlayerSpawned가
            // 동기적으로 트리거함)도 스폰 인덱스로 이 값을 즉시 읽으므로, 다른 무엇보다 먼저 설정해야 함
            _playerIndex.Value = LobbyManager.Instance.MyPlayerIndex;

            NetworkGameManager.Instance.NotifyLocalPlayerSpawned(_playerTank);

            // 목숨이 바뀔 때마다 서버에 보고
            GameManager.Instance.PlayerData.OnLifeChanged += SetLifeValue;
            SetLifeValue(GameManager.Instance.PlayerData.Life); // 스폰 시점의 현재 값도 즉시 반영

            // HP 바뀔 때마다 서버에 보고
            _playerTank.Model.OnHpChanged += HandleLocalHpChanged;
            HandleLocalHpChanged(_playerTank.Model.CurrentHp, _playerTank.Model.MaxHp);

            // 닉네임 동기화:로비에서 설정한 닉네임을 네트워크로 전파(FixedString64Bytes 용량 초과 방지를 위해 안전 길이로 절단)
            string nickname = LobbyManager.Instance.Nickname;
            _nickname.Value = (nickname.Length > 20) ? nickname.Substring(0, 20) : nickname;
        }
    }

    /// <summary>
    /// 네트워크 디스폰 시 정리 — OnNetworkSpawn에서 구독한 외부(NetworkGameManager) 이벤트 해제
    /// _playerTank/NetworkVariable 등 같은 오브젝트 안의 구독은 이 오브젝트와 함께 파괴되므로 별도 해제가 필요 없지만,
    /// NetworkGameManager.OnShopActiveChanged는 외부 싱글톤 이벤트라 해제하지 않으면 디스폰 후에도 파괴된 오브젝트를 향해 계속 호출됨
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnShopActiveChanged -= HandleShopActiveChanged;
        }
    }

    /// <summary>
    /// 원격 관찰자는 소유자의 로컬 입력(PlayerTank.Move)이 전혀 돌지 않으므로,
    /// NetworkTransform으로 받은 위치 변화를 직접 관찰해서 엔진 이펙트 여부를 스스로 판단
    /// (소유자 자신은 이미 Move()에서 SetEngineEffect를 호출하므로 스킵)
    /// </summary>
    void Update()
    {
        if (IsOwner) return;
        if (_playerTank.IsDead) return; // 사망 중엔 판정하지 않음(순간이동성 위치 이동으로 오탐 방지)
        if (Time.deltaTime <= 0f) return; // 일시정지 등으로 deltaTime이 0이면 스킵(0으로 나누기 방지)

        float speed = Vector3.Distance(transform.position, _lastPosition) / Time.deltaTime;
        bool isMoving = (speed > _movementThreshold);

        if (isMoving != _wasMovingLocally)
        {
            _wasMovingLocally = isMoving;
            _playerTank.SetEngineEffect(isMoving);
        }

        _lastPosition = transform.position;
    }

    void SetLifeValue(int life)
    {
        _life.Value = life;
    }

    void HandleLocalHpChanged(int current, int max) => _hp.Value = current;

    void HandleHpValueChanged(int previousValue, int currentValue)
    {
        if (IsOwner) return; // 오너 자신은 TakeDamage에서 이미 직접 반영
        _playerTank.ApplySyncedHp(currentValue);
    }

    /// <summary>
    /// 원격 관찰자 전용:소유자의 초기 playerIndex/life 값이 네트워크로 도착할 때까지 기다렸다가 한 번 동기화
    /// playerIndex(-1이 "미도착" sentinel)가 정상 값으로 바뀌는 시점을 기준으로 삼음 — life는 0이 정상값일 수도 있어
    /// 그 자체론 미도착 여부를 못 가리므로, 소유자 쪽에서 거의 동시에 세팅되는 playerIndex에 편승해서 판단함
    /// </summary>
    IEnumerator SyncInitialStateWhenReady()
    {
        const float timeout = 3f; // 이 정도 지나도 안 오면 다른 문제로 보고 포기(무한 대기 방지)
        float elapsed = 0f;

        while (_playerIndex.Value < 0 && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        HandlePlayerIndexValueChanged(-1, _playerIndex.Value);
        HandleLifeValueChanged(0, _life.Value);
        HandleHpValueChanged(0, _hp.Value);
    }

    /// <summary>
    /// 목숨 UI 동기화 — OwnerClientId는 사망 판정용 안정적 키(NetworkGameManager._playerLives)로,
    /// PlayerIndex는 UI 표시 슬롯으로 각각 따로 씀(용도가 달라서 하나로 겸용할 수 없음)
    /// </summary>
    void HandleLifeValueChanged(int previousValue, int currentValue)
    {
        NetworkGameManager.Instance.NotifyLifeChanged(OwnerClientId, PlayerIndex, currentValue);
    }

    /// <summary>
    /// 완전히 패배 처리(목숨 0에서 한 번 더 사망) — GameScene이 호출
    /// PlayerData.Life는 0 아래로 안 내려가서 "0으로 생존 중"과 "완전히 패배"를 구분 못 하므로, 네트워크 값만 별도로 EliminatedLife로 내림
    /// </summary>
    public void MarkEliminated()
    {
        _life.Value = EliminatedLife;
    }

    /// <summary>
    /// 탈락 상태에서 다음 스테이지 진입(상점 오픈 시점) — 팀이 스테이지를 클리어하면 탈락자도 마지막 목숨(0)으로 복귀
    /// 탈락 상태가 아니면(정상적으로 목숨이 남아있으면) 아무 것도 안 함 — GameScene이 호출
    /// </summary>
    public void ReviveFromEliminationAtShop()
    {
        if (_life.Value != EliminatedLife) return;

        _life.Value = 0;
    }

    /// <summary>
    /// 무적 판정 네트워크 반영(PlayerTank가 호출) — 소유 클라이언트만 쓸 수 있음
    /// </summary>
    public void SetInvincible(bool invincible)
    {
        _isInvincible.Value = invincible;
    }

    /// <summary>
    /// 무적 판정 동기화 — 서버를 포함한 모든 관찰자가 각자 로컬 TankModel에 반영
    /// 이래야 서버의 데미지 판정(Shell)에서도 실제로 걸러짐(연출은 PlayerTank.ActivateShieldVisual이 별도 담당)
    /// </summary>
    void HandleInvincibleValueChanged(bool previousValue, bool currentValue)
    {
        _playerTank.Model.SetNoDamage(currentValue);
    }

    /// <summary>
    /// 룸 자리 인덱스 동기화 — 값이 도착하는(또는 바뀌는) 즉시 탱크 색상 적용
    /// clientId 기반 즉시 호출과 달리, NetworkVariable 동기화를 기다려야 해서 반응형으로 처리함
    /// (원격 관찰자 입장에서 스폰 시점에 값이 아직 안 왔더라도, 도착하는 순간 이 핸들러가 다시 불려 색상이 뒤늦게라도 맞게 적용됨)
    /// </summary>
    void HandlePlayerIndexValueChanged(int previousValue, int currentValue)
    {
        _playerTank.SetTankColorByIndex(currentValue);
        _playerTank.SetCommanderByIndex(currentValue);
    }

    /// <summary>
    /// 상점 UI 열림/닫힘 반영 — 소유자 자신은 장비칸 미리보기에 계속 보여야 하므로 제외, 원격 관찰자만 로컬에서 탱크 모델을 숨기고 복원
    /// </summary>
    void HandleShopActiveChanged(bool active)
    {
        // 방어적 가드:OnNetworkDespawn에서 구독을 해제하지만, 연결 종료로 인한 디스폰 시점에 따라
        // 해제가 완료되기 전에 이미 파괴된 인스턴스로 이벤트가 도달하는 경우가 있어 추가로 체크
        if (this == null) return;
        if (IsOwner) return; // 소유자 자신은 장비칸 미리보기 대상이라 계속 보여야 함

        _playerTank.SetShopVisualHidden(active);
    }

    /// <summary>
    /// 멀티플레이:소유자가 아닌 관찰자 쪽에서 리스폰을 재현
    /// GameScene은 로컬 플레이어(소유자)만 처리하므로, 원격 플레이어의 리스폰은 각자 로컬로 독립 재현해야 함
    /// 생명력 체크/게임오버 등 로컬 전용 로직은 소유자 쪽에서 GameScene이 이미 처리하므로 여기선 순수 연출만
    /// </summary>
    void HandleRemoteRespawn()
    {
        if (IsOwner) return; // 소유자 자신은 GameScene이 이미 처리
        StartCoroutine(PlayRemoteRespawnIfNotEliminated());
    }

    /// <summary>
    /// 이 이벤트는 소유자 쪽 DeadRoutine 사망 타이머가 끝나면 무조건 발생하는데, 그 시점에 목숨이 0이면
    /// "마지막 목숨으로 부활"인지 "완전히 탈락"인지 구분이 안 됨 — 탈락 판정(MarkEliminated)은 소유자가
    /// 별도로 내려서 네트워크로 전파하는 값이라, 이 타이머보다 늦게 도착하는 경우가 흔함(특히 관찰자 입장)
    /// 그래서 0으로 들어오면 짧게 기다려서 탈락 신호가 뒤늦게라도 오는지 한 번 더 확인한 뒤에 재생 여부를 결정함
    /// </summary>
    IEnumerator PlayRemoteRespawnIfNotEliminated()
    {
        if (CurrentLife == 0)
        {
            const float graceWindow = 1f; // 탈락 판정 + 네트워크 전파를 기다려줄 유예 시간
            float elapsed = 0f;

            while (CurrentLife == 0 && elapsed < graceWindow)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        if (CurrentLife == EliminatedLife) yield break; // 유예 시간 안에 탈락으로 확정됨 — 리스폰 연출 재생 안 함

        StageScene stageScene = NetworkGameManager.Instance.StageScene;
        if (stageScene == null) yield break;

        Vector3 spawnPos = stageScene.GetSpawnPoint(PlayerIndex);
        _playerTank.Respawn(spawnPos, null); // CinemachineBrain은 RespawnRoutine에서 실제로 쓰이지 않아 null 전달
    }

    /// <summary>
    /// 실제 포탄 생성 + 전원에게 연출 신호 전달(서버 전용)
    /// 호스트 자신의 발사(PlayerTank가 직접 호출) / 원격 클라이언트의 발사 요청(RequestAttackServerRpc) 양쪽에서 사용
    /// 위치/회전은 발사자 본인이 보낸 값을 그대로 사용(서버가 자기 쪽에서 다시 읽으면 라운드트립 지연만큼 밀려 보임)
    /// </summary>
    public void HandleAttackOnServer(Vector3 firePosition, Quaternion fireRotation)
    {
        if (IsServer == false) return; // 방어적 가드

        _playerTank.SpawnShellOnServer(firePosition, fireRotation);
        NotifyAttackClientRpc();
    }

    /// <summary>
    /// 비호스트 클라이언트가 발사 입력을 받았을 때 서버에 요청
    /// </summary>
    [ServerRpc]
    public void RequestAttackServerRpc(Vector3 firePosition, Quaternion fireRotation)
    {
        HandleAttackOnServer(firePosition, fireRotation);
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
    /// 무적 연출 신호를 전원에게 전달(서버 전용)
    /// 호스트 자신의 아이템 사용(PlayerTank가 직접 호출) / 비호스트 클라이언트의 요청(RequestHyperShieldServerRpc) 양쪽에서 사용
    /// </summary>
    public void HandleHyperShieldOnServer(float duration)
    {
        if (IsServer == false) return; // 방어적 가드

        NotifyHyperShieldClientRpc(duration);
    }

    /// <summary>
    /// 비호스트 클라이언트가 무적 연출 시작을 서버에 요청(소유자만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestHyperShieldServerRpc(float duration)
    {
        HandleHyperShieldOnServer(duration);
    }

    /// <summary>
    /// 서버가 전원에게 무적 연출 신호 전달
    /// 소유 클라이언트 본인은 아이템 사용 즉시 로컬에서 이미 재생 중이므로 제외
    /// </summary>
    [ClientRpc]
    void NotifyHyperShieldClientRpc(float duration)
    {
        if (IsOwner) return;
        _playerTank.PlayShieldVisual(duration);
    }

    /// <summary>
    /// 실제 아이템 드롭 스폰 처리(서버 전용)
    /// 호스트 자신의 드롭(PlayerTank가 직접 호출) / 비호스트 클라이언트의 드롭 요청(RequestDropItemServerRpc) 양쪽에서 사용
    /// </summary>
    public void HandleDropItemOnServer(int itemID, Vector3 position)
    {
        if (IsServer == false) return; // 방어적 가드

        _playerTank.DropItemOnServer(itemID, position);
    }

    /// <summary>
    /// 비호스트 클라이언트가 아이템 드롭 입력을 받았을 때 서버에 요청(소유자만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestDropItemServerRpc(int itemID, Vector3 position)
    {
        HandleDropItemOnServer(itemID, position);
    }

    /// <summary>
    /// 비호스트 클라이언트의 장비 장착/해제를 서버 쪽 TankModel 사본에 반영(소유자만 호출 가능)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestEquipServerRpc(int itemID, bool isEquip)
    {
        if (IsServer == false) return; // 방어적 가드

        ItemConfig config = GameManager.Instance.DataManager.GetItemConfig(itemID);
        if (config == null) return;

        _playerTank.Model.ApplyEquipment(config, isEquip);
    }

    /// <summary>
    /// 이 플레이어에게만 골드 지급을 알림(서버 전용, EnemyTank가 처치 판정 후 호출)
    /// </summary>
    public void NotifyGoldEarned(int amount)
    {
        if (IsServer == false) return; // 방어적 가드

        ClientRpcParams targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        NotifyGoldEarnedClientRpc(amount, targetParams);
    }

    /// <summary>
    /// 골드 지급 반영 — 소유 클라이언트(자기 자신)에게만 전달됨
    /// </summary>
    [ClientRpc]
    void NotifyGoldEarnedClientRpc(int amount, ClientRpcParams rpcParams = default)
    {
        GameManager.Instance.PlayerData.AddGold(amount);
        GameManager.Instance.GameStatistics.AddGoldEarned(amount);
    }
}