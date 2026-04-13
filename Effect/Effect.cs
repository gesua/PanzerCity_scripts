using UnityEngine;

/// <summary>
/// VFX(비주얼 이펙트)를 담당
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
    /// 이펙트를 재생하는 함수
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