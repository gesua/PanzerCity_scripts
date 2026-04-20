using UnityEngine;

/// <summary>
/// 탱크 종류별 스탯 데이터테이블
/// </summary>
[System.Serializable]
public class TankData
{
    public int TankID;
    public string TankType;
    public int MaxHP;
    public float MoveSpeed_Fwd;
    public float MoveSpeed_Bwd;
    public float RotateSpeed;
    public float TurretRotateSpeed;
    public float MinAttackTime;
    public float MaxAttackTime;
    public int ShellDamage;
    public float ShellSpeed;
    public float ExplosionRadius;
}
