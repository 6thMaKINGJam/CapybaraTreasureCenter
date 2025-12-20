using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class NicknameInputDialog : MonoBehaviour
{
    [Header("UI 요소")]
    public TMP_InputField NicknameInputField;
    public Button ConfirmButton;
    public GameObject LoadingPanel; // "등록 중..." 로딩 UI
   
    private Action<string> onConfirmCallback;
    private int retryCount = 0;
    private const int MAX_RETRY = 3;
    
   private Action onSuccessCallback;
private Action onFailureCallback;

public void Setup(Action successCallback, Action failureCallback)
{
    onSuccessCallback = successCallback;
    onFailureCallback = failureCallback;
    
    LoadingPanel.SetActive(false);
    
    ConfirmButton.onClick.RemoveAllListeners();
    ConfirmButton.onClick.AddListener(OnClickConfirm);
    
    NicknameInputField.ActivateInputField();
}

    private void OnClickConfirm()
{
    string nickname = NicknameInputField.text.Trim();
    
    if (string.IsNullOrWhiteSpace(nickname))
    {
        ShowWarning("닉네임을 입력해주세요.");
        return;
    }
    
    ShowLoading();
    
    // 중복 체크
    StartCoroutine(CheckAndRegister(nickname));
}

private IEnumerator CheckAndRegister(string nickname)
{
    var checkTask = RankingManager.Instance.IsNicknameExists(nickname);
    yield return new WaitUntil(() => checkTask.IsCompleted);
    
    if (checkTask.Result)
    {
        HideLoading();
        ShowWarning("이미 사용 중인 닉네임입니다.\n다른 닉네임을 입력해주세요.");
        yield break;
    }
    
    RegisterToRanking(nickname);
}

    
    private void RegisterToRanking(string nickname)
{
    // ProgressData에서 BestTime 로드
    ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
    long bestTime = progressData.BestTime;
    
    RankingManager.Instance.RegisterRanking(
        nickname, 
        bestTime, 
        OnRegistrationSuccess, 
        OnRegistrationFailed
    );
}


    
   private void OnRegistrationSuccess()
{
    Debug.Log("[NicknameInputDialog] 랭킹 등록 성공!");
    HideLoading();
    
    // ===== 내 닉네임 저장 (HallOfFame에서 강조 표시용) =====
    string nickname = NicknameInputField.text.Trim();
    PlayerPrefs.SetString("MyNickname", nickname);
    PlayerPrefs.Save();
    
    onSuccessCallback?.Invoke();
}

    private void OnRegistrationFailed(string error)
{
    Debug.LogError($"[NicknameInputDialog] 랭킹 등록 실패: {error}");
    HideLoading();
    
    retryCount++;
    
    if (retryCount >= MAX_RETRY)
    {
        ShowFinalWarning();
    }
    else
    {
        ShowRetryWarning();
    }
}

private void ShowRetryWarning()
{
    GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseWarningPopup"), transform.parent);
    BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
    popup.Setup("네트워크 등 문제로 랭킹 등록에 실패하였습니다.\n다시 한 번 확인 후 재시도 해주세요.", () => {
        // 재시도
        string nickname = NicknameInputField.text.Trim();
        ShowLoading();
        RegisterToRanking(nickname);
    });
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