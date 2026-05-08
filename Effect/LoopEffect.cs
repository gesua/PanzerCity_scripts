using UnityEngine;

/// <summary>
/// 지속적인 이펙트
/// Pool을 사용하지 않는 이펙트
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class LoopEffect : MonoBehaviour
{
    ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    public void Play()
    {
        if (_ps.isPlaying) return;
        _ps.Play();
    }

    public void Stop()
    {
        if (_ps.isPlaying == false) return;
        _ps.Stop();
    }
}