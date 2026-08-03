using UnityEngine;

/// <summary>
/// 월드 UI의 방향을 카메라와 맞춰줌
/// </summary>
public class BillboardUI : MonoBehaviour
{
    Camera _camera;

    private void OnEnable()
    {
        if (_camera != null) return;
        _camera = Camera.main;
    }

    private void LateUpdate() // 마지막으로 UI가 움직이게
    {
        if (_camera == null) return;

        // 카메라의 정면 방향과 자신 게임오브젝트의 정면 방향을 일치시킨다
        transform.forward = _camera.transform.forward;
    }
}