using UnityEngine;

/// <summary>
/// 전차장 캐릭터 제어
/// 표정, 모션, 눈 깜빡임
/// </summary>
public class CommanderController : MonoBehaviour
{[Header("----- 컴포넌트 -----")]
    [SerializeField] Animator _animator;
    [SerializeField] SkinnedMeshRenderer _face;

    [Header("----- 눈 깜빡임 -----")]
    [SerializeField] float _eyeCloseWeight = 100f;
    [SerializeField] float _blinkSpeed = 0.1f;
    [SerializeField] float _blinkSpanMin = 2f;
    [SerializeField] float _blinkSpanMax = 5f;

    //[Header("----- BlendShape 인덱스 -----")]
    int _eyeBlkIndex = 0;
    int _eyeSadIndex = 7;
    int _blwSadIndex = 8;
    int _mthDropIndex = 18;

    // 눈 깜빡임 관련
    bool _isEyeClose;
    bool _isBlinkStart;
    float _nextBlinkTime;
    float _blinkTimer;

    void Start()
    {
        SetNextBlink();
    }

    void LateUpdate()
    {
        EyeBlink();
    }
    void EyeBlink()
    {
        if (_isBlinkStart) // 눈 깜빡이기
        {
            _blinkTimer += _isEyeClose ? -Time.deltaTime : Time.deltaTime;

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
    /// 슬픈 표정
    /// </summary>
    public void SetSadFace()
    {
        _face.SetBlendShapeWeight(_eyeSadIndex, 100f);
        _face.SetBlendShapeWeight(_blwSadIndex, 100f);
        _face.SetBlendShapeWeight(_mthDropIndex, 100f);
    }

    /// <summary>
    /// 표정 초기화
    /// </summary>
    public void ResetFace()
    {
        _face.SetBlendShapeWeight(_eyeSadIndex, 0f);
        _face.SetBlendShapeWeight(_blwSadIndex, 0f);
        _face.SetBlendShapeWeight(_mthDropIndex, 0f);
    }

    /// <summary>
    /// 카메라 바라보기
    /// </summary>
    public void LookAtCamera()
    {
        Vector3 dirToCamera = Camera.main.transform.position - transform.position;
        dirToCamera.y = 0f;
        transform.rotation = Quaternion.LookRotation(-dirToCamera);
    }
}
