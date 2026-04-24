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

    public void Play()
    {
        _ps.Play(true);
    }

    public void Stop()
    {
        _ps.Stop();
    }

    private void OnParticleSystemStopped()
    {
        gameObject.DestroyOrReturnToPool();
    }
}