using UnityEngine;

/// <summary>
/// VFX(비주얼 이펙트)를 담당
/// 이펙트에 붙여놓으면 됨
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class Effect : MonoBehaviour
{
    ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// 이펙트 재생
    /// </summary>
    public void Play()
    {
        _ps.Play(true);
    }

    private void OnParticleSystemStopped()
    {
        gameObject.DestroyOrReturnToPool();
    }
}