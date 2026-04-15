using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FragmentCube : MonoBehaviour, IExplosionDamageable
{
    [SerializeField] Material _transparentMat; // 교체할 머터리얼
    float _fadeDuration; // 페이드 지속시간
    MeshRenderer _renderer;

    bool _isFading = false;
    float _timer = 0f;
    Color _color;

    // 주변 터질 때 영향 받을거
    Rigidbody _rigid;

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
            _color.a = Mathf.Lerp(0.5f, 0f, _timer / _fadeDuration);
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

    public void TakeDamage(int damage, float explosionForce, Vector3 pos)
    {
        //_rigid.AddForce(transform.position * explosionForce, ForceMode.Impulse);
        //_rigid.AddExplosionForce(explosionForce, transform.position, explosionForce);
        _rigid.AddExplosionForce(explosionForce, pos, explosionForce * 100f);
    }
}