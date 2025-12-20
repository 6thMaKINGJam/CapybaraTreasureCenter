using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

// 네모 박스창 및 대화창용 (단순 텍스트 표시)
public class TutorialTextDialog : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI MessageText;
    public Button ClickArea; // 전체 영역을 클릭 가능하게
    
    private Action onClickCallback;
    
    public void Setup(string message, Action clickCallback)
    {
        MessageText.text = message;
        onClickCallback = clickCallback;
        
        ClickArea.onClick.RemoveAllListeners();
        ClickArea.onClick.AddListener(OnClick);
    }
    
    private void OnClick()
    {
        onClickCallback?.Invoke();
    }
}