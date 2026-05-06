using UnityEngine;

/// <summary>
/// 눈 자동 깜빡이기
/// </summary>
public class AutoBlink : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] SkinnedMeshRenderer _face; // 움직일 얼굴
    [SerializeField] Animator _animator;

    // 테스트중
    //string _blinkStateName = "eye_close";
    //int _faceLayerIndex = 1; // Face 레이어 인덱스

    const int EYE_INDEX = 0;
    [Header("----- 런타임 데이터 -----")]
    [SerializeField] float _eyeCloseWight = 100f;
    //[SerializeField] float _blinkSpan = 3f;     // 눈 깜빡임 간격
    [SerializeField] float _blinkSpeed = 0.1f;  // 눈 깜빡임 속도

    bool _isEyeClose;   // 눈 감았는지
    bool _isStart;      // 깜빡임 시작

    // 깜빡임 랜덤
    float _blinkSpanMin = 2f;
    float _blinkSpanMax = 5f;
    float _nextBlinkTime;
    float _timer;
    void Start()
    {
        SetNextBlink();
    }

    private void LateUpdate()
    {
        // 애니메이터 적용 후 덮어쓰기
        EyeBlink();
    }

    void EyeBlink()
    {
        if (_isStart) // 눈 깜빡이기
        {
            _timer += _isEyeClose ? -Time.deltaTime : Time.deltaTime;

            if (_timer >= _blinkSpeed) // 감는중
            {
                _timer = _blinkSpeed;
                _isEyeClose = true;
            }
            else if (_timer <= 0) // 뜨는중
            {
                _timer = 0;
                _isEyeClose = false;
                _isStart = false;
            }

            // 스킨 접었다폈다
            _face.SetBlendShapeWeight(EYE_INDEX, _eyeCloseWight * (_timer / _blinkSpeed));
        }
        else // 기다리기
        {
            _timer += Time.deltaTime;
            if (_timer >= _nextBlinkTime)
            {
                _timer = 0;
                _isStart = true;

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
}