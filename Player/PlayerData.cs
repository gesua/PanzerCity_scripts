using System;
using UnityEngine;

/// <summary>
/// 플레이어 데이터
/// </summary>
public class PlayerData : MonoBehaviour
{
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] int _gold;
    [SerializeField] int _life;
    [SerializeField] int _maxLife = 99;

    // 스테이지 진입 시점 저장용
    int _savedLife; // 목숨
    int _savedGold; // 골드

    public int Gold => _gold;
    public int Life => _life;
    public int MaxLife => _maxLife;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnLifeChanged;

    public void Initialize(int startGold, int startLife)
    {
        _gold = startGold;
        _life = startLife;
    }

    /// <summary>
    /// 골드 추가
    /// </summary>
    public void AddGold(int amount)
    {
        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }

    /// <summary>
    /// 골드 차감
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (_gold < amount) return false;
        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    /// <summary>
    /// 목숨 추가
    /// </summary>
    public void AddLife(int amount)
    {
        _life = Mathf.Min(_life + amount, _maxLife);
        OnLifeChanged?.Invoke(_life);
    }

    /// <summary>
    /// 목숨 차감
    /// </summary>
    public bool SpendLife(int amount)
    {
        if (_life <= 0) return false;
        _life -= amount;
        OnLifeChanged?.Invoke(_life);
        return true;
    }

    /// <summary>
    /// 스냅샷 저장 (스테이지 진입 시점)
    /// </summary>
    public void SaveSnapshot()
    {
        _savedLife = _life;
        _savedGold = _gold;
    }

    /// <summary>
    /// 스냅샷 복구 (재시작 시)
    /// </summary>
    public void RestoreSnapshot()
    {
        _life = _savedLife;
        OnLifeChanged?.Invoke(_life);

        _gold = _savedGold;
        OnGoldChanged?.Invoke(_gold);
    }
}
