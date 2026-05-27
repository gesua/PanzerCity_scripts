using UnityEngine;

/// <summary>
/// 벽에 들어있는 큐브 조각
/// 벽 하나에 큐브 4개 들어있음
/// Wall이 파괴될 때 굴러다니며 투명해짐
/// 이미 투명해진 큐브들도 사라지기 전까지 포탄에 영향을 받음
/// </summary>
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

    // 기지벽 복구시킬 용도
    Material _originalMat;
    Vector3 _originalPos;
    Quaternion _originalRot;

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

    public void TakeHit(HitData hitData, float explosionForce, Vector3 pos)
    {
        _rigid.AddExplosionForce(explosionForce, pos, 1000f, 0f, ForceMode.Impulse);
    }

    /// <summary>
    /// 원래 값 세팅
    /// </summary>
    public void SetOriginal()
    {
        _originalPos = transform.localPosition;
        _originalRot = transform.localRotation;
        _originalMat = _renderer.material;
    }

    public void Reset()
    {
        _isFading = false;
        _rigid.isKinematic = true;
        _rigid.linearVelocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;
        transform.localPosition = _originalPos;
        transform.localRotation = _originalRot;
        
        // 알파값 복구
        if (_color != null) _color.a = 1;
        _renderer.material.color = _color;
        _renderer.material = _originalMat;

        gameObject.SetActive(true);
    }
}