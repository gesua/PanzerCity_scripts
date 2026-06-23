using UnityEngine;

public class MoveTutorial : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 튜토리얼 트리거에 들어옴!");
            //tutorialManager.OnStepComplete(stepNumber);

            // 중복 실행 방지를 위해 트리거 비활성화
            gameObject.SetActive(false);
        }

    }
}
