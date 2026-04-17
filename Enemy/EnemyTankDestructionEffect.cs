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
        // 원래 모델 비활성화
        _model.SetActive(false);
        // 파괴된 모델 활성화
        _destroyedModel.SetActive(true);

        // 자식 rigidbody 전부 날리기
        Rigidbody[] parts = _destroyedModel.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody part in parts)
        {
            //Vector3 dir = (part.transform.position - transform.position).normalized;
            //part.AddForce(dir * _explosionForce, ForceMode.Impulse);

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
}