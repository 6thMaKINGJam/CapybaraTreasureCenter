using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System;

public class EndingManager : MonoBehaviour
{
    [Header("엔딩 시퀀스")]
    public List<EndingSequence> Sequences = new List<EndingSequence>();
    
    [Header("UI 요소")]
    public Image BackgroundImage;
    public GameObject BoxDialogPrefab;
    public GameObject SpeechDialogPrefab;
    public GameObject ChoiceDialogPrefab;
    public GameObject NicknameInputDialogPrefab;
    public Transform DialogParent;
    
    [Header("트랜지션 설정")]
    public float BackgroundFadeDuration = 0.5f;
    public float DialogFadeOutDuration = 0.3f;
    public float DialogFadeInDuration = 0.3f;
    public Ease BackgroundEaseType = Ease.InOutQuad;
    public Ease DialogEaseType = Ease.OutQuad;
    
    private int currentSequenceIndex = 0;
    private GameObject currentDialog;
    private Sprite previousBackgroundSprite;
    private bool isTransitioning = false;
    
    // 엔딩 완료 콜백
    public event Action OnEndingCompleted;
    
    void Start()
    {
        // 1. 네트워크 확인
        if (!CheckNetworkConnection())
        {
            ShowNetworkWarning();
            return;
        }
        
        if (Sequences.Count == 0)
        {
            Debug.LogError("[EndingManager] 시퀀스가 설정되지 않았습니다!");
            return;
        }
        
        // 초기 배경 설정
        if (BackgroundImage != null)
        {
            Color initialColor = BackgroundImage.color;
            initialColor.a = 0f;
            BackgroundImage.color = initialColor;
        }
        
        ShowSequence(0, false);
    }
    
