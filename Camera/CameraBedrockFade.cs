using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어와 카메라 사이 오브젝트 투명화
/// </summary>
public class CameraObstacleFade : MonoBehaviour
{
    public Transform player;
    public LayerMask obstacleLayer; // 투명화 시킬 레이어(Material - SurfaceType - Transparent)

    private List<MeshRenderer> fadedObjects = new List<MeshRenderer>(); // 투명화된 객체

    void Update()
    {
        // 투명화된 객체 복구
        if (fadedObjects.Count > 0) RestoreObjects();

        Vector3 dir = player.position - transform.position;
        float distance = dir.magnitude;

        Ray ray = new Ray(transform.position, dir.normalized);
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, obstacleLayer);

        foreach (RaycastHit hit in hits)
        {
            MeshRenderer rend = hit.collider.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                FadeObject(rend);
                fadedObjects.Add(rend);
            }
        }
    }

    /// <summary>
    /// 객체 투명화
    /// </summary>
    /// <param name="rend"></param>
    void FadeObject(MeshRenderer rend)
    {
        foreach (Material mat in rend.materials)
        {
            Color c = mat.color;
            c.a = 0.1f;
            mat.color = c;
        }
    }

    /// <summary>
    /// 투명화된 객체 복구
    /// </summary>
    void RestoreObjects()
    {
        foreach (MeshRenderer rend in fadedObjects)
        {
            foreach (Material mat in rend.materials)
            {
                Color c = mat.color;
                c.a = 1f;
                mat.color = c;
            }
        }
        fadedObjects.Clear();
    }
}