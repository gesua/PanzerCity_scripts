using UnityEngine;

/// <summary>
/// 풀 블록들 관리하는 그룹
/// </summary>
public class BushGroup : MonoBehaviour
{
    [SerializeField] Renderer[] _renderers;
    [SerializeField] Material _transparentMat;
    Material[] _originalMats;

    void Awake()
    {
        _originalMats = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMats[i] = _renderers[i].material;
        }
    }
    int _enterCount = 0;

    public void OnPlayerEnter()
    {
        _enterCount++;
        if (_enterCount == 1) SetTransparent(true);
    }

    public void OnPlayerExit()
    {
        _enterCount--;
        if (_enterCount <= 0)
        {
            _enterCount = 0;
            SetTransparent(false);
        }
    }

    /// <summary>
    /// 투명 머터리얼로 교체
    /// </summary>
    void SetTransparent(bool transparent)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].material = transparent ? _transparentMat : _originalMats[i];
        }
    }
}
