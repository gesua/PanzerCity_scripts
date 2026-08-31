using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VFX(비주얼 이펙트)를 담당
/// 단발성 이펙트에 붙여놓으면 됨
/// Pool을 사용하는 이펙트들
/// </summary>
public class Effect : MonoBehaviour
{
    ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponentInChildren<ParticleSystem>();
    }

    public void Play()
    {
        _ps.Play(true);
    }

    public void Stop()
    {
        _ps.Stop();
    }

    public void StopImmediate()
    {
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnParticleSystemStopped()
    {
        gameObject.DestroyOrReturnToPool();
    }
}