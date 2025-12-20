using UnityEngine;
using UnityEngine.UI;
using System;


// Yes(확인) 클릭 시 호출자에게 콜백 전달

public class BaseWarningPopup : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public Text MessageText;
    public Button ConfirmButton; // 확인 버튼

    private Action OnConfirmAction;

    public void Setup(string Message, Action ConfirmCallback)
    {
        MessageText.text = Message;
        OnConfirmAction = ConfirmCallback;

        ConfirmButton.onClick.RemoveAllListeners(); // 초기화
        ConfirmButton.onClick.AddListener(OnClickConfirm);
    }

    // OnClickConfirm 정의
    private void OnClickConfirm()
    {
        OnConfirmAction?.Invoke(); // 확인 Action 실행
        Destroy(gameObject); // 팝업 비활성화
    }
}
