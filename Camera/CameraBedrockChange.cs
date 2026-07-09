using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어와 카메라 사이 오브젝트 교체
/// 맵 테두리에 있는 모델과 빨간색 투명한 벽 교체
/// 카메라가 외곽벽으로 나갈 때 카메라가 모델에 가려지는 현상 방지
/// </summary>
public class CameraBedrockChange : MonoBehaviour
{
    [SerializeField] Transform _player;
    [SerializeField] LayerMask _obstacleLayer;

    private List<BedrockController> _changedBedrocks = new List<BedrockController>();

    void Update()
    {
        if (_player == null) return;

        // 외곽벽 모델 복구
        if (_changedBedrocks.Count > 0) RestoreBedrocks();

        // 플레이어에서 카메라로 향하는 Raycast
        Vector3 dir = transform.position - _player.position;
        float distance = dir.magnitude;

        Ray ray = new Ray(_player.position, dir.normalized);
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, _obstacleLayer); // RaycastAll로 안 해도 되지만 혹시 변경될 수 있으니 유지

        // 외곽벽 모델 교체
        foreach (RaycastHit hit in hits)
        {
            if(hit.collider.TryGetComponent(out BedrockController block))
            {
                block.ShowTransparent();
                if (_changedBedrocks.Contains(block) == false) _changedBedrocks.Add(block);
            }
        }
    }

    /// <summary>
    /// 외곽벽 모델 복구
    /// </summary>
    void RestoreBedrocks()
    {
        foreach (BedrockController bedrock in _changedBedrocks)
        {
            if (bedrock != null) bedrock.ShowModel();
        }
        _changedBedrocks.Clear();
    }

    /// <summary>
    /// 로컬 플레이어 주입(멀티플레이 전용)
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;
    }
}