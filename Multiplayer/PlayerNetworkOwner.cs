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

    // 목숨 UI 동기화용:Owner만 로컬에서 직접 쓸 수 있음(목숨은 서버 권위가 아니라 각자 로컬 판단 기반)
    NetworkVariable<int> _life = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // 무적 판정 동기화용:서버가 자기 쪽 TankModel 사본에도 반영해야 데미지 판정에서 실제로 걸러짐(연출과 별개)
    NetworkVariable<bool> _isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int CurrentLife => _life.Value; // 다른 클라이언트가 초기 UI 세팅 시 조회용

    void Awake()
    {
        TryGetComponent(out _playerTank);
    }

    public override void OnNetworkSpawn()
    {
        // Lobby 씬이 언로드돼도 파괴되지 않도록 보호(각 컴퓨터에서 로컬로 각자 적용됨)
        DontDestroyOnLoad(gameObject);

        //Debug.Log(
        //$"OnNetworkSpawn | {name} | " +
        //$"Owner:{IsOwner} | " +
        //$"Pos:{transform.position} | " +
        //$"Active:{gameObject.activeInHierarchy}");

        _playerTank.SetNetworkOwnership(IsOwner);
        _playerTank.SetNetworkOwner(this);
        _playerTank.SetTankColorByIndex((int)OwnerClientId); // 플레이어 구분 색상 적용
        
        _playerTank.OnPlayerRespawn += HandleRemoteRespawn;
        // 목숨 UI:전원이 이 값의 변경을 받아서 NetworkGameManager로 릴레이
        _life.OnValueChanged += HandleLifeValueChanged;
        // 무적 판정:전원(서버 포함)이 이 값의 변경을 받아서 각자 로컬 TankModel에 반영
        _isInvincible.OnValueChanged += HandleInvincibleValueChanged;

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
}