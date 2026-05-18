using UnityEngine;

/// <summary>
/// 풀 블록들 관리하는 그룹
/// </summary>
public class BushGroup : MonoBehaviour
{
    [SerializeField] Renderer[] _renderers;
    [SerializeField] Material _transparentMat;
    
    Material[] _originalMats;
    int _plyaerEnterCount = 0;

    void Awake()
    {
        _originalMats = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMats[i] = _renderers[i].material;
        }
    }

    public void OnPlayerEnter(TankBase tank)
    {
        tank.OnBushEnter(this); // 풀숲그룹 지정
        if (tank is PlayerTank == false) return;

        // 플레이어 탱크면 풀숲 반투명
        _plyaerEnterCount++;
        if (_plyaerEnterCount == 1)
        {
            SetTransparent(true);
        }
    }

    public void OnPlayerExit(TankBase tank)
    {
        tank.OnBushExit(this);
        if (tank is PlayerTank == false) return;

        // 반투명 해제
        _plyaerEnterCount--;
        if (_plyaerEnterCount <= 0)
        {
            _plyaerEnterCount = 0;
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
