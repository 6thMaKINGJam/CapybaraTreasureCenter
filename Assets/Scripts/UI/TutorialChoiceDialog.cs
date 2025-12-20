using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

// 선택창용 (Yes/Yes 버튼)
public class TutorialChoiceDialog : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI MessageText;
    public Button YesButton1;
    public Button YesButton2;
   
    
    private Action onChoiceCallback;
    
    public void Setup(string message, string button1Text, string button2Text, Action choiceCallback)
    {
        MessageText.text = message;

        onChoiceCallback = choiceCallback;
        
        YesButton1.onClick.RemoveAllListeners();
        YesButton1.onClick.AddListener(OnChoice);
        
        YesButton2.onClick.RemoveAllListeners();
        YesButton2.onClick.AddListener(OnChoice);
    }
    
    private void OnChoice()
    {
        // 어떤 버튼을 눌러도 같은 결과
        onChoiceCallback?.Invoke();
    }
}