using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] Mover _mover;

    public void Move(Vector3 dir)
    {
        _mover.Move(dir);
    }
}
