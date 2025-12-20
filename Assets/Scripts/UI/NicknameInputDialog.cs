using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NicknameInputDialog : MonoBehaviour
{
    [Header("UI 요소")]
    public TMP_InputField NicknameInputField;
    public Button ConfirmButton;
    public GameObject LoadingPanel; // "등록 중..." 로딩 UI
   
    private Action<string> onConfirmCallback;
    private int retryCount = 0;
    private const int MAX_RETRY = 3;
    
    public void Setup( Action<string> confirmCallback)
    {
       
        onConfirmCallback = confirmCallback;
        
        LoadingPanel.SetActive(false);
        
        ConfirmButton.onClick.RemoveAllListeners();
        ConfirmButton.onClick.AddListener(OnClickConfirm);
        
        // InputField 포커스
        NicknameInputField.ActivateInputField();
    }
    
    private void OnClickConfirm()
    {
        string nickname = NicknameInputField.text.Trim();
        
        // 공백 체크
        if (string.IsNullOrWhiteSpace(nickname))
        {
            ShowWarning("닉네임을 입력해주세요.");
            return;
        }
        
        // TODO: RankingManager 구현 후 중복 체크
        // if (RankingManager.Instance.IsNicknameExists(nickname))
        // {
        //     ShowWarning("이미 사용 중인 닉네임입니다. 다른 닉네임을 입력해주세요.");
        //     return;
        // }
        
        // 로딩 표시
        ShowLoading();
        
        // 랭킹 등록 시도
        RegisterToRanking(nickname);
    }
    
    private void RegisterToRanking(string nickname)
    {
        // TODO: RankingManager 구현 후 Firebase 등록
        // RankingManager.Instance.RegisterRanking(nickname, score, OnRegistrationSuccess, OnRegistrationFailed);
        
        // 임시: 2초 후 성공으로 처리 (테스트용)
        Invoke(nameof(OnRegistrationSuccess), 2f);
    }
    
    private void OnRegistrationSuccess()
    {
        Debug.Log("[NicknameInputDialog] 랭킹 등록 성공!");
        HideLoading();
        onConfirmCallback?.Invoke(NicknameInputField.text.Trim());
    }
    
    private void OnRegistrationFailed(string error)
    {
        Debug.LogError($"[NicknameInputDialog] 랭킹 등록 실패: {error}");
        HideLoading();
        
        retryCount++;
        
        if (retryCount >= MAX_RETRY)
        {
            // 3회 재시도 실패
            ShowFinalWarning();
        }
        else
        {
            // 재시도 안내
            ShowWarning($"랭킹 등록에 실패했습니다.\n다시 시도해주세요. ({retryCount}/{MAX_RETRY})");
        }
    }
    
    private void ShowLoading()
    {
        LoadingPanel.SetActive(true);
    
        ConfirmButton.interactable = false;
        NicknameInputField.interactable = false;
    }
    
    private void HideLoading()
    {
        LoadingPanel.SetActive(false);
        ConfirmButton.interactable = true;
        NicknameInputField.interactable = true;
    }
    
    private void ShowWarning(string message)
    {
        // BaseWarningPopup 생성
        GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseWarningPopup"), transform.parent);
        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup(message, () => {
            // 확인 버튼 누르면 팝업 닫힘 (Destroy는 BaseWarningPopup에서 처리)
        });
    }
    
    private void ShowFinalWarning()
    {
        GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseWarningPopup"), transform.parent);
        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup("네트워크를 확인하고\n앱을 재실행해주세요.", () => {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }
}