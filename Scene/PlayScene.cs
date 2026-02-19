using UnityEngine;

public class PlayScene : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] InputSystemHandler _inputSystemHandler;
    [SerializeField] Player _player;

    private void Start()
    {
        _inputSystemHandler.OnMoveInput += HandleMoveInput;
    }

    void HandleMoveInput(Vector2 inputVector)
    {
        // x,y 축을 x,z축으로 변경
        Vector3 moveVector = Vector3.forward * inputVector.y + Vector3.right * inputVector.x;
        _player.Move(moveVector);
    }
}
