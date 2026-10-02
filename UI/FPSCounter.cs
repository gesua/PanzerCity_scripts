using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text _fpsText;

    private float _deltaTime;

    private void Awake()
    {
        // 씬이 바뀌어도 FPS 표시를 유지합니다.
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 프레임 간 시간의 평균을 이용해 FPS 변화를 조금 완화합니다.
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

        float fps = 1f / _deltaTime;

        _fpsText.text = $"{fps:0} FPS";
    }
}