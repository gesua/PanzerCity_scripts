using UnityEngine;

public class MoveTutorial : TutorialStep
{
    [SerializeField] Transform _arrow;
    Camera _camera;
    bool _completed;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        // 화살표 카메라 바라보게
        Vector3 camForward = _camera.transform.forward;
        camForward.y = 0f;       // y 성분 제거
        camForward.Normalize();  // 정규화

        _arrow.forward = -camForward; // 180도 반전
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_completed) return;

        if (other.CompareTag("PlayerHitZone"))
        {
            _completed = true;

            // 비활성화
            gameObject.SetActive(false);

            Complete();
        }
    }
}