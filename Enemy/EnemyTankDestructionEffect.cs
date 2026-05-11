using UnityEngine;

/// <summary>
/// 적 탱크 파괴됐을 때 연출
/// 파괴된 모델로 교체 후 부품 다 날려버리기
/// </summary>
public class EnemyTankDestructionEffect : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] GameObject _model; // 원래 모델
    [SerializeField] GameObject _destroyedModel; // 파괴된 모델
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
        _model.SetActive(false); // 원래 모델 비활성화
        _destroyedModel.SetActive(true); // 파괴된 모델 활성화

        // 포탑 회전값 동기화
        _destroyedTurretTr.rotation = _turretTr.rotation;

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
        for(int i = 0; i < _destroyedTr.Length; i++)
        {
            _destroyedTr[i].localPosition = _orgPos[i];
            _destroyedTr[i].localRotation = _orgRot[i];
        }

        _destroyedModel.SetActive(false);
        _model.SetActive(true);
    }

    /// <summary>
    /// 모델 표시 설정
    /// </summary>
    public void SetModelVisible(bool visible)
    {
        _model.SetActive(visible);
    }
}