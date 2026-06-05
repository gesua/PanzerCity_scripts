using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] float _typeSpeed = 0.05f;
    [Header("----- 상점주인 표정 -----")]
    [SerializeField] Sprite _spriteSmile;
    [SerializeField] Sprite _spriteNormal;
    [SerializeField] Sprite _spriteSad;

    string[] _tipKeys = { "SHOP_TIP_01", "SHOP_TIP_02", "SHOP_TIP_03" };

    string _currentText;
    Coroutine _typeRoutine;

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
    /// 상점 주인 클릭(팁 대사 출력)
    /// </summary>
    public void OnClickOwner()
    {
        string tipKey = _tipKeys[Random.Range(0, _tipKeys.Length)];
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
    void SetExpression(string aniID)
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
        foreach (char c in text)
        {
            _dialogueText.text += c;
            yield return new WaitForSeconds(_typeSpeed);
        }
        _typeRoutine = null;
    }

    /// <summary>
    /// 한번에 전부 출력
    /// </summary>
    public void OnClickDialoguePanel()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
            _dialogueText.text = _currentText;
        }
    }
}