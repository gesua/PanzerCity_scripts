using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 멀티플레이:관전 모드 진입 시 살아있는 아군 사이를 순환하며 카메라 대상을 전환
/// GameScene이 관전 진입/해제 시점과 Q/E 입력을 전달해주는 방식으로 동작(스스로 게임 상태를 판단하지 않음)
/// </summary>
public class SpectatorController : MonoBehaviour
{
    CameraTarget _cameraTarget;
    List<PlayerNetworkOwner> _aliveTargets = new(); // 현재 관전 가능한(목숨이 남은) 아군 목록
    int _currentIndex;

    /// <summary>
    /// 관전 모드 진입 — 로컬 플레이어를 제외한 살아있는 아군 목록을 구성하고 첫 대상으로 카메라 전환
    /// 목숨 변경 이벤트는 관전 중일 때만 들을 필요가 있어 여기서 구독(OnEnable이 아님:씬 로드 순서상 이 시점엔 NetworkGameManager.Instance가 아직 준비 안 됐을 수 있음)
    /// </summary>
    public void EnterSpectate(CameraTarget cameraTarget)
    {
        _cameraTarget = cameraTarget;

        NetworkGameManager.Instance.OnPlayerLifeChanged += HandlePlayerLifeChanged;

        RefreshAliveTargets();

        _currentIndex = 0;
        ApplyCurrentTarget(true);
    }

    /// <summary>
    /// 관전 모드 해제 — 내부 목록 정리(카메라 타겟 복구는 GameScene이 직접 처리)
    /// </summary>
    public void ExitSpectate()
    {
        // Instance가 씬 전환 등으로 먼저 파괴됐을 수 있어 방어적으로 체크
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnPlayerLifeChanged -= HandlePlayerLifeChanged;
        }

        _cameraTarget = null;
        _aliveTargets.Clear();
    }

    /// <summary>
    /// 이전 관전 대상으로 전환(Q키, GameScene이 호출)
    /// </summary>
    public void SpectatePrev()
    {
        if (_aliveTargets.Count == 0) return;

        _currentIndex = (_currentIndex - 1 + _aliveTargets.Count) % _aliveTargets.Count;
        ApplyCurrentTarget(true);
    }

    /// <summary>
    /// 다음 관전 대상으로 전환(E키, GameScene이 호출)
    /// </summary>
    public void SpectateNext()
    {
        if (_aliveTargets.Count == 0) return;

        _currentIndex = (_currentIndex + 1) % _aliveTargets.Count;
        ApplyCurrentTarget(true);
    }

    /// <summary>
    /// 목숨 변경 수신 — 관전 중인 대상이 죽으면 목록을 갱신하고 다음 생존자로 자동 스킵
    /// 갱신 후에도 지금 보고 있던 대상이 살아있다면 그 대상을 계속 보여줌(인덱스가 아닌 대상 기준으로 위치를 다시 찾음)
    /// 각도 리셋은 실제로 대상이 바뀐 경우(보던 대상이 죽어서 자동 전환된 경우)에만 적용 — 그 외엔 자유시점 유지
    /// </summary>
    void HandlePlayerLifeChanged(int playerIndex, int life)
    {
        if (_cameraTarget == null) return; // 관전 중이 아니면 무시

        PlayerNetworkOwner currentTarget = (_aliveTargets.Count > 0) ? _aliveTargets[_currentIndex] : null;

        RefreshAliveTargets();

        if (_aliveTargets.Count == 0) return; // 전원 사망 케이스는 GameOverUI 쪽에서 별도 처리됨

        int foundIndex = (currentTarget != null) ? _aliveTargets.IndexOf(currentTarget) : -1;
        bool targetChanged = (foundIndex < 0); // 보던 대상을 목록에서 못 찾았으면(사망) 전환된 것

        _currentIndex = (foundIndex >= 0) ? foundIndex : 0; // 보던 대상이 사라졌으면(사망) 목록의 첫 생존자로

        ApplyCurrentTarget(targetChanged);
    }

    /// <summary>
    /// 접속한 클라이언트 중 로컬 플레이어를 제외한 생존자 목록을 다시 구성
    /// </summary>
    void RefreshAliveTargets()
    {
        _aliveTargets.Clear();

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            if (client.PlayerObject.TryGetComponent(out PlayerNetworkOwner networkOwner) == false) continue;
            if (networkOwner.IsOwner) continue; // 로컬 플레이어(관전 주체 본인) 제외
            if (networkOwner.CurrentLife <= 0) continue; // 목숨이 없으면 관전 대상 아님

            _aliveTargets.Add(networkOwner);
        }
    }

    /// <summary>
    /// 현재 인덱스의 대상으로 카메라 전환
    /// </summary>
    /// <param name="resetAngle">true면 관전 전용 고정 각도로 리셋(대상이 실제로 바뀔 때만 true로 호출해야 함)</param>
    void ApplyCurrentTarget(bool resetAngle)
    {
        if (_aliveTargets.Count == 0) return;
        if (_aliveTargets[_currentIndex].TryGetComponent(out PlayerTank playerTank) == false) return;

        _cameraTarget.SetTarget(playerTank.transform);

        if (resetAngle)
        {
            _cameraTarget.ResetToSpectateAngle();
        }
    }
}