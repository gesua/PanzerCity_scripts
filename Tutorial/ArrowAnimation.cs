using System.Collections;
using UnityEngine;

/// <summary>
/// 화살표 위아래 이동 + 바닥 도달 시 바운스 애니메이션
/// </summary>
public class ArrowAnimation : MonoBehaviour
{
    [SerializeField] AnimationCurve _yScaleCurve;  // Y축 스케일 곡선 (눌림/튀어오름)
    [SerializeField] AnimationCurve _xzScaleCurve; // XZ축 스케일 곡선 (옆으로 퍼짐)
    [SerializeField] AnimationCurve _yMoveCurve;   // Y축 이동 곡선 (위→아래)
    [SerializeField] float _animDuration = 0.6f;   // 바운스 애니메이션 재생 시간
    [SerializeField] float _interval = 1.0f;       // 위아래 이동 반복 간격
    [SerializeField] float _moveDistance = 1f;     // 위아래 이동 거리
    [SerializeField] float _bounceDelay = 0.35f;   // 애니메이션 딜레이

    Camera _camera;

    Vector3 _originalScale;
    Vector3 _startLocalPos;

    private void Reset()
    {
        // Y축 스케일: 눌림 → 튀어오름 → 복귀
        _yScaleCurve = new AnimationCurve(
            new Keyframe(0.0f, 1.0f),
            new Keyframe(0.2f, 0.6f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(1.0f, 1.0f)
        );

        // XZ축 스케일: Y의 반대 (눌릴 때 퍼짐)
        _xzScaleCurve = new AnimationCurve(
            new Keyframe(0.0f, 1.0f),
            new Keyframe(0.2f, 1.3f),
            new Keyframe(0.5f, 0.9f),
            new Keyframe(1.0f, 1.0f)
        );

        // Y 이동: 천천히 내려왔다가 올라감
        _yMoveCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.0f),
            new Keyframe(1.0f, 1.0f)
        );
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    private void OnEnable()
    {
        _originalScale = transform.localScale;
        _startLocalPos = transform.localPosition;
        StartCoroutine(BounceRoutine());
    }

    private void LateUpdate()
    {
        // 화살표 카메라 바라보게
        Vector3 camForward = _camera.transform.forward;
        camForward.y = 0f;      // y 성분 제거
        camForward.Normalize(); // 정규화

        transform.forward = -camForward; // 180도 반전
    }

    /// <summary>
    /// 위아래 이동 + 바닥 도달 시 바운스 반복
    /// </summary>
    IEnumerator BounceRoutine()
    {
        float halfInterval = _interval * 0.5f;

        while (true)
        {
            // 아래로 이동과 바운스를 동시에 실행
            StartCoroutine(ScaleArrow(Mathf.Max(0f, _bounceDelay)));
            yield return MoveArrow(0f, 1f, halfInterval);

            // 위로 이동
            yield return MoveArrow(1f, 0f, halfInterval);
        }
    }

    /// <summary>
    /// Y축 이동 (from/to: 0 = 시작 위치, 1 = 아래)
    /// </summary>
    IEnumerator MoveArrow(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float lerped = Mathf.Lerp(from, to, _yMoveCurve.Evaluate(t));
            transform.localPosition = _startLocalPos + Vector3.down * (_moveDistance * lerped);
            yield return null;
        }
    }

    /// <summary>
    /// 바운스 스케일 애니메이션
    /// </summary>
    IEnumerator ScaleArrow(float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < _animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _animDuration);

            float y = _yScaleCurve.Evaluate(t);
            float xz = _xzScaleCurve.Evaluate(t);

            transform.localScale = new Vector3(
                _originalScale.x * xz,
                _originalScale.y * y,
                _originalScale.z * xz
            );

            yield return null;
        }

        transform.localScale = _originalScale;
    }
}
