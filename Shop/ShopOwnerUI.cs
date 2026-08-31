using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// 상점 주인 UI
/// </summary>
public class ShopOwnerUI : MonoBehaviour
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] DialogueConfig _dialogueConfig;
    [SerializeField] TextMeshProUGUI _dialogueText;
    [SerializeField] GameObject _dialoguePanel;
    [SerializeField] Image _ownerImage;
    [Header("----- 글자 속도 -----")]
    [SerializeField] float _typeSpeed = 0.02f;
    [Header("----- 상점주인 표정 -----")]
    [SerializeField] Sprite _spriteSmile;
    [SerializeField] Sprite _spriteNormal;
    [SerializeField] Sprite _spriteSad;

    int _tipCount; // 팁 갯수
    int _lastTipNumber; // 마지막으로 보여준 팁(중복 방지)

    string _currentText;
    Coroutine _typeRoutine;
    bool _isInteractable = true; // 상호 작용 가능한 상태인지

    private void Awake()
    {
        _tipCount = _dialogueConfig.GetDialogueCount("SHOP_TIP_"); // 팁 갯수 세팅
    }

    public void ShowWelcome()
    {
        ShowDialogue("SHOP_WELCOME");
    }

    public void ShowBuySuccess()
    {
        ShowDialogue("SHOP_BUYSUCCESS");
    }

    public void ShowBuyFailGold()
    {
        ShowDialogue("SHOP_BUYFAIL_GOLD");
    }

    public void ShowBuyFailSpace()
    {
        ShowDialogue("SHOP_BUYFAIL_SPACE");
    }

    public void ShowExit()
    {
        ShowDialogue("SHOP_EXIT");
    }

    /// <summary>
    /// 대사 패널 초기화(즉시 숨김). 상점이 새로 열릴 때(SetShopActive(true)) 호출해서
    /// 이전 세션의 나가기 대사가 남아있는 상태로 등장 연출이 재생되는 걸 방지하기 위함
    /// 타이핑 도중이었을 경우를 대비해 진행 중인 코루틴도 함께 정리
    /// </summary>
    public void ResetDialogue()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        _dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// 상호작용 가능 여부 설정
    /// </summary>
    public void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;
    }

    /// <summary>
    /// 상점 주인 클릭(팁 대사 출력)
    /// </summary>
    public void OnClickOwner()
    {
        // 대사 출력 중이면 전체 출력만 하고 종료
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;

            _dialogueText.text = _currentText;
            return;
        }

        if (_isInteractable == false) return;

        int tipNumber = Random.Range(1, _tipCount);

        if (tipNumber >= _lastTipNumber)
        {
            tipNumber++;
        }

        _lastTipNumber = tipNumber;

        string tipKey = $"SHOP_TIP_{tipNumber:00}";

        ShowDialogue(tipKey);
    }

    /// <summary>
    /// 대사 출력
    /// </summary>
    void ShowDialogue(string key)
    {
        DialogueData data = _dialogueConfig.GetDialogue(key);
        if (data == null) return;

        // 표정 변경
        SetExpression(data.AniID);

        // 로컬라이즈 텍스트
        string text = new LocalizedString("Localization", key).GetLocalizedString();

        // 한글자씩 출력
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeRoutine(text));

        _dialoguePanel.SetActive(true);
    }

    /// <summary>
    /// 표정(이미지) 변경
    /// </summary>
    void SetExpression(string aniID = "Ani_Normal")
    {
        _ownerImage.sprite = aniID switch
        {
            "Ani_Smile" => _spriteSmile,
            "Ani_Normal" => _spriteNormal,
            "Ani_Sad" => _spriteSad,
            _ => _spriteNormal
        };
    }

    /// <summary>
    /// 한글자씩 출력
    /// </summary>
    IEnumerator TypeRoutine(string text)
    {
        _currentText = text;
        _dialogueText.text = string.Empty;

        int index = 0;

        while (index < text.Length)
        {
            // 리치텍스트 태그(<...>)는 한번에 추가
            if (text[index] == '<')
            {
                int endIndex = text.IndexOf('>', index);

                if (endIndex != -1)
                {
                    _dialogueText.text += text.Substring(index, endIndex - index + 1);
                    index = endIndex + 1;
                    continue;
                }
            }

            // 일반 글자는 한글자씩 출력
            _dialogueText.text += text[index];
            index++;

            yield return new WaitForSeconds(_typeSpeed);
        }

        _typeRoutine = null;
    }

    /// <summary>
    /// 상점 아무 곳이나 누름
    /// </summary>
    public void OnClickDialoguePanel()
    {
        // 대사 출력 중이면 전부 출력
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
            _dialogueText.text = _currentText;
        }
        else // 다 출력됐으면 대사 창 닫음
        {
            _dialoguePanel.SetActive(false);
        }
    }
}