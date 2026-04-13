using System.Collections;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    [SerializeField] float _fadeDelay = 0.5f;  // 날아간 후 페이드 시작까지 대기
    [SerializeField] float _fadeDuration = 0.5f;
    MeshRenderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
    }

    public void StartFade()
    {
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        yield return new WaitForSeconds(_fadeDelay);

        float elapsed = 0f;
        Material mat = _renderer.material;
        Color color = mat.color;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
            mat.color = color;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}