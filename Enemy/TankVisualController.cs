using UnityEngine;

/// <summary>
/// 적 탱크 모델 관련
/// 적 탱크 파괴됐을 때 연출
/// 파괴된 모델로 교체 후 부품 다 날려버리기
/// </summary>
public class TankVisualController : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] MeshRenderer[] _normalVisualRenderers; // 플레이 모델 렌더러
    [SerializeField] GameObject _destroyedModel; // 파괴된 모델
    [SerializeField] GameObject[] _hitZone; // 꺼질 히트존
    [SerializeField] Transform _turretTr; // 원래 포탑
    [SerializeField] Transform _destroyedTurretTr; // 파괴된 포탑
    [SerializeField] Transform[] _destroyedTr; // 파괴된 모델 트랜스폼
    [SerializeField] Rigidbody[] _destroyedRigids; // 파괴된 모델의 rigidbody들

    Vector3[] _orgPos; // 원래 위치값
    Quaternion[] _orgRot; // 원래 회전값
    float _explosionForce = 5f; // 폭발력

    private void Awake()
    {
        _orgPos = new Vector3[_destroyedTr.Length];
        _orgRot = new Quaternion[_destroyedTr.Length];
        for (int i = 0; i < _destroyedTr.Length; i++)
        {
            _orgPos[i] = _destroyedTr[i].localPosition;
            _orgRot[i] = _destroyedTr[i].localRotation;
        }
    }

    /// <summary>
    /// 파괴 효과 재생
    /// </summary>
    public void Play()
    {
        SetModelVisible(false); // 원래 모델 비활성화
        foreach (GameObject hitZone in _hitZone) hitZone.SetActive(false); // 사망 시에만 히트존 비활성화
        _destroyedModel.SetActive(true); // 파괴된 모델 활성화

        // 포탑 회전값 동기화
        if (_turretTr != null) _destroyedTurretTr.rotation = _turretTr.rotation;

        // 자식 rigidbody 전부 날리기
        Rigidbody[] parts = _destroyedModel.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody part in parts)
        {
            part.AddExplosionForce(_explosionForce, transform.position, 1000f, 0f, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 파괴된 모델 리셋
    /// </summary>
    public void ResetState()
    {
        for (int i = 0; i < _destroyedTr.Length; i++)
        {
            _destroyedTr[i].localPosition = _orgPos[i];
            _destroyedTr[i].localRotation = _orgRot[i];
        }

        _destroyedModel.SetActive(false);
        SetModelVisible(true);
        foreach (GameObject hitZone in _hitZone) hitZone.SetActive(true); // 재사용 시 히트존 복구
    }

    /// <summary>
    /// 모델 표시 설정
    /// </summary>
    public void SetModelVisible(bool visible)
    {
        // 이미 파괴 연출이 재생됐으면 이후의 모든 호출(EMP 등)을 방향 상관없이 무시
        if (_destroyedModel != null && _destroyedModel.activeSelf) return;

        // 렌더러만 토글
        foreach (MeshRenderer renderer in _normalVisualRenderers)
        {
            renderer.enabled = visible;
        }
    }
}