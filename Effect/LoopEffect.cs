using UnityEngine;

/// <summary>
/// 지속적인 이펙트
/// Pool을 사용하지 않는 이펙트
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class LoopEffect : MonoBehaviour
{
    ParticleSystem _ps;

    // 유니티의 isPlaying 대신 직접 재생 상태를 추적(따닥 값이 들어오면 stop 해놔도 isPlaying이 true라 씹힘)
    bool _isActive = false;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    public void Play()
    {
        if (_isActive) return;

        _isActive = true;
        _ps.Play();
    }

    public void Stop()
    {
        if (_isActive == false) return;

        _isActive = false;
        _ps.Stop();
    }
}