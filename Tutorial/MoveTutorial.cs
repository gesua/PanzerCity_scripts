using UnityEngine;

public class MoveTutorial : TutorialStep
{
    [SerializeField] Transform _arrow;
    Camera _camera;

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
        int playerLayer = LayerMask.NameToLayer("Player");

        if (other.gameObject.layer == playerLayer)
        {
            Debug.Log("플레이어가 튜토리얼 트리거에 들어옴!");

            // 중복 실행 방지를 위해 트리거 비활성화
            gameObject.SetActive(false);

            Complete();
        }
    }
}