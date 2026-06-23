using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] GameObject go;

    int _step = 0;
    /*
    void Start()
    {
        StartCoroutine(RunTutorial());
    }
    
    IEnumerator RunTutorial()
    {
        // Step 1: 이동 설명
        ShowMessage("WASD 키로 이동하세요");
        //yield return new WaitUntil(() => Input.GetKey(KeyCode.W));

        // Step 2: 공격 설명
        ShowMessage("마우스 클릭으로 공격하세요");
        //yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

    }
    */
    void ShowMessage(string msg)
    {
        // UI 매니저와 연결해서 메시지 출력
        Debug.Log(msg);
    }
}