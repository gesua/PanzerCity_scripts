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
        // 외곽벽 모델 복구
        if (_changedBedrocks.Count > 0) RestoreBedrocks();

        Vector3 dir = _player.position - transform.position;
        float distance = dir.magnitude;

        Ray ray = new Ray(transform.position, dir.normalized);
        float radius = 0.5f;
        RaycastHit[] hits = Physics.SphereCastAll(ray, radius, distance, _obstacleLayer);
        //RaycastHit[] hits = Physics.RaycastAll(ray, distance, _obstacleLayer);

        foreach (RaycastHit hit in hits)
        {
            BedrockController block = hit.collider.GetComponent<BedrockController>();
            if (block != null)
            {
                block.ShowTransparent();
                if (!_changedBedrocks.Contains(block)) _changedBedrocks.Add(block);
            }
        }
    }

    void RestoreBedrocks()
    {
        foreach (BedrockController bedrock in _changedBedrocks)
        {
            if (bedrock != null) bedrock.ShowModel();
        }
        _changedBedrocks.Clear();
    }
}