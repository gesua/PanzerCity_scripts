using UnityEngine;

/// <summary>
/// 전차장 캐릭터 제어
/// 표정, 모션, 눈 깜빡임
/// </summary>
public class CommanderController : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Transform _commanderRoot;
    [SerializeField] Animator _animator;
    [SerializeField] CommanderLookAt _lookAt;
    [SerializeField] SkinnedMeshRenderer _face;
    [SerializeField] SkinnedMeshRenderer[] _body;

    [Header("----- 눈 깜빡임 -----")]
    [SerializeField] float _eyeCloseWeight = 100f;
    [SerializeField] float _blinkSpeed = 0.1f;
    [SerializeField] float _blinkSpanMin = 2f;
    [SerializeField] float _blinkSpanMax = 5f;

    //[Header("----- BlendShape 인덱스 -----")]
    int _eyeBlkIndex = 0;
    int _faceLayer = 1;

    // 눈 깜빡임 관련
    bool _isEyeClose;
    bool _isBlinkStart;
    float _nextBlinkTime;
    float _blinkTimer;

    bool _isDead; // 사망 상태시 항상 슬픈 표정

    public Transform CommanderRoot => _commanderRoot;

    void Start()
    {
        SetNextBlink();
    }

    void LateUpdate()
    {
        EyeBlink();
    }

    /// <summary>
    /// 눈 깜빡임
    /// </summary>
    void EyeBlink()
    {
        if (_isBlinkStart) // 눈 깜빡이기
        {
            _blinkTimer += (_isEyeClose) ? -Time.deltaTime : Time.deltaTime;

            if (_blinkTimer >= _blinkSpeed) // 감는중
            {
                _blinkTimer = _blinkSpeed;
                _isEyeClose = true;
            }
            else if (_blinkTimer <= 0) // 뜨는중
            {
                _blinkTimer = 0;
                _isEyeClose = false;
                _isBlinkStart = false;
            }

            // 스킨 접었다폈다
            _face.SetBlendShapeWeight(_eyeBlkIndex, _eyeCloseWeight * (_blinkTimer / _blinkSpeed));
        }
        else // 기다리기
        {
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= _nextBlinkTime)
            {
                _blinkTimer = 0;
                _isBlinkStart = true;

                SetNextBlink();
            }
        }
    }

    /// <summary>
    /// 다음 깜박임 랜덤 설정
    /// </summary>
    void SetNextBlink()
    {
        // 20% 확률로 빠른 연속 깜빡임
        if (Random.value < 0.2f)
        {
            _nextBlinkTime = Random.Range(0.2f, 0.5f);
        }
        else
        {
            _nextBlinkTime = Random.Range(_blinkSpanMin, _blinkSpanMax);
        }
    }

    /// <summary>
    /// 전차장 표시 여부 설정
    /// </summary>
    public void SetVisible(bool visible)
    {
        _face.enabled = visible;
        foreach (SkinnedMeshRenderer renderer in _body)
        {
            renderer.enabled = visible;
        }
    }

    /// <summary>
    /// 사망 상태로 변경
    /// </summary>
    public void SetDead()
    {
        _isDead = true;
        _animator.ResetTrigger("OnHit"); // 밀려있는 피격 트리거 제거(사망 후 표정 덮어쓰기 방지)
    }

    /// <summary>
    /// 슬픈 표정
    /// </summary>
    public void SetSadFace()
    {
        if (_animator.isActiveAndEnabled)
        {
            _animator.CrossFade("sad", 0.1f, _faceLayer);
        }
    }

    /// <summary>
    /// 초기화
    /// </summary>
    public void Reset()
    {
        _isDead = false;
        SetLookAtCam(false);
        _commanderRoot.localRotation = Quaternion.identity; // 회전값 돌아가있는거 초기화

        if (_animator.isActiveAndEnabled)
        {
            _animator.CrossFade("default", 0.1f, _faceLayer);
        }
    }

    public void SetLookAtCam(bool active)
    {
        _lookAt.SetLookAt(active);
    }

    /// <summary>
    /// 피격 모션
    /// </summary>
    public void PlayHitMotion()
    {
        _animator.SetTrigger("OnHit");
    }

    /// <summary>
    /// 표정 교체 이벤트
    /// </summary>
    public void OnCallChangeFace(string faceName)
    {
        if (_isDead) return;
        _animator.CrossFade(faceName, 0.1f, _faceLayer);
    }
}