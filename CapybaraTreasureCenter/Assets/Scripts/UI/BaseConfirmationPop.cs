using UnityEngine;
using UnityEngine.UI; // Text, Button 기능
using System; // Action(콜백) 기능

public class BaseConfirmationPopup : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public Text MessageText; // 팝업에 표시될 질문 내용
    public Button YesButton; // Yes 버튼
    public Button NoButton; // No 버튼

    // 버튼을 눌렀을 때 실행될 Action들
    private Action OnYesAction;
    private Action OnNoAction;

    // 팝업 설정하고 화면에 표시
    public void Setup(string Message, Action YesCallback, Action NoCallback)
    {
        MessageText.text = Message;
        OnYesAction = YesCallback;
        OnNoAction = NoCallback;

        // 버튼 클릭 이벤트 연결
        YesButton.onClick.RemoveAllListeners(); // 이전 연결 삭제 (중복 방지)
        YesButton.onClick.AddListener(OnClickYes);

        NoButton.onClick.RemoveAllListeners();
        NoButton.onClick.AddListener(OnClickNo);
    }

    // OnClickYes 정의
    private void OnClickYes()
    {
        OnYesAction?.Invoke(); // 예약된 Yes Action 실행
        ClosePopup();
    }

    // OnClickNo 정의
    private void OnClickNo()
    {
        OnNoAction?.Invoke(); // 예약된 No Action 실행
        ClosePopup();
    }

    // ClosePopup 정의
    private void ClosePopup()
    {
        Destroy(gameObject); // 팝업 비활성화
    }
}
