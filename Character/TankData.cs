using UnityEngine;

/// <summary>
/// 데이터테이블에서 탱크의 데이터를 가져와 저장하는 클래스
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
