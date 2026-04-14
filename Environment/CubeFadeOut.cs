using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubeFadeOut : MonoBehaviour, IExplosionDamageable
{
    [SerializeField] Material _transparentMat; // 교체할 머터리얼
    float _fadeDuration; // 페이드 지속시간
    MeshRenderer _renderer;

    bool _isFading = false;
    float _timer = 0f;
    Color _color;

    // 주변 터질 때 영향 받을거
    Rigidbody _rigid;
    float _explosionForce = 10f; // 폭발력

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _rigid = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (_isFading)
        {
            _timer+= Time.deltaTime;
            _color.a = Mathf.Lerp(1f, 0f, _timer / _fadeDuration);
            _renderer.material.color = _color;
        }
    }

    /// <summary>
    /// 페이드 시작
    /// </summary>
    /// <param name="fadeDuration">페이드 지속시간</param>
    public void StartFade(float fadeDuration)
    {
        _isFading = true;
        _fadeDuration = fadeDuration;

        _renderer.material = _transparentMat; // 머터리얼 교체
        _color = _renderer.material.color;
        gameObject.layer = 0; // 레이어를 Default로 변경하여 충돌 감지 방지
    }

    public void TakeDamage(int damage)
    {
        Vector3 dir = (transform.position - transform.parent.position).normalized;
        _rigid.AddForce(dir * _explosionForce, ForceMode.Impulse);
    }
}