    private bool CheckNetworkConnection()
    {
        // TODO: NetworkManager 구현 후 실제 네트워크 체크
        // return NetworkManager.Instance.IsConnected();
        
        // 임시: 항상 연결된 것으로 처리 (테스트용)
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
    
    private void ShowNetworkWarning()
    {
        GameObject popupObj = Instantiate(Resources.Load<GameObject>("Prefabs/UI/BaseWarningPopup"), transform);
        BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
        popup.Setup("네트워크 연결 후\n재접속해주세요.", () => {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }
    
    private void ShowSequence(int index, bool fadeBackground = true)
    {
        if (isTransitioning) return;
        
        if (index >= Sequences.Count)
        {
            // 엔딩 완료 (닉네임 입력 완료 = 엔딩 완료)
            CompleteEnding();
            return;
        }
        
        currentSequenceIndex = index;
        EndingSequence sequence = Sequences[index];
        
        // 진동 실행
        if (sequence.UseVibration)
        {
            VibrationManager.Instance.Vibrate(
                sequence.VibrationPattern, 
                sequence.VibrationDelay, 
                sequence.CustomVibrationPattern
            );
        }
        
        // 배경 전환 확인
        bool needsBackgroundTransition = fadeBackground && 
                                         sequence.BackgroundImage != null && 
                                         previousBackgroundSprite != sequence.BackgroundImage;
        
        if (needsBackgroundTransition)
        {
            StartCoroutine(TransitionSequence(sequence));
        }
        else
        {
            SetupBackgroundImmediate(sequence);
            ShowDialog(sequence);
        }
        
        previousBackgroundSprite = sequence.BackgroundImage;
    }
    
    private IEnumerator TransitionSequence(EndingSequence sequence)
    {
        isTransitioning = true;
        
        // 1. 현재 창 페이드 아웃
        if (currentDialog != null)
        {
            yield return FadeOutDialog(currentDialog);
            Destroy(currentDialog);
            currentDialog = null;
        }
        
        // 2. 배경 페이드 아웃
        yield return FadeOutBackground();
        
        // 3. 배경 이미지 변경
        if (sequence.BackgroundImage != null)
        {
            BackgroundImage.sprite = sequence.BackgroundImage;
        }
        
        // 4. 배경 페이드 인
        yield return FadeInBackground();
        
        // 5. 새 창 표시
        ShowDialog(sequence);
        
        isTransitioning = false;
    }
    
    private void SetupBackgroundImmediate(EndingSequence sequence)
    {
        if (sequence.BackgroundImage != null && BackgroundImage.sprite == null)
        {
            BackgroundImage.sprite = sequence.BackgroundImage;
            
            if (currentSequenceIndex == 0)
            {
                StartCoroutine(FadeInBackground());
            }
            else
            {
                Color color = BackgroundImage.color;
                color.a = 1f;
                BackgroundImage.color = color;
            }
        }
    }
    
    private void ShowDialog(EndingSequence sequence)
    {
        GameObject dialogPrefab = GetDialogPrefab(sequence.DialogType);
        if (dialogPrefab != null)
        {
            currentDialog = Instantiate(dialogPrefab, DialogParent);
            SetupDialog(currentDialog, sequence);
            
            StartCoroutine(FadeInDialog(currentDialog));
        }
        else
        {
            Debug.LogError($"[EndingManager] {sequence.DialogType} Prefab이 할당되지 않았습니다!");
        }
    }
    
    private GameObject GetDialogPrefab(EndingDialogType type)
    {
        switch (type)
        {
            case EndingDialogType.Box:
                return BoxDialogPrefab;
            case EndingDialogType.Speech:
                return SpeechDialogPrefab;
            case EndingDialogType.Choice:
                return ChoiceDialogPrefab;
            case EndingDialogType.NicknameInput:
                return NicknameInputDialogPrefab;
            default:
                return null;
        }
    }
    
    private void SetupDialog(GameObject dialog, EndingSequence sequence)
    {
        if (sequence.DialogType == EndingDialogType.Box || sequence.DialogType == EndingDialogType.Speech)
        {
            TutorialTextDialog textDialog = dialog.GetComponent<TutorialTextDialog>();
            if (textDialog != null)
            {
                textDialog.Setup(sequence.MessageText, OnDialogClicked);
            }
        }
        else if (sequence.DialogType == EndingDialogType.Choice)
        {
            TutorialChoiceDialog choiceDialog = dialog.GetComponent<TutorialChoiceDialog>();
            if (choiceDialog != null)
            {
                choiceDialog.Setup(
                    sequence.MessageText,
                    sequence.YesButton1Text,
                    sequence.YesButton2Text,
                    OnDialogClicked
                );
            }
        }
        else if (sequence.DialogType == EndingDialogType.NicknameInput)
        {
            NicknameInputDialog nicknameDialog = dialog.GetComponent<NicknameInputDialog>();
            if (nicknameDialog != null)
            {
                nicknameDialog.Setup( OnNicknameConfirmed);
            }
        }
    }
    
    private void OnDialogClicked()
    {
        if (isTransitioning) return;
        
        if (currentDialog != null)
        {
            StartCoroutine(FadeOutAndNext());
        }
    }
    
    private void OnNicknameConfirmed(string nickname)
    {
        Debug.Log($"[EndingManager] 닉네임 입력 완료: {nickname}");
        
        // 닉네임 입력 완료 = 랭킹 등록 완료 = 엔딩 완료
        // 여기서 바로 엔딩 종료
        CompleteEnding();
    }
    
    private IEnumerator FadeOutAndNext()
    {
        isTransitioning = true;
        
        yield return FadeOutDialog(currentDialog);
        Destroy(currentDialog);
        currentDialog = null;
        
        isTransitioning = false;
        
        ShowSequence(currentSequenceIndex + 1);
    }
    
    private IEnumerator FadeOutBackground()
    {
        if (BackgroundImage == null) yield break;
        
        yield return BackgroundImage.DOFade(0f, BackgroundFadeDuration)
            .SetEase(BackgroundEaseType)
            .WaitForCompletion();
    }
    
    private IEnumerator FadeInBackground()
    {
        if (BackgroundImage == null) yield break;
        
        yield return BackgroundImage.DOFade(1f, BackgroundFadeDuration)
            .SetEase(BackgroundEaseType)
            .WaitForCompletion();
    }
    
    private IEnumerator FadeOutDialog(GameObject dialog)
    {
        if (dialog == null) yield break;
        
        CanvasGroup canvasGroup = dialog.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = dialog.AddComponent<CanvasGroup>();
        }
        
        yield return canvasGroup.DOFade(0f, DialogFadeOutDuration)
            .SetEase(DialogEaseType)
            .WaitForCompletion();
    }
    
    private IEnumerator FadeInDialog(GameObject dialog)
    {
        if (dialog == null) yield break;
        
        CanvasGroup canvasGroup = dialog.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = dialog.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        
        yield return canvasGroup.DOFade(1f, DialogFadeInDuration)
            .SetEase(DialogEaseType)
            .WaitForCompletion();
    }
    
    private void CompleteEnding()
    {
        Debug.Log("[EndingManager] 엔딩 완료!");
        
        // ProgressData에 엔딩 완료 저장
        ProgressData progressData = SaveManager.LoadData<ProgressData>("ProgressData");
        progressData.EndingCompleted = true;
        SaveManager.Save(progressData, "ProgressData");
        
        // 배경과 마지막 창 페이드 아웃 후 콜백 호출
        StartCoroutine(FadeOutAndComplete());
    }
    
    private IEnumerator FadeOutAndComplete()
    {
        if (currentDialog != null)
        {
            yield return FadeOutDialog(currentDialog);
        }
        
        yield return FadeOutBackground();
        
        // 엔딩 완료 콜백 호출
        OnEndingCompleted?.Invoke();
    }
    
    private void OnDestroy()
    {
        DOTween.KillAll();
    }
}