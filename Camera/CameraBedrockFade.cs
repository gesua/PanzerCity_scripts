using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어와 카메라 사이 오브젝트 투명화
/// 알파값을 바꾸는게 아니라 준비한 머터리얼과 교체
/// Transparent로 배치해 놓으면 그림자 같은 그래픽이 이상하게 보임
/// Opaque로 배치해 놓고, 투명화 할 때만 Transparent 머터리얼로 교체
/// </summary>
public class CameraObstacleFade : MonoBehaviour
{
    [SerializeField] Transform _player;
    [SerializeField] LayerMask _obstacleLayer; // 투명화 시킬 레이어
    [SerializeField] Material _transparentMat; // 교체할 머터리얼

    private List<MeshRenderer> _fadedObjects = new List<MeshRenderer>(); // 투명화된 객체
    private Dictionary<MeshRenderer, Material[]> _originalMaterials = new Dictionary<MeshRenderer, Material[]>(); // 원본 머터리얼 보관


    void Update()
    {
        // 이전에 교체된 객체 복구
        if (_fadedObjects.Count > 0) RestoreObjects();

        Vector3 dir = _player.position - transform.position;
        float distance = dir.magnitude;

        Ray ray = new Ray(transform.position, dir.normalized);
        RaycastHit[] hits = Physics.RaycastAll(ray, distance, _obstacleLayer);

        foreach (RaycastHit hit in hits)
        {
            MeshRenderer rend = hit.collider.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                FadeObject(rend);
                if (!_fadedObjects.Contains(rend)) _fadedObjects.Add(rend);
            }
        }
    }

    /// <summary>
    /// 객체 투명화
    /// </summary>
    void FadeObject(MeshRenderer rend)
    {
        if (rend == null) return;
        if (_originalMaterials.ContainsKey(rend)) return; // 이미 교체된 경우 중복 처리 방지

        // 원본 머터리얼 저장
        Material[] origMats = rend.materials;
        _originalMaterials[rend] = origMats;

        // 슬롯 수에 맞추어 머터리얼 교체
        int slotCount = origMats.Length;
        Material[] replacement = new Material[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            replacement[i] = _transparentMat;
        }
        rend.materials = replacement;
    }

    /// <summary>
    /// 투명화된 객체 복구
    /// </summary>
    void RestoreObjects()
    {
        foreach (MeshRenderer rend in _fadedObjects)
        {
            if (rend == null) continue;
            if (_originalMaterials.TryGetValue(rend, out Material[] origMats))
            {
                rend.materials = origMats; // 원본 머터리얼로 복구
                _originalMaterials.Remove(rend);
            }
        }
        _fadedObjects.Clear();
    }
}