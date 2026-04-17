using UnityEngine;

/// <summary>
/// 반투명 Bedrock과 모델을 교체하는 컨트롤러
/// </summary>
public class BedrockController : MonoBehaviour
{
    MeshRenderer _bedrockMesh;
    [SerializeField] GameObject _model; // 건물 모델

    private void Awake()
    {
        _bedrockMesh = GetComponent<MeshRenderer>();
    }

    public void ShowTransparent()
    {
        if (_bedrockMesh != null) _bedrockMesh.enabled = true;
        if (_model != null) _model.SetActive(false);
    }

    public void ShowModel()
    {
        if (_bedrockMesh != null) _bedrockMesh.enabled = false;
        if (_model != null) _model.SetActive(true);
    }
}
