using UnityEngine;

/// <summary>
/// 3D 위치에서 재생되고, 재생이 끝나면 자동으로 Pool에 반환되는 효과음 전용 컴포넌트
/// 포탄 터지는 소리처럼 위치가 매번 다르고 동시에 여러 개 재생될 수 있는 SFX에 사용
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PooledSfx : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    float _timer;
    bool _isPlaying;

    /// <summary>
    /// 지정 위치에서 클립 재생 시작
    /// </summary>
    public void Play(AudioClip clip, Vector3 position)
    {
        transform.position = position;

        _audioSource.clip = clip;
        _audioSource.Play();

        _timer = 0f;
        _isPlaying = true;
    }

    void Update()
    {
        if (_isPlaying == false) return;

        _timer += Time.deltaTime;
        if (_timer >= _audioSource.clip.length)
        {
            _isPlaying = false;
            gameObject.DestroyOrReturnToPool();
        }
    }
}
