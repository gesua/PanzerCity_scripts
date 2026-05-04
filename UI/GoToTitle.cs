using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인화면으로 가는 버튼
/// </summary>
public class GoToTitle : MonoBehaviour
{
    public void LoadTitleScene()
    {
        SceneManager.LoadScene("Title");
    }
}
