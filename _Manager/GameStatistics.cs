using UnityEngine;

/// <summary>
/// 게임 전체 진행 통계
/// PlayerData/인벤토리와 달리 재시작해도 초기화되지 않고 계속 누적됨
/// </summary>
public class GameStatistics : MonoBehaviour
{
    int _totalGoldEarned; // 획득한 총 골드
    int _itemsUsed;       // 사용한 아이템 수
    int _shellKills;      // 포탄으로 격파한 적 수(아이템 격파 제외)
    int _deathCount;      // 죽은 횟수

    float _startTime; // 플레이 시작 시각
    bool _hasStarted;  // 시작 기록 여부(최초 1회만 기록)

    public int TotalGoldEarned => _totalGoldEarned;
    public int ItemsUsed => _itemsUsed;
    public int ShellKills => _shellKills;
    public int DeathCount => _deathCount;

    /// <summary>
    /// 플레이 시간 시작 기록 (최초 스테이지 진입 시 1회만 동작)
    /// </summary>
    public void MarkGameStart()
    {
        if (_hasStarted) return;
        _hasStarted = true;
        _startTime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// 플레이 시간(초) 반환 - 일시정지 시간도 포함
    /// </summary>
    public float GetPlayTime()
    {
        return Time.realtimeSinceStartup - _startTime;
    }

    /// <summary>
    /// 골드 획득 기록
    /// </summary>
    public void AddGoldEarned(int amount)
    {
        _totalGoldEarned += amount;
    }

    /// <summary>
    /// 아이템 사용 기록
    /// </summary>
    public void AddItemUsed()
    {
        _itemsUsed++;
    }

    /// <summary>
    /// 적 격파 기록(아이템으로 격파한 경우는 제외)
    /// </summary>
    /// <param name="isItemKill">아이템으로 격파했는지</param>
    public void AddKill(bool isItemKill)
    {
        if (isItemKill) return;
        _shellKills++;
    }

    /// <summary>
    /// 사망 횟수 기록
    /// </summary>
    public void AddDeath()
    {
        _deathCount++;
    }
}
