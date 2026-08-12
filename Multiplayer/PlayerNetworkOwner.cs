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

    // 목숨 UI 동기화용:Owner만 로컬에서 직접 쓸 수 있음(목숨은 서버 권위가 아니라 각자 로컬 판단 기반)
    NetworkVariable<int> _life = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 무적 판정 동기화용:서버가 자기 쪽 TankModel 사본에도 반영해야 데미지 판정에서 실제로 걸러짐(연출과 별개)
    NetworkVariable<bool> _isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int CurrentLife => _life.Value; // 다른 클라이언트가 초기 UI 세팅 시 조회용

    // 완전히 패배(목숨 0 상태에서 한 번 더 사망)했음을 나타내는 네트워크 동기화 전용 값
    // PlayerData.Life는 UI 등 다른 소비자가 있어 0 밑으로 못 내려가므로, 네트워크 동기화 값(_life)에만 별도로 표시
    public const int EliminatedLife = -1;

    void Awake()
    {
        TryGetComponent(out _playerTank);
    }

    public override void OnNetworkSpawn()
    {
        // Lobby 씬이 언로드돼도 파괴되지 않도록 보호(각 컴퓨터에서 로컬로 각자 적용됨)
        DontDestroyOnLoad(gameObject);

        _playerTank.SetNetworkOwnership(IsOwner);
        _playerTank.SetNetworkOwner(this);
        _playerTank.SetTankColorByIndex((int)OwnerClientId); // 플레이어 구분 색상 적용
        _equipCamera.enabled = IsOwner; // 상점 장비칸 미리보기

        _playerTank.OnPlayerRespawn += HandleRemoteRespawn;
        // 목숨 UI:전원이 이 값의 변경을 받아서 NetworkGameManager로 릴레이
        _life.OnValueChanged += HandleLifeValueChanged;
        // 무적 판정:전원(서버 포함)이 이 값의 변경을 받아서 각자 로컬 TankModel에 반영
        _isInvincible.OnValueChanged += HandleInvincibleValueChanged;
        // 상점 열림/닫힘:로컬에서 다른 플레이어의 탱크 모델을 숨기고 복원하기 위해 구독
        NetworkGameManager.Instance.OnShopActiveChanged += HandleShopActiveChanged;

        // 로컬 소유일 때만 GameScene에 스폰 완료를 알림
        if (IsOwner)
        {
            NetworkGameManager.Instance.NotifyLocalPlayerSpawned(_playerTank);

            // 목숨이 바뀔 때마다 서버에 보고
            GameManager.Instance.PlayerData.OnLifeChanged += SetLifeValue;
            SetLifeValue(GameManager.Instance.PlayerData.Life); // 스폰 시점의 현재 값도 즉시 반영
        }
    }

    void SetLifeValue(int life)
    {
        _life.Value = life;
    }

    /// <summary>
    /// 목숨 UI 동기화
    /// </summary>
    void HandleLifeValueChanged(int previousValue, int currentValue)
    {
        NetworkGameManager.Instance.NotifyLifeChanged((int)OwnerClientId, currentValue);
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
    /// 상점 UI 열림/닫힘 반영 — 소유자 자신은 장비칸 미리보기에 계속 보여야 하므로 제외, 원격 관찰자만 로컬에서 탱크 모델을 숨기고 복원
    /// </summary>
    void HandleShopActiveChanged(bool active)
    {
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

        StageScene stageScene = NetworkGameManager.Instance.StageScene;
        if (stageScene == null) return;

        Vector3 spawnPos = stageScene.GetSpawnPoint((int)OwnerClientId);
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